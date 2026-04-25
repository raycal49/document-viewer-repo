using UnityEngine;

public class DocumentNavigationControls : MonoBehaviour
{
    [Header("Navigation wiring")]
    [SerializeField] private DocumentNavigationController navigationController;

    [Header("Navigation message sources")]
    [SerializeField] private string prevSource = "quest-prev-button";
    [SerializeField] private string nextSource = "quest-next-button";
    [SerializeField] private string jumpSource = "quest-jump-input";

    [Header("Quest controller shortcuts")]
    [Tooltip("When enabled, pressing A advances and pressing B goes back.")]
    [SerializeField] private bool enableQuestABPageTurn = true;

    [Tooltip("A button action source tag.")]
    [SerializeField] private string aButtonSource = "quest-a-button";

    [Tooltip("B button action source tag.")]
    [SerializeField] private string bButtonSource = "quest-b-button";

    [Tooltip("Minimum seconds between accepted A/B presses.")]
    [SerializeField] private float controllerDebounceSeconds = 0.2f;

    private float _nextControllerInputTime;

    private void Update()
    {
        if (!enableQuestABPageTurn)
            return;

        if (Time.unscaledTime < _nextControllerInputTime)
            return;

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            NavigateNextFromController();
            _nextControllerInputTime = Time.unscaledTime + Mathf.Max(0f, controllerDebounceSeconds);
            return;
        }

        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            NavigatePrevFromController();
            _nextControllerInputTime = Time.unscaledTime + Mathf.Max(0f, controllerDebounceSeconds);
        }
    }

    public void OnPrevPressed()
    {
        if (navigationController == null)
        {
            Debug.LogWarning("DocumentNavigationControls: DocumentNavigationController is not assigned.");
            return;
        }

        navigationController.NavigatePrevious(prevSource);
    }

    public void OnNextPressed()
    {
        if (navigationController == null)
        {
            Debug.LogWarning("DocumentNavigationControls: DocumentNavigationController is not assigned.");
            return;
        }

        navigationController.NavigateNext(nextSource);
    }

    public void OnJumpSubmitted(string pageText)
    {
        if (navigationController == null)
        {
            Debug.LogWarning("DocumentNavigationControls: DocumentNavigationController is not assigned.");
            return;
        }

        if (!int.TryParse(pageText, out var oneBasedPage))
        {
            Debug.LogWarning($"DocumentNavigationControls: invalid jump input '{pageText}'.");
            return;
        }

        var zeroBasedPage = oneBasedPage - 1;
        navigationController.NavigateToPage(zeroBasedPage, jumpSource);
    }

    private void NavigateNextFromController()
    {
        if (navigationController == null)
        {
            Debug.LogWarning("DocumentNavigationControls: DocumentNavigationController is not assigned.");
            return;
        }

        navigationController.NavigateNext(aButtonSource);
    }

    private void NavigatePrevFromController()
    {
        if (navigationController == null)
        {
            Debug.LogWarning("DocumentNavigationControls: DocumentNavigationController is not assigned.");
            return;
        }

        navigationController.NavigatePrevious(bButtonSource);
    }
}
