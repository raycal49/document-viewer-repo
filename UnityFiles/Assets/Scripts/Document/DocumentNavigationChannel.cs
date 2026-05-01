using System;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.WebRTC;
using UnityEngine;

public class DocumentNavigationChannel : MonoBehaviour
{
    public event Action<DocumentNavigateMessage> OnNavigateReceived;
    public event Action<string> OnInboundMessageSerialized;
    public event Action<string> OnOutboundMessageSerialized;

    private RTCDataChannel _dataChannel;

    public void SetDataChannel(RTCDataChannel dataChannel)
    {
        _dataChannel = dataChannel;
    }

    public bool TryHandleInboundMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (Exception)
        {
            return false;
        }

        var type = (string)root["type"];
        if (string.IsNullOrWhiteSpace(type))
            return false;

        OnInboundMessageSerialized?.Invoke(json);

        switch (type)
        {
            case "document-navigate":
                OnNavigateReceived?.Invoke(root.ToObject<DocumentNavigateMessage>());
                return true;

            default:
                return false;
        }
    }

    public bool SendNavigate(DocumentNavigateMessage message)
    {
        return SendMessage("document-navigate", message);
    }

    private bool SendMessage(string type, object payload)
    {
        var root = JObject.FromObject(payload ?? new object());
        root["type"] = type;
        var json = root.ToString(Newtonsoft.Json.Formatting.None);

        OnOutboundMessageSerialized?.Invoke(json);

        if (_dataChannel == null)
        {
            Debug.LogWarning($"DocumentNavigationChannel: data channel unavailable; outbound payload not sent. type='{type}'.");
            return false;
        }

        _dataChannel.Send(Encoding.UTF8.GetBytes(json));
        return true;
    }
}
