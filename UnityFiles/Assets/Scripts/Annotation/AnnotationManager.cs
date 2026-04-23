using System;
using System.Collections.Concurrent;
using Unity.WebRTC;
using UnityEngine;
using Newtonsoft.Json;

public class AnnotationManager: MonoBehaviour
{
    public event Action<ParsedAnnotation> OnAnnotationReceived;
    public event Action OnClearAnnotations;

    private RTCDataChannel _dataChannel;

    public void HandleDataChannel(RTCDataChannel channel, ConcurrentQueue<string> annotationQueue)
    {
        if (_dataChannel != null)
            _dataChannel.OnMessage = null;

        _dataChannel = channel;
        _dataChannel.OnMessage = bytes =>
        {
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            annotationQueue.Enqueue(json);
        };
    }

    public void HandleMessage(string json)
    {
        //Debug.Log($"AnnotationManager.HandleMessage: {json}");
 
        AnnotationData data;
        try { data = JsonConvert.DeserializeObject<AnnotationData>(json); }
        catch (Exception e) { Debug.LogError($"AnnotationManager: JSON parse error — {e.Message}"); return; }
 
        if (data == null) return;

        if (string.Equals(data.Type, "clear-annotations", StringComparison.OrdinalIgnoreCase))
        {
            OnClearAnnotations?.Invoke();
            return;
        }
 
        if (data.Vector == null || data.Vector.Length == 0 || data.Color == null)
        {
            Debug.LogWarning("AnnotationManager: stroke message missing Vector or Color: ignored.");
            return;
        }
 
        var points = new AnnotationPoint[data.Vector.Length];
        for (int i = 0; i < data.Vector.Length; i++)
            points[i] = new AnnotationPoint { x = data.Vector[i][0], y = data.Vector[i][1] };
 
        var color = $"#{data.Color[0]:X2}{data.Color[1]:X2}{data.Color[2]:X2}";
 
        bool isFading = string.Equals(data.StrokeType, "fading", StringComparison.OrdinalIgnoreCase);
        float fadeSecs = isFading
            ? (data.FadeDuration > 0 ? data.FadeDuration / 1000f : 2f)
            : 0f;
 
        OnAnnotationReceived?.Invoke(new ParsedAnnotation
        {
            points             = points,
            color              = color,
            isFading           = isFading,
            FadeDurationSeconds = fadeSecs,
        });
    }

    public void ClearAllAnnotations()
    {
        OnClearAnnotations?.Invoke();
    }

    void OnDestroy()
    {
        if (_dataChannel != null)
        {
            _dataChannel.OnMessage = null;
            _dataChannel = null;
        }
    }
}
