using System;
using System.Collections;
using System.Collections.Concurrent;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class WebRTCSender : MonoBehaviour
{
    [Header("Imported Modules")]
    [SerializeField] private VideoCompositor videoCompositor;
    [SerializeField] private SignalingClient signalingClient;
    [SerializeField] private PeerConnectionManager peerConnectionManager;
    [SerializeField] private AnnotationManager annotationManager;
    [SerializeField] private DocumentManager documentManager;
    [SerializeField] private MicrophoneCapture microphoneCapture;

    [Header("Call Button UI")]
    [SerializeField] private Button callButton;
    [SerializeField] private TextMesh callButtonText;
    [SerializeField] private Color colorIdle   = Color.green;
    [SerializeField] private Color colorActive = Color.red;

    private string callerName = "Meta Quest User";
    private readonly ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<string> annotationQueue = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<string> documentQueue = new ConcurrentQueue<string>();
    private string userId;
    private string room;
    private bool _callActive = false;

    void Awake()
    {
        string suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        userId = $"quest-{suffix}";
        room   = $"quest-{suffix}";
    }

    void Start()
    {
        StartCoroutine(WebRTC.Update());

        videoCompositor.OnTrackReady += track => StartCoroutine(AddTrackWhenReady(track));
        microphoneCapture.OnTrackReady += track => StartCoroutine(AddAudioTrackWhenReady(track));

        signalingClient.OnIceConfigReady       += peerConnectionManager.SetupPeerConnection;
        signalingClient.OnCallAccepted         += peerConnectionManager.SendOffer;
        signalingClient.OnAnswerReceived       += peerConnectionManager.SetRemoteAnswer;
        signalingClient.OnIceCandidateReceived += peerConnectionManager.HandleIceCandidate;

        peerConnectionManager.OnLocalIceCandidate += candidate =>
            signalingClient.SendIceCandidate(room, candidate);

        peerConnectionManager.OnOfferReady += sdp => signalingClient.SendWs(new
        {
            type = "sendToGroup",
            group = room,
            dataType = "json",
            data = new { type = "offer", room, sdp }
        });

        //annotationManager.OnAnnotationReceived += data =>
        //    Debug.Log($"WebRTCSender: Annotation received - {data.points.Length} points, color={data.color}");

        peerConnectionManager.OnConnectionLost += () =>
        {
            //Debug.Log("WebRTCSender: OnConnectionLost fired");
            signalingClient.SendCallEnded(room);
            peerConnectionManager.Disconnect();
            videoCompositor.StopCompositor();
            signalingClient.httpClient.IceConfig = null;
            annotationManager.ClearAllAnnotations();
            microphoneCapture.StopCapture();
            SetButtonIdle();
            _callActive = false;
        };

        SetButtonIdle();
    }

    public void OnCallButtonPressed()
    {
        if (_callActive)
            CancelCall();
        else
            StartCoroutine(RequestPermissionThenInit());
    }

    private void CancelCall()
    {
        signalingClient.SendCallEnded(room);
        peerConnectionManager.Disconnect();
        videoCompositor.StopCompositor();
        microphoneCapture.StopCapture();
        signalingClient.httpClient.IceConfig = null;
        annotationManager.ClearAllAnnotations();
        SetButtonIdle();
        _callActive = false;
        Debug.Log("WebRTCSender: Call cancelled.");
    }

    private void SetButtonIdle()
    {
        if (callButton != null) callButton.image.color = colorIdle;
        if (callButtonText != null) callButtonText.text = "Call";
    }

    private void SetButtonActive()
    {
        if (callButton != null) callButton.image.color = colorActive;
        if (callButtonText != null) callButtonText.text = "End";
    }

    IEnumerator AddTrackWhenReady(VideoStreamTrack track)
    {
        float timeout = 10f, elapsed = 0f;
        while (!peerConnectionManager.IsReady && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (!peerConnectionManager.IsReady)
        {
            Debug.LogError("WebRTCSender: PeerConnectionManager never became ready.");
            yield break;
        }

        peerConnectionManager.AddVideoTrack(track);
    }

    IEnumerator AddAudioTrackWhenReady(AudioStreamTrack track)
    {
        float timeout = 10f, elapsed = 0f;
        while (!peerConnectionManager.IsReady && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (!peerConnectionManager.IsReady)
        {
            Debug.LogError("WebRTCSender: PeerConnectionManager never became ready.");
            yield break;
        }

        peerConnectionManager.AddAudioTrack(track);
    }

    IEnumerator RequestPermissionThenInit()
    {
        string perm = "horizonos.permission.HEADSET_CAMERA";
        string micPerm = UnityEngine.Android.Permission.Microphone;

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(micPerm))
        {
            bool decided = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { decided = true; };
            callbacks.PermissionDenied  += _ => { decided = true; Debug.LogError("WebRTC: Mic permission denied"); };
            UnityEngine.Android.Permission.RequestUserPermission(micPerm, callbacks);
            yield return new WaitUntil(() => decided);
        }

        if (!Permission.HasUserAuthorizedPermission(perm))
        {
            bool decided = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { decided = true; };
            callbacks.PermissionDenied  += _ => { decided = true; Debug.LogError("WebRTC: Camera permission denied"); };
            Permission.RequestUserPermission(perm, callbacks);
            yield return new WaitUntil(() => decided);
        }

        if (!Permission.HasUserAuthorizedPermission(perm))
        {
            Debug.LogError("WebRTC: Cannot proceed without camera permission.");
            yield break;
        }

        string scenePerm = "com.oculus.permission.USE_SCENE";

        if (!Permission.HasUserAuthorizedPermission(scenePerm))
        {
            bool decided = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { decided = true; };
            callbacks.PermissionDenied += _ => { decided = true; Debug.LogWarning("WebRTC: Scene permission denied: depth will use fallback"); };
            Permission.RequestUserPermission(scenePerm, callbacks);
            yield return new WaitUntil(() => decided);
        }

        videoCompositor.StartCompositor();
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        signalingClient.Configure(room, callerName);

        yield return StartCoroutine(signalingClient.httpClient.GetIceConfig());

        if (signalingClient.httpClient.IceConfig == null)
        {
            Debug.LogError("WebRTCSender: Failed to get ICE config.");
            yield break;
        }

        peerConnectionManager.SetupPeerConnection(signalingClient.httpClient.IceConfig);

        yield return StartCoroutine(microphoneCapture.StartCapture());

        if (peerConnectionManager.AnnotationChannel != null)
            annotationManager.HandleDataChannel(peerConnectionManager.AnnotationChannel, annotationQueue);

        if (peerConnectionManager.DocumentsChannel == null)
            Debug.LogError("WebRTCSender: Documents data channel was not created.");
        else
        {
            Debug.Log("WebRTCSender: Documents data channel is ready.");
            if (documentManager != null)
                documentManager.HandleDataChannel(peerConnectionManager.DocumentsChannel, documentQueue);
            else
                Debug.LogWarning("WebRTCSender: DocumentManager is not assigned.");
        }

        // no need for this. keeping here just in case.
        //microphoneCapture.StartCapture();
        //yield return new WaitForSeconds(0.2f);
        //    if (microphoneCapture.IsReady)
        //        peerConnectionManager.AddAudioTrack(microphoneCapture.Track);
        //    else
        //        Debug.LogWarning("WebRTCSender: Mic not ready, sending without audio.");

        yield return StartCoroutine(signalingClient.httpClient.GetNegotiateUrl(userId, room));

        if (string.IsNullOrEmpty(signalingClient.httpClient.NegotiateUrl))
        {
            Debug.LogError("WebRTCSender: Failed to get negotiate URL.");
            yield break;
        }

        _ = signalingClient.ConnectWebSocket(signalingClient.httpClient.NegotiateUrl, messageQueue);

        _callActive = true;
        SetButtonActive();

        StartCoroutine(ProcessMessageQueue());
    }

    IEnumerator ProcessMessageQueue()
    {
        while (_callActive)
        {
            while (messageQueue.TryDequeue(out string raw))
                signalingClient.HandleMessage(raw);
            while (annotationQueue.TryDequeue(out string json))
                annotationManager.HandleMessage(json);
            while (documentQueue.TryDequeue(out string documentJson))
                documentManager?.HandleMessage(documentJson);

            yield return null;
        }
    }

    void OnDestroy()
    {
        if (_callActive)
            signalingClient.SendCallEnded(room);
    }
}