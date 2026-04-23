using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SignalingClient : MonoBehaviour
{
    [Header("Dependancies")]
    [SerializeField] public SignalingHttpClient httpClient;
    //private string signalingServerUrl = "https://ar-signaling-server.azurewebsites.net";

    // Fired after ICE config and negotiate requests succeed
    public event Action<IceConfigResponse> OnIceConfigReady;

    // Signaling events for WebRTCSender to handle
    public event Action OnCallAccepted;
    public event Action OnCallDeclined;
    public event Action<string> OnAnswerReceived;
    public event Action<Dictionary<string, object>> OnIceCandidateReceived;

    private ClientWebSocket _ws;
    private CancellationTokenSource _cts = new CancellationTokenSource();

    private string _room;
    private string _callerName;

    public void Configure(string room, string callerName)
    {
        _room = room;
        _callerName = callerName;
    }

    public async Task ConnectWebSocket(string url, ConcurrentQueue<string> messageQueue)
    {
        try
        {
            if (_cts == null || _cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }

            _ws?.Dispose();
            _ws = new ClientWebSocket();
            _ws.Options.AddSubProtocol("json.webpubsub.azure.v1");

            Debug.Log($"SignalingClient: Connecting to {url}");
            await _ws.ConnectAsync(new Uri(url), _cts.Token);

            Debug.Log("SignalingClient: Connected to Web PubSub");

            SendJoinGroup();

            await Task.Delay(2000);

            SendCallRequest();

            Debug.Log($"SignalingClient: Call request sent to lobby for room={_room}");

            await ReceiveLoop(messageQueue);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SignalingClient: WebSocket connect failed: {ex}");
            throw;
        }
    }

    public async Task ReceiveLoop(ConcurrentQueue<string> messageQueue)
    {
        var buffer = new byte[8192];

        while (_ws != null && _ws.State == WebSocketState.Open)
        {
            var sb = new StringBuilder();
            WebSocketReceiveResult result;

            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.LogWarning("SignalingClient: WebSocket close frame received.");
                    return;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            var raw = sb.ToString();
            //Debug.Log($"SignalingClient: WS IN -> {raw}");

            if (!string.IsNullOrEmpty(raw))
            {
                messageQueue.Enqueue(raw);
            }
        }

        Debug.LogWarning($"SignalingClient: ReceiveLoop exited. socketState={_ws?.State}");
    }

    public void SendWs(object data)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        _ = _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
    }

    public void SendJoinGroup()
    {
        SendWs(new
        {
            type = "joinGroup",
            group = _room
        });
    }

    public void SendCallRequest()
    {
        SendWs(new
        {
            type = "sendToGroup",
            group = "lobby",
            dataType = "json",
            data = new { type = "call-request", room = _room, callerName = _callerName }
        });
    }

    public void SendIceCandidate(string room, object candidate)
    {
        SendWs(new
        {
            type = "sendToGroup",
            group = room,
            dataType = "json",
            data = new { type = "ice-candidate", room, candidate }
        });
    }

    public void SendCallEnded(string room)
    {
        if (_ws != null && _ws.State == WebSocketState.Open)
        {
            SendWs(new
            {
                type = "sendToGroup",
                group = "lobby",
                dataType = "json",
                data = new { type = "call-ended", room }
            });
        }
    }

    public void HandleMessage(string raw)
    {
        var msg = JObject.Parse(raw);

        var data = msg["data"];
        if (data == null) return;

        var type = (string)data["type"];
        if (string.IsNullOrEmpty(type)) return;

        var room = (string)data["room"];
        if (room != null && room != _room) return;

        switch (type)
        {
            case "call-accepted":
                Debug.Log("SignalingClient: Call accepted");
                OnCallAccepted?.Invoke();
                break;

            case "call-declined":
                Debug.Log("SignalingClient: Call declined");
                OnCallDeclined?.Invoke();
                break;

            case "answer":
                var sdp = (string)data["sdp"];
                OnAnswerReceived?.Invoke(sdp);
                break;

            case "ice-candidate":
                var candidateDict = data.ToObject<Dictionary<string, object>>();
                OnIceCandidateReceived?.Invoke(candidateDict);
                break;

            default:
                Debug.LogWarning($"SignalingClient: Unrecognized message type '{type}'");
                break;
        }
    }

void OnDestroy()
    {
        _cts?.Cancel();
        _ws?.Dispose();
    }
}