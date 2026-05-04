using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace Cornucopia.UI
{
    /// <summary>
    /// Manages the footer navigation bar with themed icons.
    /// Handles selection states and navigation between scenes.
    /// </summary>
    public class FooterNavigation : MonoBehaviour, IPointerClickHandler
    {
        [Header("Navigation Icons")]
        [SerializeField] private Image homeIcon;
        [SerializeField] private Image notificationIcon;
        [SerializeField] private Image collectiblesIcon;
        [SerializeField] private Image profileIcon;

        [Header("Notification Badge")]
        [SerializeField] private GameObject notificationBadge;
        [SerializeField] private TMPro.TMP_Text badgeCountText;

        [Header("Scene Names")]
        [SerializeField] private string homeScene = "Home";
        [SerializeField] private string notificationScene = "Notification";
        [SerializeField] private string collectiblesScene = "Collectibles";
        [SerializeField] private string profileScene = "Profile";

        private NavItem _currentSelection = NavItem.Home;

        public enum NavItem
        {
            Home,
            Notification,
            Collectibles,
            Profile
        }

        private void Start()
        {
            // Determine current scene and set selection
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene.Contains("Home"))
                _currentSelection = NavItem.Home;
            else if (currentScene.Contains("Notification"))
                _currentSelection = NavItem.Notification;
            else if (currentScene.Contains("Collectibles"))
                _currentSelection = NavItem.Collectibles;
            else if (currentScene.Contains("Profile"))
                _currentSelection = NavItem.Profile;

            // Ensure footer image receives raycasts
            var img = GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            UpdateNavVisuals();
            UpdateNotificationBadge();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var rt = GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, eventData.position, eventData.pressEventCamera, out Vector2 local);

            float normalizedX = (local.x + rt.rect.width * 0.5f) / rt.rect.width;

            if (normalizedX < 0.25f)       OnHomeClick();
            else if (normalizedX < 0.5f)   OnNotificationClick();
            else if (normalizedX < 0.75f)  OnCollectiblesClick();
            else                           OnProfileClick();
        }

        /// <summary>
        /// Update all navigation icon colors based on current selection.
        /// </summary>
        public void UpdateNavVisuals()
        {
            var theme = ThemeManager.Instance?.Theme;
            if (theme == null) return;

            // Apply colors based on selection state
            ApplyIconColor(homeIcon, _currentSelection == NavItem.Home, theme);
            ApplyIconColor(notificationIcon, _currentSelection == NavItem.Notification, theme);
            ApplyIconColor(collectiblesIcon, _currentSelection == NavItem.Collectibles, theme);
            ApplyIconColor(profileIcon, _currentSelection == NavItem.Profile, theme);
        }

        private void ApplyIconColor(Image icon, bool isSelected, CornucopiaTheme theme)
        {
            if (icon == null) return;
            icon.color = isSelected ? theme.primaryIndigo : theme.neutralDarkGrey;
        }

        /// <summary>
        /// Update notification badge visibility and count.
        /// </summary>
        public void UpdateNotificationBadge()
        {
            int count = PlayerPrefs.GetInt("notifyCount", 0);

            if (notificationBadge != null)
            {
                notificationBadge.SetActive(count > 0);
            }

            if (badgeCountText != null && count > 0)
            {
                badgeCountText.text = count > 99 ? "99+" : count.ToString();

                // Apply gold badge color
                var theme = ThemeManager.Instance?.Theme;
                if (theme != null)
                {
                    var badgeImage = notificationBadge?.GetComponent<Image>();
                    if (badgeImage != null)
                    {
                        badgeImage.color = theme.accentGold;
                    }
                    badgeCountText.color = theme.neutralBlack;
                }
            }
        }

        /// <summary>
        /// Navigate to Home scene.
        /// </summary>
        public void OnHomeClick()
        {
            if (_currentSelection != NavItem.Home)
            {
                SceneManager.LoadScene(homeScene);
            }
        }

        /// <summary>
        /// Navigate to Notification scene.
        /// </summary>
        public void OnNotificationClick()
        {
            if (_currentSelection != NavItem.Notification)
            {
                SceneManager.LoadScene(notificationScene);
            }
        }

        /// <summary>
        /// Navigate to Collectibles scene.
        /// </summary>
        public void OnCollectiblesClick()
        {
            if (_currentSelection != NavItem.Collectibles)
            {
                SceneManager.LoadScene(collectiblesScene);
            }
        }

        /// <summary>
        /// Navigate to Profile scene.
        /// </summary>
        public void OnProfileClick()
        {
            if (_currentSelection != NavItem.Profile)
            {
                SceneManager.LoadScene(profileScene);
            }
        }

        /// <summary>
        /// Set the current selection programmatically.
        /// </summary>
        public void SetSelection(NavItem item)
        {
            _currentSelection = item;
            UpdateNavVisuals();
        }
    }
}
