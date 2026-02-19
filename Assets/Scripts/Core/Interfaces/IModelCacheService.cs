using System.Threading.Tasks;

namespace Cornucopia.Core.Interfaces
{
    public interface IModelCacheService
    {
        bool IsCached(string modelName);
        string GetCachedPath(string modelName);
        Task<string> DownloadAndCache(string modelName);
        void ClearCache();
    }
}
