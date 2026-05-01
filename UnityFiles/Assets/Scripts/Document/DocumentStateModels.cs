public sealed class DocumentSessionState
{
    public bool IsDocumentOpen;
    public string CurrentDocumentName;
    public int TotalPages;
    public int CurrentPageIndex = -1;
}

public sealed class PageAssemblyState
{
    public int PageIndex;
    public int TotalPages;
    public int Width;
    public int Height;
    public int TotalChunks;
    public byte[][] Chunks;
    public int ReceivedChunks;
    public float CreatedAt;
}
