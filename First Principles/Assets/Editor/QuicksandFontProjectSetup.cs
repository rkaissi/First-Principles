#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Generates a TextMesh Pro SDF asset from <b>Quicksand</b> (Google Fonts, SIL OFL 1.1),
/// sets it as the project default, and assigns it to all TMP components in scenes &amp; prefabs.
/// CJK / Arabic / etc. still use <see cref="TmpGlobalFallbackBootstrap"/> (Nanum + Noto).
/// </summary>
public static class QuicksandFontProjectSetup
{
    public const string TtfPath = "Assets/Fonts/Quicksand-VariableFont_wght.ttf";
    public const string SdfPath = "Assets/Fonts/Quicksand SDF.asset";
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    const string LiberationFallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("First Principles/Fonts/Apply Quicksand for all TextMesh Pro (recommended UI)")]
    public static void GenerateAndApplyFromMenu()
    {
        if (!GenerateAndApplyAll())
            EditorUtility.DisplayDialog("Quicksand font", "Setup failed — see Console.", "OK");
        else
            EditorUtility.DisplayDialog(
                "Quicksand font",
                "Quicksand is now the default TMP font and applied across scenes & prefabs.\n\n" +
                "Other scripts use Nanum + Noto fallbacks at runtime.",
                "OK");
    }

    /// <summary>Unity Batchmode: <c>-executeMethod QuicksandFontProjectSetup.GenerateAndApplyAllBatch</c>.</summary>
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

        var asset = GetOrCreateQuicksandSdfAsset();
        if (asset == null)
        {
            Debug.LogError("[Quicksand] Could not create TMP font asset. Is the TTF at " + TtfPath + "?");
            return false;
        }

        if (!AssignTmpSettingsDefault(asset))
            return false;

        RetargetAllTmpComponents(asset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Quicksand] Font setup complete.");
        return true;
    }

    public static TMP_FontAsset GetOrCreateQuicksandSdfAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
        if (existing != null)
            return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            Debug.LogError("[Quicksand] Missing font file: " + TtfPath);
            return null;
        }

        var created = TMP_FontAsset.CreateFontAsset(
            font,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);

        if (created == null)
        {
            Debug.LogError("[Quicksand] TMP_FontAsset.CreateFontAsset returned null.");
            return null;
        }

        created.name = "Quicksand SDF";
        AssetDatabase.CreateAsset(created, SdfPath);

        var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFallbackPath);
        if (liberation != null)
        {
            if (created.fallbackFontAssetTable == null)
                created.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!created.fallbackFontAssetTable.Contains(liberation))
                created.fallbackFontAssetTable.Add(liberation);
            EditorUtility.SetDirty(created);
        }

        AssetDatabase.SaveAssets();
        return created;
    }

    static bool AssignTmpSettingsDefault(TMP_FontAsset quicksand)
    {
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
        {
            Debug.LogError("[Quicksand] TMP Settings not found at " + TmpSettingsPath);
            return false;
        }

        var so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = quicksand;

        var fallback = so.FindProperty("m_fallbackFontAssets");
        if (fallback != null && fallback.isArray)
        {
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFallbackPath);
            if (liberation != null && liberation != quicksand)
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

    static void RetargetAllTmpComponents(TMP_FontAsset quicksand)
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
                    Undo.RecordObject(tmp, "Quicksand font");
                    tmp.font = quicksand;
                    EditorUtility.SetDirty(tmp);
                    dirty = true;
                }

                foreach (var tmp3d in root.GetComponentsInChildren<TextMeshPro>(true))
                {
                    Undo.RecordObject(tmp3d, "Quicksand font");
                    tmp3d.font = quicksand;
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
                    tmp.font = quicksand;
                    EditorUtility.SetDirty(tmp);
                }

                foreach (var tmp3d in root.GetComponentsInChildren<TextMeshPro>(true))
                {
                    tmp3d.font = quicksand;
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
