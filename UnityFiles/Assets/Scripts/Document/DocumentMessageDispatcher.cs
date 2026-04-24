using System;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.WebRTC;
using UnityEngine;

public class DocumentMessageDispatcher : MonoBehaviour
{
    public event Action<DocumentStartMessage> OnDocumentStart;
    public event Action<DocumentPageMessage> OnDocumentPage;
    public event Action<DocumentCloseMessage> OnDocumentClose;

    private RTCDataChannel _dataChannel;

    public void HandleDataChannel(RTCDataChannel channel, ConcurrentQueue<string> documentQueue)
    {
        if (_dataChannel != null)
            _dataChannel.OnMessage = null;

        _dataChannel = channel;

        if (_dataChannel == null)
            return;

        _dataChannel.OnMessage = bytes =>
        {
            var json = Encoding.UTF8.GetString(bytes);
            documentQueue.Enqueue(json);
        };
    }

    public void HandleMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"DocumentMessageDispatcher: invalid JSON payload. {ex.Message}");
            return;
        }

        var type = (string)root["type"];
        if (string.IsNullOrWhiteSpace(type))
        {
            Debug.LogWarning("DocumentMessageDispatcher: payload missing 'type'.");
            return;
        }

        switch (type)
        {
            case "document-start":
                OnDocumentStart?.Invoke(root.ToObject<DocumentStartMessage>());
                break;

            case "document-page":
                OnDocumentPage?.Invoke(root.ToObject<DocumentPageMessage>());
                break;

            case "document-close":
                OnDocumentClose?.Invoke(root.ToObject<DocumentCloseMessage>());
                break;

            default:
                Debug.LogWarning($"DocumentMessageDispatcher: unsupported type '{type}'.");
                break;
        }
    }

    private void OnDestroy()
    {
        if (_dataChannel != null)
        {
            _dataChannel.OnMessage = null;
            _dataChannel = null;
        }
    }
}
