using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DocumentUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private DocumentNavigationController navigationController;

    [Header("Footer Navigation")]
    [SerializeField] private TextMeshProUGUI pageStatusLabel;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Header Info")]
    [SerializeField] private TextMeshProUGUI documentTitleLabel;

    [Header("Zoom Info")]
    [SerializeField] private TextMeshProUGUI zoomPercentLabel;

    private void OnEnable()
    {
        if (documentManager != null)
        {
            documentManager.OnPageIndexChanged += HandlePageIndexChanged;
            documentManager.OnDocumentClose += HandleDocumentClose;
        }

        if (prevButton != null) prevButton.onClick.AddListener(OnPrevClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);

        UpdateUI();
    }

    private void OnDisable()
    {
        if (documentManager != null)
        {
            documentManager.OnPageIndexChanged -= HandlePageIndexChanged;
            documentManager.OnDocumentClose -= HandleDocumentClose;
        }

        if (prevButton != null) prevButton.onClick.RemoveListener(OnPrevClicked);
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
    }

    private void HandleDocumentStart(DocumentStartMessage message)
    {
        if (documentTitleLabel != null)
            documentTitleLabel.text = message.documentName;
        UpdateUI();
    }

    private void HandlePageIndexChanged(int current, int total)
    {
        UpdateUI();
    }

    private void HandleDocumentClose(DocumentCloseMessage message)
    {
        if (documentTitleLabel != null)
            documentTitleLabel.text = "No Document";
        UpdateUI();
    }

    public void OnPrevClicked()
    {
        if (navigationController != null)
            navigationController.NavigatePrevious();
    }

    public void OnNextClicked()
    {
        if (navigationController != null)
            navigationController.NavigateNext();
    }

    private void UpdateUI()
    {
        if (documentManager == null) return;

        if (pageStatusLabel != null)
        {
            int currentPage = documentManager.CurrentPageIndex + 1;
            int totalPages = documentManager.TotalPages;
            
            if (totalPages <= 0)
            {
                pageStatusLabel.text = "- / -";
            }
            else
            {
                pageStatusLabel.text = $"{currentPage} / {totalPages}";
            }
        }

        bool canNavigate = documentManager.IsDocumentOpen && documentManager.TotalPages > 0;

        if (prevButton != null)
            prevButton.interactable = canNavigate && documentManager.CurrentPageIndex > 0;

        if (nextButton != null)
            nextButton.interactable = canNavigate && documentManager.CurrentPageIndex < documentManager.TotalPages - 1;
    }
}