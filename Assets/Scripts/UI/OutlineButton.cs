using UnityEngine;
using UnityEngine.UI;

namespace Cornucopia.UI
{
    /// <summary>
    /// Adds an outline border to a button using a border image approach.
    /// Creates a border frame behind the button content.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class OutlineButton : MonoBehaviour
    {
        [SerializeField] private Color outlineColor = new Color(0.247f, 0.318f, 0.710f); // Indigo
        [SerializeField] private float outlineWidth = 3f;

        private GameObject _borderObject;
        private Image _borderImage;
        private Image _mainImage;

        private void Start()
        {
            CreateBorder();
            ApplyThemeColor();
        }

        private void CreateBorder()
        {
            _mainImage = GetComponent<Image>();

            // Create border as parent wrapper
            // We'll create 4 edge images to form a border frame

            // Top border
            CreateEdge("BorderTop",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -outlineWidth), Vector2.zero);

            // Bottom border
            CreateEdge("BorderBottom",
                new Vector2(0, 0), new Vector2(1, 0),
                Vector2.zero, new Vector2(0, outlineWidth));

            // Left border
            CreateEdge("BorderLeft",
                new Vector2(0, 0), new Vector2(0, 1),
                Vector2.zero, new Vector2(outlineWidth, 0));

            // Right border
            CreateEdge("BorderRight",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-outlineWidth, 0), Vector2.zero);
        }

        private void CreateEdge(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject edge = new GameObject(name);
            edge.transform.SetParent(transform, false);
            edge.transform.SetAsFirstSibling();

            RectTransform rect = edge.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image img = edge.AddComponent<Image>();
            img.color = outlineColor;
            img.raycastTarget = false;
        }

        private void ApplyThemeColor()
        {
            var theme = ThemeManager.Instance?.Theme;
            if (theme != null)
            {
                outlineColor = theme.primaryIndigo;

                // Update all border edges
                foreach (Transform child in transform)
                {
                    if (child.name.StartsWith("Border"))
                    {
                        var img = child.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = outlineColor;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the outline color.
        /// </summary>
        public void SetOutlineColor(Color color)
        {
            outlineColor = color;
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Border"))
                {
                    var img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = color;
                    }
                }
            }
        }
    }
}
