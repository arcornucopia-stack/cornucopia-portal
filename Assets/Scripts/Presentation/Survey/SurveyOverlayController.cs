using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Cornucopia.Core.Models;
using Cornucopia.Core.Interfaces;
using Cornucopia.Core.UseCases;
#if FIREBASE_FIRESTORE
using Cornucopia.Data.Firebase;
#endif

namespace Cornucopia.Presentation.Survey
{
    /// <summary>
    /// Full-screen survey overlay with star rating, multiple choice, comment field, and submit.
    /// Slides in from bottom when triggered from the Product Info Sheet.
    /// </summary>
    public class SurveyOverlayController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform overlayPanel;
        [SerializeField] private TMP_Text productNameText;
        [SerializeField] private StarRatingWidget starRating;
        [SerializeField] private ToggleGroup answerToggleGroup;
        [SerializeField] private Toggle yesToggle;
        [SerializeField] private Toggle noToggle;
        [SerializeField] private Toggle maybeToggle;
        [SerializeField] private TMP_InputField commentInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.35f;

        public event Action OnSurveyCompleted;
        public event Action OnSurveyCancelled;

        private ProductModel _currentProduct;
        private IFeedbackRepository _feedbackRepo;
        private bool _isSubmitting;

        private void Awake()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClick);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClick);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the survey overlay for a given product.
        /// </summary>
        public void Show(ProductModel product, IFeedbackRepository feedbackRepo)
        {
            _currentProduct = product;
            _feedbackRepo = feedbackRepo;
#if FIREBASE_FIRESTORE
            if (_feedbackRepo == null)
                _feedbackRepo = new FirestoreFeedbackRepository();
#endif
            _isSubmitting = false;
            gameObject.SetActive(true);

            // Reset form
            if (productNameText != null)
                productNameText.text = product.name?.Replace(".glb", "").Replace(".gltf", "") ?? "Product";

            if (starRating != null)
                starRating.Reset();

            if (commentInput != null)
                commentInput.text = "";

            if (statusText != null)
                statusText.text = "";

            if (yesToggle != null) yesToggle.isOn = false;
            if (noToggle != null) noToggle.isOn = false;
            if (maybeToggle != null) maybeToggle.isOn = false;

            // Animate in
            if (overlayPanel != null)
            {
                overlayPanel.anchoredPosition = new Vector2(0, -Screen.height);
                overlayPanel.DOAnchorPosY(0, slideDuration).SetEase(Ease.OutCubic);
            }
        }

        private async void OnSubmitClick()
        {
            if (_isSubmitting) return;
            if (_feedbackRepo == null)
            {
                if (statusText != null)
                    statusText.text = "Feedback service not available.";
                return;
            }

            int rating = starRating != null ? starRating.Rating : 0;
            if (rating < 1)
            {
                if (statusText != null)
                    statusText.text = "Please select a star rating.";
                return;
            }

            _isSubmitting = true;
            if (submitButton != null)
                submitButton.interactable = false;

            string answer = GetSelectedAnswer();
            string comment = commentInput != null ? commentInput.text : "";

            var feedback = new FeedbackModel
            {
                productId = _currentProduct?.id ?? _currentProduct?.name ?? "",
                userId = PlayerPrefs.GetString("userId", ""),
                rating = rating,
                answerChoice = answer,
                comment = comment
            };

            var useCase = new SubmitFeedbackUseCase(_feedbackRepo);
            bool success = await useCase.Execute(feedback);

            if (success)
            {
                if (statusText != null)
                    statusText.text = "Thank you for your feedback!";

                // Brief delay then close
                await System.Threading.Tasks.Task.Delay(1000);
                Close();
                OnSurveyCompleted?.Invoke();
            }
            else
            {
                if (statusText != null)
                    statusText.text = "Failed to submit. Please try again.";
                _isSubmitting = false;
                if (submitButton != null)
                    submitButton.interactable = true;
            }
        }

        private string GetSelectedAnswer()
        {
            if (yesToggle != null && yesToggle.isOn) return "yes";
            if (noToggle != null && noToggle.isOn) return "no";
            if (maybeToggle != null && maybeToggle.isOn) return "maybe";
            return "";
        }

        private void OnCancelClick()
        {
            Close();
            OnSurveyCancelled?.Invoke();
        }

        private void Close()
        {
            if (overlayPanel != null)
            {
                overlayPanel.DOAnchorPosY(-Screen.height, slideDuration)
                    .SetEase(Ease.InCubic)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
