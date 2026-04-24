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

    public bool IsDocumentOpen => _isDocumentOpen;
    public string CurrentDocumentId => _currentDocumentId;
    public string CurrentDocumentName => _currentDocumentName;
    public int TotalPages => _totalPages;
    public int CurrentPageIndex => _currentPageIndex;

    private readonly Dictionary<string, PageAssemblyState> _pageAssemblies = new Dictionary<string, PageAssemblyState>();

    private RTCDataChannel _dataChannel;
    private bool _isDocumentOpen;
    private string _currentDocumentId;
    private string _currentDocumentName;
    private int _totalPages;
    private int _currentPageIndex = -1;

    private sealed class PageAssemblyState
    {
        public int PageIndex;
        public int TotalPages;
        public int Width;
        public int Height;
        public int TotalChunks;
        public byte[][] Chunks;
        public int ReceivedChunks;
        public float CreatedAt;
    }

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

            default:
                Debug.LogWarning($"DocumentManager: unsupported type '{type}'.");
                break;
        }
    }

    private void HandleDocumentStart(DocumentStartMessage message)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.documentId))
        {
            Debug.LogWarning("DocumentManager: ignoring invalid document-start payload.");
            return;
        }

        _isDocumentOpen = true;
        _currentDocumentId = message.documentId;
        _currentDocumentName = message.documentName;
        _totalPages = Mathf.Max(0, message.totalPages);
        _currentPageIndex = _totalPages > 0 ? 0 : -1;

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

        if (!_isDocumentOpen)
        {
            Debug.LogWarning("DocumentManager: ignoring document-page because no document is open.");
            return;
        }

        if (!string.Equals(message.documentId, _currentDocumentId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"DocumentManager: ignoring document-page for non-active document '{message.documentId}'. Active='{_currentDocumentId}'.");
            return;
        }

        if (message.pageIndex < 0 || (_totalPages > 0 && message.pageIndex >= _totalPages))
        {
            Debug.LogWarning($"DocumentManager: ignoring out-of-range page index {message.pageIndex} for totalPages={_totalPages}.");
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

        var assembledBytes = AssembleBytes(assembly.Chunks);
        _pageAssemblies.Remove(assemblyKey);

        _currentPageIndex = assembly.PageIndex;

        if (pdfPageDisplay != null)
            pdfPageDisplay.ShowFromBytes(assembledBytes, assembly.Width, assembly.Height);
        else
            Debug.LogWarning("DocumentManager: PdfPageDisplay is not assigned; skipping render.");

        var completedMessage = new DocumentPageMessage
        {
            documentId = _currentDocumentId,
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

        if (_isDocumentOpen && !string.Equals(message.documentId, _currentDocumentId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"DocumentManager: ignoring document-close for non-active document '{message.documentId}'. Active='{_currentDocumentId}'.");
            return;
        }

        ResetDocumentState();
        ClearAssembliesForCurrentDocument();
        OnDocumentClose?.Invoke(message);
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

    private static byte[] AssembleBytes(byte[][] chunks)
    {
        var totalLength = 0;
        for (var i = 0; i < chunks.Length; i++)
            totalLength += chunks[i].Length;

        var combined = new byte[totalLength];
        var offset = 0;
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            Buffer.BlockCopy(chunk, 0, combined, offset, chunk.Length);
            offset += chunk.Length;
        }

        return combined;
    }

    private void ResetDocumentState()
    {
        _isDocumentOpen = false;
        _currentDocumentId = null;
        _currentDocumentName = null;
        _totalPages = 0;
        _currentPageIndex = -1;
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
