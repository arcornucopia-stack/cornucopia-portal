using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Cornucopia.Core.Models;

namespace Cornucopia.Presentation.Admin
{
    /// <summary>
    /// Exports grouped feedback data to a CSV file in persistent storage.
    /// </summary>
    public class FeedbackExportController : MonoBehaviour
    {
        public void ExportToCSV(Dictionary<string, List<FeedbackModel>> groupedFeedback)
        {
            if (groupedFeedback == null || groupedFeedback.Count == 0)
            {
                Debug.LogWarning("[FeedbackExport] No feedback to export.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("ProductId,UserId,Rating,AnswerChoice,Comment,Timestamp,SessionId");

            foreach (var kvp in groupedFeedback)
            {
                string productId = Escape(kvp.Key);
                foreach (var fb in kvp.Value)
                {
                    sb.Append(productId).Append(",");
                    sb.Append(Escape(fb.userId)).Append(",");
                    sb.Append(fb.rating).Append(",");
                    sb.Append(Escape(fb.answerChoice)).Append(",");
                    sb.Append(Escape(fb.comment)).Append(",");
                    sb.Append(fb.timestamp).Append(",");
                    sb.Append(Escape(fb.sessionId)).AppendLine();
                }
            }

            string fileName = $"feedback_export_{System.DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            string outputPath = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[FeedbackExport] Export complete: {outputPath}");
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }
}
