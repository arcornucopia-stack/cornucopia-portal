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

    void Awake()
    {
        //capture.SetActive(false);
        m_RaycastManager = GetComponent<ARRaycastManager>();
        userInterface.SetActive(false);
        progressBar.SetActive(true);
        url = "gs://cornucopia-54b02.appspot.com/model/" + PlayerPrefs.GetString("modelName");
        filePath = $"{Application.persistentDataPath}/Files/models/"+ PlayerPrefs.GetString("modelName");
        DownloadFileAsync(url);
       
    }



    public void DownloadFileAsync(string url)
    {
        string path = filePath;      //GetFilePath(url); 

        if (File.Exists(path))
        {
            Debug.Log("Found the same file locally, Loading!!!");
            userInterface.SetActive(true);
            progressBar.SetActive(false);
            LoadModel(path);

            return;
        }
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        StorageReference gsReference =storage.GetReferenceFromUrl(url);
        

        // Start downloading a file
        Task task = gsReference.GetFileAsync(path,
            new StorageProgress<DownloadState>(state => {
        // called periodically during the download
        Debug.Log(String.Format(
                    "Progress: {0} of {1} bytes transferred.",
                    state.BytesTransferred,
                    state.TotalByteCount
                ));
                pb.BarValue = (int)(Convert.ToDouble(state.BytesTransferred) / Convert.ToDouble(state.TotalByteCount) * 100);
            }), CancellationToken.None);

        task.ContinueWithOnMainThread(resultTask => {
            if (!resultTask.IsFaulted && !resultTask.IsCanceled)
            {
                progressBar.SetActive(false);
                userInterface.SetActive(true);
                Debug.Log("Download finished.");
                LoadModel(path);
            }
        });
   
    }
    void LoadModel(string path)
    {
        AnimationClip[] animClips;
     //   errors.text = "abc";
        GameObject model = Importer.LoadFromFile(path, new ImportSettings(), out animClips);
     //   errors.text ="aaaa"+ model.name;
        model.transform.SetParent(m_PlacedPrefab.transform);
       // mp = m_PlacedPrefab;
       
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
                if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = s_Hits[0].pose;

                    if (m_NumberOfPlacedObjects < m_MaxNumberOfObjectsToPlace)
                    {
                        m_PlacedPrefab.SetActive(true);
                        spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);
                        m_PlacedPrefab.SetActive(false);
                        m_NumberOfPlacedObjects++;
                        capture.SetActive(true);
                        mtitleText.text = PlayerPrefs.GetString("productName");
                        userInterface.SetActive(false);
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
