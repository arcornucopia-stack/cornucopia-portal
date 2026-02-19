//Script for loading 3D Models(GLTF) from Firebase into Unity at runtime using GLTF Model Loader

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO; 
using UnityEngine;
using UnityEngine.Networking;
using Siccity.GLTFUtility;
using Firebase;
using Firebase.Extensions;
using Firebase.Storage;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase.Database;
using TMPro;
using EasyUI.Dialogs;
using Cornucopia.Core.Models;

public class FirebaseModelLoader : MonoBehaviour
{
    string filePath;
    string finalpath;
    string picPath;
    public GameObject panel;
    public GameObject spaceShip;
    string url;               //URL of the 3D model stored in Firebase Storage 
    public ProgressBarCircle pb;
    public Text modelName;
    public Text picName;
    public TMP_InputField que;
    FirebaseStorage storage;
    StorageReference storageReference;
    StorageReference spaceShipModel;
    String timezone;
    public GameObject uploadBtn;
    int slectedm = 0;
    int slectedp = 0;
    public GameObject backbutn;
    // public TMP_Text errors;
    // Start is called before the first frame update
    void Start()
    {
        //Application.persistDataPath stores the data at runtime to a specific location as per the platform
        //More details at:- https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html

        filePath = $"{Application.persistentDataPath}/Files/Chair.glb";
        panel.SetActive(false);
        uploadBtn.SetActive(false);

        //initialize storage reference
        storage = FirebaseStorage.DefaultInstance;
       // storageReference = storage.GetReferenceFromUrl("Your Firebase Storage Bucket URL");

        spaceShipModel = storageReference.Child("Chair.glb");

    }

    // Update is called once per frame
    void Update()
    {
        if (slectedm == 1 && slectedp == 1)
        {
            uploadBtn.SetActive(true);
        }
        else
        {
            uploadBtn.SetActive(false);
        }
    }

    public void DownloadFile(string url)
    {
        string path = filePath;      //GetFilePath(url); 

        if (File.Exists(path))
        {
            Debug.Log("Found the same file locally, Loading!!!");

            LoadModel(path);

            return;
        }

        StartCoroutine(GetFileRequest(url, (UnityWebRequest req) =>
        {
            if (req.isNetworkError || req.isHttpError)
            {
                //Logging any errors that may happen
                Debug.Log($"{req.error} : {req.downloadHandler.text}");
            }

            else
            {
                //Save the model fetched from firebase into spaceShip 
                LoadModel(path);


            }
        }

        ));
    }

    string GetFilePath(string url)
    {
        string[] pieces = url.Split('/');
        string filename = pieces[pieces.Length - 1];

        return $"{filePath}{filename}";
    }

    void LoadModel(string path)
    {
        ResetSpaceShip();

        GameObject model = Importer.LoadFromFile(filePath);

        model.transform.SetParent(spaceShip.transform);
        SceneManager.LoadScene("UXManagerScene");
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

    void ResetSpaceShip()
    {
        if (spaceShip != null)
        {
            foreach (Transform trans in spaceShip.transform)
            {
                Destroy(trans.gameObject);
            }
        }

    }
    public void callFcn()
    {
        uploadmodel();
    }
    public void uploadmodel()
    {


        if (finalpath == "")
        {

        }
        else
        {
            string queText = "";
            if (que.text == null || que.text == "")
            {
                queText = "Would you use this product?";
            }
            else
            {
                queText = que.text;
            }
            panel.SetActive(true);
            backbutn.SetActive(false);
            FirebaseStorage storage = FirebaseStorage.DefaultInstance;
            StorageReference storageRef = storage.RootReference;

            DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
            // File located on disk
            //  string localFile = $"{Application.persistentDataPath}/Files/Desk.glb";
            String modelName = Path.GetFileName(finalpath).Replace(".glb", "") + timezone;
            String picSelect = Path.GetFileName(picPath).Replace(".png", "") + timezone;
            LegacyModel m = new LegacyModel(Path.GetFileName(finalpath), modelName, picSelect, queText);
            string json = JsonUtility.ToJson(m);
            LegacyModelData d = new LegacyModelData(0, 0, 0, 0, "0");
            string json2 = JsonUtility.ToJson(d);
            reference.Child("cornucopia").Child("models").Child(modelName).SetRawJsonValueAsync(json);
            reference.Child("cornucopia").Child("models").Child(modelName).Child("data").SetRawJsonValueAsync(json2);
            // Create a reference to the file you want to upload
            StorageReference riversRef = storageRef.Child("model/" + modelName + ".glb");
            StorageReference picRef = storageRef.Child("pics/" + picSelect + ".png");


            // Upload the model to the path "images/rivers.jpg"

            var task = riversRef
        .PutFileAsync("file://" + finalpath, null,
            new StorageProgress<UploadState>(state =>
            {
                // called periodically during the upload
                Debug.Log(String.Format("Progress: {0} of {1} bytes transferred.",
                        state.BytesTransferred, state.TotalByteCount));
                //updating progress bar value
                pb.BarValue = (int)(Convert.ToDouble(state.BytesTransferred) / Convert.ToDouble(state.TotalByteCount) * 100);

            }), CancellationToken.None, null);

            task.ContinueWithOnMainThread(resultTask =>
           {
               if (!resultTask.IsFaulted && !resultTask.IsCanceled)
               {
                   Debug.Log("Upload finished.");

                    //upload image
                    var task2 = picRef
              .PutFileAsync("file://" + picPath, null,
                  new StorageProgress<UploadState>(state =>
                  {
                       // called periodically during the upload
                       Debug.Log(String.Format("Progress: {0} of {1} bytes transferred.",
                                state.BytesTransferred, state.TotalByteCount));
                       //updating progress bar value
                       pb.BarValue = (int)(Convert.ToDouble(state.BytesTransferred) / Convert.ToDouble(state.TotalByteCount) * 100);

                  }), CancellationToken.None, null);

                   task2.ContinueWithOnMainThread(resultTask =>
                   {
                       if (!resultTask.IsFaulted && !resultTask.IsCanceled)
                       {
                           Debug.Log("Upload finished.");
                           panel.SetActive(false);
                           backbutn.SetActive(true);
                           DialogUI.Instance
                        .SetTitle("Finished")
                        .SetMessage("Upload Finished...")
                        .SetButtonColor(DialogButtonColor.Black)
                        .OnClose(() => Debug.Log("Closed 1"))
                        .Show();
                           slectedm = 0;
                           slectedp = 0;
                       }
                       else
                       {
                           Debug.Log("Upload failled");
                           panel.SetActive(false);
                           backbutn.SetActive(true);
                           DialogUI.Instance
                         .SetTitle("Finished")
                         .SetMessage("Upload Failed due to some errors...")
                         .SetButtonColor(DialogButtonColor.Black)
                         .OnClose(() => Debug.Log("Closed 1"))
                         .Show();
                           slectedm = 0;
                           slectedp = 0;
                       }
                   });

               }
               else
               {
                   Debug.Log("Upload failled");

                   panel.SetActive(false);
                   backbutn.SetActive(true);
                   DialogUI.Instance
                       .SetTitle("Finished")
                       .SetMessage("Upload Failed due to some errors...")
                       .SetButtonColor(DialogButtonColor.Black)
                       .OnClose(() => Debug.Log("Closed 1"))
                       .Show();
                   slectedm = 0;
                   slectedp = 0;
               }
           });




            Debug.Log("done");
        }
    }
    public void SelectFile()
    {
        //Set extension for selection model
        string FileType = NativeFilePicker.ConvertExtensionToFileType("bin");


        //picking model from diectory
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                finalpath = "";
                Debug.Log("Operation Cancelled File Picking");
            }
            else
            {
                timezone = System.DateTime.UtcNow.ToString("HH mm dd MMMM yyyy");
                timezone = timezone.Replace(" ", "");
                Debug.Log(timezone.Replace(" ", ""));
                finalpath = path;

                // name of file selected
                if (Path.GetFileName(finalpath).Contains(".glb"))
                {
                    modelName.text = Path.GetFileName(finalpath);
                    finalpath = path;
                    slectedm = 1;
                }
                else
                {
                    DialogUI.Instance
                         .SetTitle("Error")
                         .SetMessage("Only .glb Formats are supported...")
                         .SetButtonColor(DialogButtonColor.Black)
                         .OnClose(() => Debug.Log("Closed 1"))
                         .Show();
                    slectedm = 0;
                }


                //path of picked file
                Debug.Log("picked file:" + finalpath);
                //name of picked file 
                Debug.Log(Path.GetFileName(finalpath));

            }
        }, new string[] { FileType }
        );
    }
    public void SelectPic()
    {
        //Set extension for selection model
        string FileType = NativeFilePicker.ConvertExtensionToFileType("png");

        NativeFilePicker.RequestPermission();

        //picking model from diectory
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                picPath = "";
                Debug.Log("Operation Cancelled File Picking");
                slectedm = 0;
            }
            else
            {
                timezone = System.DateTime.UtcNow.ToString("HH mm dd MMMM yyyy");
                timezone = timezone.Replace(" ", "");
                Debug.Log(timezone.Replace(" ", ""));
                picPath = path;
                slectedp = 1;
                // name of file selected
                picName.text = Path.GetFileName(picPath);
                picPath = path;
                //path of picked file


            }
        }, new string[] { FileType }
        );

        Debug.Log("" + permission);
    }
    public void backClick()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Admin");
    }
}
