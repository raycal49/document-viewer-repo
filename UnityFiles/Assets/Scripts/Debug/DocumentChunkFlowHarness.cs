using System;
using UnityEngine;

public class DocumentChunkFlowHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DocumentManager documentManager;

    [Header("Document metadata")]
    [SerializeField] private string documentId = "manual-001";
    [SerializeField] private string documentName = "Sample Manual";
    [SerializeField] private int totalPages = 10;
    [SerializeField] private int pageIndex = 0;
    [SerializeField] private int width = 1200;
    [SerializeField] private int height = 1600;

    [Header("Chunk source")]
    [Tooltip("JPEG bytes used for chunk simulation.")]
    [SerializeField] private TextAsset pageJpegBytes;
    [SerializeField] private int chunkSizeBytes = 16 * 1024;

    [ContextMenu("Send Start")]
    public void SendStart()
    {
        if (!CanSend()) return;

        var json = $"{{\"type\":\"document-start\",\"documentName\":\"{documentId}\",\"documentName\":\"{documentName}\",\"totalPages\":{Mathf.Max(0, totalPages)}}}";
        documentManager.HandleMessage(json);
        Debug.Log($"DocumentChunkFlowHarness: sent document-start id={documentId}, totalPages={totalPages}");
    }

    [ContextMenu("Send Chunked Page (In Order)")]
    public void SendChunkedPageInOrder()
    {
        if (!CanSend(requireBytes: true)) return;

        var chunks = BuildChunks(pageJpegBytes.bytes, Mathf.Max(1, chunkSizeBytes));
        for (var i = 0; i < chunks.Length; i++)
            SendPageChunk(i, chunks.Length, chunks[i]);
    }

    [ContextMenu("Send Chunked Page (Out Of Order)")]
    public void SendChunkedPageOutOfOrder()
    {
        if (!CanSend(requireBytes: true)) return;

        var chunks = BuildChunks(pageJpegBytes.bytes, Mathf.Max(1, chunkSizeBytes));
        for (var i = chunks.Length - 1; i >= 0; i--)
            SendPageChunk(i, chunks.Length, chunks[i]);
    }

    [ContextMenu("Send Chunked Page (With Duplicate First Chunk)")]
    public void SendChunkedPageWithDuplicateFirstChunk()
    {
        if (!CanSend(requireBytes: true)) return;

        var chunks = BuildChunks(pageJpegBytes.bytes, Mathf.Max(1, chunkSizeBytes));
        if (chunks.Length == 0) return;

        SendPageChunk(0, chunks.Length, chunks[0]);
        SendPageChunk(0, chunks.Length, chunks[0]);

        for (var i = 1; i < chunks.Length; i++)
            SendPageChunk(i, chunks.Length, chunks[i]);
    }

    [ContextMenu("Send Close")]
    public void SendClose()
    {
        if (!CanSend()) return;

        var json = $"{{\"type\":\"document-close\",\"documentName\":\"{documentId}\"}}";
        documentManager.HandleMessage(json);
        Debug.Log($"DocumentChunkFlowHarness: sent document-close id={documentId}");
    }

    [ContextMenu("Run Full Happy Path")]
    public void RunFullHappyPath()
    {
        SendStart();
        SendChunkedPageInOrder();
    }

    private bool CanSend(bool requireBytes = false)
    {
        if (documentManager == null)
        {
            Debug.LogError("DocumentChunkFlowHarness: DocumentManager is not assigned.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            Debug.LogError("DocumentChunkFlowHarness: documentName is empty.");
            return false;
        }

        if (!requireBytes)
            return true;

        if (pageJpegBytes == null || pageJpegBytes.bytes == null || pageJpegBytes.bytes.Length == 0)
        {
            Debug.LogError("DocumentChunkFlowHarness: pageJpegBytes is missing or empty.");
            return false;
        }

        return true;
    }

    private void SendPageChunk(int chunkIndex, int totalChunks, byte[] chunkBytes)
    {
        var payload = Convert.ToBase64String(chunkBytes);
        var json =
            $"{{\"type\":\"document-page\",\"documentName\":\"{documentId}\",\"pageIndex\":{pageIndex},\"totalPages\":{Mathf.Max(0, totalPages)},\"width\":{Mathf.Max(1, width)},\"height\":{Mathf.Max(1, height)},\"chunkIndex\":{chunkIndex},\"totalChunks\":{totalChunks},\"data\":\"{payload}\"}}";

        documentManager.HandleMessage(json);
        Debug.Log($"DocumentChunkFlowHarness: sent chunk {chunkIndex + 1}/{totalChunks} for page={pageIndex}, bytes={chunkBytes.Length}");
    }

    private static byte[][] BuildChunks(byte[] source, int chunkSize)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<byte[]>();

        var totalChunks = Mathf.CeilToInt(source.Length / (float)chunkSize);
        var chunks = new byte[totalChunks][];

        for (var i = 0; i < totalChunks; i++)
        {
            var offset = i * chunkSize;
            var length = Mathf.Min(chunkSize, source.Length - offset);
            var chunk = new byte[length];
            Buffer.BlockCopy(source, offset, chunk, 0, length);
            chunks[i] = chunk;
        }

        return chunks;
    }
}
