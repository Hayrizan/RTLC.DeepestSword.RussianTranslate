using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RTLC.DeepestSword.RussianTranslate.Editor
{
    internal static class AssetBundleBuilder
    {
        private const string BundleName = "rtlc";
        private const string PluginFolder = "RTLC.DeepestSword.RussianTranslate";

        public static void BuildBundle()
        {
            try
            {
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                var repositoryRoot = Directory.GetParent(Directory.GetParent(projectRoot).FullName).FullName;
                var sourceAssets = Path.Combine(
                    repositoryRoot,
                    "src",
                    PluginFolder,
                    "Assets");
                var unityAssets = Path.Combine(Application.dataPath, "RTLC");

                CopyAsset(sourceAssets, unityAssets, "Logo.png");
                CopyAsset(sourceAssets, unityAssets, "ending.png");
                CopyAsset(sourceAssets, unityAssets, "translations.json");
                CopyAsset(sourceAssets, unityAssets, "cyrillic_glyphs.tsv", "cyrillic_glyphs.tsv.txt");
                CopyAsset(sourceAssets, unityAssets, "font_atlas_1024.alpha8.gz", "font_atlas_1024.alpha8.gz.bytes");

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureTexture("Assets/RTLC/Logo.png");
                ConfigureTexture("Assets/RTLC/ending.png");

                var build = new AssetBundleBuild
                {
                    assetBundleName = BundleName,
                    assetNames = new[]
                    {
                        "Assets/RTLC/Logo.png",
                        "Assets/RTLC/ending.png",
                        "Assets/RTLC/translations.json",
                        "Assets/RTLC/cyrillic_glyphs.tsv.txt",
                        "Assets/RTLC/font_atlas_1024.alpha8.gz.bytes"
                    },
                    addressableNames = new[]
                    {
                        "Logo",
                        "ending",
                        "translations",
                        "glyphs",
                        "font_atlas"
                    }
                };

                var intermediateOutput = Path.Combine(projectRoot, "AssetBundleBuild");
                Directory.CreateDirectory(intermediateOutput);
                var options = BuildAssetBundleOptions.ChunkBasedCompression |
                              BuildAssetBundleOptions.ForceRebuildAssetBundle;
                var manifest = BuildPipeline.BuildAssetBundles(
                    intermediateOutput,
                    new[] { build },
                    options,
                    BuildTarget.StandaloneWindows64);

                if (manifest == null)
                {
                    throw new InvalidOperationException("Unity did not produce an AssetBundle manifest.");
                }

                var builtBundle = Path.Combine(intermediateOutput, BundleName);
                if (!File.Exists(builtBundle))
                {
                    throw new FileNotFoundException("Unity did not produce the rtlc AssetBundle.", builtBundle);
                }

                var packageDirectory = Path.Combine(repositoryRoot, "dist", PluginFolder);
                Directory.CreateDirectory(packageDirectory);
                var packageBundle = Path.Combine(packageDirectory, "rtlc.bundle");
                File.Copy(builtBundle, packageBundle, true);

                Debug.Log($"Built {packageBundle} ({new FileInfo(packageBundle).Length} bytes).");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void CopyAsset(string sourceDirectory, string targetDirectory, string sourceName, string targetName = null)
        {
            var source = Path.Combine(sourceDirectory, sourceName);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("Asset source file was not found.", source);
            }

            Directory.CreateDirectory(targetDirectory);
            var target = Path.Combine(targetDirectory, targetName ?? sourceName);
            if (!File.Exists(target) || !FilesEqual(source, target))
            {
                File.Copy(source, target, true);
            }
        }

        private static bool FilesEqual(string left, string right)
        {
            var leftInfo = new FileInfo(left);
            var rightInfo = new FileInfo(right);
            if (leftInfo.Length != rightInfo.Length)
            {
                return false;
            }

            using (var leftStream = File.OpenRead(left))
            using (var rightStream = File.OpenRead(right))
            {
                int leftByte;
                while ((leftByte = leftStream.ReadByte()) >= 0)
                {
                    if (leftByte != rightStream.ReadByte())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void ConfigureTexture(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Unity did not import '{assetPath}' as a texture.");
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }
}
