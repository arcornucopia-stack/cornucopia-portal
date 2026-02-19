using System.Threading.Tasks;
using UnityEngine;
using Cornucopia.Core.Interfaces;
using Cornucopia.Core.Models;

namespace Cornucopia.Core.UseCases
{
    /// <summary>
    /// Validates and submits feedback through the feedback repository.
    /// Encapsulates business rules for feedback submission.
    /// </summary>
    public class SubmitFeedbackUseCase
    {
        private readonly IFeedbackRepository _feedbackRepo;

        public SubmitFeedbackUseCase(IFeedbackRepository feedbackRepo)
        {
            _feedbackRepo = feedbackRepo;
        }

        /// <summary>
        /// Validates and submits feedback. Returns true on success.
        /// </summary>
        public async Task<bool> Execute(FeedbackModel feedback)
        {
            if (feedback == null)
            {
                Debug.LogError("[SubmitFeedback] Feedback is null.");
                return false;
            }

            if (!feedback.IsValid())
            {
                Debug.LogError($"[SubmitFeedback] Invalid feedback — productId: {feedback.productId}, userId: {feedback.userId}, rating: {feedback.rating}");
                return false;
            }

            if (feedback.timestamp == 0)
                feedback.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await _feedbackRepo.SubmitFeedback(feedback);
            Debug.Log($"[SubmitFeedback] Feedback submitted for product {feedback.productId}, rating: {feedback.rating}");
            return true;
        }
    }
}
