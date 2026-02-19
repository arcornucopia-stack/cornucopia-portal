using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cornucopia.UI
{
    /// <summary>
    /// Singleton manager for applying the Cornucopia theme across the app.
    /// Attach to a persistent GameObject or use via static instance.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        private static ThemeManager _instance;
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ThemeManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ThemeManager");
                        _instance = go.AddComponent<ThemeManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [SerializeField]
        private CornucopiaTheme _theme;

        public CornucopiaTheme Theme
        {
            get
            {
                if (_theme == null)
                {
                    _theme = Resources.Load<CornucopiaTheme>("CornucopiaTheme");
                    if (_theme == null)
                    {
                        Debug.LogWarning("[ThemeManager] No theme found. Using default colors.");
                        _theme = ScriptableObject.CreateInstance<CornucopiaTheme>();
                    }
                }
                return _theme;
            }
            set => _theme = value;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Apply primary button styling to a Button component.
        /// </summary>
        public void ApplyPrimaryButton(Button button)
        {
            ApplyButtonStyle(button, ButtonType.Primary);
        }

        /// <summary>
        /// Apply secondary button styling to a Button component.
        /// </summary>
        public void ApplySecondaryButton(Button button)
        {
            ApplyButtonStyle(button, ButtonType.Secondary);
        }

        /// <summary>
        /// Apply accent (gold) button styling to a Button component.
        /// </summary>
        public void ApplyAccentButton(Button button)
        {
            ApplyButtonStyle(button, ButtonType.Accent);
        }

        /// <summary>
        /// Apply button styling based on type.
        /// </summary>
        public void ApplyButtonStyle(Button button, ButtonType type)
        {
            if (button == null) return;

            var colors = Theme.GetButtonColors(type);
            var colorBlock = button.colors;

            colorBlock.normalColor = colors.normal;
            colorBlock.highlightedColor = colors.hover;
            colorBlock.pressedColor = colors.pressed;
            colorBlock.disabledColor = colors.disabled;
            colorBlock.selectedColor = colors.hover;

            button.colors = colorBlock;

            // Update text color if present
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = colors.text;
            }

            var tmpText = button.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                tmpText.color = colors.text;
            }
        }

        /// <summary>
        /// Apply navigation icon color based on selection state.
        /// </summary>
        public void ApplyNavIcon(Image icon, bool isSelected)
        {
            if (icon == null) return;
            icon.color = Theme.GetNavIconColor(isSelected);
        }

        /// <summary>
        /// Apply card/panel background styling.
        /// </summary>
        public void ApplyCardBackground(Image background, bool elevated = false)
        {
            if (background == null) return;
            background.color = elevated ? Theme.neutralLightGrey : Theme.neutralWhite;
        }

        /// <summary>
        /// Apply heading text styling.
        /// </summary>
        public void ApplyHeadingText(TMP_Text text)
        {
            if (text == null) return;
            text.color = Theme.neutralBlack;
            if (Theme.primaryFont != null)
            {
                // Note: For TMP, you'd use a TMP_FontAsset instead
            }
        }

        /// <summary>
        /// Apply body text styling.
        /// </summary>
        public void ApplyBodyText(TMP_Text text)
        {
            if (text == null) return;
            text.color = Theme.neutralDarkGrey;
        }

        /// <summary>
        /// Apply notification badge styling.
        /// </summary>
        public void ApplyBadge(Image badge, TMP_Text badgeText = null)
        {
            if (badge != null)
            {
                badge.color = Theme.GetBadgeColor();
            }
            if (badgeText != null)
            {
                badgeText.color = Theme.neutralBlack;
            }
        }

        /// <summary>
        /// Apply AR overlay styling.
        /// </summary>
        public void ApplyAROverlay(Image overlay)
        {
            if (overlay == null) return;
            overlay.color = Theme.arOverlay;
        }

        /// <summary>
        /// Apply progress bar styling.
        /// </summary>
        public void ApplyProgressBar(Image background, Image fill)
        {
            if (background != null)
            {
                background.color = Theme.neutralLightGrey;
            }
            if (fill != null)
            {
                fill.color = Theme.primaryIndigo;
            }
        }

        /// <summary>
        /// Get spacing value by size.
        /// </summary>
        public float GetSpacing(SpacingSize size)
        {
            return size switch
            {
                SpacingSize.XS => Theme.spacingXS,
                SpacingSize.S => Theme.spacingS,
                SpacingSize.M => Theme.spacingM,
                SpacingSize.L => Theme.spacingL,
                SpacingSize.XL => Theme.spacingXL,
                SpacingSize.XXL => Theme.spacingXXL,
                _ => Theme.spacingM
            };
        }
    }

    public enum SpacingSize
    {
        XS,
        S,
        M,
        L,
        XL,
        XXL
    }
}
