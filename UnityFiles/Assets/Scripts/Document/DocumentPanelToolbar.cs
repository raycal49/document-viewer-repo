using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Toolbar UI")]
    [SerializeField] private Button previousButton;
    [SerializeField] private TMP_Text pageNumberLabel;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private TMP_Text zoomPercentLabel;
    [SerializeField] private Button zoomInButton;

    [Header("Zoom (UI-only)")]
    [SerializeField] private int defaultZoomPercent = 100;
    [SerializeField] private int minZoomPercent = 50;
    [SerializeField] private int maxZoomPercent = 200;
    [SerializeField] private int zoomStepPercent = 25;

    private int _zoomPercent;

    private void Awake()
    {
        AutoWireMissingReferences();
        _zoomPercent = Mathf.Clamp(defaultZoomPercent, minZoomPercent, maxZoomPercent);
        RefreshLabels();
    }

    private void OnEnable()
    {
        WireButtons(true);
        WireDocumentEvents(true);
        RefreshLabels();
    }

    private void OnDisable()
    {
        WireButtons(false);
        WireDocumentEvents(false);
    }

    private void WireButtons(bool subscribe)
    {
        if (previousButton != null)
        {
            if (subscribe) previousButton.onClick.AddListener(HandlePreviousClicked);
            else previousButton.onClick.RemoveListener(HandlePreviousClicked);
        }

        if (nextButton != null)
        {
            if (subscribe) nextButton.onClick.AddListener(HandleNextClicked);
            else nextButton.onClick.RemoveListener(HandleNextClicked);
        }

        if (zoomOutButton != null)
        {
            if (subscribe) zoomOutButton.onClick.AddListener(HandleZoomOutClicked);
            else zoomOutButton.onClick.RemoveListener(HandleZoomOutClicked);
        }

        if (zoomInButton != null)
        {
            if (subscribe) zoomInButton.onClick.AddListener(HandleZoomInClicked);
            else zoomInButton.onClick.RemoveListener(HandleZoomInClicked);
        }
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

    private void HandlePreviousClicked()
    {
        if (navigationControls == null)
        {
            Debug.LogWarning("DocumentPanelToolbar: DocumentNavigationControls is not assigned.");
            return;
        }

        navigationControls.OnPrevPressed();
        RefreshPageLabel();
    }

    private void HandleNextClicked()
    {
        if (navigationControls == null)
        {
            Debug.LogWarning("DocumentPanelToolbar: DocumentNavigationControls is not assigned.");
            return;
        }

        navigationControls.OnNextPressed();
        RefreshPageLabel();
    }

    private void HandleZoomOutClicked()
    {
        _zoomPercent = Mathf.Clamp(_zoomPercent - Mathf.Max(1, zoomStepPercent), minZoomPercent, maxZoomPercent);
        RefreshZoomLabel();
    }

    private void HandleZoomInClicked()
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

    private void AutoWireMissingReferences()
    {
        if (documentManager == null)
            documentManager = GetComponentInParent<DocumentManager>();

        if (navigationControls == null)
            navigationControls = GetComponentInParent<DocumentNavigationControls>();

        if (previousButton == null)
            previousButton = FindChildComponentByName<Button>("PreviousButton");
        if (pageNumberLabel == null)
            pageNumberLabel = FindChildComponentByName<TMP_Text>("PageNumberLabel");
        if (nextButton == null)
            nextButton = FindChildComponentByName<Button>("NextButton");
        if (zoomOutButton == null)
            zoomOutButton = FindChildComponentByName<Button>("ZoomOutButton");
        if (zoomPercentLabel == null)
            zoomPercentLabel = FindChildComponentByName<TMP_Text>("ZoomPercentLabel");
        if (zoomInButton == null)
            zoomInButton = FindChildComponentByName<Button>("ZoomInButton");
    }

    private T FindChildComponentByName<T>(string objectName) where T : Component
    {
        var children = GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in children)
        {
            if (!string.Equals(child.name, objectName))
                continue;

            var component = child.GetComponent<T>();
            if (component != null)
                return component;
        }

        return null;
    }
}
