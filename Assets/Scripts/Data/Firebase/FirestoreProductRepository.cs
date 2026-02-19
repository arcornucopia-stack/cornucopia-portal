#if FIREBASE_FIRESTORE
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using Cornucopia.Core.Constants;
using Cornucopia.Core.Interfaces;
using Cornucopia.Core.Models;

namespace Cornucopia.Data.Firebase
{
    /// <summary>
    /// Reads product data from Firestore products collection.
    /// Enable by importing FirebaseFirestore.unitypackage and adding FIREBASE_FIRESTORE scripting define.
    /// </summary>
    public class FirestoreProductRepository : IProductRepository
    {
        private FirebaseFirestore _db;

        private FirebaseFirestore Db
        {
            get
            {
                if (_db == null)
                    _db = FirebaseFirestore.DefaultInstance;
                return _db;
            }
        }

        public async Task<List<ProductModel>> GetAllProducts()
        {
            var products = new List<ProductModel>();
            var snapshot = await Db.Collection(FirebasePaths.ProductsCollection).GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                var product = DocToProduct(doc);
                if (product != null)
                    products.Add(product);
            }

            return products;
        }

        public async Task<ProductModel> GetProduct(string productId)
        {
            var doc = await Db.Collection(FirebasePaths.ProductsCollection).Document(productId).GetSnapshotAsync();
            if (!doc.Exists) return null;
            return DocToProduct(doc);
        }

        public async Task<List<ProductModel>> GetProductsByCategory(string category)
        {
            var products = new List<ProductModel>();
            var query = Db.Collection(FirebasePaths.ProductsCollection)
                .WhereEqualTo("category", category);
            var snapshot = await query.GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                var product = DocToProduct(doc);
                if (product != null)
                    products.Add(product);
            }

            return products;
        }

        private ProductModel DocToProduct(DocumentSnapshot doc)
        {
            var dict = doc.ToDictionary();
            var product = new ProductModel
            {
                id = doc.Id,
                name = dict.ContainsKey("name") ? dict["name"].ToString() : "",
                category = dict.ContainsKey("category") ? dict["category"].ToString() : "",
                description = dict.ContainsKey("description") ? dict["description"].ToString() : "",
                price = dict.ContainsKey("price") ? ToFloat(dict["price"]) : 0f,
                modelUrl = dict.ContainsKey("modelUrl") ? dict["modelUrl"].ToString() : "",
                thumbnailUrl = dict.ContainsKey("thumbnailUrl") ? dict["thumbnailUrl"].ToString() : "",
                surveyEnabled = dict.ContainsKey("surveyEnabled") && ToBool(dict["surveyEnabled"]),
                createdAt = dict.ContainsKey("createdAt") ? ToLong(dict["createdAt"]) : 0
            };
            return product;
        }

        private static float ToFloat(object value)
        {
            if (value == null) return 0f;
            if (value is double d) return (float)d;
            if (value is float f) return f;
            if (value is long l) return l;
            if (value is int i) return i;
            float parsed;
            return float.TryParse(value.ToString(), out parsed) ? parsed : 0f;
        }

        private static long ToLong(object value)
        {
            if (value == null) return 0;
            if (value is long l) return l;
            if (value is int i) return i;
            if (value is double d) return (long)d;
            long parsed;
            return long.TryParse(value.ToString(), out parsed) ? parsed : 0;
        }

        private static bool ToBool(object value)
        {
            if (value is bool b) return b;
            bool parsed;
            return bool.TryParse(value?.ToString(), out parsed) && parsed;
        }
    }
}
#endif
