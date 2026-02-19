#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Cornucopia.UI;

namespace Cornucopia.UI.Editor
{
    /// <summary>
    /// Editor utility to create the new Home screen layout.
    /// Menu: Cornucopia > Create Home Screen
    /// </summary>
    public static class HomeScreenSetup
    {
        [MenuItem("Cornucopia/Apply Outline to Search Button")]
        public static void ApplyOutlineToSearchButton()
        {
            // Find SearchRoomButton in scene
            var searchButton = GameObject.Find("SearchRoomButton");
            if (searchButton == null)
            {
                Debug.LogError("[HomeScreenSetup] SearchRoomButton not found in scene.");
                return;
            }

            // Add OutlineButton if not present
            if (searchButton.GetComponent<OutlineButton>() == null)
            {
                searchButton.AddComponent<OutlineButton>();
                Debug.Log("[HomeScreenSetup] OutlineButton added to SearchRoomButton.");
                EditorUtility.SetDirty(searchButton);
            }
            else
            {
                Debug.Log("[HomeScreenSetup] OutlineButton already exists on SearchRoomButton.");
            }
        }

        [MenuItem("Cornucopia/Create Home Screen Layout")]
        public static void CreateHomeScreen()
        {
            // Find or create Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Create main container
            GameObject homeScreen = new GameObject("HomeScreen");
            homeScreen.transform.SetParent(canvas.transform, false);
            RectTransform homeRect = homeScreen.AddComponent<RectTransform>();
            homeRect.anchorMin = Vector2.zero;
            homeRect.anchorMax = Vector2.one;
            homeRect.offsetMin = Vector2.zero;
            homeRect.offsetMax = Vector2.zero;

            // Add background
            Image bg = homeScreen.AddComponent<Image>();
            bg.color = Color.white;

            // Create Welcome Section
            GameObject welcomeSection = CreateWelcomeSection(homeScreen.transform);

            // Create Main Actions Section
            GameObject actionsSection = CreateActionsSection(homeScreen.transform);

            // Create Stats Section
            GameObject statsSection = CreateStatsSection(homeScreen.transform);

            // Add HomeScreenController
            HomeScreenController controller = homeScreen.AddComponent<HomeScreenController>();

            // Wire up references (user needs to assign in inspector)
            Debug.Log("[HomeScreenSetup] Home screen layout created. Please assign references in HomeScreenController.");

            Selection.activeGameObject = homeScreen;
        }

        private static GameObject CreateWelcomeSection(Transform parent)
        {
            GameObject section = new GameObject("WelcomeSection");
            section.transform.SetParent(parent, false);
            RectTransform rect = section.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.75f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(32, 0);
            rect.offsetMax = new Vector2(-32, -48);

            // Welcome text
            GameObject welcomeGO = new GameObject("WelcomeText");
            welcomeGO.transform.SetParent(section.transform, false);
            RectTransform welcomeRect = welcomeGO.AddComponent<RectTransform>();
            welcomeRect.anchorMin = new Vector2(0, 0.6f);
            welcomeRect.anchorMax = new Vector2(1, 1);
            welcomeRect.offsetMin = Vector2.zero;
            welcomeRect.offsetMax = Vector2.zero;

            TextMeshProUGUI welcomeText = welcomeGO.AddComponent<TextMeshProUGUI>();
            welcomeText.text = "Welcome back,";
            welcomeText.fontSize = 18;
            welcomeText.color = new Color(0.459f, 0.459f, 0.459f); // Dark grey
            welcomeText.alignment = TextAlignmentOptions.Left;

            // Username text
            GameObject usernameGO = new GameObject("UsernameText");
            usernameGO.transform.SetParent(section.transform, false);
            RectTransform usernameRect = usernameGO.AddComponent<RectTransform>();
            usernameRect.anchorMin = new Vector2(0, 0);
            usernameRect.anchorMax = new Vector2(1, 0.6f);
            usernameRect.offsetMin = Vector2.zero;
            usernameRect.offsetMax = Vector2.zero;

            TextMeshProUGUI usernameText = usernameGO.AddComponent<TextMeshProUGUI>();
            usernameText.text = "Explorer";
            usernameText.fontSize = 28;
            usernameText.fontStyle = FontStyles.Bold;
            usernameText.color = new Color(0.129f, 0.129f, 0.129f); // Black
            usernameText.alignment = TextAlignmentOptions.Left;

            return section;
        }

        private static GameObject CreateActionsSection(Transform parent)
        {
            GameObject section = new GameObject("ActionsSection");
            section.transform.SetParent(parent, false);
            RectTransform rect = section.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.3f);
            rect.anchorMax = new Vector2(1, 0.75f);
            rect.offsetMin = new Vector2(24, 0);
            rect.offsetMax = new Vector2(-24, 0);

            // Vertical layout
            VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(0, 0, 16, 16);

            // Scan Button (Primary - Indigo)
            CreateActionButton(section.transform, "ScanButton", "SCAN",
                new Color(0.247f, 0.318f, 0.710f), Color.white, 80);

            // Search Room Button (Secondary - White/Indigo border)
            CreateActionButton(section.transform, "SearchRoomButton", "SEARCH ROOM",
                Color.white, new Color(0.247f, 0.318f, 0.710f), 64, true);

            // Explore Button (Accent - Gold)
            CreateActionButton(section.transform, "ExploreButton", "EXPLORE COLLECTIBLES",
                new Color(1f, 0.843f, 0f), new Color(0.129f, 0.129f, 0.129f), 64);

            return section;
        }

        private static void CreateActionButton(Transform parent, string name, string text, Color bgColor, Color textColor, float height, bool isOutline = false)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            Image image = buttonGO.AddComponent<Image>();
            image.color = bgColor;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;

            Button button = buttonGO.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; // Use white for color tint mode
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            button.colors = colors;

            // Add outline for secondary button style
            if (isOutline)
            {
                buttonGO.AddComponent<OutlineButton>();
            }

            // Button text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 18;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.color = textColor;
            tmpText.alignment = TextAlignmentOptions.Center;
        }

        private static GameObject CreateStatsSection(Transform parent)
        {
            GameObject section = new GameObject("StatsSection");
            section.transform.SetParent(parent, false);
            RectTransform rect = section.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.15f);
            rect.anchorMax = new Vector2(1, 0.3f);
            rect.offsetMin = new Vector2(24, 0);
            rect.offsetMax = new Vector2(-24, 0);

            // Horizontal layout for stats cards
            HorizontalLayoutGroup layout = section.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            // Collectibles stat card
            CreateStatCard(section.transform, "CollectiblesCard", "COLLECTIBLES", "0");

            // New items stat card
            CreateStatCard(section.transform, "NewItemsCard", "NEW ITEMS", "0");

            return section;
        }

        private static void CreateStatCard(Transform parent, string name, string label, string value)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);

            RectTransform rect = cardGO.AddComponent<RectTransform>();

            Image bg = cardGO.AddComponent<Image>();
            bg.color = new Color(0.961f, 0.961f, 0.961f); // Light grey

            VerticalLayoutGroup layout = cardGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(16, 16, 16, 16);

            // Value text
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(cardGO.transform, false);
            TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 32;
            valueText.fontStyle = FontStyles.Bold;
            valueText.color = new Color(0.247f, 0.318f, 0.710f); // Indigo
            valueText.alignment = TextAlignmentOptions.Center;

            LayoutElement valueLayout = valueGO.AddComponent<LayoutElement>();
            valueLayout.preferredHeight = 40;

            // Label text
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(cardGO.transform, false);
            TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 12;
            labelText.color = new Color(0.459f, 0.459f, 0.459f); // Dark grey
            labelText.alignment = TextAlignmentOptions.Center;

            LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 20;
        }
    }
}
#endif
