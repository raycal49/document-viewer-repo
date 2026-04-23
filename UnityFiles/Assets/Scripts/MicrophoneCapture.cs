using System;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;

public class MicrophoneCapture : MonoBehaviour
{
    [SerializeField] private AudioSource micSource;

    public event Action<AudioStreamTrack> OnTrackReady;

    public AudioStreamTrack micTrack;
    public bool IsReady { get; private set; }
    public string deviceName;

    public IEnumerator StartCapture(string preferredDevice = null, int sampleRate = 48000)
    {
        deviceName = preferredDevice; // null/empty = default mic

        if (micSource == null)
            micSource = gameObject.AddComponent<AudioSource>();

        micSource.playOnAwake = false;
        micSource.loop = true;
        micSource.spatialBlend = 0f;

        micSource.clip = Microphone.Start(deviceName, true, 1, sampleRate);

        while (Microphone.GetPosition(deviceName) <= 0)
            yield return null;

        micSource.Play();

        micTrack = new AudioStreamTrack(micSource);

        micTrack.Loopback = false;

        IsReady = true;

        OnTrackReady?.Invoke(micTrack);
    }

    public void StopCapture()
    {
        micTrack?.Dispose();
        micTrack = null;

        if (micSource != null)
            micSource.Stop();

        if (Microphone.IsRecording(deviceName))
            Microphone.End(deviceName);

        IsReady = false;
    }
}