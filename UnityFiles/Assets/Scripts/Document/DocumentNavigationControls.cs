using UnityEngine;

public class DocumentNavigationControls : MonoBehaviour
{
    [SerializeField] private DocumentNavigationController navigationController;
    [SerializeField] private string prevSource = "quest-prev-button";
    [SerializeField] private string nextSource = "quest-next-button";
    [SerializeField] private string jumpSource = "quest-jump-input";

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
}
