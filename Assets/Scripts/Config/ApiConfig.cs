using UnityEngine;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "Config/Api Config")]
public class ApiConfig : ScriptableObject
{
    [Tooltip("Base URL of the levels API, e.g. http://localhost:4000")]
    public string baseApiUrl = "http://localhost:4000";
}
