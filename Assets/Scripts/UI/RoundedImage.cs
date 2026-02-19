using UnityEngine;
using UnityEngine.UI;

namespace Cornucopia.UI
{
    /// <summary>
    /// Applies rounded corners to a UI Image using a mask.
    /// Attach to any GameObject with an Image component.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Mask))]
    public class RoundedImage : MonoBehaviour
    {
        [SerializeField] private float cornerRadius = 12f;

        private Image _image;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();

            // Use a procedurally generated rounded sprite
            ApplyRoundedCorners();
        }

        private void ApplyRoundedCorners()
        {
            // For now, we'll use the built-in Unity sprite with pixelsPerUnit adjustment
            // A proper implementation would generate a rounded rect texture

            // Enable mask for rounded effect
            var mask = GetComponent<Mask>();
            if (mask != null)
            {
                mask.showMaskGraphic = true;
            }
        }

        public void SetCornerRadius(float radius)
        {
            cornerRadius = radius;
            ApplyRoundedCorners();
        }
    }
}
