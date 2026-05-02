using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocumentNavigationController : MonoBehaviour
{
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private DocumentNavigationChannel navigationChannel;

    [Header("Jump-to-page UI")]
    [SerializeField] private GameObject jumpPanel;
    [SerializeField] private TMP_InputField jumpInputField;

    public event Action<DocumentNavigateMessage> OnNavigateApplied;

    [Header("PageCountJump")]
    [SerializeField] private TextMeshProUGUI pageCountJumpLabel;

    public void Configure(DocumentManager manager, DocumentNavigationChannel channel)
    {
        DetachFromChannel();
        documentManager = manager;
        navigationChannel = channel;
        AttachToChannel();
    }

    private void Start()
    {
        documentManager.OnDocumentStart += RefreshPageCountJumpLabel;
    }

    private void OnEnable()
    {
        AttachToChannel();
    }

    private void OnDisable()
    {
        DetachFromChannel();
    }

    // actually, due to the nature of how this works, really, its just `NavigateToPage` that must call `SendNavigate`! 
    // this is because, well, everything else just calls `NavigateToPage`!
    public bool NavigatePrevious()
    {
        if (documentManager == null)
            return false;

        // this needs to call `SendNavigate`
        return NavigateToPage(documentManager.CurrentPageIndex - 1);
    }

    public bool NavigateNext()
    {
        if (documentManager == null)
            return false;

        return NavigateToPage(documentManager.CurrentPageIndex + 1);
    }

    // Now THIS
    public bool NavigateToPage(int targetPageIndex)
    {
        if (!CanNavigate())
            return false;

        if (!documentManager.TrySetCurrentPageIndex(targetPageIndex, out var clampedPageIndex))
            return false;

        int previousPageIndex = documentManager.CurrentPageIndex-1;

        int delta = clampedPageIndex - previousPageIndex;
        if (delta != 0)
        {
            RefreshPageCountJumpLabel();
        }

        var message = new DocumentNavigateMessage
        {
            pageIndex = clampedPageIndex,
        };

        OnNavigateApplied?.Invoke(message);
        return true;
    }

    private void RefreshPageCountJumpLabel()
    {
        if (pageCountJumpLabel == null || documentManager == null)
            return;

        int totalPages = documentManager.TotalPages;

        if (totalPages <= 0)
        {
            pageCountJumpLabel.text = "- / -";
            return;
        }

        // Convert 0-based index to 1-based page number for display.
        int currentDisplayPage = Mathf.Clamp(documentManager.CurrentPageIndex + 1, 1, totalPages);
        pageCountJumpLabel.text = $"{currentDisplayPage} / {totalPages}";
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

    public void OnPageCountJumpClicked()
    {
        if (jumpPanel == null || jumpInputField == null)
        {
            Debug.LogWarning("DocumentNavigationControls: jump panel/input not assigned.");
            return;
        }

        jumpPanel.SetActive(true);
        jumpInputField.text = string.Empty;
        jumpInputField.Select();
        jumpInputField.ActivateInputField();
    }

    public void OnJumpConfirmClicked()
    {
        if (jumpInputField == null)
        {
            Debug.LogWarning("DocumentNavigationControls: jump input field is not assigned.");
            return;
        }

        var raw = jumpInputField.text?.Trim();
        if (!int.TryParse(raw, out var oneBasedPage))
        {
            Debug.LogWarning($"DocumentNavigationControls: invalid jump input '{raw}'.");
            return; // keep panel open so user can fix
        }

        // convert user-facing 1-based page number to 0-based index
        int zeroBasedPage = oneBasedPage - 1;

        bool moved = NavigateToPage(zeroBasedPage);
        if (!moved)
        {
            Debug.LogWarning($"DocumentNavigationControls: jump target out of range or navigation unavailable ({oneBasedPage}).");
            return; // keep open
        }

        jumpPanel.SetActive(false);
    }


    public void OnJumpCancelClicked()
    {
        if (jumpPanel != null)
            jumpPanel.SetActive(false);
    }
}
