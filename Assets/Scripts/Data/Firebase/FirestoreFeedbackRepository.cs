#if FIREBASE_FIRESTORE
using System;
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
    /// Reads/writes feedback data to Firestore product_feedback collection.
    /// Enable by importing FirebaseFirestore.unitypackage and adding FIREBASE_FIRESTORE scripting define.
    /// </summary>
    public class FirestoreFeedbackRepository : IFeedbackRepository
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

        public async Task SubmitFeedback(FeedbackModel feedback)
        {
            if (!feedback.IsValid())
            {
                Debug.LogError("[FirestoreFeedback] Invalid feedback — skipping submit.");
                return;
            }

            if (feedback.timestamp == 0)
                feedback.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var data = new Dictionary<string, object>
            {
                { "productId", feedback.productId },
                { "userId", feedback.userId },
                { "rating", feedback.rating },
                { "answerChoice", feedback.answerChoice ?? "" },
                { "comment", feedback.comment ?? "" },
                { "timestamp", feedback.timestamp },
                { "sessionId", feedback.sessionId ?? "" }
            };

            var docRef = await Db.Collection(FirebasePaths.FeedbackCollection).AddAsync(data);
            feedback.id = docRef.Id;
            Debug.Log($"[FirestoreFeedback] Submitted feedback {feedback.id} for product {feedback.productId}");
        }

        public async Task<List<FeedbackModel>> GetFeedbackForProduct(string productId)
        {
            var feedbackList = new List<FeedbackModel>();
            var query = Db.Collection(FirebasePaths.FeedbackCollection)
                .WhereEqualTo("productId", productId)
                .OrderByDescending("timestamp");
            var snapshot = await query.GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                var fb = DocToFeedback(doc);
                if (fb != null)
                    feedbackList.Add(fb);
            }

            return feedbackList;
        }

        public async Task<float> GetAverageRating(string productId)
        {
            var feedbackList = await GetFeedbackForProduct(productId);
            if (feedbackList.Count == 0) return 0f;

            float sum = 0;
            foreach (var fb in feedbackList)
                sum += fb.rating;

            return sum / feedbackList.Count;
        }

        public async Task<Dictionary<string, List<FeedbackModel>>> GetFeedbackGroupedByProduct()
        {
            var grouped = new Dictionary<string, List<FeedbackModel>>();
            var snapshot = await Db.Collection(FirebasePaths.FeedbackCollection)
                .OrderByDescending("timestamp")
                .GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                var fb = DocToFeedback(doc);
                if (fb == null) continue;

                if (!grouped.ContainsKey(fb.productId))
                    grouped[fb.productId] = new List<FeedbackModel>();

                grouped[fb.productId].Add(fb);
            }

            return grouped;
        }

        private FeedbackModel DocToFeedback(DocumentSnapshot doc)
        {
            var dict = doc.ToDictionary();
            return new FeedbackModel
            {
                id = doc.Id,
                productId = dict.ContainsKey("productId") ? dict["productId"].ToString() : "",
                userId = dict.ContainsKey("userId") ? dict["userId"].ToString() : "",
                rating = dict.ContainsKey("rating") ? ToInt(dict["rating"]) : 0,
                answerChoice = dict.ContainsKey("answerChoice") ? dict["answerChoice"].ToString() : "",
                comment = dict.ContainsKey("comment") ? dict["comment"].ToString() : "",
                timestamp = dict.ContainsKey("timestamp") ? ToLong(dict["timestamp"]) : 0,
                sessionId = dict.ContainsKey("sessionId") ? dict["sessionId"].ToString() : ""
            };
        }

        private static int ToInt(object value)
        {
            if (value == null) return 0;
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
            int parsed;
            return int.TryParse(value.ToString(), out parsed) ? parsed : 0;
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
    }
}
#endif
