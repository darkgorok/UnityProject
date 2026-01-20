using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ResultScreenPrefabBuilder
{
    private const string BasePath = "Assets/Prefabs/UI";

    [MenuItem("Tools/AlphaTest/Create Result Screen Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(BasePath);

        CreateScreenPrefab(
            Path.Combine(BasePath, "WinScreen.prefab"),
            "WinScreen",
            "WIN");

        CreateScreenPrefab(
            Path.Combine(BasePath, "LoseScreen.prefab"),
            "LoseScreen",
            "LOSE");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateScreenPrefab(string path, string rootName, string label)
    {
        DeleteIfExists(path);

        var root = new GameObject(rootName);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);

        var textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(panel.transform, false);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        var text = textObj.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 64;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (rootName == "WinScreen")
            root.AddComponent<WinScreenView>();
        else
            root.AddComponent<LoseScreenView>();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path);
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    private static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
}
