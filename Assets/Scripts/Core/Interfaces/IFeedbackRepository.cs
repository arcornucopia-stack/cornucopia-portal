using System.Collections.Generic;
using System.Threading.Tasks;
using Cornucopia.Core.Models;

namespace Cornucopia.Core.Interfaces
{
    public interface IFeedbackRepository
    {
        Task SubmitFeedback(FeedbackModel feedback);
        Task<List<FeedbackModel>> GetFeedbackForProduct(string productId);
        Task<float> GetAverageRating(string productId);
        Task<Dictionary<string, List<FeedbackModel>>> GetFeedbackGroupedByProduct();
    }
}
