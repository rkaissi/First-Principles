#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Generates a TextMesh Pro SDF asset from <b>Inter</b> (OFL — close to Apple’s system UI rhythm),
/// sets it as the project default, and assigns it to all TMP components in scenes &amp; prefabs.
/// Korean and other scripts still use <see cref="TmpGlobalFallbackBootstrap"/> (Nanum Gothic + Noto).
/// </summary>
public static class InterFontProjectSetup
{
    public const string TtfPath = "Assets/Fonts/Inter-VariableFont_opsz,wght.ttf";
    public const string SdfPath = "Assets/Fonts/Inter SDF.asset";
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    const string LiberationFallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("First Principles/Fonts/Apply Inter for all TextMesh Pro")]
    public static void GenerateAndApplyFromMenu()
    {
        if (!GenerateAndApplyAll())
            EditorUtility.DisplayDialog("Inter font", "Setup failed — see Console.", "OK");
        else
            EditorUtility.DisplayDialog(
                "Inter font",
                "Inter is now the default TMP font and applied across scenes & prefabs.\n\n" +
                "Korean uses Nanum Gothic when bundled under Resources/Fonts; other locales use Noto fallbacks at runtime.",
                "OK");
    }

    /// <summary>Unity Batchmode: <c>-executeMethod InterFontProjectSetup.GenerateAndApplyAllBatch</c>.</summary>
    public static void GenerateAndApplyAllBatch()
    {
        if (!GenerateAndApplyAll())
            EditorApplication.Exit(1);
        else
            EditorApplication.Exit(0);
    }

    static bool GenerateAndApplyAll()
    {
        AssetDatabase.Refresh();

        var inter = GetOrCreateInterSdfAsset();
        if (inter == null)
        {
            Debug.LogError("[Inter] Could not create TMP font asset. Is the TTF at " + TtfPath + "?");
            return false;
        }

        if (!AssignTmpSettingsDefault(inter))
            return false;

        RetargetAllTmpComponents(inter);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Inter] Font setup complete.");
        return true;
    }

    public static TMP_FontAsset GetOrCreateInterSdfAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
        if (existing != null)
            return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            Debug.LogError("[Inter] Missing font file: " + TtfPath);
            return null;
        }

        var asset = TMP_FontAsset.CreateFontAsset(
            font,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);

        if (asset == null)
        {
            Debug.LogError("[Inter] TMP_FontAsset.CreateFontAsset returned null.");
            return null;
        }

        asset.name = "Inter SDF";
        AssetDatabase.CreateAsset(asset, SdfPath);

        var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFallbackPath);
        if (liberation != null)
        {
            if (asset.fallbackFontAssetTable == null)
                asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!asset.fallbackFontAssetTable.Contains(liberation))
                asset.fallbackFontAssetTable.Add(liberation);
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        return asset;
    }

    static bool AssignTmpSettingsDefault(TMP_FontAsset inter)
    {
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
        {
            Debug.LogError("[Inter] TMP Settings not found at " + TmpSettingsPath);
            return false;
        }

        var so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = inter;

        var fallback = so.FindProperty("m_fallbackFontAssets");
        if (fallback != null && fallback.isArray)
        {
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFallbackPath);
            if (liberation != null && liberation != inter)
            {
                bool hasLib = false;
                for (int i = 0; i < fallback.arraySize; i++)
                {
                    if (fallback.GetArrayElementAtIndex(i).objectReferenceValue == liberation)
                        hasLib = true;
                }

                if (!hasLib)
                {
                    int i = fallback.arraySize;
                    fallback.InsertArrayElementAtIndex(i);
                    fallback.GetArrayElementAtIndex(i).objectReferenceValue = liberation;
                }
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        return true;
    }

    static void RetargetAllTmpComponents(TMP_FontAsset inter)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool dirty = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    Undo.RecordObject(tmp, "Inter font");
                    tmp.font = inter;
                    EditorUtility.SetDirty(tmp);
                    dirty = true;
                }

                foreach (var tmp3d in root.GetComponentsInChildren<TextMeshPro>(true))
                {
                    Undo.RecordObject(tmp3d, "Inter font");
                    tmp3d.font = inter;
                    EditorUtility.SetDirty(tmp3d);
                    dirty = true;
                }
            }

            if (dirty)
                EditorSceneManager.MarkSceneDirty(scene);

            EditorSceneManager.SaveScene(scene);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || path.Contains("PackageCache"))
                continue;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    tmp.font = inter;
                    EditorUtility.SetDirty(tmp);
                }

                foreach (var tmp3d in root.GetComponentsInChildren<TextMeshPro>(true))
                {
                    tmp3d.font = inter;
                    EditorUtility.SetDirty(tmp3d);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
