using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cornucopia.Core.Interfaces;
using Cornucopia.Core.Models;
using Cornucopia.Core.Utilities;
using Cornucopia.Data.Firebase;

namespace Cornucopia.Presentation.AR
{
    /// <summary>
    /// Horizontal scrollable product carousel at bottom of AR view.
    /// Loads products from CompositeProductRepository and displays thumbnail cards.
    /// Tapping a card triggers model loading in ARPlacementController.
    /// </summary>
    public class ProductCarouselController : MonoBehaviour
    {
        [SerializeField] private ARPlacementController placementController;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private ScrollRect scrollRect;

        private List<ProductModel> _products = new List<ProductModel>();
        private IProductRepository _repository;

        private async void Start()
        {
            try
            {
                _repository = new CompositeProductRepository(new RealtimeDbProductRepository());
                await LoadProducts();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ProductCarousel] Failed to load: {e.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadProducts()
        {
            _products = await _repository.GetAllProducts();
            PopulateCards();
        }

        private void PopulateCards()
        {
            if (cardPrefab == null || cardContainer == null)
            {
                Debug.LogWarning("[ProductCarousel] cardPrefab or cardContainer not assigned — skipping carousel.");
                return;
            }

            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var product in _products)
            {
                var card = Instantiate(cardPrefab, cardContainer);
                SetupCard(card, product);
            }
        }

        private void SetupCard(GameObject card, ProductModel product)
        {
            // Set product name text
            var nameText = card.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
            {
                string displayName = product.name.Replace(".glb", "").Replace(".gltf", "");
                nameText.text = displayName;
            }

            // Load thumbnail if cached
            string thumbPath = ImageHelper.GetPicCachePath(product.thumbnailUrl ?? product.id);
            var rawImage = card.GetComponentInChildren<RawImage>();
            if (rawImage != null)
            {
                var tex = ImageHelper.LoadFromFile(thumbPath);
                if (tex != null)
                    rawImage.texture = tex;
            }

            // Add click handler
            var button = card.GetComponent<Button>();
            if (button == null)
                button = card.AddComponent<Button>();

            button.onClick.AddListener(() => OnCardSelected(product));
        }

        private void OnCardSelected(ProductModel product)
        {
            if (placementController == null)
            {
                Debug.LogError("[ProductCarousel] No ARPlacementController assigned.");
                return;
            }

            placementController.LoadProduct(product);
            Debug.Log($"[ProductCarousel] Selected product: {product.name}");
        }
    }
}
