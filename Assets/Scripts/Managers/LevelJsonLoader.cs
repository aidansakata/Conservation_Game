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

    // Resolves the directory that level JSON is fetched from, and is the only difference
    // between the API path and the bundled path. Both return a root that the callers below
    // suffix with "/<id>.json" or "/catalog.json", so the final URL shape is identical.
    //
    //   API      -> http://127.0.0.1:4000/levels
    //   bundled  -> <streamingAssetsPath>/Levels
    //
    // The bundled folder is "Levels" with a capital L, matching Assets/StreamingAssets/Levels
    // on disk character for character. Windows does not care; the server hosting the build does.
    private static string ResolveLevelsRoot(string baseApiUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // A hosted build cannot reach the owner's localhost API, so this path is unconditional.
        // streamingAssetsPath is a URL here, not a filesystem path -- it must be fetched, not read.
        return Application.streamingAssetsPath + "/Levels";
#else
        var cfg = Resources.Load<ApiConfig>("ApiConfig");
        if (cfg != null && cfg.useBundledLevels)
        {
            // Editor/standalone: streamingAssetsPath IS a filesystem path, so give
            // UnityWebRequest an explicit file:// scheme rather than a bare drive letter.
            string p = Application.streamingAssetsPath + "/Levels";
            return p.Contains("://") ? p : "file://" + p;
        }
        // Preserves the original guard: an empty baseApiUrl still reports "Empty baseApiUrl".
        if (string.IsNullOrEmpty(baseApiUrl)) return null;
        return baseApiUrl.TrimEnd('/') + "/levels";
#endif
    }

    // Overload for Editor and/or Standalone: accepts a base API URL and level id.
    public static IEnumerator LoadLevelJsonById(string baseApiUrl, string levelId, System.Action<string> onSuccess, System.Action<string> onError)
    {
        if (string.IsNullOrEmpty(levelId)) { onError?.Invoke("Empty levelId"); yield break; }

        string root = ResolveLevelsRoot(baseApiUrl);
        if (string.IsNullOrEmpty(root)) { onError?.Invoke("Empty baseApiUrl"); yield break; }

        string url = root + "/" + levelId + ".json";

        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error + " (" + url + ")");
            else onSuccess?.Invoke(req.downloadHandler.text);
        }
    }

    // Overload mirroring LoadLevelJsonById(baseApiUrl, ...): fetches the catalog
    // from {root}/catalog.json. Used by CatalogService.
    public static IEnumerator LoadCatalog(string baseApiUrl, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string root = ResolveLevelsRoot(baseApiUrl);
        if (string.IsNullOrEmpty(root)) { onError?.Invoke("Empty baseApiUrl"); yield break; }

        string url = root + "/catalog.json";

        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) onError?.Invoke(req.error + " (" + url + ")");
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
