using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Logs document-related JSON traffic for local harness observability.
/// </summary>
public class DocumentJsonTrafficLogger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private DocumentNavigationChannel navigationChannel;

    [Header("Formatting")]
    [SerializeField] private bool includeTimestamp = true;
    [SerializeField] private bool prettyPrintJson = false;

    private void OnEnable()
    {
        if (documentManager != null)
            documentManager.OnRawJsonReceived += HandleDocumentManagerInbound;

        if (navigationChannel != null)
        {
            navigationChannel.OnOutboundMessageSerialized += HandleNavigationOutbound;
            navigationChannel.OnInboundMessageObserved += HandleNavigationInbound;
        }
    }

    private void OnDisable()
    {
        if (documentManager != null)
            documentManager.OnRawJsonReceived -= HandleDocumentManagerInbound;

        if (navigationChannel != null)
        {
            navigationChannel.OnOutboundMessageSerialized -= HandleNavigationOutbound;
            navigationChannel.OnInboundMessageObserved -= HandleNavigationInbound;
        }
    }

    private void HandleDocumentManagerInbound(string json)
    {
        Log("DOC-IN", json);
    }

    private void HandleNavigationOutbound(string json)
    {
        Log("NAV-OUT", json);
    }

    private void HandleNavigationInbound(string json)
    {
        Log("NAV-IN", json);
    }

    private void Log(string lane, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        var payload = prettyPrintJson ? TryPrettyPrint(json) : json;
        var prefix = includeTimestamp
            ? $"[DocumentJsonTrafficLogger][{DateTime.UtcNow:HH:mm:ss.fff}][{lane}]"
            : $"[DocumentJsonTrafficLogger][{lane}]";

        Debug.Log($"{prefix} {payload}");
    }

    private static string TryPrettyPrint(string json)
    {
        try
        {
            var token = JToken.Parse(json);
            return token.ToString(Newtonsoft.Json.Formatting.Indented);
        }
        catch
        {
            return json;
        }
    }
}
