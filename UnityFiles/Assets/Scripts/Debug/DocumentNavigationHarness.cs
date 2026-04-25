using Newtonsoft.Json.Linq;
using UnityEngine;

public class DocumentNavigationHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private DocumentNavigationController navigationController;
    [SerializeField] private DocumentNavigationChannel navigationChannel;

    [Header("Session setup")]
    [SerializeField] private string documentId = "manual-001";
    [SerializeField] private string documentName = "Sample Manual";
    [SerializeField] private int totalPages = 12;

    [Header("Actions")]
    [SerializeField] private int jumpToPageOneBased = 1;
    [SerializeField] private int inboundNavigatePageOneBased = 1;

    private void OnEnable()
    {
        if (navigationChannel != null)
            navigationChannel.OnOutboundMessageSerialized += HandleOutboundMessage;
    }

    private void OnDisable()
    {
        if (navigationChannel != null)
            navigationChannel.OnOutboundMessageSerialized -= HandleOutboundMessage;
    }

    [ContextMenu("Setup Session (document-start)")]
    public void SetupSession()
    {
        if (!HasManager())
            return;

        if (navigationController != null && navigationChannel != null)
            navigationController.Configure(documentManager, navigationChannel);

        var json = $"{{\"type\":\"document-start\",\"documentId\":\"{documentId}\",\"documentName\":\"{documentName}\",\"totalPages\":{Mathf.Max(0, totalPages)}}}";
        documentManager.HandleMessage(json);
        Debug.Log($"DocumentNavigationHarness: setup session for document '{documentId}' totalPages={Mathf.Max(0, totalPages)}.");
    }

    [ContextMenu("Press Prev")]
    public void PressPrev()
    {
        if (!HasManagerAndController())
            return;

        var sent = navigationController.NavigatePrevious("harness-prev");
        Debug.Log($"DocumentNavigationHarness: prev pressed. sent={sent}, currentPage={documentManager.CurrentPageIndex + 1}/{documentManager.TotalPages}");
    }

    [ContextMenu("Press Next")]
    public void PressNext()
    {
        if (!HasManagerAndController())
            return;

        var sent = navigationController.NavigateNext("harness-next");
        Debug.Log($"DocumentNavigationHarness: next pressed. sent={sent}, currentPage={documentManager.CurrentPageIndex + 1}/{documentManager.TotalPages}");
    }

    [ContextMenu("Press Jump (configured page)")]
    public void PressJump()
    {
        if (!HasManagerAndController())
            return;

        var targetPageIndex = Mathf.Max(1, jumpToPageOneBased) - 1;
        var sent = navigationController.NavigateToPage(targetPageIndex, "harness-jump");
        Debug.Log($"DocumentNavigationHarness: jump pressed target={jumpToPageOneBased}, sent={sent}, currentPage={documentManager.CurrentPageIndex + 1}/{documentManager.TotalPages}");
    }

    [ContextMenu("Simulate Inbound Navigate")]
    public void SimulateInboundNavigate()
    {
        if (!HasManagerAndController() || navigationChannel == null)
            return;

        var pageIndex = Mathf.Max(1, inboundNavigatePageOneBased) - 1;
        var json = $"{{\"type\":\"document-navigate\",\"documentId\":\"{documentId}\",\"pageIndex\":{pageIndex},\"source\":\"harness-remote\"}}";
        navigationChannel.TryHandleInboundMessage(json);
        Debug.Log($"DocumentNavigationHarness: simulated inbound navigate to {inboundNavigatePageOneBased}. currentPage={documentManager.CurrentPageIndex + 1}/{documentManager.TotalPages}");
    }

    private bool HasManagerAndController()
    {
        if (documentManager == null)
        {
            Debug.LogError("DocumentNavigationHarness: DocumentManager is not assigned.");
            return false;
        }

        if (navigationController == null)
        {
            Debug.LogError("DocumentNavigationHarness: DocumentNavigationController is not assigned.");
            return false;
        }

        return true;
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
