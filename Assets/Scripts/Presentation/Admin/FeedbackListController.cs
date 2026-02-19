using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Cornucopia.Core.Interfaces;
using Cornucopia.Core.Models;
#if FIREBASE_FIRESTORE
using Cornucopia.Data.Firebase;
#endif

namespace Cornucopia.Presentation.Admin
{
    /// <summary>
    /// Displays survey feedback grouped by product with average ratings and response counts.
    /// Used in the Admin Feedback Dashboard scene.
    /// </summary>
    public class FeedbackListController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject productGroupPrefab;
        [SerializeField] private GameObject feedbackItemPrefab;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private Button exportButton;
        [SerializeField] private Button backButton;

        [Header("Dependencies")]
        [SerializeField] private FeedbackExportController exportController;

        private IFeedbackRepository _feedbackRepo;
        private Dictionary<string, List<FeedbackModel>> _groupedFeedback;

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClick);

            if (exportButton != null)
                exportButton.onClick.AddListener(OnExportClick);

#if FIREBASE_FIRESTORE
            if (_feedbackRepo == null)
                _feedbackRepo = new FirestoreFeedbackRepository();
#endif
            LoadFeedback();
        }

        /// <summary>
        /// Inject the feedback repository. Call before Start if not using DI.
        /// </summary>
        public void SetRepository(IFeedbackRepository repo)
        {
            _feedbackRepo = repo;
        }

        private async void LoadFeedback()
        {
            if (_feedbackRepo == null)
            {
                ShowEmpty("Feedback service not configured. Import Firestore SDK first.");
                return;
            }

            if (headerText != null)
                headerText.text = "Loading feedback...";

            _groupedFeedback = await _feedbackRepo.GetFeedbackGroupedByProduct();

            if (_groupedFeedback == null || _groupedFeedback.Count == 0)
            {
                ShowEmpty("No feedback received yet.");
                return;
            }

            if (headerText != null)
            {
                int totalResponses = _groupedFeedback.Values.Sum(list => list.Count);
                headerText.text = $"Feedback Dashboard — {_groupedFeedback.Count} products, {totalResponses} responses";
            }

            PopulateList();
        }

        private void PopulateList()
        {
            // Clear existing
            foreach (Transform child in listContainer)
                Destroy(child.gameObject);

            foreach (var kvp in _groupedFeedback)
            {
                string productId = kvp.Key;
                var feedbackList = kvp.Value;

                if (productGroupPrefab != null)
                {
                    var groupObj = Instantiate(productGroupPrefab, listContainer);

                    // Set product group header
                    var texts = groupObj.GetComponentsInChildren<TMP_Text>();
                    if (texts.Length > 0)
                    {
                        float avgRating = (float)feedbackList.Average(f => f.rating);
                        texts[0].text = $"{productId} — Avg: {avgRating:F1}/5 ({feedbackList.Count} responses)";
                    }
                }

                // Add individual feedback items
                foreach (var feedback in feedbackList)
                {
                    if (feedbackItemPrefab != null)
                    {
                        var itemObj = Instantiate(feedbackItemPrefab, listContainer);
                        var itemTexts = itemObj.GetComponentsInChildren<TMP_Text>();
                        if (itemTexts.Length >= 3)
                        {
                            itemTexts[0].text = $"Rating: {feedback.rating}/5";
                            itemTexts[1].text = !string.IsNullOrEmpty(feedback.answerChoice) ? feedback.answerChoice : "—";
                            itemTexts[2].text = !string.IsNullOrEmpty(feedback.comment) ? feedback.comment : "No comment";
                        }
                        else if (itemTexts.Length > 0)
                        {
                            itemTexts[0].text = $"{feedback.rating}/5 — {feedback.answerChoice} — {feedback.comment}";
                        }
                    }
                }
            }
        }

        private void ShowEmpty(string message)
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(true);
                emptyText.text = message;
            }
            if (headerText != null)
                headerText.text = "Feedback Dashboard";
        }

        private void OnExportClick()
        {
            if (exportController != null && _groupedFeedback != null)
                exportController.ExportToCSV(_groupedFeedback);
        }

        private void OnBackClick()
        {
            SceneManager.LoadScene("Admin");
        }
    }
}
