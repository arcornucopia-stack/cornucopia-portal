namespace Cornucopia.Core.Constants
{
    public static class FirebasePaths
    {
        // Realtime Database root
        public const string Root = "cornucopia";

        // Realtime Database child paths
        public const string Models = "models";
        public const string Users = "users";
        public const string UserModels = "models";
        public const string UserData = "userData";
        public const string ModelData = "data";

        // Firebase Storage
        public const string StorageBucket = "gs://cornucopia-54b02.appspot.com";
        public const string PicsFolder = "pics";
        public const string ModelFolder = "model";

        // Firestore collections (Phase 2)
        public const string ProductsCollection = "products";
        public const string FeedbackCollection = "product_feedback";

        // Helper methods
        public static string StoragePicUrl(string picName)
        {
            return $"{StorageBucket}/{PicsFolder}/{picName}.png";
        }

        public static string StorageModelUrl(string modelName)
        {
            return $"{StorageBucket}/{ModelFolder}/{modelName}";
        }
    }
}
