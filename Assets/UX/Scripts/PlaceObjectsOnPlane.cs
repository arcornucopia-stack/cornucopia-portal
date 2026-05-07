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
        CreateBackButton();
    }

    void CreateBackButton()
    {
        var canvas = FindObjectOfType<UnityEngine.Canvas>();
        if (canvas == null) return;

        var btnGo = new GameObject("BackButton", typeof(RectTransform));
        btnGo.transform.SetParent(canvas.transform, false);
        btnGo.layer = 5; // UI layer

        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(120, 50);

        var img = btnGo.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0.6f);

        var btn = btnGo.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(BackToHome);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(btnGo.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "← Back";
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
    }

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        if (userInterface != null) userInterface.SetActive(false);
        if (progressBar != null) progressBar.SetActive(true);

        string modelName = PlayerPrefs.GetString("modelName");
        string storagePath = PlayerPrefs.GetString("modelStoragePath", "");

        if (!string.IsNullOrEmpty(storagePath))
            url = "gs://cornucopia-54b02.appspot.com/" + storagePath;
        else
            url = "gs://cornucopia-54b02.appspot.com/model/" + modelName;

        string dir = $"{Application.persistentDataPath}/Files/models/";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        filePath = dir + modelName;

        DownloadFileAsync(url);
    }



    public void DownloadFileAsync(string url)
    {
        string path = filePath;      //GetFilePath(url); 

        if (File.Exists(path))
        {
            // Validate it's actually a GLB (magic bytes: "glTF")
            byte[] header = new byte[4];
            using (var fs = File.OpenRead(path)) fs.Read(header, 0, 4);
            bool validGlb = header[0] == 0x67 && header[1] == 0x6C && header[2] == 0x54 && header[3] == 0x46;

            if (validGlb)
            {
                Debug.Log("Found valid cached file, Loading!!!");
                if (userInterface != null) userInterface.SetActive(true);
                if (progressBar != null) progressBar.SetActive(false);
                LoadModel(path);
                return;
            }
            else
            {
                Debug.LogWarning("[PlaceObjects] Cached file is invalid, re-downloading.");
                File.Delete(path);
            }
        }
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        StorageReference gsReference =storage.GetReferenceFromUrl(url);
        

        // Start downloading a file
        Task task = gsReference.GetFileAsync(path, null, CancellationToken.None);

        task.ContinueWithOnMainThread(resultTask => {
            if (progressBar != null) progressBar.SetActive(false);
            if (userInterface != null) userInterface.SetActive(true);
            if (!resultTask.IsFaulted && !resultTask.IsCanceled)
            {
                Debug.Log("Download finished.");
                LoadModel(path);
            }
            else
            {
                Debug.LogError("[PlaceObjects] Download failed: " + (resultTask.Exception?.Message ?? "unknown"));
            }
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



    void Update()
    {
        // Always allow users to leave scan scene using device back key / Escape.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToHome();
            return;
        }

      //  errors.text = m_PlacedPrefab.transform.GetChild(0).name;
        m_PlacedPrefab.SetActive(false);
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                // Skip if touch is over UI (e.g. back button)
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return;
                if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
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
