using UnityEngine;
using UnityEngine.UI;

namespace Cornucopia.UI
{
    /// <summary>
    /// Attach to navigation icon Images to automatically apply theme styling.
    /// Handles selected/unselected states with indigo/grey colors.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ThemedNavIcon : MonoBehaviour
    {
        [SerializeField]
        private bool _isSelected = false;

        [SerializeField]
        private bool _applyOnStart = true;

        private Image _image;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                ApplyTheme();
            }
        }

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Start()
        {
            if (_applyOnStart)
            {
                ApplyTheme();
            }
        }

        public void ApplyTheme()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.ApplyNavIcon(_image, _isSelected);
            }
        }

        /// <summary>
        /// Toggle selection state.
        /// </summary>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _image != null)
            {
                ApplyTheme();
            }
        }
    }
}
