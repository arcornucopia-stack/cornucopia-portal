using UnityEngine;
using UnityEngine.UI;

namespace Cornucopia.UI
{
    /// <summary>
    /// Attach to Button GameObjects to automatically apply theme styling.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ThemedButton : MonoBehaviour
    {
        [SerializeField]
        private ButtonType _buttonType = ButtonType.Primary;

        [SerializeField]
        private bool _applyOnStart = true;

        private Button _button;

        public ButtonType ButtonType
        {
            get => _buttonType;
            set
            {
                _buttonType = value;
                ApplyTheme();
            }
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
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
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.ApplyButtonStyle(_button, _buttonType);
            }
        }

        private void OnValidate()
        {
            // Apply in editor for preview
            if (Application.isPlaying && _button != null)
            {
                ApplyTheme();
            }
        }
    }
}
