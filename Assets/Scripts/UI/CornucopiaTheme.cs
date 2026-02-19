using UnityEngine;

namespace Cornucopia.UI
{
    /// <summary>
    /// Centralized design system for Cornucopia AR app.
    /// Based on UX Project Brief: Modern, clean, high-contrast with indigo and gold accents.
    /// </summary>
    [CreateAssetMenu(fileName = "CornucopiaTheme", menuName = "Cornucopia/UI Theme")]
    public class CornucopiaTheme : ScriptableObject
    {
        [Header("Primary Colors - Indigo")]
        [Tooltip("Primary brand color - deep indigo")]
        public Color primaryIndigo = new Color(0.247f, 0.318f, 0.710f, 1f);      // #3F51B5

        [Tooltip("Light variant for backgrounds and hover states")]
        public Color primaryLight = new Color(0.482f, 0.545f, 0.835f, 1f);       // #7B8BD5

        [Tooltip("Dark variant for pressed states and emphasis")]
        public Color primaryDark = new Color(0.188f, 0.247f, 0.624f, 1f);        // #303F9F

        [Header("Accent Colors - Gold")]
        [Tooltip("Gold accent for highlights, rewards, and CTAs")]
        public Color accentGold = new Color(1f, 0.843f, 0f, 1f);                 // #FFD700

        [Tooltip("Muted gold for secondary accents")]
        public Color accentGoldMuted = new Color(0.855f, 0.647f, 0.125f, 1f);    // #DAA520

        [Tooltip("Light gold for subtle highlights")]
        public Color accentGoldLight = new Color(1f, 0.922f, 0.612f, 1f);        // #FFEB9C

        [Header("Neutral Colors")]
        [Tooltip("Pure white for backgrounds")]
        public Color neutralWhite = new Color(1f, 1f, 1f, 1f);                   // #FFFFFF

        [Tooltip("Light grey for cards and elevated surfaces")]
        public Color neutralLightGrey = new Color(0.961f, 0.961f, 0.961f, 1f);   // #F5F5F5

        [Tooltip("Medium grey for borders and dividers")]
        public Color neutralMediumGrey = new Color(0.741f, 0.741f, 0.741f, 1f);  // #BDBDBD

        [Tooltip("Dark grey for secondary text")]
        public Color neutralDarkGrey = new Color(0.459f, 0.459f, 0.459f, 1f);    // #757575

        [Tooltip("Near-black for primary text")]
        public Color neutralBlack = new Color(0.129f, 0.129f, 0.129f, 1f);       // #212121

        [Header("Semantic Colors")]
        [Tooltip("Success state - kept distinct from gold")]
        public Color semanticSuccess = new Color(0.298f, 0.686f, 0.314f, 1f);    // #4CAF50

        [Tooltip("Warning state - warm orange")]
        public Color semanticWarning = new Color(1f, 0.596f, 0f, 1f);            // #FF9800

        [Tooltip("Error state - red for alerts")]
        public Color semanticError = new Color(0.957f, 0.263f, 0.212f, 1f);      // #F44336

        [Tooltip("Info state - light indigo")]
        public Color semanticInfo = new Color(0.129f, 0.588f, 0.953f, 1f);       // #2196F3

        [Header("AR Overlay Colors")]
        [Tooltip("Semi-transparent overlay for AR camera UI")]
        public Color arOverlay = new Color(0f, 0f, 0f, 0.4f);

        [Tooltip("AR reticle/crosshair color")]
        public Color arReticle = new Color(1f, 1f, 1f, 0.8f);

        [Tooltip("AR plane visualization")]
        public Color arPlaneColor = new Color(0.247f, 0.318f, 0.710f, 0.3f);     // Indigo with transparency

        [Header("Typography")]
        [Tooltip("Primary font for headings")]
        public Font primaryFont;

        [Tooltip("Secondary font for body text")]
        public Font secondaryFont;

        [Header("Spacing (in pixels)")]
        public float spacingXS = 4f;
        public float spacingS = 8f;
        public float spacingM = 16f;
        public float spacingL = 24f;
        public float spacingXL = 32f;
        public float spacingXXL = 48f;

        [Header("Border Radius")]
        public float radiusSmall = 4f;
        public float radiusMedium = 8f;
        public float radiusLarge = 16f;
        public float radiusRound = 9999f;

        [Header("Shadows")]
        public Color shadowColor = new Color(0f, 0f, 0f, 0.15f);
        public Vector2 shadowOffset = new Vector2(0f, 2f);

        /// <summary>
        /// Get button colors based on button type.
        /// </summary>
        public ButtonColors GetButtonColors(ButtonType type)
        {
            return type switch
            {
                ButtonType.Primary => new ButtonColors
                {
                    normal = primaryIndigo,
                    hover = primaryLight,
                    pressed = primaryDark,
                    disabled = neutralMediumGrey,
                    text = neutralWhite
                },
                ButtonType.Secondary => new ButtonColors
                {
                    normal = neutralWhite,
                    hover = neutralLightGrey,
                    pressed = neutralMediumGrey,
                    disabled = neutralLightGrey,
                    text = primaryIndigo
                },
                ButtonType.Accent => new ButtonColors
                {
                    normal = accentGold,
                    hover = accentGoldLight,
                    pressed = accentGoldMuted,
                    disabled = neutralMediumGrey,
                    text = neutralBlack
                },
                ButtonType.Danger => new ButtonColors
                {
                    normal = semanticError,
                    hover = new Color(0.898f, 0.224f, 0.208f, 1f),
                    pressed = new Color(0.827f, 0.184f, 0.184f, 1f),
                    disabled = neutralMediumGrey,
                    text = neutralWhite
                },
                _ => new ButtonColors
                {
                    normal = primaryIndigo,
                    hover = primaryLight,
                    pressed = primaryDark,
                    disabled = neutralMediumGrey,
                    text = neutralWhite
                }
            };
        }

        /// <summary>
        /// Get navigation icon colors based on selection state.
        /// </summary>
        public Color GetNavIconColor(bool isSelected)
        {
            return isSelected ? primaryIndigo : neutralDarkGrey;
        }

        /// <summary>
        /// Get notification badge color.
        /// </summary>
        public Color GetBadgeColor()
        {
            return accentGold;
        }
    }

    public enum ButtonType
    {
        Primary,
        Secondary,
        Accent,
        Danger
    }

    [System.Serializable]
    public struct ButtonColors
    {
        public Color normal;
        public Color hover;
        public Color pressed;
        public Color disabled;
        public Color text;
    }
}
