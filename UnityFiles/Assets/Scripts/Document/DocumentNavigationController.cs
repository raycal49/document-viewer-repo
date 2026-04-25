using System;
using UnityEngine;

public class DocumentNavigationController : MonoBehaviour
{
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private DocumentNavigationChannel navigationChannel;

    public event Action<DocumentNavigateMessage> OnNavigateIntent;
    public event Action<DocumentRequestPageMessage> OnRequestPageIntent;
    public event Action<DocumentNavigateMessage> OnNavigateApplied;

    public void Configure(DocumentManager manager, DocumentNavigationChannel channel)
    {
        DetachFromChannel();
        documentManager = manager;
        navigationChannel = channel;
        AttachToChannel();
    }

    private void OnEnable()
    {
        AttachToChannel();
    }

    private void OnDisable()
    {
        DetachFromChannel();
    }

    public bool NavigatePrevious(string source = "quest-prev")
    {
        if (documentManager == null)
            return false;

        return NavigateToPage(documentManager.CurrentPageIndex - 1, source);
    }

    public bool NavigateNext(string source = "quest-next")
    {
        if (documentManager == null)
            return false;

        return NavigateToPage(documentManager.CurrentPageIndex + 1, source);
    }

    public bool NavigateToPage(int targetPageIndex, string source = "quest-jump")
    {
        if (!CanNavigate())
            return false;

        if (!documentManager.TrySetCurrentPageIndex(targetPageIndex, out var clampedPageIndex))
            return false;

        var message = new DocumentNavigateMessage
        {
            documentId = documentManager.CurrentDocumentId,
            pageIndex = clampedPageIndex,
            source = source
        };

        OnNavigateIntent?.Invoke(message);
        OnNavigateApplied?.Invoke(message);
        return true;
    }

    public bool RequestPage(int pageIndex)
    {
        if (!CanNavigate())
            return false;

        if (!documentManager.TrySetCurrentPageIndex(pageIndex, out var clampedPageIndex))
            return false;

        var message = new DocumentRequestPageMessage
        {
            documentId = documentManager.CurrentDocumentId,
            pageIndex = clampedPageIndex
        };

        OnRequestPageIntent?.Invoke(message);
        return true;
    }

    private void HandleInboundNavigate(DocumentNavigateMessage message)
    {
        if (!IsValidForCurrentDocument(message?.documentId))
            return;

        if (!documentManager.TrySetCurrentPageIndex(message.pageIndex, out var clampedPageIndex))
            return;

        message.pageIndex = clampedPageIndex;
        OnNavigateApplied?.Invoke(message);
    }

    private void HandleInboundRequestPage(DocumentRequestPageMessage message)
    {
        if (!IsValidForCurrentDocument(message?.documentId))
            return;

        if (!documentManager.TrySetCurrentPageIndex(message.pageIndex, out _))
            return;
    }

    private bool CanNavigate()
    {
        if (documentManager == null)
        {
            Debug.LogWarning("DocumentNavigationController: DocumentManager is not assigned.");
            return false;
        }

        if (!documentManager.IsDocumentOpen || string.IsNullOrWhiteSpace(documentManager.CurrentDocumentId))
        {
            Debug.LogWarning("DocumentNavigationController: no active document to navigate.");
            return false;
        }

        if (documentManager.TotalPages <= 0)
        {
            Debug.LogWarning("DocumentNavigationController: active document has no pages.");
            return false;
        }

        return true;
    }

    private bool IsValidForCurrentDocument(string documentId)
    {
        if (!CanNavigate())
            return false;

        if (!string.Equals(documentId, documentManager.CurrentDocumentId, StringComparison.Ordinal))
        {
            Debug.LogWarning($"DocumentNavigationController: ignoring navigation for non-active document '{documentId}'. Active='{documentManager.CurrentDocumentId}'.");
            return false;
        }

        return true;
    }

    private void AttachToChannel()
    {
        if (navigationChannel == null)
            return;

        navigationChannel.OnNavigateReceived += HandleInboundNavigate;
        navigationChannel.OnRequestPageReceived += HandleInboundRequestPage;
        OnNavigateIntent += ForwardNavigateIntent;
        OnRequestPageIntent += ForwardRequestPageIntent;
    }

    private void DetachFromChannel()
    {
        if (navigationChannel == null)
            return;

        navigationChannel.OnNavigateReceived -= HandleInboundNavigate;
        navigationChannel.OnRequestPageReceived -= HandleInboundRequestPage;
        OnNavigateIntent -= ForwardNavigateIntent;
        OnRequestPageIntent -= ForwardRequestPageIntent;
    }

    private void ForwardNavigateIntent(DocumentNavigateMessage message)
    {
        navigationChannel?.SendNavigate(message);
    }

    private void ForwardRequestPageIntent(DocumentRequestPageMessage message)
    {
        navigationChannel?.SendRequestPage(message);
    }
}
