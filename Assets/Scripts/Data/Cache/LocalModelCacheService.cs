using System.IO;
using System.Threading.Tasks;
using Firebase.Storage;
using UnityEngine;
using Cornucopia.Core.Constants;
using Cornucopia.Core.Interfaces;
using Cornucopia.Core.Utilities;

namespace Cornucopia.Data.Cache
{
    /// <summary>
    /// Downloads and caches 3D model files from Firebase Storage to the local filesystem.
    /// </summary>
    public class LocalModelCacheService : IModelCacheService
    {
        private FirebaseStorage _storage;

        private FirebaseStorage Storage
        {
            get
            {
                if (_storage == null)
                    _storage = FirebaseStorage.DefaultInstance;
                return _storage;
            }
        }

        public bool IsCached(string modelName)
        {
            return File.Exists(GetCachedPath(modelName));
        }

        public string GetCachedPath(string modelName)
        {
            return ImageHelper.GetModelCachePath(modelName);
        }

        public async Task<string> DownloadAndCache(string modelName)
        {
            ImageHelper.EnsureCacheDirectory();

            string localPath = GetCachedPath(modelName);

            if (File.Exists(localPath))
            {
                Debug.Log($"[ModelCache] Already cached: {modelName}");
                return localPath;
            }

            string storageUrl = FirebasePaths.StorageModelUrl(modelName);
            StorageReference gsRef = Storage.GetReferenceFromUrl(storageUrl);

            Debug.Log($"[ModelCache] Downloading {modelName} from {storageUrl}");
            await gsRef.GetFileAsync(localPath);
            Debug.Log($"[ModelCache] Downloaded to {localPath}");

            return localPath;
        }

        public void ClearCache()
        {
            string cacheDir = Path.Combine(Application.persistentDataPath, "Files");
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
                Debug.Log("[ModelCache] Cache cleared.");
            }
        }
    }
}
