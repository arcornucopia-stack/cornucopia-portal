using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cornucopia.Presentation.Survey
{
    /// <summary>
    /// A 5-star rating widget. Stars are gold when filled, grey when empty.
    /// Each star is a Button that sets the rating when tapped.
    /// </summary>
    public class StarRatingWidget : MonoBehaviour
    {
        [SerializeField] private Button[] starButtons = new Button[5];
        [SerializeField] private Image[] starImages = new Image[5];
        [SerializeField] private Color filledColor = new Color(1f, 0.843f, 0f); // Gold #FFD700
        [SerializeField] private Color emptyColor = new Color(0.7f, 0.7f, 0.7f); // Light grey

        private int _rating;

        public int Rating
        {
            get => _rating;
            set
            {
                _rating = Mathf.Clamp(value, 0, 5);
                UpdateStarDisplay();
                OnRatingChanged?.Invoke(_rating);
            }
        }

        public event Action<int> OnRatingChanged;

        private void Awake()
        {
            for (int i = 0; i < starButtons.Length; i++)
            {
                int starIndex = i + 1;
                if (starButtons[i] != null)
                    starButtons[i].onClick.AddListener(() => Rating = starIndex);
            }
            UpdateStarDisplay();
        }

        private void UpdateStarDisplay()
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                    starImages[i].color = i < _rating ? filledColor : emptyColor;
            }
        }

        public void Reset()
        {
            _rating = 0;
            UpdateStarDisplay();
        }
    }
}
