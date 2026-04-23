using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class TextureDownloader
{
    public static IEnumerator Download(string url, Action<Texture2D> onReady, Action<string> onError)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }

        onReady?.Invoke(DownloadHandlerTexture.GetContent(req));
    }
}
