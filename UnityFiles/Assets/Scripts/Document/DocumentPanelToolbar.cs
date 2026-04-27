using TMPro;
using UnityEngine;

/// <summary>
/// Basic toolbar wiring for the document panel structure:
/// DocumentPanelRoot
/// ├── BackplatePlane / BackplateQuad
/// ├── PdfPageQuad
/// └── ToolbarCanvas
///     ├── PreviousButton
///     ├── PageNumberLabel
///     ├── NextButton
///     ├── ZoomOutButton
///     ├── ZoomPercentLabel
///     └── ZoomInButton
///
/// Zoom controls are UI-only for now.
/// </summary>
public class DocumentPanelToolbar : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private DocumentNavigationControls navigationControls;

    [Header("Toolbar UI (wire in Inspector)")]
    [SerializeField] private TMP_Text pageNumberLabel;
    [SerializeField] private TMP_Text zoomPercentLabel;

    [Header("Zoom (UI-only)")]
    [SerializeField] private int defaultZoomPercent = 100;
    [SerializeField] private int minZoomPercent = 50;
    [SerializeField] private int maxZoomPercent = 200;
    [SerializeField] private int zoomStepPercent = 25;

    private int _zoomPercent;

    private void Awake()
    {
        _zoomPercent = Mathf.Clamp(defaultZoomPercent, minZoomPercent, maxZoomPercent);
        RefreshLabels();
    }

    private void OnEnable()
    {
        WireDocumentEvents(true);
        RefreshLabels();
    }

    private void OnDisable()
    {
        WireDocumentEvents(false);
    }

    private void OnValidate()
    {
        _zoomPercent = Mathf.Clamp(_zoomPercent <= 0 ? defaultZoomPercent : _zoomPercent, minZoomPercent, maxZoomPercent);
        if (!Application.isPlaying)
            RefreshLabels();
    }

    private void WireDocumentEvents(bool subscribe)
    {
        if (documentManager == null)
            return;

        if (subscribe)
        {
            documentManager.OnDocumentStart += HandleDocumentStart;
            documentManager.OnDocumentPage += HandleDocumentPage;
            documentManager.OnDocumentClose += HandleDocumentClose;
        }
        else
        {
            documentManager.OnDocumentStart -= HandleDocumentStart;
            documentManager.OnDocumentPage -= HandleDocumentPage;
            documentManager.OnDocumentClose -= HandleDocumentClose;
        }
    }

    private void HandleDocumentStart(DocumentStartMessage _)
    {
        RefreshPageLabel();
    }

    private void HandleDocumentPage(DocumentPageMessage _)
    {
        RefreshPageLabel();
    }

    private void HandleDocumentClose(DocumentCloseMessage _)
    {
        RefreshPageLabel();
    }

    public void OnPreviousPressed()
    {
        if (navigationControls == null)
        {
            Debug.LogWarning("DocumentPanelToolbar: DocumentNavigationControls is not assigned.");
            return;
        }

        navigationControls.OnPrevPressed();
        RefreshPageLabel();
    }

    public void OnNextPressed()
    {
        if (navigationControls == null)
        {
            Debug.LogWarning("DocumentPanelToolbar: DocumentNavigationControls is not assigned.");
            return;
        }

        navigationControls.OnNextPressed();
        RefreshPageLabel();
    }

    public void OnZoomOutPressed()
    {
        _zoomPercent = Mathf.Clamp(_zoomPercent - Mathf.Max(1, zoomStepPercent), minZoomPercent, maxZoomPercent);
        RefreshZoomLabel();
    }

    public void OnZoomInPressed()
    {
        _zoomPercent = Mathf.Clamp(_zoomPercent + Mathf.Max(1, zoomStepPercent), minZoomPercent, maxZoomPercent);
        RefreshZoomLabel();
    }

    private void RefreshLabels()
    {
        RefreshPageLabel();
        RefreshZoomLabel();
    }

    private void RefreshPageLabel()
    {
        if (pageNumberLabel == null)
            return;

        var total = documentManager != null ? Mathf.Max(0, documentManager.TotalPages) : 0;
        var currentOneBased = 0;

        if (documentManager != null && total > 0)
            currentOneBased = Mathf.Clamp(documentManager.CurrentPageIndex + 1, 1, total);

        pageNumberLabel.text = $"Page {currentOneBased} / {total}";
    }

    private void RefreshZoomLabel()
    {
        if (zoomPercentLabel == null)
            return;

        zoomPercentLabel.text = $"{_zoomPercent}%";
    }

}
