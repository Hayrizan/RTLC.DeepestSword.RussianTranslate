using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace RTLC.DeepestSword.RussianTranslate
{
    internal static class Patches
    {
        private static readonly FieldInfo TextUiFormattingField =
            AccessTools.Field(typeof(TextUI), "_formatting");

        [HarmonyPatch(typeof(TextUI), nameof(TextUI.Init))]
        [HarmonyPrefix]
        private static void TextUiInitPrefix(TextUI __instance)
        {
            if (!Plugin.EnabledConfig.Value)
            {
                return;
            }

            FontPatcher.EnsureFontPatched();

            if (__instance != null && __instance._text != null && !string.IsNullOrEmpty(__instance._text.text))
            {
                __instance._text.text = TranslationData.Translate(__instance._text.text);
            }
        }

        [HarmonyPatch(typeof(TextUI), nameof(TextUI.Init))]
        [HarmonyPostfix]
        private static void TextUiInitPostfix(TextUI __instance)
        {
            if (!Plugin.EnabledConfig.Value || __instance == null || TextUiFormattingField == null)
            {
                return;
            }

            var fmt = TextUiFormattingField.GetValue(__instance) as string;
            if (!string.IsNullOrEmpty(fmt))
            {
                var translated = TranslationData.Translate(fmt);
                if (translated != fmt)
                {
                    TextUiFormattingField.SetValue(__instance, translated);
                }
            }
        }

        [HarmonyPatch(typeof(TextUI), nameof(TextUI.SetTextOverride))]
        [HarmonyPrefix]
        private static void TextUiSetTextOverridePrefix(ref string txt)
        {
            txt = TranslationData.Translate(txt);
        }

        [HarmonyPatch(typeof(DialogUI), nameof(DialogUI.ShowDialog))]
        [HarmonyPrefix]
        private static void DialogUiShowDialogPrefix(ref string dialog)
        {
            if (!Plugin.EnabledConfig.Value)
            {
                return;
            }

            FontPatcher.EnsureFontPatched();
            dialog = TranslationData.Translate(dialog);
        }

        [HarmonyPatch(typeof(Main), nameof(Main.GetBestTime))]
        [HarmonyPostfix]
        private static void MainGetBestTimePostfix(ref string __result)
        {
            __result = TranslationData.Translate(__result);
        }

        [HarmonyPatch(typeof(Main), nameof(Main.GetLapCounter))]
        [HarmonyPostfix]
        private static void MainGetLapCounterPostfix(ref string __result)
        {
            if (!Plugin.EnabledConfig.Value || string.IsNullOrEmpty(__result))
            {
                return;
            }

            __result = TranslationData.TranslateFragments(__result);
        }

        [HarmonyPatch(typeof(PauseScreenUI), nameof(PauseScreenUI.KeyCodeToString))]
        [HarmonyPostfix]
        private static void PauseScreenKeyCodeToStringPostfix(ref string __result)
        {
            __result = TranslationData.TranslateKeyName(__result);
        }

        [HarmonyPatch(typeof(PauseScreenUI), "Awake")]
        [HarmonyPostfix]
        private static void PauseScreenAwakePostfix(PauseScreenUI __instance)
        {
            if (!Plugin.EnabledConfig.Value || __instance == null)
            {
                return;
            }

            TMP_Text[] keyLabels =
            {
                __instance._moveLeft,
                __instance._moveRight,
                __instance._swordLeft,
                __instance._swordRight,
                __instance._checkpoint,
                __instance._fullReset
            };

            foreach (var label in keyLabels)
            {
                if (label == null)
                {
                    continue;
                }

                label.enableAutoSizing = true;
                label.fontSizeMin = 8f;
                label.fontSizeMax = 16f;
            }
        }

        [HarmonyPatch(typeof(TextDoober), nameof(TextDoober.Display))]
        [HarmonyPrefix]
        private static void TextDooberDisplayPrefix(ref string txt)
        {
            if (!Plugin.EnabledConfig.Value || string.IsNullOrEmpty(txt))
            {
                return;
            }

            txt = TranslationData.TranslateFragments(txt);
        }

        [HarmonyPatch(typeof(WinUI), "OnEnable")]
        [HarmonyPrefix]
        private static void WinUiOnEnablePrefix()
        {
            TexturePatcher.ApplyTextures();
            SceneLocalizer.LocalizeAll();
        }
    }
}
