using System;
using System.IO;
using UnityEngine;

namespace Cornucopia.Core.Utilities
{
    public static class ImageHelper
    {
        /// <summary>
        /// Loads a Texture2D from a file on disk. Returns null if file doesn't exist or loading fails.
        /// Replaces the GetImage() method previously duplicated across 6+ scripts.
        /// </summary>
        public static Texture2D LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.Log($"[ImageHelper] File not found: {filePath}");
                    return null;
                }

                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(1, 1);
                texture.LoadImage(bytes);
                return texture;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImageHelper] Failed to load image: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns the local cache path for a picture by name.
        /// </summary>
        public static string GetPicCachePath(string picName)
        {
            return Path.Combine(Application.persistentDataPath, "Files", $"{picName}.png");
        }

        /// <summary>
        /// Returns the local cache path for a model file.
        /// </summary>
        public static string GetModelCachePath(string modelName)
        {
            return Path.Combine(Application.persistentDataPath, "Files", modelName);
        }

        /// <summary>
        /// Ensures the Files cache directory exists.
        /// </summary>
        public static void EnsureCacheDirectory()
        {
            string dir = Path.Combine(Application.persistentDataPath, "Files");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
