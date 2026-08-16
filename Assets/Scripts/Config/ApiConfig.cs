using UnityEngine;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "Config/Api Config")]
public class ApiConfig : ScriptableObject
{
    [Tooltip("Base URL of the levels API, e.g. http://127.0.0.1:4000")]
    public string baseApiUrl = "http://127.0.0.1:4000";

    [Tooltip("Read levels from StreamingAssets instead of the API. Always on in a WebGL " +
             "build, which cannot reach a localhost API. Turn on in the Editor to test the " +
             "bundled path with the API stopped.")]
    public bool useBundledLevels = false;
}