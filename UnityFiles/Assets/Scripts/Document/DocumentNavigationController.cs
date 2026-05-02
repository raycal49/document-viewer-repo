using System;
using UnityEngine;

public class DocumentNavigationController : MonoBehaviour
{
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private DocumentNavigationChannel navigationChannel;

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

    public bool NavigatePrevious()
    {
        if (documentManager == null)
            return false;

        return NavigateToPage(documentManager.CurrentPageIndex - 1);
    }

    public bool NavigateNext()
    {
        if (documentManager == null)
            return false;

        return NavigateToPage(documentManager.CurrentPageIndex + 1);
    }

    public bool NavigateToPage(int targetPageIndex)
    {
        if (!CanNavigate())
            return false;

        if (!documentManager.TrySetCurrentPageIndex(targetPageIndex, out var clampedPageIndex))
            return false;

        var message = new DocumentNavigateMessage
        {
            pageIndex = clampedPageIndex,
        };

        OnNavigateApplied?.Invoke(message);
        return true;
    }


    private void HandleInboundNavigate(DocumentNavigateMessage message)
    {
        if (!documentManager.TrySetCurrentPageIndex(message.pageIndex, out var clampedPageIndex))
            return;

        message.pageIndex = clampedPageIndex;
        OnNavigateApplied?.Invoke(message);
    }

    private bool CanNavigate()
    {
        if (documentManager == null)
        {
            Debug.LogWarning("DocumentNavigationController: DocumentManager is not assigned.");
            return false;
        }


        if (documentManager.TotalPages <= 0)
        {
            Debug.LogWarning("DocumentNavigationController: active document has no pages.");
            return false;
        }

        return true;
    }


    private void AttachToChannel()
    {
        if (navigationChannel == null)
            return;

        navigationChannel.OnNavigateReceived += HandleInboundNavigate;
    }

    private void DetachFromChannel()
    {
        if (navigationChannel == null)
            return;

        navigationChannel.OnNavigateReceived -= HandleInboundNavigate;
    }
}
