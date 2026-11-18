using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace RTLC.DeepestSword.RussianTranslate
{
    internal static class FontPatcher
    {
        private readonly struct GlyphDef
        {
            public readonly uint Unicode;
            public readonly int X;
            public readonly int Y;
            public readonly int W;
            public readonly int H;
            public readonly float MetricsW;
            public readonly float MetricsH;
            public readonly float BearingX;
            public readonly float BearingY;
            public readonly float Advance;

            public GlyphDef(
                uint unicode, int x, int y, int w, int h,
                float mw, float mh, float bx, float by, float adv)
            {
                Unicode = unicode;
                X = x;
                Y = y;
                W = w;
                H = h;
                MetricsW = mw;
                MetricsH = mh;
                BearingX = bx;
                BearingY = by;
                Advance = adv;
            }
        }

        private static readonly FieldInfo AtlasTextureField =
            AccessTools.Field(typeof(TMP_FontAsset), "m_AtlasTexture");

        private static readonly FieldInfo AtlasWidthField =
            AccessTools.Field(typeof(TMP_FontAsset), "m_AtlasWidth");

        private static readonly FieldInfo AtlasHeightField =
            AccessTools.Field(typeof(TMP_FontAsset), "m_AtlasHeight");

        private static Texture2D _atlasTex;
        private static List<GlyphDef> _glyphDefs;

        public static void Initialize(TextAsset atlasAsset, TextAsset glyphsAsset)
        {
            if (_atlasTex != null && _glyphDefs != null)
            {
                return;
            }

            _glyphDefs = LoadGlyphDefs(glyphsAsset);
            _atlasTex = LoadAtlasTexture(atlasAsset);
        }

        public static void EnsureFontPatched()
        {
            if (_atlasTex == null || _glyphDefs == null)
            {
                throw new InvalidOperationException("Font assets were not initialized from rtlc.bundle.");
            }

            var patchedAny = false;
            var targets = new HashSet<TMP_FontAsset>();

            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null && defaultFont.name == "pixelFJ8pt1__")
            {
                targets.Add(defaultFont);
            }

            foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (font != null && font.name == "pixelFJ8pt1__")
                {
                    targets.Add(font);
                }
            }

            foreach (var font in targets)
            {
                if (PatchFontAsset(font))
                {
                    patchedAny = true;
                }
            }

            UpdatePixelMaterials();

            if (patchedAny)
            {
                Plugin.Log.LogInfo("Patched pixelFJ8pt1__ TMP_FontAsset with 1024x1024 Cyrillic atlas.");
            }
        }

        private static bool PatchFontAsset(TMP_FontAsset font)
        {
            bool needsAtlasResize = font.atlasWidth != 1024 || font.atlasHeight != 1024 ||
                                    font.atlasTextures == null || font.atlasTextures.Length == 0 ||
                                    font.atlasTextures[0] != _atlasTex;

            bool needsGlyphs = font.characterLookupTable == null || !font.characterLookupTable.ContainsKey('А');

            if (!needsAtlasResize && !needsGlyphs)
            {
                return false;
            }

            font.atlasTextures = new[] { _atlasTex };
            AtlasTextureField?.SetValue(font, _atlasTex);
            AtlasWidthField?.SetValue(font, 1024);
            AtlasHeightField?.SetValue(font, 1024);

            if (needsGlyphs)
            {
                uint maxGlyphIndex = 0;
                foreach (var g in font.glyphTable)
                {
                    if (g != null && g.index > maxGlyphIndex)
                    {
                        maxGlyphIndex = g.index;
                    }
                }

                var existingUnicodes = new HashSet<uint>();
                foreach (var c in font.characterTable)
                {
                    if (c != null)
                    {
                        existingUnicodes.Add(c.unicode);
                    }
                }

                uint nextIndex = maxGlyphIndex + 1;
                foreach (var def in _glyphDefs)
                {
                    if (existingUnicodes.Contains(def.Unicode))
                    {
                        continue;
                    }

                    var metrics = new GlyphMetrics(
                        def.MetricsW, def.MetricsH, def.BearingX, def.BearingY, def.Advance);
                    var rect = new GlyphRect(def.X, def.Y, def.W, def.H);
                    var glyph = new Glyph(nextIndex++, metrics, rect, 1f, 0);
                    var ch = new TMP_Character(def.Unicode, font, glyph);

                    font.glyphTable.Add(glyph);
                    font.characterTable.Add(ch);
                }
            }

            font.ReadFontAssetDefinition();

            if (font.material != null)
            {
                ApplyAtlasToMaterial(font.material);
            }

            return true;
        }

        private static void UpdatePixelMaterials()
        {
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat != null && mat.name != null && mat.name.Contains("pixelFJ8pt1__"))
                {
                    ApplyAtlasToMaterial(mat);
                }
            }
        }

        private static void ApplyAtlasToMaterial(Material mat)
        {
            if (mat.GetTexture(ShaderUtilities.ID_MainTex) != _atlasTex)
            {
                mat.SetTexture(ShaderUtilities.ID_MainTex, _atlasTex);
            }
            if (mat.HasProperty(ShaderUtilities.ID_TextureWidth))
            {
                mat.SetFloat(ShaderUtilities.ID_TextureWidth, 1024f);
            }
            if (mat.HasProperty(ShaderUtilities.ID_TextureHeight))
            {
                mat.SetFloat(ShaderUtilities.ID_TextureHeight, 1024f);
            }
        }

        private static Texture2D LoadAtlasTexture(TextAsset atlasAsset)
        {
            if (atlasAsset == null)
            {
                throw new ArgumentNullException(nameof(atlasAsset));
            }

            byte[] rawBytes;
            using (var stream = new MemoryStream(atlasAsset.bytes, false))
            {
                using var gz = new GZipStream(stream, CompressionMode.Decompress);
                using var ms = new MemoryStream(1024 * 1024);
                gz.CopyTo(ms);
                rawBytes = ms.ToArray();
            }

            if (rawBytes.Length != 1024 * 1024)
            {
                throw new InvalidDataException($"Unexpected font atlas size: {rawBytes.Length} bytes");
            }

            var tex = new Texture2D(1024, 1024, TextureFormat.Alpha8, false)
            {
                name = "pixelFJ8pt1__ Atlas (RU 1024)",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.LoadRawTextureData(rawBytes);
            tex.Apply(false, false);
            UnityEngine.Object.DontDestroyOnLoad(tex);
            return tex;
        }

        private static List<GlyphDef> LoadGlyphDefs(TextAsset glyphsAsset)
        {
            if (glyphsAsset == null)
            {
                throw new ArgumentNullException(nameof(glyphsAsset));
            }

            var list = new List<GlyphDef>(80);
            using var reader = new StringReader(glyphsAsset.text);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split('\t');
                if (parts.Length < 10)
                {
                    continue;
                }

                list.Add(new GlyphDef(
                    uint.Parse(parts[0], CultureInfo.InvariantCulture),
                    int.Parse(parts[1], CultureInfo.InvariantCulture),
                    int.Parse(parts[2], CultureInfo.InvariantCulture),
                    int.Parse(parts[3], CultureInfo.InvariantCulture),
                    int.Parse(parts[4], CultureInfo.InvariantCulture),
                    float.Parse(parts[5], CultureInfo.InvariantCulture),
                    float.Parse(parts[6], CultureInfo.InvariantCulture),
                    float.Parse(parts[7], CultureInfo.InvariantCulture),
                    float.Parse(parts[8], CultureInfo.InvariantCulture),
                    float.Parse(parts[9], CultureInfo.InvariantCulture)));
            }

            return list;
        }
    }
}
