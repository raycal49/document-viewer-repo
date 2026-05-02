using System;
using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;

public class PeerConnectionManager : MonoBehaviour
{
    public event Action<object> OnLocalIceCandidate;
    public event Action<string> OnOfferReady;
    public event Action OnConnectionLost;

    private RTCPeerConnection _peerConnection;

    private VideoStreamTrack _videoTrack;
    private AudioStreamTrack _audioTrack;

    private AudioSource _remoteAudioSource;
    private AudioStreamTrack _remoteAudioTrack;

    private RTCDataChannel _annotationChannel;
    private RTCDataChannel _documentsChannel;

    public RTCDataChannel AnnotationChannel => _annotationChannel;
    public RTCDataChannel DocumentsChannel => _documentsChannel;

    public bool IsReady => _peerConnection != null;

    public void SetupPeerConnection(IceConfigResponse iceConfig)
    {
        if (iceConfig == null)
            throw new ArgumentNullException(nameof(iceConfig));

        if (iceConfig.iceServers == null || iceConfig.iceServers.Length == 0)
            throw new ArgumentException(nameof(iceConfig));

        var iceServers = new List<RTCIceServer>();
        foreach (var s in iceConfig.iceServers)
            iceServers.Add(new RTCIceServer
            {
                urls       = s.urls,
                username   = s.username,
                credential = s.credential
            });

        var config = new RTCConfiguration { iceServers = iceServers.ToArray() };
        _peerConnection = new RTCPeerConnection(ref config);

        if (_remoteAudioSource == null)
            _remoteAudioSource = gameObject.AddComponent<AudioSource>();

        _remoteAudioSource.playOnAwake = false;
        _remoteAudioSource.loop = true;
        _remoteAudioSource.spatialBlend = 0f;

        _annotationChannel = _peerConnection.CreateDataChannel("annotations");
        _documentsChannel = _peerConnection.CreateDataChannel("documents");
        HookUpDataChannelCallbacks(_annotationChannel, "annotations");
        HookUpDataChannelCallbacks(_documentsChannel, "documents");
        HookUpEvents();
        Debug.Log("PeerConnectionManager: Peer connection ready (channels: annotations, documents)");
    }

    public void Disconnect()
    {
        UnhookEvents();
        UnhookDataChannelCallbacks(_annotationChannel);
        _annotationChannel?.Close();
        _annotationChannel = null;

        UnhookDataChannelCallbacks(_documentsChannel);
        _documentsChannel?.Close();
        _documentsChannel = null;
        _videoTrack?.Dispose();
        _videoTrack = null;
        _peerConnection?.Close();
        _peerConnection = null;
        _audioTrack?.Dispose();
        _audioTrack = null;
        _remoteAudioSource?.Stop();
        _remoteAudioTrack?.Dispose();
        _remoteAudioTrack = null;

        Debug.Log("PeerConnectionManager: Disconnected.");
    }


    private static void HookUpDataChannelCallbacks(RTCDataChannel channel, string channelName)
    {
        if (channel == null) return;

        channel.OnOpen = () => Debug.Log($"PeerConnectionManager: Data channel '{channelName}' opened.");
        channel.OnClose = () => Debug.Log($"PeerConnectionManager: Data channel '{channelName}' closed.");
        channel.OnError = error => Debug.LogError($"PeerConnectionManager: Data channel '{channelName}' error: {error}");
    }

    private static void UnhookDataChannelCallbacks(RTCDataChannel channel)
    {
        if (channel == null) return;

        channel.OnOpen = null;
        channel.OnClose = null;
        channel.OnError = null;
    }

    private void HookUpEvents()
    {
        _peerConnection.OnIceCandidate = candidate =>
        {
            if (string.IsNullOrEmpty(candidate.Candidate)) return;
            OnLocalIceCandidate?.Invoke(new
            {
                candidate     = candidate.Candidate,
                sdpMid        = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0
            });
        };

        _peerConnection.OnIceConnectionChange  = state => Debug.Log($"WebRTC ICE: {state}");
        _peerConnection.OnConnectionStateChange = state =>
        {
            Debug.Log($"WebRTC Connection: {state}");
            if (state == RTCPeerConnectionState.Failed){
                OnConnectionLost?.Invoke();
            }
            else if (state == RTCPeerConnectionState.Disconnected)
            {
                StartCoroutine(WaitAndCheckConnection());
            }
        };

         _peerConnection.OnTrack = e =>
        {
            if (e.Track is AudioStreamTrack audioTrack)
            {
                Debug.Log("Received remote audio track");
                _remoteAudioTrack = audioTrack;
                _remoteAudioSource.SetTrack(_remoteAudioTrack);
                _remoteAudioSource.Play();
            }
        };
    }

    private IEnumerator WaitAndCheckConnection()
    {
        yield return new WaitForSeconds(5f);
        if (_peerConnection != null &&
            (_peerConnection.ConnectionState == RTCPeerConnectionState.Disconnected ||
            _peerConnection.ConnectionState == RTCPeerConnectionState.Failed))
        {
            Debug.LogWarning("PeerConnectionManager: Connection did not recover, firing OnConnectionLost");
            OnConnectionLost?.Invoke();
        }
    }

    private void UnhookEvents()
    {
        if (_peerConnection == null) return;
        _peerConnection.OnIceCandidate         = null;
        _peerConnection.OnIceConnectionChange  = null;
        _peerConnection.OnConnectionStateChange = null;
    }

    public void AddVideoTrack(VideoStreamTrack track)
    {
        if (_peerConnection == null)
        {
            Debug.LogError("PeerConnectionManager: Cannot add track, peer connection is null");
            return;
        }
        _videoTrack = track;
        _peerConnection.AddTrack(_videoTrack);
        Debug.Log("PeerConnectionManager: Video track added");
    }

    public void AddAudioTrack(AudioStreamTrack track)
    {
        if (_peerConnection == null)
        {
            Debug.LogError("PeerConnectionManager: Cannot add audio track, peer connection is null");
            return;
        }
        _audioTrack = track;
        _peerConnection.AddTrack(_audioTrack);
        Debug.Log("PeerConnectionManager: Audio track added");
    }

    public void SendOffer() => StartCoroutine(SendOfferCoroutine());

    private IEnumerator SendOfferCoroutine()
    {
        float timeout = 10f, elapsed = 0f;
        while ((_videoTrack == null || _videoTrack.ReadyState != TrackState.Live) && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (_videoTrack == null || _videoTrack.ReadyState != TrackState.Live)
        {
            Debug.LogError("PeerConnectionManager: Video track never became live");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        var op    = _peerConnection.CreateOffer();
        yield return op;
        var offer = op.Desc;
        var setLocal = _peerConnection.SetLocalDescription(ref offer);
        yield return setLocal;

        OnOfferReady?.Invoke(offer.sdp);
        Debug.Log("PeerConnectionManager: Offer sent");
    }

    public void SetRemoteAnswer(string sdp) => StartCoroutine(SetRemoteAnswerCoroutine(sdp));

    private IEnumerator SetRemoteAnswerCoroutine(string sdp)
    {
        var answer = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = sdp };
        var op     = _peerConnection.SetRemoteDescription(ref answer);
        yield return op;
        Debug.Log("PeerConnectionManager: Remote answer set, WebRTC connected");
    }

    public void HandleIceCandidate(Dictionary<string, object> data)
    {
        if (!IceCandidateParser.TryParse(data, out var init)) return;
        _peerConnection.AddIceCandidate(new RTCIceCandidate(init));
    }

    void OnDestroy()
    {
        Disconnect();
    }
}