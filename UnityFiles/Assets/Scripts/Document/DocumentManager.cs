using System;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.WebRTC;
using UnityEngine;

public class DocumentManager : MonoBehaviour
{
    [SerializeField] private PdfPageDisplay pdfPageDisplay;

    public event Action<DocumentStartMessage> OnDocumentStart;
    public event Action<DocumentPageMessage> OnDocumentPage;
    public event Action<DocumentCloseMessage> OnDocumentClose;

    public bool IsDocumentOpen => _isDocumentOpen;
    public string CurrentDocumentId => _currentDocumentId;
    public string CurrentDocumentName => _currentDocumentName;
    public int TotalPages => _totalPages;
    public int CurrentPageIndex => _currentPageIndex;

    private RTCDataChannel _dataChannel;
    private bool _isDocumentOpen;
    private string _currentDocumentId;
    private string _currentDocumentName;
    private int _totalPages;
    private int _currentPageIndex = -1;

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

        if (message.data == null || message.data.Length == 0)
        {
            Debug.LogWarning("DocumentManager: ignoring document-page with empty image payload.");
            return;
        }

        _currentPageIndex = message.pageIndex;

        if (pdfPageDisplay != null)
            pdfPageDisplay.ShowFromBytes(message.data, message.width, message.height);
        else
            Debug.LogWarning("DocumentManager: PdfPageDisplay is not assigned; skipping render.");

        OnDocumentPage?.Invoke(message);
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
        OnDocumentClose?.Invoke(message);
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
