using UnityEngine;
using UnityEditor;
using TMPro;

namespace VacuumVille.Editor
{
    /// <summary>
    /// One-time utility: creates a TMP Font Asset from Arial with Czech + Emoji
    /// character support and sets it as the TMP default font.
    ///
    /// Usage: VacuumVille menu → Setup Czech TMP Font
    /// </summary>
    public static class CreateCzechFontAsset
    {
        // All characters needed: Basic Latin + Czech diacritics + common UI symbols
        private const string CzechCharset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÁČĎÉĚÍŇÓŘŠŤÚŮÝŽáčďéěíňóřšťúůýž" +  // Czech
            "←→↑↓★☆✓✗•…–—" +                      // UI symbols
            "\u2190\u2192\u25B6\u2605\u2606";       // arrows / star

        [MenuItem("VacuumVille/Setup Czech TMP Font")]
        public static void CreateFont()
        {
            // Find Arial on this machine
            Font arialFont = null;

            string[] arialPaths =
            {
                @"C:\Windows\Fonts\arial.ttf",
                @"C:\Windows\Fonts\Arial.ttf",
                "/Library/Fonts/Arial.ttf",          // macOS
                "/usr/share/fonts/truetype/msttcorefonts/Arial.ttf"  // Linux
            };

            foreach (var path in arialPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    arialFont = AssetDatabase.LoadAssetAtPath<Font>(path);
                    if (arialFont == null)
                    {
                        // Not yet imported — copy into project
                        string destDir  = "Assets/Fonts";
                        string destPath = destDir + "/Arial.ttf";
                        if (!AssetDatabase.IsValidFolder(destDir))
                            AssetDatabase.CreateFolder("Assets", "Fonts");
                        if (!System.IO.File.Exists(Application.dataPath + "/Fonts/Arial.ttf"))
                            System.IO.File.Copy(path, Application.dataPath + "/Fonts/Arial.ttf");
                        AssetDatabase.Refresh();
                        arialFont = AssetDatabase.LoadAssetAtPath<Font>(destPath);
                    }
                    break;
                }
            }

            if (arialFont == null)
            {
                EditorUtility.DisplayDialog("VacuumVille",
                    "Arial font not found on this machine.\n" +
                    "Please copy arial.ttf into Assets/Fonts/ manually, then re-run this tool.",
                    "OK");
                return;
            }

            // Create TMP Font Asset (uses SDFAA 1024x1024 defaults)
            string savePath = "Assets/Fonts/Arial_Czech_SDF.asset";

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(arialFont);

            if (fontAsset == null)
            {
                Debug.LogError("[VacuumVille] Failed to create TMP Font Asset.");
                return;
            }

            // Pre-warm with Czech characters
            fontAsset.TryAddCharacters(CzechCharset);

            AssetDatabase.CreateAsset(fontAsset, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Set as TMP default font
            TMP_Settings tmpSettings = Resources.Load<TMP_Settings>("TMP Settings");
            if (tmpSettings != null)
            {
                SerializedObject so = new SerializedObject(tmpSettings);
                SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = fontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(tmpSettings);
                    AssetDatabase.SaveAssets();
                }
            }

            Debug.Log($"[VacuumVille] Czech TMP font created at {savePath} and set as default.");
            EditorUtility.DisplayDialog("VacuumVille",
                "Czech TMP font created at:\n" + savePath +
                "\n\nIt has been set as the TMP default font.",
                "Done");
        }
    }
}
