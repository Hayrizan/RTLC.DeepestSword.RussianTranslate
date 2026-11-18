using System;
using System.IO;
using UnityEngine;

namespace RTLC.DeepestSword.RussianTranslate
{
    internal static class AssetBundleContent
    {
        private static AssetBundle _bundle;

        public static void Open(string pluginDirectory)
        {
            var bundlePath = Path.Combine(pluginDirectory, "rtlc.bundle");
            if (!File.Exists(bundlePath))
            {
                throw new FileNotFoundException("The localization asset bundle was not found.", bundlePath);
            }

            _bundle = AssetBundle.LoadFromFile(bundlePath);
            if (_bundle == null)
            {
                throw new InvalidDataException($"Unity could not load the localization asset bundle: {bundlePath}");
            }

            Plugin.Log.LogInfo($"Loaded localization asset bundle from {bundlePath}");
        }

        public static T Load<T>(string assetName) where T : UnityEngine.Object
        {
            if (_bundle == null)
            {
                throw new InvalidOperationException("The localization asset bundle is not open.");
            }

            var asset = _bundle.LoadAsset<T>(assetName);
            if (asset == null)
            {
                throw new InvalidDataException(
                    $"Asset '{assetName}' ({typeof(T).Name}) is missing from rtlc.bundle.");
            }

            return asset;
        }

        public static void Close()
        {
            if (_bundle == null)
            {
                return;
            }

            _bundle.Unload(false);
            _bundle = null;
        }
    }
}
