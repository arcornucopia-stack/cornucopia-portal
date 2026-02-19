using System;

namespace Cornucopia.Core.Models
{
    /// <summary>
    /// Product definition for the AR catalog.
    /// Stored in Firestore products collection.
    /// </summary>
    [Serializable]
    public class ProductModel
    {
        public string id;
        public string name;
        public string category;
        public string description;
        public float price;
        public string modelUrl;
        public string thumbnailUrl;
        public bool surveyEnabled;
        public long createdAt;

        /// <summary>
        /// Creates a ProductModel from a legacy RTDB model entry.
        /// </summary>
        public static ProductModel FromLegacy(LegacyModel legacy, LegacyModelData legacyData = null)
        {
            return new ProductModel
            {
                id = legacy.modelNamee,
                name = legacy.name,
                category = "Uncategorized",
                description = legacy.question ?? "",
                price = 0f,
                // Legacy storage key is modelNamee and Firebase Storage files are stored as *.glb
                modelUrl = $"{legacy.modelNamee}.glb",
                thumbnailUrl = legacy.picPathh,
                surveyEnabled = !string.IsNullOrEmpty(legacy.question)
            };
        }
    }
}
