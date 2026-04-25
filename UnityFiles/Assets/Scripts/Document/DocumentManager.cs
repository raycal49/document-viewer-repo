using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.WebRTC;
using UnityEngine;

public class DocumentManager : MonoBehaviour
{
    [SerializeField] private PdfPageDisplay pdfPageDisplay;
    [SerializeField] private float chunkAssemblyTimeoutSeconds = 15f;

    public event Action<DocumentStartMessage> OnDocumentStart;
    public event Action<DocumentPageMessage> OnDocumentPage;
    public event Action<DocumentCloseMessage> OnDocumentClose;
    public event Action<DocumentNavigateMessage> OnDocumentNavigate;
    public event Action<DocumentRequestPageMessage> OnDocumentRequestPage;
    public event Action<string> OnOutboundDocumentMessage;

    public bool IsDocumentOpen => _sessionState.IsDocumentOpen;
    public string CurrentDocumentId => _sessionState.CurrentDocumentId;
    public string CurrentDocumentName => _sessionState.CurrentDocumentName;
    public int TotalPages => _sessionState.TotalPages;
    public int CurrentPageIndex => _sessionState.CurrentPageIndex;

    private readonly Dictionary<string, PageAssemblyState> _pageAssemblies = new Dictionary<string, PageAssemblyState>();
    private readonly DocumentSessionState _sessionState = new DocumentSessionState();

    private RTCDataChannel _dataChannel;

    public void HandleDataChannel(RTCDataChannel channel, ConcurrentQueue<string> documentQueue)
    {
        if (_dataChannel != null)
            _dataChannel.OnMessage = null;

        _dataChannel = channel;

        if (_dataChannel == null)
            return;

        _dataChannel.OnMessage = bytes =>
        {
            var json = Encoding.UTF8.GetString(bytes);
            documentQueue.Enqueue(json);
        };
    }

    public void HandleMessage(string json)
    {
        CleanupExpiredAssemblies();

        if (string.IsNullOrWhiteSpace(json))
            return;

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"DocumentManager: invalid JSON payload. {ex.Message}");
            return;
        }

        var type = (string)root["type"];
        if (string.IsNullOrWhiteSpace(type))
        {
            Debug.LogWarning("DocumentManager: payload missing 'type'.");
            return;
        }

        switch (type)
        {
            case "document-start":
                HandleDocumentStart(root.ToObject<DocumentStartMessage>());
                break;

            case "document-page":
                HandleDocumentPage(root.ToObject<DocumentPageMessage>());
                break;

            case "document-close":
                HandleDocumentClose(root.ToObject<DocumentCloseMessage>());
                break;

            case "document-navigate":
                HandleDocumentNavigate(root.ToObject<DocumentNavigateMessage>());
                break;

            case "document-request-page":
                HandleDocumentRequestPage(root.ToObject<DocumentRequestPageMessage>());
                break;

            default:
                Debug.LogWarning($"DocumentManager: unsupported type '{type}'.");
                break;
        }
    }

    public bool NavigatePrevious(string source = "quest-prev")
    {
        return NavigateToPage(_sessionState.CurrentPageIndex - 1, source);
    }

    public bool NavigateNext(string source = "quest-next")
    {
        return NavigateToPage(_sessionState.CurrentPageIndex + 1, source);
    }

    public bool NavigateToPage(int targetPageIndex, string source = "quest-jump")
    {
        if (!_sessionState.IsDocumentOpen || string.IsNullOrWhiteSpace(_sessionState.CurrentDocumentId))
        {
            Debug.LogWarning("DocumentManager: ignoring navigation because no active document is open.");
            return false;
        }

        if (_sessionState.TotalPages <= 0)
        {
            Debug.LogWarning("DocumentManager: ignoring navigation because total pages is not positive.");
            return false;
        }

        var clampedPageIndex = Mathf.Clamp(targetPageIndex, 0, _sessionState.TotalPages - 1);
        if (clampedPageIndex == _sessionState.CurrentPageIndex)
            return false;

        _sessionState.CurrentPageIndex = clampedPageIndex;

        var navigateMessage = new DocumentNavigateMessage
        {
            documentId = _sessionState.CurrentDocumentId,
            pageIndex = clampedPageIndex,
            source = source
        };

        var sent = SendDocumentMessage("document-navigate", navigateMessage);
        OnDocumentNavigate?.Invoke(navigateMessage);
        return sent;
    }

    public bool RequestPage(int pageIndex)
    {
        if (!_sessionState.IsDocumentOpen || string.IsNullOrWhiteSpace(_sessionState.CurrentDocumentId))
        {
            Debug.LogWarning("DocumentManager: ignoring page request because no active document is open.");
            return false;
        }

        if (_sessionState.TotalPages <= 0)
        {
            Debug.LogWarning("DocumentManager: ignoring page request because total pages is not positive.");
            return false;
        }

        var clampedPageIndex = Mathf.Clamp(pageIndex, 0, _sessionState.TotalPages - 1);
        var requestMessage = new DocumentRequestPageMessage
        {
            documentId = _sessionState.CurrentDocumentId,
            pageIndex = clampedPageIndex
        };

        var sent = SendDocumentMessage("document-request-page", requestMessage);
        OnDocumentRequestPage?.Invoke(requestMessage);
        return sent;
    }

    private void HandleDocumentStart(DocumentStartMessage message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.documentId))
        {
            Debug.LogWarning("DocumentManager: ignoring invalid document-start payload.");
            return;
        }

        _sessionState.IsDocumentOpen = true;
        _sessionState.CurrentDocumentId = message.documentId;
        _sessionState.CurrentDocumentName = message.documentName;
        _sessionState.TotalPages = Mathf.Max(0, message.totalPages);
        _sessionState.CurrentPageIndex = _sessionState.TotalPages > 0 ? 0 : -1;

        ClearAssembliesForCurrentDocument();
        OnDocumentStart?.Invoke(message);
    }

    private void HandleDocumentPage(DocumentPageMessage message)
    {
        if (message == null)
        {
            Debug.LogWarning("DocumentManager: ignoring null document-page payload.");
            return;
        }

        if (!_sessionState.IsDocumentOpen)
        {
            Debug.LogWarning("DocumentManager: ignoring document-page because no document is open.");
            return;
        }

        if (!string.Equals(message.documentId, _sessionState.CurrentDocumentId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"DocumentManager: ignoring document-page for non-active document '{message.documentId}'. Active='{_sessionState.CurrentDocumentId}'.");
            return;
        }

        if (message.pageIndex < 0 || (_sessionState.TotalPages > 0 && message.pageIndex >= _sessionState.TotalPages))
        {
            Debug.LogWarning($"DocumentManager: ignoring out-of-range page index {message.pageIndex} for totalPages={_sessionState.TotalPages}.");
            return;
        }

        if (message.totalChunks <= 0 || message.chunkIndex < 0 || message.chunkIndex >= message.totalChunks)
        {
            Debug.LogWarning($"DocumentManager: ignoring invalid chunk metadata chunkIndex={message.chunkIndex}, totalChunks={message.totalChunks}.");
            return;
        }

        if (message.data == null || message.data.Length == 0)
        {
            Debug.LogWarning("DocumentManager: ignoring document-page with empty chunk payload.");
            return;
        }

        var assemblyKey = BuildAssemblyKey(message.documentId, message.pageIndex);
        if (!_pageAssemblies.TryGetValue(assemblyKey, out var assembly))
        {
            assembly = new PageAssemblyState
            {
                PageIndex = message.pageIndex,
                TotalPages = message.totalPages,
                Width = message.width,
                Height = message.height,
                TotalChunks = message.totalChunks,
                Chunks = new byte[message.totalChunks][],
                CreatedAt = Time.realtimeSinceStartup
            };
            _pageAssemblies[assemblyKey] = assembly;
        }

        if (assembly.TotalChunks != message.totalChunks)
        {
            Debug.LogWarning($"DocumentManager: chunk count mismatch for page {message.pageIndex}; resetting assembly.");
            _pageAssemblies.Remove(assemblyKey);
            return;
        }

        if (assembly.Chunks[message.chunkIndex] != null)
        {
            Debug.Log($"DocumentManager: duplicate chunk ignored for page={message.pageIndex}, chunk={message.chunkIndex}.");
            return;
        }

        assembly.Chunks[message.chunkIndex] = message.data;
        assembly.ReceivedChunks++;

        if (assembly.ReceivedChunks < assembly.TotalChunks)
            return;

        var assembledBytes = PageByteAssembler.AssembleChunks(assembly.Chunks);
        _pageAssemblies.Remove(assemblyKey);

        _sessionState.CurrentPageIndex = assembly.PageIndex;

        if (pdfPageDisplay != null)
            pdfPageDisplay.ShowFromBytes(assembledBytes, assembly.Width, assembly.Height);
        else
            Debug.LogWarning("DocumentManager: PdfPageDisplay is not assigned; skipping render.");

        var completedMessage = new DocumentPageMessage
        {
            documentId = _sessionState.CurrentDocumentId,
            pageIndex = assembly.PageIndex,
            totalPages = assembly.TotalPages,
            width = assembly.Width,
            height = assembly.Height,
            chunkIndex = 0,
            totalChunks = 1,
            data = assembledBytes
        };

        OnDocumentPage?.Invoke(completedMessage);
    }

    private void HandleDocumentClose(DocumentCloseMessage message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.documentId))
        {
            Debug.LogWarning("DocumentManager: ignoring invalid document-close payload.");
            return;
        }

        if (_sessionState.IsDocumentOpen && !string.Equals(message.documentId, _sessionState.CurrentDocumentId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"DocumentManager: ignoring document-close for non-active document '{message.documentId}'. Active='{_sessionState.CurrentDocumentId}'.");
            return;
        }

        ResetDocumentState();
        ClearAssembliesForCurrentDocument();
        OnDocumentClose?.Invoke(message);
    }

    private void HandleDocumentNavigate(DocumentNavigateMessage message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.documentId))
        {
            Debug.LogWarning("DocumentManager: ignoring invalid document-navigate payload.");
            return;
        }

        if (!_sessionState.IsDocumentOpen || !string.Equals(message.documentId, _sessionState.CurrentDocumentId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"DocumentManager: ignoring document-navigate for non-active document '{message.documentId}'. Active='{_sessionState.CurrentDocumentId}'.");
            return;
        }

        if (_sessionState.TotalPages <= 0)
        {
            Debug.LogWarning("DocumentManager: ignoring document-navigate because total pages is not positive.");
            return;
        }

        var clampedPageIndex = Mathf.Clamp(message.pageIndex, 0, _sessionState.TotalPages - 1);
        _sessionState.CurrentPageIndex = clampedPageIndex;
        message.pageIndex = clampedPageIndex;
        OnDocumentNavigate?.Invoke(message);
    }

    private void HandleDocumentRequestPage(DocumentRequestPageMessage message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.documentId))
        {
            Debug.LogWarning("DocumentManager: ignoring invalid document-request-page payload.");
            return;
        }

        if (!_sessionState.IsDocumentOpen || !string.Equals(message.documentId, _sessionState.CurrentDocumentId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"DocumentManager: ignoring document-request-page for non-active document '{message.documentId}'. Active='{_sessionState.CurrentDocumentId}'.");
            return;
        }

        if (_sessionState.TotalPages <= 0)
        {
            Debug.LogWarning("DocumentManager: ignoring document-request-page because total pages is not positive.");
            return;
        }

        message.pageIndex = Mathf.Clamp(message.pageIndex, 0, _sessionState.TotalPages - 1);
        OnDocumentRequestPage?.Invoke(message);
    }

    private bool SendDocumentMessage(string type, object payload)
    {
        var root = JObject.FromObject(payload ?? new object());
        root["type"] = type;
        var json = root.ToString(Newtonsoft.Json.Formatting.None);

        OnOutboundDocumentMessage?.Invoke(json);

        if (_dataChannel == null)
        {
            Debug.LogWarning($"DocumentManager: documents channel unavailable; queued outbound payload only. type='{type}'.");
            return false;
        }

        _dataChannel.Send(Encoding.UTF8.GetBytes(json));
        return true;
    }

    private static string BuildAssemblyKey(string documentId, int pageIndex)
    {
        return $"{documentId}:{pageIndex}";
    }

    private void ClearAssembliesForCurrentDocument()
    {
        _pageAssemblies.Clear();
    }

    private void CleanupExpiredAssemblies()
    {
        if (_pageAssemblies.Count == 0)
            return;

        var now = Time.realtimeSinceStartup;
        var expiredKeys = new List<string>();

        foreach (var pair in _pageAssemblies)
        {
            if (now - pair.Value.CreatedAt > chunkAssemblyTimeoutSeconds)
                expiredKeys.Add(pair.Key);
        }

        foreach (var key in expiredKeys)
        {
            _pageAssemblies.Remove(key);
            Debug.LogWarning($"DocumentManager: dropped stale chunk assembly '{key}'.");
        }

    }

    private void ResetDocumentState()
    {
        _sessionState.IsDocumentOpen = false;
        _sessionState.CurrentDocumentId = null;
        _sessionState.CurrentDocumentName = null;
        _sessionState.TotalPages = 0;
        _sessionState.CurrentPageIndex = -1;
    }

    private void OnDestroy()
    {
        if (_dataChannel != null)
        {
            _dataChannel.OnMessage = null;
            _dataChannel = null;
        }
    }
}
