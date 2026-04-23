using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles all HTTP communication with the signaling server.
/// Fetches the ICE config and negotiates a WebPubSub token,
/// then fires events so SignalingClient can open the WebSocket.
/// </summary>
// if i could, i would just make this thing a static function and use Newtonsoft.Json
public class SignalingHttpClient : MonoBehaviour
{
    private readonly string _serverUrl = "https://ar-signalingserver.azurewebsites.net";

    public IceConfigResponse IceConfig { get; set; }

    public string NegotiateUrl;

    public IEnumerator GetIceConfig()
    {
        using var req = UnityWebRequest.Get($"{_serverUrl}/ice-config");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            //Debug.LogError("ICE config failed: " + req.error);
            IceConfig = null;
            yield break;
        }

        IceConfig = JsonUtility.FromJson<IceConfigResponse>(req.downloadHandler.text);
    }

    public IEnumerator GetNegotiateUrl(string userId, string room)
    {
        using var negReq = UnityWebRequest.Get(
            $"{_serverUrl}/negotiate?userId={userId}&room={room}");
        yield return negReq.SendWebRequest();

        if (negReq.result != UnityWebRequest.Result.Success)
        {
            //Debug.LogError("Negotiate failed: " + negReq.error);
            yield break;
        }

        var negotiateResponse = JsonUtility.FromJson<NegotiateResponse>(negReq.downloadHandler.text);

        NegotiateUrl = negotiateResponse.url;
    }
}