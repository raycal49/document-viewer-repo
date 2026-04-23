using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Meta.XR;
using Unity.WebRTC;

public class VideoCompositor : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PassthroughManager passthroughManager;

    [Header("Render Textures")]
    [SerializeField] private RenderTexture LeftCameraRT;

    public event Action<VideoStreamTrack> OnTrackReady;

    private RenderTexture _webRtcRenderTexture;
    private Material _stereoBlendMaterial;
    private VideoStreamTrack _videoTrack;

    public bool IsReady { get; private set; } = false;
    public VideoStreamTrack Track => _videoTrack;

    public void StartCompositor()
    {
        if (passthroughManager.IsReady)
            StartCoroutine(SetupRenderPipeline());
        else
        {
            passthroughManager.OnPassthroughReady += OnPassthroughReady;
            passthroughManager.StartPassthrough();
        }
    }

    // -------------------------------------------------------
    // Call this on cancel — stops the compositor loop cleanly
    // -------------------------------------------------------
    public void StopCompositor()
    {
        IsReady = false;
        // The CompositorLoop checks IsReady each frame and will exit naturally.
        // Dispose the track so WebRTC stops encoding.
        _videoTrack?.Dispose();
        _videoTrack = null;
        Debug.Log("VideoCompositor: Stopped.");
    }

    private void OnPassthroughReady()
    {
        passthroughManager.OnPassthroughReady -= OnPassthroughReady;
        StartCoroutine(SetupRenderPipeline());
    }

    private IEnumerator SetupRenderPipeline()
    {
        var sourceRt = passthroughManager.GetLeftRenderTexture();
        if (sourceRt == null)
        {
            Debug.LogError("VideoCompositor: Could not get RenderTexture from PassthroughManager!");
            yield break;
        }

        _webRtcRenderTexture = new RenderTexture(sourceRt.width, sourceRt.height, 0)
        {
            useMipMap        = false,
            autoGenerateMips = false,
            graphicsFormat   = GraphicsFormat.B8G8R8A8_SRGB
        };
        _webRtcRenderTexture.Create();

        var shader = Shader.Find("Custom/StereoBlend");
        if (shader == null)
        {
            Debug.LogError("VideoCompositor: Could not find shader Custom/StereoBlend!");
            yield break;
        }
        _stereoBlendMaterial = new Material(shader);

        _videoTrack = new VideoStreamTrack(_webRtcRenderTexture);
        IsReady = true;

        OnTrackReady?.Invoke(_videoTrack);
        Debug.Log("VideoCompositor: Track ready, starting compositor loop");

        StartCoroutine(CompositorLoop());
    }

    private IEnumerator CompositorLoop()
    {
        while (IsReady)   // exits automatically when StopCompositor() sets IsReady = false
        {
            yield return new WaitForEndOfFrame();

            if (_webRtcRenderTexture == null) continue;
            if (!passthroughManager.IsPlaying) continue;

            var leftSrc = passthroughManager.GetLeftTexture();
            if (leftSrc == null) continue;

            _stereoBlendMaterial.SetTexture("_LeftTex",    leftSrc);
            _stereoBlendMaterial.SetTexture("_LeftAssets", LeftCameraRT);

            Graphics.Blit(null, _webRtcRenderTexture, _stereoBlendMaterial);
        }
    }

    private void OnDestroy()
    {
        StopCompositor();
        if (_webRtcRenderTexture != null) Destroy(_webRtcRenderTexture);
        if (LeftCameraRT != null)         Destroy(LeftCameraRT);
    }
}