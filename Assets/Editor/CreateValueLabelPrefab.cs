using UnityEngine;
using UnityEditor;
using TMPro;

public static class CreateValueLabelPrefab
{
    [MenuItem("Tools/Conservation/Create Value Label Prefab")]
    public static void Create()
    {
        var go = new GameObject("ValueLabel");

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = 3f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "0";
        tmp.sortingOrder = 10;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1f, 0.5f);
        rect.localPosition = Vector3.zero;

        string path = "Assets/Prefabs/ValueLabel.prefab";
        bool success;
        PrefabUtility.SaveAsPrefabAsset(go, path, out success);
        Object.DestroyImmediate(go);

        if (success)
            Debug.Log($"[CreateValueLabelPrefab] Saved to {path}");
        else
            Debug.LogError("[CreateValueLabelPrefab] Failed to save prefab.");

        AssetDatabase.Refresh();
    }
}
