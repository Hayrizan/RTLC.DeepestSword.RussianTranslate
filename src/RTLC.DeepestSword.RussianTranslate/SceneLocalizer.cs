using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace RTLC.DeepestSword.RussianTranslate
{
    internal static class SceneLocalizer
    {
        private static readonly FieldInfo FormattingField =
            AccessTools.Field(typeof(TextUI), "_formatting");

        private static readonly FieldInfo InitializedField =
            AccessTools.Field(typeof(TextUI), "_initialized");

        public static void LocalizeAll()
        {
            if (!Plugin.EnabledConfig.Value)
            {
                return;
            }

            FontPatcher.EnsureFontPatched();
            TexturePatcher.ApplyTextures();

            LocalizeTextUIs();
            LocalizeAllTmpTexts();
            LocalizeDialogsAndTriggers();
        }

        private static void LocalizeTextUIs()
        {
            foreach (var ui in Resources.FindObjectsOfTypeAll<TextUI>())
            {
                if (ui == null)
                {
                    continue;
                }

                if (ui._text != null && !string.IsNullOrEmpty(ui._text.text))
                {
                    var translated = TranslationData.Translate(ui._text.text);
                    if (translated != ui._text.text)
                    {
                        ui._text.text = translated;
                    }
                }

                if (FormattingField != null)
                {
                    var fmt = FormattingField.GetValue(ui) as string;
                    if (!string.IsNullOrEmpty(fmt))
                    {
                        var translatedFmt = TranslationData.Translate(fmt);
                        if (translatedFmt != fmt)
                        {
                            FormattingField.SetValue(ui, translatedFmt);
                        }
                    }
                }
            }

            RefreshHudIfReady();
        }

        private static void RefreshHudIfReady()
        {
            var hud = Object.FindObjectOfType<GameHUD>();
            if (hud == null || PlayerController.Instance == null || PlayerController.Instance._playerActor == null)
            {
                return;
            }

            var player = PlayerController.Instance._playerActor;
            if (hud._hpText != null)
            {
                hud._hpText.SetText(player._hp, player._maxHP);
            }
            if (hud._mpText != null)
            {
                hud._mpText.SetText(Mathf.FloorToInt(player._mp), player._maxMp);
            }
            if (hud._potionsText != null)
            {
                hud._potionsText.SetText(PlayerController.Instance._potionsCount, PlayerController.Instance._maxPotions);
            }
            if (hud._baseAtkText != null)
            {
                int atk = player._baseAtk;
                if (player._weapon != null)
                {
                    atk += player._weapon._attackPower;
                }
                hud._baseAtkText.SetText(atk);
            }
            if (hud._moneyText != null)
            {
                hud._moneyText.SetText(PlayerController.Instance._money);
            }
        }

        private static void LocalizeAllTmpTexts()
        {
            foreach (var tmp in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (tmp == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(tmp.text))
                {
                    var translated = TranslationData.Translate(tmp.text);
                    if (translated != tmp.text)
                    {
                        tmp.text = translated;
                    }
                }

                ApplyLayoutAdjustments(tmp);
                tmp.SetAllDirty();
                tmp.ForceMeshUpdate(ignoreActiveState: true);
            }
        }

        private static void ApplyLayoutAdjustments(TMP_Text tmp)
        {
            var parent = tmp.transform.parent;
            if (parent == null)
            {
                return;
            }

            var rt = tmp.rectTransform;
            if (rt == null)
            {
                return;
            }

            if (tmp.gameObject.name == "Text (TMP)" && parent.name == "Button - Back")
            {
                if (rt.sizeDelta.x < 95f)
                {
                    rt.sizeDelta = new Vector2(95f, rt.sizeDelta.y);
                }
            }
            else if (parent.name == "Button - KoFi")
            {
                if (rt.sizeDelta.x < 210f)
                {
                    rt.sizeDelta = new Vector2(210f, rt.sizeDelta.y);
                }
            }
            else if (tmp is TextMeshPro && parent.name.StartsWith("TutorialText"))
            {
                if (rt.sizeDelta.x < 6.5f)
                {
                    rt.sizeDelta = new Vector2(7.0f, rt.sizeDelta.y);
                }
            }
        }

        private static void LocalizeDialogsAndTriggers()
        {
            foreach (var data in Resources.FindObjectsOfTypeAll<DialogData>())
            {
                if (data == null || data._dialog == null)
                {
                    continue;
                }
                for (int i = 0; i < data._dialog.Length; i++)
                {
                    data._dialog[i] = TranslationData.Translate(data._dialog[i]);
                }
            }

            foreach (var greeting in Resources.FindObjectsOfTypeAll<GreetingTrigger>())
            {
                if (greeting == null || greeting._greetingLines == null)
                {
                    continue;
                }
                for (int i = 0; i < greeting._greetingLines.Length; i++)
                {
                    greeting._greetingLines[i] = TranslationData.Translate(greeting._greetingLines[i]);
                }
            }

            foreach (var trigger in Resources.FindObjectsOfTypeAll<SwordTrigger>())
            {
                if (trigger == null)
                {
                    continue;
                }
                trigger._dialog = TranslationData.Translate(trigger._dialog);
                trigger._blackSmithDialog = TranslationData.Translate(trigger._blackSmithDialog);
            }
        }
    }
}
