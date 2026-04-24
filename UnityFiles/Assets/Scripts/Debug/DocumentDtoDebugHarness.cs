using UnityEngine;

public class DocumentDtoDebugHarness : MonoBehaviour
{
    [Header("Sample JSON payloads")]
    [TextArea(2, 5)]
    [SerializeField] private string _startJson = "{\"documentId\":\"manual-001\",\"documentName\":\"Pump Manual\",\"totalPages\":42}";

    [TextArea(2, 8)]
    [SerializeField] private string _pageJson = "{\"documentId\":\"manual-001\",\"pageIndex\":2,\"totalPages\":42,\"width\":1200,\"height\":1600,\"chunkIndex\":0,\"totalChunks\":3,\"data\":\"AQID\"}";

    [TextArea(2, 4)]
    [SerializeField] private string _closeJson = "{\"documentId\":\"manual-001\"}";

    [ContextMenu("Parse Start Message")]
    public void ParseStartMessage()
    {
        var message = JsonUtility.FromJson<DocumentStartMessage>(_startJson);
        if (message == null)
        {
            Debug.LogError("DocumentDtoDebugHarness: Start message parse failed.");
            return;
        }

        Debug.Log($"DocumentStartMessage OK: id={message.documentId}, name={message.documentName}, totalPages={message.totalPages}");
    }

    [ContextMenu("Parse Page Message")]
    public void ParsePageMessage()
    {
        var message = JsonUtility.FromJson<DocumentPageMessage>(_pageJson);
        if (message == null)
        {
            Debug.LogError("DocumentDtoDebugHarness: Page message parse failed.");
            return;
        }

        var byteCount = message.data != null ? message.data.Length : 0;
        Debug.Log($"DocumentPageMessage OK: id={message.documentId}, page={message.pageIndex}/{message.totalPages - 1}, size={message.width}x{message.height}, chunk={message.chunkIndex + 1}/{message.totalChunks}, bytes={byteCount}");
    }

    [ContextMenu("Parse Close Message")]
    public void ParseCloseMessage()
    {
        var message = JsonUtility.FromJson<DocumentCloseMessage>(_closeJson);
        if (message == null)
        {
            Debug.LogError("DocumentDtoDebugHarness: Close message parse failed.");
            return;
        }

        Debug.Log($"DocumentCloseMessage OK: id={message.documentId}");
    }

    [ContextMenu("Parse All Messages")]
    public void ParseAllMessages()
    {
        ParseStartMessage();
        ParsePageMessage();
        ParseCloseMessage();
    }
}
