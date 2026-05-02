using System;

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

public static class PageByteAssembler
{
    public static byte[] AssembleChunks(byte[][] chunks)
    {
        if (chunks == null || chunks.Length == 0)
            return Array.Empty<byte>();

        var totalLength = 0;
        for (var i = 0; i < chunks.Length; i++)
            totalLength += chunks[i].Length;

        var combined = new byte[totalLength];
        var offset = 0;
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            Buffer.BlockCopy(chunk, 0, combined, offset, chunk.Length);
            offset += chunk.Length;
        }

        return combined;
    }
}