#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Cornucopia.UI.Editor
{
    /// <summary>
    /// Generates rounded rectangle sprites for UI buttons.
    /// </summary>
    public static class RoundedButtonGenerator
    {
        [MenuItem("Cornucopia/Generate Button Sprites")]
        public static void GenerateButtonSprites()
        {
            // Create sprites folder if needed
            string spritesPath = "Assets/UI/Sprites";
            if (!AssetDatabase.IsValidFolder(spritesPath))
            {
                AssetDatabase.CreateFolder("Assets/UI", "Sprites");
            }

            // Generate filled rounded button (for primary/accent buttons)
            GenerateRoundedRect(spritesPath + "/RoundedButtonFilled.png", 256, 80, 16, Color.white, Color.clear, 0);

            // Generate outline rounded button (for secondary button)
            GenerateRoundedRect(spritesPath + "/RoundedButtonOutline.png", 256, 80, 16, Color.clear, Color.white, 3);

            AssetDatabase.Refresh();

            // Import as sprites with correct settings
            SetSpriteImportSettings(spritesPath + "/RoundedButtonFilled.png", 16);
            SetSpriteImportSettings(spritesPath + "/RoundedButtonOutline.png", 16);

            Debug.Log("[RoundedButtonGenerator] Button sprites generated in Assets/UI/Sprites/");
        }

        private static void GenerateRoundedRect(string path, int width, int height, int radius, Color fillColor, Color strokeColor, int strokeWidth)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Fill with transparent
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            // Draw rounded rectangle
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRect(x, y, width, height, radius);

                    if (strokeWidth > 0)
                    {
                        // Outline mode
                        if (dist <= 0 && dist > -strokeWidth)
                        {
                            // Anti-aliasing at edges
                            float alpha = Mathf.Clamp01(-dist);
                            float innerAlpha = Mathf.Clamp01(dist + strokeWidth);
                            pixels[y * width + x] = new Color(strokeColor.r, strokeColor.g, strokeColor.b, alpha * innerAlpha);
                        }
                        else if (dist <= -strokeWidth)
                        {
                            // Inside the outline - keep transparent for outline-only
                            pixels[y * width + x] = Color.clear;
                        }
                    }
                    else
                    {
                        // Filled mode
                        if (dist <= 0)
                        {
                            // Anti-aliasing at edges
                            float alpha = Mathf.Clamp01(-dist);
                            pixels[y * width + x] = new Color(fillColor.r, fillColor.g, fillColor.b, alpha);
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Save to file
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(path, pngData);

            Object.DestroyImmediate(texture);
        }

        private static float DistanceToRoundedRect(int x, int y, int width, int height, int radius)
        {
            // Calculate distance to rounded rectangle edge
            // Negative = inside, Positive = outside

            int left = radius;
            int right = width - radius - 1;
            int bottom = radius;
            int top = height - radius - 1;

            if (x < left && y < bottom)
            {
                // Bottom-left corner
                return Vector2.Distance(new Vector2(x, y), new Vector2(left, bottom)) - radius;
            }
            else if (x > right && y < bottom)
            {
                // Bottom-right corner
                return Vector2.Distance(new Vector2(x, y), new Vector2(right, bottom)) - radius;
            }
            else if (x < left && y > top)
            {
                // Top-left corner
                return Vector2.Distance(new Vector2(x, y), new Vector2(left, top)) - radius;
            }
            else if (x > right && y > top)
            {
                // Top-right corner
                return Vector2.Distance(new Vector2(x, y), new Vector2(right, top)) - radius;
            }
            else if (x < left)
            {
                // Left edge
                return left - x - radius;
            }
            else if (x > right)
            {
                // Right edge
                return x - right - radius;
            }
            else if (y < bottom)
            {
                // Bottom edge
                return bottom - y - radius;
            }
            else if (y > top)
            {
                // Top edge
                return y - top - radius;
            }
            else
            {
                // Inside
                return -Mathf.Min(x - left + radius, right - x + radius, y - bottom + radius, top - y + radius);
            }
        }

        private static void SetSpriteImportSettings(string path, int border)
        {
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(border, border, border, border); // 9-slice borders
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
