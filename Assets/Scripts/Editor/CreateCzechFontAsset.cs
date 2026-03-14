using UnityEngine;
using UnityEditor;
using TMPro;

namespace VacuumVille.Editor
{
    public static class CreateCzechFontAsset
    {
        private const string CzechCharset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÁČĎÉĚÍŇÓŘŠŤÚŮÝŽáčďéěíňóřšťúůýž" +
            "←→↑↓★☆✓✗•…–—";

        // ── Reset (run this first if buttons show blank text) ───────────────────

        [MenuItem("VacuumVille/Reset TMP Font to Default (fix blank text)")]
        public static void ResetTmpFont()
        {
            TMP_Settings tmpSettings = Resources.Load<TMP_Settings>("TMP Settings");
            if (tmpSettings == null)
            {
                EditorUtility.DisplayDialog("VacuumVille", "TMP Settings asset not found.", "OK");
                return;
            }

            // Load the built-in LiberationSans SDF from TMP package
            TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Packages/com.unity.textmeshpro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            if (liberation == null)
            {
                // Try alternate path used in older TMP versions
                liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }

            if (liberation == null)
            {
                EditorUtility.DisplayDialog("VacuumVille",
                    "Could not find LiberationSans SDF.asset.\n" +
                    "Open Window > TextMeshPro > Import TMP Essential Resources first.",
                    "OK");
                return;
            }

            SerializedObject so = new SerializedObject(tmpSettings);
            SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = liberation;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmpSettings);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[VacuumVille] TMP default font reset to LiberationSans SDF.");
            EditorUtility.DisplayDialog("VacuumVille",
                "TMP default font reset to LiberationSans SDF.\n\nRestart Play mode — buttons should now show text.",
                "Done");
        }

        // ── Create Czech font (run after Reset if you want Czech characters) ───

        [MenuItem("VacuumVille/Setup Czech TMP Font")]
        public static void CreateFont()
        {
            Font arialFont = null;
            string[] arialPaths =
            {
                @"C:\Windows\Fonts\arial.ttf",
                @"C:\Windows\Fonts\Arial.ttf",
                "/Library/Fonts/Arial.ttf",
                "/usr/share/fonts/truetype/msttcorefonts/Arial.ttf"
            };

            foreach (var path in arialPaths)
            {
                if (!System.IO.File.Exists(path)) continue;

                arialFont = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (arialFont == null)
                {
                    string destDir = "Assets/Fonts";
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

            if (arialFont == null)
            {
                EditorUtility.DisplayDialog("VacuumVille",
                    "Arial font not found. Copy arial.ttf into Assets/Fonts/ then re-run.", "OK");
                return;
            }

            string savePath = "Assets/Fonts/Arial_Czech_SDF.asset";

            // Delete old broken asset if present
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(savePath) != null)
                AssetDatabase.DeleteAsset(savePath);

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(arialFont);
            if (fontAsset == null)
            {
                Debug.LogError("[VacuumVille] Failed to create TMP Font Asset.");
                return;
            }

            fontAsset.name = "Arial_Czech_SDF";
            fontAsset.TryAddCharacters(CzechCharset);

            // Save main asset
            AssetDatabase.CreateAsset(fontAsset, savePath);

            // Embed atlas texture(s) and material as sub-assets — this is the critical step
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    var tex = fontAsset.atlasTextures[i];
                    if (tex != null)
                    {
                        tex.name = i == 0 ? "Atlas" : $"Atlas {i}";
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = "Arial_Czech_SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);

            // Set as TMP default
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

            Debug.Log($"[VacuumVille] Czech TMP font created at {savePath}");
            EditorUtility.DisplayDialog("VacuumVille",
                "Czech font created and set as TMP default.\n\n" + savePath, "Done");
        }
    }
}
