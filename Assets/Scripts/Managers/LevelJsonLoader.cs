using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class LevelJsonLoader
{
    // Entry: returns a coroutine for WebGL/Android, or immediate string for file platforms.
    public static IEnumerator LoadLevelJson(int levelNumber, System.Action<string> onLoaded, System.Action<string> onError)
    {
        string fileName = $"level-{levelNumber}.json";

#if UNITY_WEBGL && !UNITY_EDITOR
    // Served statically by Next.js under /levels/
    string root = Application.absoluteURL;
    if (string.IsNullOrEmpty(root)) root = "/";
    if (!root.EndsWith("/")) root += "/";
    string url = root + "levels/" + fileName;

        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error);
            else onLoaded?.Invoke(req.downloadHandler.text);
        }
#else
        string path = Path.Combine(Application.streamingAssetsPath, "Levels", fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (var req = UnityWebRequest.Get(path))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error);
            else onLoaded?.Invoke(req.downloadHandler.text);
        }
#else
        try
        {
            string json = File.ReadAllText(path);
            onLoaded?.Invoke(json);
        }
        catch (System.Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
#endif
#endif
    }

    public static IEnumerator LoadLevelJsonById(string levelId, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string fileName = levelId + ".json";

#if UNITY_WEBGL && !UNITY_EDITOR
        string root = Application.absoluteURL;
        if (string.IsNullOrEmpty(root)) root = "/";
        if (!root.EndsWith("/")) root += "/";
        string url = root + "levels/" + fileName;

        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error);
            else onSuccess?.Invoke(req.downloadHandler.text);
        }
#else
        string path = Path.Combine(Application.streamingAssetsPath, "Levels", fileName);
        try
        {
            string json = File.ReadAllText(path);
            onSuccess?.Invoke(json);
        }
        catch (System.Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
#endif
    }

    // Overload for Editor and/or Standalone: accepts a base API URL and level id.
    public static IEnumerator LoadLevelJsonById(string baseApiUrl, string levelId, System.Action<string> onSuccess, System.Action<string> onError)
    {
        if (string.IsNullOrEmpty(baseApiUrl)) { onError?.Invoke("Empty baseApiUrl"); yield break; }
        if (string.IsNullOrEmpty(levelId)) { onError?.Invoke("Empty levelId"); yield break; }

        string url = baseApiUrl.TrimEnd('/') + "/levels/" + levelId + ".json";

        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error);
            else onSuccess?.Invoke(req.downloadHandler.text);
        }
    }

    public static IEnumerator LoadCatalog(System.Action<string> onSuccess, System.Action<string> onError)
    {
        string fileName = "catalog.json";
#if UNITY_WEBGL && !UNITY_EDITOR
        string root = Application.absoluteURL;
        if (string.IsNullOrEmpty(root)) root = "/";
        if (!root.EndsWith("/")) root += "/";
        string url = root + "levels/" + fileName;

        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error);
            else onSuccess?.Invoke(req.downloadHandler.text);
        }
#else
        string path = Path.Combine(Application.streamingAssetsPath, "Levels", fileName);
        try
        {
            string json = File.ReadAllText(path);
            onSuccess?.Invoke(json);
        }
        catch (System.Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
#endif
    }
}
