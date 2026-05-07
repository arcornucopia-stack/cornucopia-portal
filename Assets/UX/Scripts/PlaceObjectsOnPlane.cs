using Siccity.GLTFUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Firebase.Storage;
using Firebase.Extensions;
using System.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceObjectsOnPlane : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the touch location.")]
    GameObject m_PlacedPrefab;
    string url;
    string filePath;
    public GameObject userInterface;
    public ProgressBarCircle pb;
    public GameObject progressBar;
   // public TMP_Text errors;
    GameObject mp;
    public GameObject capture;
    public TMP_Text mtitleText;
    [Header("Navigation")]
    [SerializeField] string m_HomeSceneName = "Home";
    [SerializeField] bool m_ShowDebugBackButton = false;
    /// <summary>
    /// The prefab to instantiate on touch.
    /// </summary>
    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }

    /// <summary>
    /// The object instantiated as a result of a successful raycast intersection with a plane.
    /// </summary>
    public GameObject spawnedObject { get; private set; }

    /// <summary>
    /// Invoked whenever an object is placed in on a plane.
    /// </summary>
    public static event Action onPlacedObject;

    ARRaycastManager m_RaycastManager;

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    
    [SerializeField]
    int m_MaxNumberOfObjectsToPlace = 1;

    int m_NumberOfPlacedObjects = 0;

    [SerializeField]
    bool m_CanReposition = true;

    public bool canReposition
    {
        get => m_CanReposition;
        set => m_CanReposition = value;
    }

    void Start()
    {
        // Hide the original scene back button to avoid duplicate
        var origBtn = GameObject.Find("Button");
        if (origBtn != null) origBtn.SetActive(false);

        // Disable UIManager — its "Tap to Place" / "Wait for tracking" overlay
        // conflicts with our own status bar
        var uiMgr = FindObjectOfType<UIManager>();
        if (uiMgr != null) uiMgr.enabled = false;

        // Hide progress bar immediately — show status text instead
        if (progressBar != null) progressBar.SetActive(false);
        UpdateStatusText("Downloading model...");

        CreateBackButton();

        // Disable raycast on the instructional overlay so taps pass through to AR
        if (userInterface != null)
        {
            foreach (var graphic in userInterface.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphic.raycastTarget = false;
        }
    }

    void CreateBackButton()
    {
        // Find the screen-space overlay canvas
        UnityEngine.Canvas targetCanvas = null;
        foreach (var c in FindObjectsOfType<UnityEngine.Canvas>())
        {
            if (c.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay)
            { targetCanvas = c; break; }
        }
        if (targetCanvas == null) targetCanvas = FindObjectOfType<UnityEngine.Canvas>();
        if (targetCanvas == null) return;

        // Create a dedicated overlay canvas for the back button so it's always on top
        var overlayCanvas = new GameObject("BackButtonCanvas").AddComponent<UnityEngine.Canvas>();
        overlayCanvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999;
        overlayCanvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        overlayCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var btnGo = new GameObject("BackButton_AR", typeof(RectTransform));
        btnGo.transform.SetParent(overlayCanvas.transform, false);
        btnGo.layer = 5;

        // Large touch target — safe area top-left
        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(16, -40);
        rect.sizeDelta = new Vector2(160, 70);

        var img = btnGo.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.05f, 0.05f, 0.05f, 0.72f);

        var btn = btnGo.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(BackToHome);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(btnGo.transform, false);
        textGo.layer = 5;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "← Back";
        tmp.fontSize = 22;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
    }

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        if (userInterface != null) userInterface.SetActive(false);
        if (progressBar != null) progressBar.SetActive(false);

        string modelName = PlayerPrefs.GetString("modelName");
        _storagePath = PlayerPrefs.GetString("modelStoragePath", "");

        if (string.IsNullOrEmpty(_storagePath))
            _storagePath = "model/" + modelName;

        string dir = $"{Application.persistentDataPath}/Files/models/";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        filePath = dir + modelName;

        StartCoroutine(DownloadWithTimeout(filePath));
    }

    private string _storagePath;

    IEnumerator DownloadWithTimeout(string path)
    {
        float elapsed = 0f;
        bool done = false;

        DownloadFileAsync(path, () => done = true);

        while (!done && elapsed < 20f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!done)
        {
            Debug.LogWarning("[AR] Download timed out after 20s — showing UI anyway.");
            if (pb != null) pb.BarValue = 100;
            if (progressBar != null) progressBar.SetActive(false);
            if (userInterface != null) userInterface.SetActive(true);
            UpdateStatusText("Download timed out. Tap a surface to try placing.");
        }
    }



    public void DownloadFileAsync(string path, System.Action onComplete = null)
    {
        if (File.Exists(path))
        {
            byte[] header = new byte[4];
            using (var fs = File.OpenRead(path)) fs.Read(header, 0, 4);
            bool validGlb = header[0] == 0x67 && header[1] == 0x6C && header[2] == 0x54 && header[3] == 0x46;

            if (validGlb)
            {
                Debug.Log("[AR] Found valid cached file, Loading...");
                if (userInterface != null) userInterface.SetActive(true);
                if (progressBar != null) progressBar.SetActive(false);
                LoadModel(path);
                onComplete?.Invoke();
                return;
            }
            else
            {
                Debug.LogWarning("[AR] Cached file invalid, re-downloading.");
                File.Delete(path);
            }
        }

        // Use GetReference with path (handles spaces/special chars) instead of URL
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        StorageReference gsReference = storage.GetReference(_storagePath);
        Debug.Log($"[AR] Downloading from path: {_storagePath}");

        Task task = gsReference.GetFileAsync(path, null, CancellationToken.None);

        task.ContinueWithOnMainThread(resultTask => {
            try { if (pb != null) pb.BarValue = 100; } catch { }
            if (progressBar != null) progressBar.SetActive(false);
            if (userInterface != null) userInterface.SetActive(true);
            if (!resultTask.IsFaulted && !resultTask.IsCanceled)
            {
                Debug.Log("[AR] Download finished.");
                LoadModel(path);
                UpdateStatusText("Move phone slowly over a flat table or floor");
            }
            else
            {
                Debug.LogError("[AR] Download failed: " + (resultTask.Exception?.Message ?? "unknown"));
                UpdateStatusText("Model load failed - move phone to scan surface");
            }
            onComplete?.Invoke();
        });
   
    }
    void LoadModel(string path)
    {
        try
        {
            AnimationClip[] animClips;
            GameObject model = Importer.LoadFromFile(path, new ImportSettings(), out animClips);
            if (model == null)
            {
                Debug.LogError("[PlaceObjects] Importer returned null for: " + path);
                return;
            }
            if (m_PlacedPrefab != null)
                model.transform.SetParent(m_PlacedPrefab.transform);
            Debug.Log("[PlaceObjects] Model loaded successfully: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("[PlaceObjects] Exception loading GLB — file may be corrupt. Deleting cache. " + e.Message);
            if (File.Exists(path)) File.Delete(path);
        }
    }
    void OnFinishAsync(GameObject result)
    {
        Debug.Log("Finished importing " + result.name);
    }
    IEnumerator GetFileRequest(string url, Action<UnityWebRequest> callback)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerFile(filePath);

            yield return req.SendWebRequest();

            callback(req);

        }
    }



    private TMPro.TextMeshProUGUI _statusText;

    void UpdateStatusText(string msg)
    {
        if (_statusText == null)
        {
            var canvases = FindObjectsOfType<UnityEngine.Canvas>();
            UnityEngine.Canvas c = null;
            foreach (var cv in canvases)
                if (cv.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay) { c = cv; break; }
            if (c == null && canvases.Length > 0) c = canvases[0];
            if (c != null)
            {
                var go = new GameObject("ARStatus", typeof(RectTransform));
                go.transform.SetParent(c.transform, false);
                go.layer = 5;
                var r = go.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0);
                r.pivot = new Vector2(0.5f, 0); r.anchoredPosition = new Vector2(0, 100);
                r.sizeDelta = new Vector2(0, 60);
                _statusText = go.AddComponent<TMPro.TextMeshProUGUI>();
                _statusText.fontSize = 18; _statusText.color = Color.white;
                _statusText.alignment = TMPro.TextAlignmentOptions.Center;
                _statusText.fontStyle = TMPro.FontStyles.Bold;
                // Add background
                go.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.5f);
            }
        }
        if (_statusText != null) _statusText.text = msg;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { BackToHome(); return; }

        if (m_PlacedPrefab == null) { UpdateStatusText("Prefab missing"); return; }
        m_PlacedPrefab.SetActive(false);

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log($"[AR] Touch at {touch.position}");
                // Only skip if the named back button is touched
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    Debug.Log("[AR] Touch blocked by UI element");
                    return;
                }
                bool hit = m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon);
                Debug.Log($"[AR] Raycast result: {hit}, planes: {s_Hits.Count}");
                UpdateStatusText(hit ? "Surface found - placing..." : "No surface yet. Point at a flat table or floor.");

                if (hit)
                {
                    Pose hitPose = s_Hits[0].pose;

                    if (m_NumberOfPlacedObjects < m_MaxNumberOfObjectsToPlace)
                    {
                        m_PlacedPrefab.SetActive(true);
                        spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);
                        spawnedObject.SetActive(true);
                        m_PlacedPrefab.SetActive(false);
                        m_NumberOfPlacedObjects++;
                        if (capture != null) capture.SetActive(true);
                        if (mtitleText != null) mtitleText.text = PlayerPrefs.GetString("productName");
                        if (userInterface != null) userInterface.SetActive(false);
                        UpdateStatusText("Placed! Tap again to move.");
                        Debug.Log("[PlaceObjects] Object placed at " + hitPose.position);
                    }
                    else
                    {
                        if (m_CanReposition)
                        {
                            spawnedObject.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                        }
                    }
                    
                    if (onPlacedObject != null)
                    {
                        onPlacedObject();
                    }
                }
            }
        }
    }

    public void BackToHome()
    {
        if (Application.CanStreamedLevelBeLoaded(m_HomeSceneName))
        {
            SceneManager.LoadScene(m_HomeSceneName);
        }
        else
        {
            Debug.LogError($"[PlaceObjectsOnPlane] Home scene '{m_HomeSceneName}' is not in Build Settings.");
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying || !m_ShowDebugBackButton) return;

        const int buttonWidth = 120;
        const int buttonHeight = 50;
        Rect rect = new Rect(20, 20, buttonWidth, buttonHeight);
        if (GUI.Button(rect, "< Back"))
        {
            BackToHome();
        }
    }
}
