#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class ImageLoader
{
    static readonly Dictionary<string, Sprite> spriteByUrl = new();

    public static async void LoadSprite(string url, Action<Sprite> onLoaded)
    {
        if (string.IsNullOrEmpty(url))
        {
            onLoaded?.Invoke(null);
            return;
        }

        if (spriteByUrl.TryGetValue(url, out var cached))
        {
            onLoaded?.Invoke(cached);
            return;
        }

        try
        {
            using var req = UnityWebRequestTexture.GetTexture(url);
            req.SetRequestHeader("User-Agent", "Mozilla/5.0");
            req.timeout = 10;

            var op = req.SendWebRequest();

            while (!op.isDone)
                await Task.Delay(1);

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ImageLoader] Load failed: {req.error}");
                onLoaded?.Invoke(null);
                return;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            spriteByUrl[url] = sprite;
            onLoaded?.Invoke(sprite);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            onLoaded?.Invoke(null);
        }
    }
}
#endif
