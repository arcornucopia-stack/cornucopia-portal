using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using Cornucopia.Core.Constants;
using Cornucopia.Core.Interfaces;
using Cornucopia.Core.Models;

namespace Cornucopia.Data.Firebase
{
    /// <summary>
    /// Reads product data from the existing Firebase Realtime Database (cornucopia/models/).
    /// Wraps LegacyModel + LegacyModelData into ProductModel.
    /// </summary>
    public class RealtimeDbProductRepository : IProductRepository
    {
        private DatabaseReference _dbRef;

        private DatabaseReference DbRef
        {
            get
            {
                if (_dbRef == null)
                    _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                return _dbRef;
            }
        }

        public async Task<List<ProductModel>> GetAllProducts()
        {
            var products = new List<ProductModel>();

            var snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference(FirebasePaths.Root)
                .Child(FirebasePaths.Models)
                .GetValueAsync();

            foreach (DataSnapshot child in snapshot.Children)
            {
                var legacy = JsonUtility.FromJson<LegacyModel>(child.GetRawJsonValue());
                if (legacy == null) continue;

                LegacyModelData legacyData = null;
                var dataChild = child.Child(FirebasePaths.ModelData);
                if (dataChild.Exists)
                {
                    legacyData = JsonUtility.FromJson<LegacyModelData>(dataChild.GetRawJsonValue());
                }

                var product = ProductModel.FromLegacy(legacy, legacyData);
                product.id = child.Key;
                products.Add(product);
            }

            return products;
        }

        public async Task<ProductModel> GetProduct(string productId)
        {
            var snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference(FirebasePaths.Root)
                .Child(FirebasePaths.Models)
                .Child(productId)
                .GetValueAsync();

            if (!snapshot.Exists) return null;

            var legacy = JsonUtility.FromJson<LegacyModel>(snapshot.GetRawJsonValue());
            if (legacy == null) return null;

            LegacyModelData legacyData = null;
            var dataChild = snapshot.Child(FirebasePaths.ModelData);
            if (dataChild.Exists)
            {
                legacyData = JsonUtility.FromJson<LegacyModelData>(dataChild.GetRawJsonValue());
            }

            var product = ProductModel.FromLegacy(legacy, legacyData);
            product.id = snapshot.Key;
            return product;
        }

        public async Task<List<ProductModel>> GetProductsByCategory(string category)
        {
            // RTDB doesn't have categories, so return all and filter client-side
            var all = await GetAllProducts();
            if (string.IsNullOrEmpty(category)) return all;

            return all.FindAll(p => p.category == category);
        }
    }
}
