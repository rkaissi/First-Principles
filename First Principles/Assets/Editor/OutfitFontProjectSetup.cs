#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Generates a TextMeshPro SDF asset from <b>Outfit</b> (Google Fonts / OFL), sets it as the project default,
/// and assigns it to all <see cref="TextMeshProUGUI"/> / <see cref="TextMeshPro"/> in scenes &amp; prefabs.
/// </summary>
public static class OutfitFontProjectSetup
{
    public const string TtfPath = "Assets/Fonts/Outfit-VariableFont_wght.ttf";
    public const string SdfPath = "Assets/Fonts/Outfit SDF.asset";
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    const string LiberationFallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("First Principles/Fonts/Apply Outfit for all TextMesh Pro")]
    public static void GenerateAndApplyFromMenu()
    {
        if (!GenerateAndApplyAll())
            EditorUtility.DisplayDialog("Outfit font", "Setup failed — see Console.", "OK");
        else
            EditorUtility.DisplayDialog("Outfit font", "Outfit is now the default TMP font and applied across scenes & prefabs.", "OK");
    }

    /// <summary>Unity Batchmode: <c>-executeMethod OutfitFontProjectSetup.GenerateAndApplyAllBatch</c> (close the editor if it has this project open).</summary>
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

        var outfit = GetOrCreateOutfitSdfAsset();
        if (outfit == null)
        {
            Debug.LogError("[Outfit] Could not create TMP font asset. Is the TTF at " + TtfPath + "?");
            return false;
        }

        if (!AssignTmpSettingsDefault(outfit))
            return false;

        RetargetAllTmpComponents(outfit);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Outfit] Font setup complete.");
        return true;
    }

    public static TMP_FontAsset GetOrCreateOutfitSdfAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
        if (existing != null)
            return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            Debug.LogError("[Outfit] Missing font file: " + TtfPath);
            return null;
        }

        // Dynamic atlas — fills glyphs as needed; includeFontData must be on the TTF import.
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
            Debug.LogError("[Outfit] TMP_FontAsset.CreateFontAsset returned null.");
            return null;
        }

        asset.name = "Outfit SDF";
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

    static bool AssignTmpSettingsDefault(TMP_FontAsset outfit)
    {
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
        {
            Debug.LogError("[Outfit] TMP Settings not found at " + TmpSettingsPath);
            return false;
        }

        var so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = outfit;

        var fallback = so.FindProperty("m_fallbackFontAssets");
        if (fallback != null && fallback.isArray)
        {
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFallbackPath);
            if (liberation != null && liberation != outfit)
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

    static void RetargetAllTmpComponents(TMP_FontAsset outfit)
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
                    Undo.RecordObject(tmp, "Outfit font");
                    tmp.font = outfit;
                    EditorUtility.SetDirty(tmp);
                    dirty = true;
                }

                foreach (var tmp3d in root.GetComponentsInChildren<TextMeshPro>(true))
                {
                    Undo.RecordObject(tmp3d, "Outfit font");
                    tmp3d.font = outfit;
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
                    tmp.font = outfit;
                    EditorUtility.SetDirty(tmp);
                }

                foreach (var tmp3d in root.GetComponentsInChildren<TextMeshPro>(true))
                {
                    tmp3d.font = outfit;
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
