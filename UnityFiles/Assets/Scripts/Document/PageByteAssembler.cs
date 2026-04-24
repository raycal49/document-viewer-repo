using System;

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
