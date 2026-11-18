using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

namespace RTLC.DeepestSword.RussianTranslate
{
    internal sealed class VerificationRunner : MonoBehaviour
    {
        private string _shotDir;

        private void Start()
        {
            _shotDir = Environment.GetEnvironmentVariable("RTLC_VERIFY_DIR");
            if (string.IsNullOrEmpty(_shotDir))
            {
                _shotDir = Path.Combine(PathsHelper.GetDefaultVerifyDir(), "verify_shots");
            }

            Directory.CreateDirectory(_shotDir);
            StartCoroutine(RunVerification());
        }

        private IEnumerator RunVerification()
        {
            Plugin.Log.LogInfo($"VERIFY_START shotDir={_shotDir}");

            // Wait for Main and level1 (StageManager) to finish loading and entering Title state
            float waitTime = 0f;
            while (Main.Instance == null || Main.Instance._state != Main.GameState.Title || StageManager.Instance == null)
            {
                if (waitTime > 20f)
                {
                    Plugin.Log.LogError("VERIFY_TIMEOUT waiting for Title state and StageManager.");
                    Application.Quit();
                    yield break;
                }
                waitTime += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.5f);
            SceneLocalizer.LocalizeAll();
            yield return new WaitForEndOfFrame();

            VerifyGlyphCoverage();
            VerifyTextures();

            // 1. Title screen
            yield return Capture("01_title.png");

            // 2. Gameplay HUD + Tutorial + Blacksmith dialog
            Main.Instance.StartGame();
            Main.Instance._showLapCounter = true;
            yield return new WaitForSecondsRealtime(1.3f);

            if (BlackSmith.Instance != null)
            {
                BlackSmith.Instance.ShowDialog();
            }
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Capture("02_gameplay_hud_dialog.png");

            // 3. Pause Menu Main
            PauseButtonUI.Instance.Toggle();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("03_pause_main.png");

            // 4-7. Pause Submenus: 0=Controls, 1=Camera & Audio, 2=Speedrun, 3=Credits
            var pauseScreen = FindObjectOfType<PauseScreenUI>(includeInactive: true);
            if (pauseScreen != null)
            {
                string[] submenuShots =
                {
                    "04_controls.png",
                    "05_audio.png",
                    "06_speedrun.png",
                    "07_credits.png"
                };

                for (int i = 0; i < submenuShots.Length; i++)
                {
                    pauseScreen.ShowSubmenu(i);
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return Capture(submenuShots[i]);
                }
            }
            else
            {
                Plugin.Log.LogError("VERIFY_ERROR PauseScreenUI not found.");
            }

            // 8-9. Ending screen (congrats fade + final illustration with Cat speech bubble)
            float savedBest = PlayerPrefs.GetFloat(Main.BEST_TIME_VALUE_KEY, -1f);
            PauseButtonUI.Instance.Toggle();
            yield return new WaitForSecondsRealtime(0.3f);
            Main.Instance.EndGame();
            yield return new WaitForSecondsRealtime(2.3f);
            yield return Capture("08_ending_congrats.png");
            yield return new WaitForSecondsRealtime(7.5f);
            VerifyTextures();
            yield return Capture("09_ending_final.png");

            if (savedBest < 0f)
            {
                PlayerPrefs.DeleteKey(Main.BEST_TIME_VALUE_KEY);
            }
            else
            {
                PlayerPrefs.SetFloat(Main.BEST_TIME_VALUE_KEY, savedBest);
            }
            PlayerPrefs.Save();

            Plugin.Log.LogInfo("VERIFY_COMPLETE_SUCCESS");
            yield return new WaitForSecondsRealtime(0.3f);
            Application.Quit();
        }

        private IEnumerator Capture(string filename)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_shotDir, filename);
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.25f);
            Plugin.Log.LogInfo($"VERIFY_SHOT {path} exists={File.Exists(path)}");
        }

        private static void VerifyGlyphCoverage()
        {
            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Plugin.Log.LogError("VERIFY_GLYPHS_FAIL defaultFontAsset is null");
                return;
            }

            var missing = new HashSet<char>();
            void CheckString(string s)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return;
                }
                foreach (char ch in s)
                {
                    if (ch == '\n' || ch == '\r' || ch == '\t')
                    {
                        continue;
                    }
                    if (!font.HasCharacter(ch, searchFallbacks: false, tryAddCharacter: false))
                    {
                        missing.Add(ch);
                    }
                }
            }

            foreach (var val in TranslationData.ExactStrings.Values)
            {
                CheckString(val);
            }
            foreach (var val in TranslationData.KeyNames.Values)
            {
                CheckString(val);
            }
            CheckString("Круг: 1 — Всего: 0:00 зелий ОЗ ОМ");

            if (missing.Count == 0)
            {
                Plugin.Log.LogInfo($"VERIFY_GLYPHS_OK atlas={font.atlasWidth}x{font.atlasHeight} chars={font.characterTable.Count}");
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var c in missing)
                {
                    sb.Append($"{c}(U+{(int)c:X4}) ");
                }
                Plugin.Log.LogError($"VERIFY_GLYPHS_MISSING: {sb}");
            }
        }

        private static void VerifyTextures()
        {
            foreach (var img in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>())
            {
                if (img == null)
                {
                    continue;
                }

                if (img.gameObject.name == "Image" && img.transform.parent != null &&
                    img.transform.parent.name == "Panel-Title")
                {
                    Plugin.Log.LogInfo($"VERIFY_LOGO sprite={(img.sprite != null ? img.sprite.name : "null")}");
                }
                else if (img.gameObject.name == "Panel-Win")
                {
                    Plugin.Log.LogInfo($"VERIFY_ENDING sprite={(img.sprite != null ? img.sprite.name : "null")}");
                }
            }
        }

        private static class PathsHelper
        {
            public static string GetDefaultVerifyDir()
            {
                return Path.GetDirectoryName(typeof(VerificationRunner).Assembly.Location) ?? ".";
            }
        }
    }
}
