using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using EasyUI.Dialogs;

namespace Cornucopia.UI
{
    /// <summary>
    /// Controller for the new Home screen matching UX brief.
    /// Entry hub with Scan, Search Room, and Explore Collectibles buttons.
    /// </summary>
    public class HomeScreenController : MonoBehaviour
    {
        [Header("Welcome Section")]
        [SerializeField] private TMP_Text welcomeText;
        [SerializeField] private TMP_Text usernameText;

        [Header("Main Action Buttons")]
        [SerializeField] private Button scanButton;
        [SerializeField] private Button searchRoomButton;
        [SerializeField] private Button exploreButton;

        [Header("Quick Stats")]
        [SerializeField] private TMP_Text collectiblesCountText;
        [SerializeField] private TMP_Text newItemsCountText;

        [Header("Notification Badge")]
        [SerializeField] private GameObject notificationBadge;
        [SerializeField] private TMP_Text notificationCountText;

        [Header("Scene References")]
        [SerializeField] private string arCameraScene = "UXManagerScene";
        [SerializeField] private string searchRoomScene = "SearchRoom";
        [SerializeField] private string exploreScene = "Collectibles";

        private string _userId;
        private int _totalCollectibles = 0;
        private int _newItems = 0;

        private void Start()
        {
            InitializeUI();
            LoadUserData();
            ApplyTheme();
        }

        private void InitializeUI()
        {
            // Set welcome message
            string userName = PlayerPrefs.GetString("userName", "Explorer");
            if (usernameText != null)
            {
                usernameText.text = userName;
            }
            if (welcomeText != null)
            {
                welcomeText.text = "Welcome back,";
            }

            _userId = PlayerPrefs.GetString("userId");

            // Setup button listeners
            if (scanButton != null)
                scanButton.onClick.AddListener(OnScanClick);
            if (searchRoomButton != null)
                searchRoomButton.onClick.AddListener(OnSearchRoomClick);
            if (exploreButton != null)
                exploreButton.onClick.AddListener(OnExploreClick);

            // Initialize notification
            PlayerPrefs.SetInt("notifyCount", 0);
            UpdateNotificationBadge(0);
        }

        private void ApplyTheme()
        {
            var theme = ThemeManager.Instance?.Theme;
            if (theme == null) return;

            // Apply primary button style to Scan (main CTA)
            if (scanButton != null)
            {
                ThemeManager.Instance.ApplyButtonStyle(scanButton, ButtonType.Primary);
            }

            // Apply accent (gold) button style to Explore
            if (exploreButton != null)
            {
                ThemeManager.Instance.ApplyButtonStyle(exploreButton, ButtonType.Accent);
            }

            // Apply secondary button style to Search Room
            if (searchRoomButton != null)
            {
                ThemeManager.Instance.ApplyButtonStyle(searchRoomButton, ButtonType.Secondary);
            }

            // Apply text colors
            if (welcomeText != null)
            {
                welcomeText.color = theme.neutralDarkGrey;
            }
            if (usernameText != null)
            {
                usernameText.color = theme.neutralBlack;
            }
        }

        private async void LoadUserData()
        {
            if (string.IsNullOrEmpty(_userId)) return;

            try
            {
                // Load user's collectibles count
                var userModelsTask = FirebaseDatabase.DefaultInstance
                    .GetReference("cornucopia")
                    .Child("users").Child(_userId).Child("models")
                    .GetValueAsync();

                await userModelsTask;
                if (userModelsTask.IsFaulted)
                {
                    string message = GetExceptionMessage(userModelsTask.Exception);
                    if (IsPermissionDenied(message))
                    {
                        Debug.LogWarning($"[HomeScreen] Permission denied loading user data: {message}");
                        SetStatsToDefaults();
                    }
                    else
                    {
                        Debug.LogError($"[HomeScreen] Failed loading user data: {message}");
                    }
                    return;
                }

                if (userModelsTask.IsCompleted && userModelsTask.Result != null)
                {
                    var snapshot = userModelsTask.Result;
                    _totalCollectibles = (int)snapshot.ChildrenCount;

                    if (collectiblesCountText != null)
                        collectiblesCountText.text = _totalCollectibles.ToString();

                    // Count new items (unsaved models)
                    await CheckForNewItems(snapshot);
                }
            }
            catch (System.Exception e)
            {
                string message = GetExceptionMessage(e);
                if (IsPermissionDenied(message))
                {
                    Debug.LogWarning($"[HomeScreen] Permission denied loading user data: {message}");
                    SetStatsToDefaults();
                }
                else
                {
                    Debug.LogError($"[HomeScreen] Error loading user data: {message}");
                }
            }
        }

        private async System.Threading.Tasks.Task CheckForNewItems(DataSnapshot userModelsSnapshot)
        {
            try
            {
                _newItems = 0;
                foreach (DataSnapshot userModel in userModelsSnapshot.Children)
                {
                    var raw = userModel.GetRawJsonValue();
                    if (string.IsNullOrEmpty(raw))
                        continue;

                    var modelData = JsonUtility.FromJson<UserModelData>(raw);
                    if (modelData != null && !modelData.saved)
                    {
                        _newItems++;
                    }
                }

                if (newItemsCountText != null)
                    newItemsCountText.text = _newItems.ToString();

                UpdateNotificationBadge(_newItems);
                PlayerPrefs.SetInt("notifyCount", _newItems);
                await System.Threading.Tasks.Task.CompletedTask;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HomeScreen] Error checking new items: {GetExceptionMessage(e)}");
            }
        }

        private void UpdateNotificationBadge(int count)
        {
            if (notificationBadge != null)
            {
                notificationBadge.SetActive(count > 0);
            }

            if (notificationCountText != null && count > 0)
            {
                notificationCountText.text = count > 99 ? "99+" : count.ToString();

                // Apply gold color to badge
                var theme = ThemeManager.Instance?.Theme;
                if (theme != null)
                {
                    var badgeImage = notificationBadge?.GetComponent<Image>();
                    if (badgeImage != null)
                    {
                        badgeImage.color = theme.accentGold;
                    }
                    notificationCountText.color = theme.neutralBlack;
                }
            }
        }

        /// <summary>
        /// Open AR camera for scanning collectibles.
        /// </summary>
        public void OnScanClick()
        {
            Debug.Log("[HomeScreen] Opening AR Scanner");
            if (Application.CanStreamedLevelBeLoaded(arCameraScene))
                SceneManager.LoadScene(arCameraScene);
            else if (Application.CanStreamedLevelBeLoaded("UXManagerScene"))
                SceneManager.LoadScene("UXManagerScene");
            else
                Debug.LogError($"[HomeScreen] AR scene not found. Tried '{arCameraScene}' and 'UXManagerScene'.");
        }

        /// <summary>
        /// Open room search/spatial scanning.
        /// </summary>
        public void OnSearchRoomClick()
        {
            Debug.Log("[HomeScreen] Opening Room Search");
            // TODO: Navigate to room search scene when implemented
            // SceneManager.LoadScene(searchRoomScene);

            if (DialogUI.Instance != null)
            {
                DialogUI.Instance
                    .SetTitle("Search Room")
                    .SetMessage("Room scanning feature coming soon!")
                    .SetButtonColor(DialogButtonColor.Blue)
                    .Show();
            }
            else
            {
                Debug.LogWarning("[HomeScreen] DialogUI instance not found. Search Room feature is coming soon.");
            }
        }

        /// <summary>
        /// Open collectibles/explore screen.
        /// </summary>
        public void OnExploreClick()
        {
            Debug.Log("[HomeScreen] Opening Explore");
            SceneManager.LoadScene(exploreScene);
        }

        /// <summary>
        /// Navigate to notifications.
        /// </summary>
        public void OnNotificationClick()
        {
            SceneManager.LoadScene("Notification");
        }

        /// <summary>
        /// Navigate to profile.
        /// </summary>
        public void OnProfileClick()
        {
            SceneManager.LoadScene("Profile");
        }

        [System.Serializable]
        private class UserModelData
        {
            public string MName;
            public bool saved;
            public string Rating;
        }

        private static string GetExceptionMessage(System.Exception exception)
        {
            if (exception == null) return "Unknown error";
            var current = exception;
            while (current.InnerException != null)
                current = current.InnerException;
            return current.Message;
        }

        private static bool IsPermissionDenied(string message)
        {
            return !string.IsNullOrEmpty(message) &&
                   message.ToLowerInvariant().Contains("does not have permission");
        }

        private void SetStatsToDefaults()
        {
            _totalCollectibles = 0;
            _newItems = 0;
            if (collectiblesCountText != null) collectiblesCountText.text = "0";
            if (newItemsCountText != null) newItemsCountText.text = "0";
            UpdateNotificationBadge(0);
            PlayerPrefs.SetInt("notifyCount", 0);
        }
    }
}
