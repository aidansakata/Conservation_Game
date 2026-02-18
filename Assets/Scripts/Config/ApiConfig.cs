using UnityEngine;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "Config/Api Config")]
public class ApiConfig : ScriptableObject
{
    [Tooltip("Base URL of the levels API, e.g. http://127.0.0.1:4000")]
    public string baseApiUrl = "http://127.0.0.1:4000";
}