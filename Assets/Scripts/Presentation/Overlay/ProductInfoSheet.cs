using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Cornucopia.Core.Models;

namespace Cornucopia.Presentation.Overlay
{
    /// <summary>
    /// Animated bottom sheet that shows product details when an AR object is tapped.
    /// Slides up from bottom ~40% of screen with product info and "Leave Feedback" button.
    /// </summary>
    public class ProductInfoSheet : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform sheetPanel;
        [SerializeField] private TMP_Text productNameText;
        [SerializeField] private TMP_Text categoryBadgeText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image scrimOverlay;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.35f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        public event Action<ProductModel> OnFeedbackRequested;

        private ProductModel _currentProduct;
        private float _sheetHeight;
        private bool _isShowing;

        private void Awake()
        {
            if (sheetPanel != null)
                _sheetHeight = sheetPanel.rect.height;

            if (feedbackButton != null)
                feedbackButton.onClick.AddListener(OnFeedbackClick);

            if (closeButton != null)
                closeButton.onClick.AddListener(Dismiss);

            if (scrimOverlay != null)
            {
                var scrimButton = scrimOverlay.GetComponent<Button>();
                if (scrimButton == null)
                    scrimButton = scrimOverlay.gameObject.AddComponent<Button>();
                scrimButton.onClick.AddListener(Dismiss);
            }

            // Start hidden
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the bottom sheet with product info.
        /// </summary>
        public void Show(ProductModel product)
        {
            if (_isShowing) return;

            _currentProduct = product;
            _isShowing = true;
            gameObject.SetActive(true);

            // Populate UI
            if (productNameText != null)
                productNameText.text = product.name?.Replace(".glb", "").Replace(".gltf", "") ?? "Product";

            if (categoryBadgeText != null)
                categoryBadgeText.text = !string.IsNullOrEmpty(product.category) ? product.category : "Uncategorized";

            if (descriptionText != null)
                descriptionText.text = !string.IsNullOrEmpty(product.description) ? product.description : "No description available.";

            if (priceText != null)
            {
                if (product.price > 0)
                    priceText.text = $"${product.price:F2}";
                else
                    priceText.gameObject.SetActive(false);
            }

            // Animate slide up
            if (sheetPanel != null)
            {
                sheetPanel.anchoredPosition = new Vector2(0, -_sheetHeight);
                sheetPanel.DOAnchorPosY(0, slideDuration).SetEase(slideEase);
            }

            // Fade in scrim
            if (scrimOverlay != null)
            {
                scrimOverlay.color = new Color(0, 0, 0, 0);
                scrimOverlay.DOFade(0.4f, slideDuration);
            }
        }

        /// <summary>
        /// Dismiss the bottom sheet with animation.
        /// </summary>
        public void Dismiss()
        {
            if (!_isShowing) return;
            _isShowing = false;

            if (sheetPanel != null)
            {
                sheetPanel.DOAnchorPosY(-_sheetHeight, slideDuration)
                    .SetEase(Ease.InCubic)
                    .OnComplete(() => gameObject.SetActive(false));
            }

            if (scrimOverlay != null)
            {
                scrimOverlay.DOFade(0f, slideDuration);
            }
        }

        private void OnFeedbackClick()
        {
            if (_currentProduct != null)
                OnFeedbackRequested?.Invoke(_currentProduct);
        }
    }
}
