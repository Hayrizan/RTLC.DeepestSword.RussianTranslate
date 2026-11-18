using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTLC.DeepestSword.RussianTranslate
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "RTLC.DeepestSword.RussianTranslate";
        public const string PluginName = "Deepest Sword Russian Translate";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log { get; private set; }
        internal static ConfigEntry<bool> EnabledConfig { get; private set; }
        internal static ConfigEntry<bool> ReplaceTexturesConfig { get; private set; }
        internal static string PluginDirectory { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            PluginDirectory = Path.GetDirectoryName(Info.Location);

            EnabledConfig = Config.Bind(
                "General",
                "Enabled",
                true,
                "Включить русский перевод текста");

            ReplaceTexturesConfig = Config.Bind(
                "General",
                "ReplaceTextures",
                true,
                "Заменять текстуры (логотип игры) на русские");

            try
            {
                AssetBundleContent.Open(PluginDirectory);
                try
                {
                    TranslationData.Initialize(AssetBundleContent.Load<TextAsset>("translations"));
                    FontPatcher.Initialize(
                        AssetBundleContent.Load<TextAsset>("font_atlas"),
                        AssetBundleContent.Load<TextAsset>("glyphs"));
                    TexturePatcher.Initialize(
                        AssetBundleContent.Load<Texture2D>("Logo"),
                        AssetBundleContent.Load<Texture2D>("ending"));
                }
                finally
                {
                    AssetBundleContent.Close();
                }

                _harmony = Harmony.CreateAndPatchAll(typeof(Patches), PluginGuid);
                SceneManager.sceneLoaded += OnSceneLoaded;

                if (Environment.GetEnvironmentVariable("RTLC_VERIFY") == "1")
                {
                    gameObject.AddComponent<VerificationRunner>();
                }

                Log.LogInfo($"{PluginName} v{PluginVersion} initialized.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to initialize {PluginName}: {ex}");
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _harmony?.UnpatchSelf();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!EnabledConfig.Value)
            {
                return;
            }

            try
            {
                SceneLocalizer.LocalizeAll();
            }
            catch (Exception ex)
            {
                Log.LogError($"Error localizing scene '{scene.name}': {ex}");
            }
        }
    }
}
