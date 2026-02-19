using UnityEngine;
using Lean.Touch;
using Cornucopia.Core.Models;

namespace Cornucopia.Presentation.AR
{
    /// <summary>
    /// Adds Lean Touch gesture components (pinch-scale, twist-rotate, drag-translate)
    /// to placed AR objects for intuitive manipulation.
    /// </summary>
    public class GestureController : MonoBehaviour
    {
        [SerializeField] private ARPlacementController placementController;

        [Header("Gesture Settings")]
        [SerializeField] private float minScale = 0.1f;
        [SerializeField] private float maxScale = 3.0f;

        public event System.Action<GameObject, ProductModel> OnObjectTapped;

        private void OnEnable()
        {
            if (placementController != null)
                placementController.OnProductPlaced += SetupGestures;
        }

        private void OnDisable()
        {
            if (placementController != null)
                placementController.OnProductPlaced -= SetupGestures;
        }

        private void SetupGestures(GameObject placedObject, ProductModel product)
        {
            if (placedObject == null) return;

            // Add selectable for tap detection
            var selectable = placedObject.GetComponent<LeanSelectableByFinger>();
            if (selectable == null)
                selectable = placedObject.AddComponent<LeanSelectableByFinger>();

            // Add pinch-to-scale
            var pinchScale = placedObject.GetComponent<LeanPinchScale>();
            if (pinchScale == null)
            {
                pinchScale = placedObject.AddComponent<LeanPinchScale>();
                pinchScale.Sensitivity = 1f;
            }

            // Add twist-to-rotate (Y axis)
            var twistRotate = placedObject.GetComponent<LeanTwistRotateAxis>();
            if (twistRotate == null)
            {
                twistRotate = placedObject.AddComponent<LeanTwistRotateAxis>();
                twistRotate.Axis = Vector3.up;
            }

            // Add drag-to-translate
            var dragTranslate = placedObject.GetComponent<LeanDragTranslate>();
            if (dragTranslate == null)
            {
                dragTranslate = placedObject.AddComponent<LeanDragTranslate>();
                dragTranslate.Sensitivity = 0.001f;
            }

            // Listen for tap (select) to fire OnObjectTapped
            selectable.OnSelectedFinger.AddListener(finger =>
            {
                OnObjectTapped?.Invoke(placedObject, product);
            });

            Debug.Log($"[GestureController] Gestures set up for {placedObject.name}");
        }
    }
}
