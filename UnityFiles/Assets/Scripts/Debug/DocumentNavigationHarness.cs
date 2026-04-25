using Newtonsoft.Json.Linq;
using UnityEngine;

public class DocumentNavigationHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DocumentManager documentManager;

    [Header("Session setup")]
    [SerializeField] private string documentId = "manual-001";
    [SerializeField] private string documentName = "Sample Manual";
    [SerializeField] private int totalPages = 12;

    [Header("Actions")]
    [SerializeField] private int jumpToPageOneBased = 1;
    [SerializeField] private int inboundNavigatePageOneBased = 1;

    private void OnEnable()
    {
        if (documentManager != null)
            documentManager.OnOutboundDocumentMessage += HandleOutboundMessage;
    }

    private void OnDisable()
    {
        if (documentManager != null)
            documentManager.OnOutboundDocumentMessage -= HandleOutboundMessage;
    }

    [ContextMenu("Setup Session (document-start)")]
    public void SetupSession()
    {
        if (!HasManager())
            return;

        var json = $"{{\"type\":\"document-start\",\"documentId\":\"{documentId}\",\"documentName\":\"{documentName}\",\"totalPages\":{Mathf.Max(0, totalPages)}}}";
        documentManager.HandleMessage(json);
        Debug.Log($"DocumentNavigationHarness: setup session for document '{documentId}' totalPages={Mathf.Max(0, totalPages)}.");
    }

    [ContextMenu("Press Prev")]
    public void PressPrev()
    {
        if (!HasManager())
            return;

        var sent = documentManager.NavigatePrevious("harness-prev");
        Debug.Log($"DocumentNavigationHarness: prev pressed. sent={sent}, currentPage={documentManager.CurrentPageIndex + 1}/{documentManager.TotalPages}");
    }

    [ContextMenu("Press Next")]
    public void PressNext()
    {
        if (!HasManager())
            return;

        var sent = documentManager.NavigateNext("harness-next");
        Debug.Log($"DocumentNavigationHarness: next pressed. sent={sent}, currentPage={documentManager.CurrentPageIndex + 1}/{documentManager.TotalPages}");
    }

    [ContextMenu("Press Jump (configured page)")]
    public void PressJump()
    {
        if (!HasManager())
            return;

        var targetPageIndex = Mathf.Max(1, jumpToPageOneBased) - 1;
        var sent = documentManager.NavigateToPage(targetPageIndex, "harness-jump");
        Debug.Log($"DocumentNavigationHarness: jump pressed target={jumpToPageOneBased}, sent={sent}, currentPage={documentManager.CurrentPageIndex + 1}/{documentManager.TotalPages}");
    }

    [ContextMenu("Simulate Inbound Navigate")]
    public void SimulateInboundNavigate()
    {
        if (!HasManager())
            return;

        var pageIndex = Mathf.Max(1, inboundNavigatePageOneBased) - 1;
        var json = $"{{\"type\":\"document-navigate\",\"documentId\":\"{documentId}\",\"pageIndex\":{pageIndex},\"source\":\"harness-remote\"}}";
        documentManager.HandleMessage(json);
        Debug.Log($"DocumentNavigationHarness: simulated inbound navigate to {inboundNavigatePageOneBased}. currentPage={documentManager.CurrentPageIndex + 1}/{documentManager.TotalPages}");
    }

    private bool HasManager()
    {
        if (documentManager != null)
            return true;

        Debug.LogError("DocumentNavigationHarness: DocumentManager is not assigned.");
        return false;
    }

    private static void HandleOutboundMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        var root = JObject.Parse(json);
        var type = (string)root["type"];
        var pageIndex = (int?)root["pageIndex"];
        var documentId = (string)root["documentId"];
        Debug.Log($"DocumentNavigationHarness: outbound '{type}' doc={documentId}, pageIndex={pageIndex}");
    }
}
