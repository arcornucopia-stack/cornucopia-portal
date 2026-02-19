using System.Collections.Generic;
using System.Threading.Tasks;
using Cornucopia.Core.Interfaces;
using Cornucopia.Core.Models;

namespace Cornucopia.Data.Firebase
{
    /// <summary>
    /// Combines products from Realtime Database (legacy) and Firestore (new).
    /// When Firestore is not available, falls back to RTDB-only.
    /// </summary>
    public class CompositeProductRepository : IProductRepository
    {
        private readonly IProductRepository _rtdbRepo;
        private readonly IProductRepository _firestoreRepo;

        public CompositeProductRepository(IProductRepository rtdbRepo, IProductRepository firestoreRepo = null)
        {
            _rtdbRepo = rtdbRepo;
            _firestoreRepo = firestoreRepo;
        }

        public async Task<List<ProductModel>> GetAllProducts()
        {
            var products = new List<ProductModel>();

            var rtdbProducts = await _rtdbRepo.GetAllProducts();
            products.AddRange(rtdbProducts);

            if (_firestoreRepo != null)
            {
                var firestoreProducts = await _firestoreRepo.GetAllProducts();
                products.AddRange(firestoreProducts);
            }

            return products;
        }

        public async Task<ProductModel> GetProduct(string productId)
        {
            // Try RTDB first (faster for existing products)
            var product = await _rtdbRepo.GetProduct(productId);
            if (product != null) return product;

            // Fall back to Firestore
            if (_firestoreRepo != null)
                product = await _firestoreRepo.GetProduct(productId);

            return product;
        }

        public async Task<List<ProductModel>> GetProductsByCategory(string category)
        {
            var products = new List<ProductModel>();

            var rtdbProducts = await _rtdbRepo.GetProductsByCategory(category);
            products.AddRange(rtdbProducts);

            if (_firestoreRepo != null)
            {
                var firestoreProducts = await _firestoreRepo.GetProductsByCategory(category);
                products.AddRange(firestoreProducts);
            }

            return products;
        }
    }
}
