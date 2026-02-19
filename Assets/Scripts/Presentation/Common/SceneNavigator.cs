using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cornucopia.Presentation.Common
{
    /// <summary>
    /// Singleton that persists across scenes, replacing PlayerPrefs for transient inter-scene data.
    /// Persistent user data (login state, userId, userName, userEmail) should still use PlayerPrefs.
    /// </summary>
    public class SceneNavigator : MonoBehaviour
    {
        private static SceneNavigator _instance;
        public static SceneNavigator Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SceneNavigator");
                    _instance = go.AddComponent<SceneNavigator>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Model details (admin flow)
        public string SelectedPicLocation { get; set; }
        public string SelectedProductName { get; set; }
        public string SelectedModelName { get; set; }
        public string SelectedQuestion { get; set; }
        public int SelectedSent { get; set; }
        public int SelectedSave { get; set; }
        public int SelectedYes { get; set; }
        public int SelectedNo { get; set; }
        public string SelectedRating { get; set; }

        // User details (admin user management flow)
        public string SelectedUserName { get; set; }
        public string SelectedUserEmail { get; set; }
        public string SelectedUserId { get; set; }

        // User model flow
        public string SelectedModRating { get; set; }
        public string SelectedGivenAnswer { get; set; }
        public int SelectedModelSaved { get; set; }
        public string SelectedModelQuestion { get; set; }

        // AR flow
        public int ArNo { get; set; }

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

        public void NavigateTo(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Clears all transient scene data. Call when logging out or resetting state.
        /// </summary>
        public void ClearTransientData()
        {
            SelectedPicLocation = null;
            SelectedProductName = null;
            SelectedModelName = null;
            SelectedQuestion = null;
            SelectedSent = 0;
            SelectedSave = 0;
            SelectedYes = 0;
            SelectedNo = 0;
            SelectedRating = null;
            SelectedUserName = null;
            SelectedUserEmail = null;
            SelectedUserId = null;
            SelectedModRating = null;
            SelectedGivenAnswer = null;
            SelectedModelSaved = 0;
            SelectedModelQuestion = null;
            ArNo = 0;
        }
    }
}
