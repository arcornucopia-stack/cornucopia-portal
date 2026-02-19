using System.Collections.Generic;
using System.Threading.Tasks;
using Cornucopia.Core.Models;

namespace Cornucopia.Core.Interfaces
{
    public interface IProductRepository
    {
        Task<List<ProductModel>> GetAllProducts();
        Task<ProductModel> GetProduct(string productId);
        Task<List<ProductModel>> GetProductsByCategory(string category);
    }
}
