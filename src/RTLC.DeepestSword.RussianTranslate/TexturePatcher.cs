using UnityEngine;
using UnityEngine.UI;

namespace RTLC.DeepestSword.RussianTranslate
{
    internal static class TexturePatcher
    {
        private static Sprite _logoSpriteRu;
        private static Sprite _endingSpriteRu;

        public static void Initialize(Texture2D logoTexture, Texture2D endingTexture)
        {
            if (_logoSpriteRu == null)
            {
                _logoSpriteRu = CreateReplacementSprite(logoTexture, "Logo_RU", 16f);
            }

            if (_endingSpriteRu == null)
            {
                _endingSpriteRu = CreateReplacementSprite(endingTexture, "ending_RU", 16f);
            }
        }

        private static Sprite CreateReplacementSprite(Texture2D texture, string assetName, float pixelsPerUnit)
        {
            if (texture == null)
            {
                throw new System.ArgumentNullException(nameof(texture));
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(texture);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = assetName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            return sprite;
        }

        public static void ApplyTextures()
        {
            if (!Plugin.ReplaceTexturesConfig.Value)
            {
                return;
            }

            foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (img == null || img.sprite == null)
                {
                    continue;
                }

                if (_logoSpriteRu != null && img.sprite.name == "Logo")
                {
                    img.sprite = _logoSpriteRu;
                }
                else if (_endingSpriteRu != null && img.sprite.name == "ending")
                {
                    img.sprite = _endingSpriteRu;
                }
            }
        }
    }
}
