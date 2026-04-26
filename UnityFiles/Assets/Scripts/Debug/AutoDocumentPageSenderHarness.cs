using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Emulates the web app's rasterizer/sender side by observing quest navigation events
/// and sending protocol-shaped document messages into DocumentManager.
///
/// This allows editor/headset demos without a live web sender.
/// </summary>
public class AutoDocumentPageSenderHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private DocumentNavigationController navigationController;
    [SerializeField] private DocumentNavigationChannel navigationChannel;

    [Header("Document metadata")]
    [SerializeField] private string documentId = "exec_summary";
    [SerializeField] private string documentName = "Exec Summary";

    [Header("Page source")]
    [Tooltip("If enabled, pages are sorted by parsing names like ...page-0001 from Unordered Assets.")]
    [SerializeField] private bool autoSortFromNames = true;

    [Tooltip("Used when Auto Sort From Names is ON. Add page assets named like exec_summary_page-0001.")]
    [SerializeField] private List<TextAsset> unorderedAssets = new List<TextAsset>();

    [Tooltip("Used when Auto Sort From Names is OFF. Must already be in page order (1..N).")]
    [SerializeField] private List<TextAsset> orderedPageAssets = new List<TextAsset>();

    [Header("Transport simulation")]
    [SerializeField] private int chunkSizeBytes = 16 * 1024;
    [SerializeField] private bool sendFirstPageOnSessionStart = true;
    [SerializeField] private bool autoStartSessionOnEnable = true;
    [SerializeField] private bool listenToNavigateApplied = true;
    [SerializeField] private bool listenToRequestPageIntent = true;

    [Header("Observability")]
    [SerializeField] private bool logAllJsonTraffic = true;
    [SerializeField] private bool prettyPrintJsonLogs = false;
    [SerializeField] private int maxJsonCharacters = 0;

    [Header("Diagnostics")]
    [SerializeField] private bool verboseLogs = true;

    private static readonly Regex PageRegex = new Regex(@"page-(\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private List<TextAsset> _resolvedPages = new List<TextAsset>();
    private bool _sessionStarted;

    private void OnEnable()
    {
        Attach();
    }

    private IEnumerator Start()
    {
        if (!autoStartSessionOnEnable || _sessionStarted)
            yield break;

        // Let dependent components finish Awake/OnEnable before first send.
        yield return null;

        _sessionStarted = TryStartSessionAndSendFirstPage();
    }

    private void OnDisable()
    {
        Detach();
    }

    [ContextMenu("Resolve Pages")]
    public void ResolvePages()
    {
        _resolvedPages = BuildResolvedPages();
        if (verboseLogs)
            Debug.Log($"AutoDocumentPageSenderHarness: resolved {_resolvedPages.Count} pages.");
    }

    [ContextMenu("Start Session + First Page")]
    public void StartSessionAndSendFirstPage()
    {
        _sessionStarted = TryStartSessionAndSendFirstPage();
    }

    private bool TryStartSessionAndSendFirstPage()
    {
        if (!ValidateCoreReferences())
            return false;

        ResolvePages();
        if (_resolvedPages.Count == 0)
        {
            Debug.LogError("AutoDocumentPageSenderHarness: no page assets resolved.");
            return false;
        }

        SendDocumentStart(_resolvedPages.Count);

        if (sendFirstPageOnSessionStart)
            SendPageByZeroBasedIndex(0);

        return true;
    }

    [ContextMenu("Close Session")]
    public void CloseSession()
    {
        if (!ValidateManager())
            return;

        var root = new JObject
        {
            ["type"] = "document-close",
            ["documentId"] = documentId
        };

        var json = root.ToString(Formatting.None);
        EmitSentJson("document-close", json);
        documentManager.HandleMessage(json);
        _sessionStarted = false;

        if (verboseLogs)
            Debug.Log($"AutoDocumentPageSenderHarness: sent document-close for '{documentId}'.");
    }

    public void SendPageByOneBasedNumber(int oneBasedPage)
    {
        SendPageByZeroBasedIndex(oneBasedPage - 1);
    }

    public void SendPageByZeroBasedIndex(int zeroBasedPage)
    {
        if (!ValidateManager())
            return;

        if (_resolvedPages == null || _resolvedPages.Count == 0)
            ResolvePages();

        if (_resolvedPages.Count == 0)
        {
            Debug.LogError("AutoDocumentPageSenderHarness: no pages available.");
            return;
        }

        var clamped = Mathf.Clamp(zeroBasedPage, 0, _resolvedPages.Count - 1);
        var asset = _resolvedPages[clamped];

        if (asset == null || asset.bytes == null || asset.bytes.Length == 0)
        {
            Debug.LogError($"AutoDocumentPageSenderHarness: page asset is null/empty for index={clamped}.");
            return;
        }

        var chunks = BuildChunks(asset.bytes, Mathf.Max(1, chunkSizeBytes));

        for (var i = 0; i < chunks.Length; i++)
        {
            var pageRoot = new JObject
            {
                ["type"] = "document-page",
                ["documentId"] = documentId,
                ["pageIndex"] = clamped,
                ["totalPages"] = _resolvedPages.Count,
                ["width"] = 0,
                ["height"] = 0,
                ["chunkIndex"] = i,
                ["totalChunks"] = chunks.Length,
                ["data"] = Convert.ToBase64String(chunks[i])
            };

            var json = pageRoot.ToString(Formatting.None);
            EmitSentJson("document-page", json);
            documentManager.HandleMessage(json);
        }

        if (verboseLogs)
            Debug.Log($"AutoDocumentPageSenderHarness: sent page {clamped + 1}/{_resolvedPages.Count} ({chunks.Length} chunks, {asset.bytes.Length} bytes).");
    }

    private void Attach()
    {
        if (navigationController != null)
        {
            if (listenToNavigateApplied)
                navigationController.OnNavigateApplied += HandleNavigateApplied;

            if (listenToRequestPageIntent)
                navigationController.OnRequestPageIntent += HandleRequestPageIntent;
        }

        if (documentManager != null)
            documentManager.OnRawJsonMessageReceived += HandleDocumentManagerReceivedJson;

        if (navigationChannel != null)
        {
            navigationChannel.OnInboundMessageSerialized += HandleNavigationChannelInboundJson;
            navigationChannel.OnOutboundMessageSerialized += HandleNavigationChannelOutboundJson;
        }
    }

    private void Detach()
    {
        if (navigationController != null)
        {
            navigationController.OnNavigateApplied -= HandleNavigateApplied;
            navigationController.OnRequestPageIntent -= HandleRequestPageIntent;
        }

        if (documentManager != null)
            documentManager.OnRawJsonMessageReceived -= HandleDocumentManagerReceivedJson;

        if (navigationChannel != null)
        {
            navigationChannel.OnInboundMessageSerialized -= HandleNavigationChannelInboundJson;
            navigationChannel.OnOutboundMessageSerialized -= HandleNavigationChannelOutboundJson;
        }
    }

    private void HandleNavigateApplied(DocumentNavigateMessage message)
    {
        if (message == null || !IsForThisDocument(message.documentId))
            return;

        SendPageByZeroBasedIndex(message.pageIndex);
    }

    private void HandleRequestPageIntent(DocumentRequestPageMessage message)
    {
        if (message == null || !IsForThisDocument(message.documentId))
            return;

        SendPageByZeroBasedIndex(message.pageIndex);
    }

    private void HandleDocumentManagerReceivedJson(string json)
    {
        EmitReceivedJson("document-manager", json);
    }

    private void HandleNavigationChannelInboundJson(string json)
    {
        EmitReceivedJson("navigation-channel", json);
    }

    private void HandleNavigationChannelOutboundJson(string json)
    {
        EmitSentJson("navigation-channel", json);
    }

    private bool IsForThisDocument(string incomingDocumentId)
    {
        return string.Equals(incomingDocumentId, documentId, StringComparison.Ordinal);
    }

    private void SendDocumentStart(int totalPages)
    {
        var root = new JObject
        {
            ["type"] = "document-start",
            ["documentId"] = documentId,
            ["documentName"] = documentName,
            ["totalPages"] = Mathf.Max(0, totalPages)
        };

        var json = root.ToString(Formatting.None);
        EmitSentJson("document-start", json);
        documentManager.HandleMessage(json);

        if (verboseLogs)
            Debug.Log($"AutoDocumentPageSenderHarness: sent document-start id={documentId}, totalPages={totalPages}.");
    }

    private List<TextAsset> BuildResolvedPages()
    {
        if (!autoSortFromNames)
            return orderedPageAssets.Where(a => a != null).ToList();

        var parsed = new List<(int pageNumberOneBased, TextAsset asset)>();

        foreach (var asset in unorderedAssets)
        {
            if (asset == null)
                continue;

            var match = PageRegex.Match(asset.name);
            if (!match.Success)
            {
                Debug.LogWarning($"AutoDocumentPageSenderHarness: could not parse page number from '{asset.name}'. Expected ...page-0001 format.");
                continue;
            }

            if (!int.TryParse(match.Groups[1].Value, out var pageNumber) || pageNumber <= 0)
            {
                Debug.LogWarning($"AutoDocumentPageSenderHarness: invalid page number in '{asset.name}'.");
                continue;
            }

            parsed.Add((pageNumber, asset));
        }

        var duplicates = parsed
            .GroupBy(item => item.pageNumberOneBased)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            Debug.LogError($"AutoDocumentPageSenderHarness: duplicate page numbers found: {string.Join(", ", duplicates)}");
            return new List<TextAsset>();
        }

        return parsed
            .OrderBy(item => item.pageNumberOneBased)
            .Select(item => item.asset)
            .ToList();
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

    private bool ValidateCoreReferences()
    {
        return ValidateManager() && ValidateNavigationController();
    }

    private bool ValidateManager()
    {
        if (documentManager != null)
            return true;

        Debug.LogError("AutoDocumentPageSenderHarness: DocumentManager is not assigned.");
        return false;
    }

    private bool ValidateNavigationController()
    {
        if (navigationController != null)
            return true;

        Debug.LogError("AutoDocumentPageSenderHarness: DocumentNavigationController is not assigned.");
        return false;
    }

    private void EmitSentJson(string route, string json)
    {
        EmitJson("sent", route, json);
    }

    private void EmitReceivedJson(string route, string json)
    {
        EmitJson("received", route, json);
    }

    private void EmitJson(string direction, string route, string json)
    {
        if (!logAllJsonTraffic || string.IsNullOrWhiteSpace(json))
            return;

        var payload = FormatJsonForLog(json);
        Debug.Log($"[AutoDocumentPageSenderHarness][{direction}][{route}] {payload}");
    }

    private string FormatJsonForLog(string json)
    {
        var candidate = json;

        if (prettyPrintJsonLogs)
        {
            try
            {
                var parsed = JToken.Parse(json);
                candidate = parsed.ToString(Formatting.Indented);
            }
            catch
            {
                // keep raw string if parsing fails
            }
        }

        if (maxJsonCharacters > 0 && candidate.Length > maxJsonCharacters)
            return candidate.Substring(0, maxJsonCharacters) + "...<truncated>";

        return candidate;
    }
}
