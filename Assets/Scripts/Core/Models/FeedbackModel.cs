using System;

namespace Cornucopia.Core.Models
{
    /// <summary>
    /// Survey feedback for a product.
    /// Stored in Firestore product_feedback collection.
    /// </summary>
    [Serializable]
    public class FeedbackModel
    {
        public string id;
        public string productId;
        public string userId;
        public int rating;
        public string answerChoice;
        public string comment;
        public long timestamp;
        public string sessionId;

        /// <summary>
        /// Validates that required fields are present and values are in range.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(productId)
                && !string.IsNullOrEmpty(userId)
                && rating >= 1 && rating <= 5;
        }
    }
}
