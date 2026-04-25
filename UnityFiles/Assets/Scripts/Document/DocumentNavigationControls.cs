using UnityEngine;

public class DocumentNavigationControls : MonoBehaviour
{
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private string prevSource = "quest-prev-button";
    [SerializeField] private string nextSource = "quest-next-button";
    [SerializeField] private string jumpSource = "quest-jump-input";

    public void OnPrevPressed()
    {
        if (documentManager == null)
        {
            Debug.LogWarning("DocumentNavigationControls: DocumentManager is not assigned.");
            return;
        }

        documentManager.NavigatePrevious(prevSource);
    }

    public void OnNextPressed()
    {
        if (documentManager == null)
        {
            Debug.LogWarning("DocumentNavigationControls: DocumentManager is not assigned.");
            return;
        }

        documentManager.NavigateNext(nextSource);
    }

    public void OnJumpSubmitted(string pageText)
    {
        if (documentManager == null)
        {
            Debug.LogWarning("DocumentNavigationControls: DocumentManager is not assigned.");
            return;
        }

        if (!int.TryParse(pageText, out var oneBasedPage))
        {
            Debug.LogWarning($"DocumentNavigationControls: invalid jump input '{pageText}'.");
            return;
        }

        var zeroBasedPage = oneBasedPage - 1;
        documentManager.NavigateToPage(zeroBasedPage, jumpSource);
    }
}
