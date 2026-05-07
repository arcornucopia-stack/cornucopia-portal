using EasyUI.Dialogs;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cornucopia.Core.Models;

public class allUserModel : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject game;
    public GameObject content;
    // string imagePath;
    string downloadPath;
    FirebaseStorage storage;
    StorageReference storageReference;
    int i;
    DataSnapshot snapshot;
    GameObject[] objectArray;
    string[] modelNameArray;
    int totalModels = 0;
    string Id;
    string[] picArray;
    string[] nameArray;
    Button[] btnsArray;
    string[] modelName;
    string[] ratingmod;
    string[] answer;
    string[] storagePathArray;
    bool[] savedArray;
    bool check = false;
    public GameObject notifyimage;
    public TMP_Text notify;
    // public TMP_Text errorr;
    void Start()
    {
        
        if (PlayerPrefs.GetInt("notifyCount") > 0)
        {
           
            notifyimage.SetActive(true);
            notify.text = "" + PlayerPrefs.GetInt("notifyCount");
        }
       
        Id = PlayerPrefs.GetString("userId");
        game.SetActive(false);
        storage = FirebaseStorage.DefaultInstance;
        storageReference = storage.RootReference;
        downloadPath = $"{Application.persistentDataPath}/Files/";
        getData();


    }

    async System.Threading.Tasks.Task getData()
    {

        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        var task = FirebaseDatabase.DefaultInstance
         .GetReference("cornucopia")
           .Child("users").Child(Id).Child("models")
           .GetValueAsync().ContinueWithOnMainThread(task => {
               if (task.IsFaulted)
               {
                   Debug.Log("error getting data");
                   snapshot = null;
                   DialogUI.Instance
                       .SetTitle("Error")
                       .SetMessage("Error getting data")
                       .SetButtonColor(DialogButtonColor.Black)
                       .OnClose(() => Debug.Log("Closed 1"))
                       .Show();
               }
               else if (task.IsCompleted)
               {
                   snapshot = task.Result;
                   Debug.Log("count:" + snapshot.ChildrenCount);
                   i = 0;
                   // Do something with snapshot...

                   totalModels = (int)snapshot.ChildrenCount;
                   modelNameArray = new string[snapshot.ChildrenCount];
                   savedArray = new bool[snapshot.ChildrenCount];
                   ratingmod = new string[snapshot.ChildrenCount];
                   answer = new string[snapshot.ChildrenCount];
                   foreach (DataSnapshot place in snapshot.Children)
                   {


                       Debug.Log("fetched Data: " + place.GetRawJsonValue());

                       LegacyModelRating m = JsonUtility.FromJson<LegacyModelRating>(place.GetRawJsonValue());
                       modelNameArray[i] = m.MName;
                       savedArray[i] = m.saved;
                       ratingmod[i] = m.Rating;
                       answer[i] = m.answer;
                       Debug.Log("name" + m.MName);
                       i++;
                   }

               }
           });

        await task;
        var task2 = FirebaseDatabase.DefaultInstance
            .GetReference("cornucopia")
              .Child("models")
              .GetValueAsync().ContinueWithOnMainThread(task => {
                  if (task.IsFaulted)
                  {
                      Debug.Log("error getting data");
                      snapshot = null;
                      DialogUI.Instance
                       .SetTitle("Error")
                       .SetMessage("Error getting data")
                       .SetButtonColor(DialogButtonColor.Black)
                       .OnClose(() => Debug.Log("Closed 1"))
                       .Show();
                  }
                  else if (task.IsCompleted)
                  {
                      snapshot = task.Result;
                      Debug.Log("count:" + snapshot.ChildrenCount);
                      i = 0;
                      // Do something with snapshot...
                      objectArray = new GameObject[totalModels];
                      picArray = new string[totalModels];
                      nameArray = new string[totalModels];
                      btnsArray = new Button[totalModels];
                      modelName = new string[totalModels];
                      storagePathArray = new string[totalModels];
                      foreach (DataSnapshot place in snapshot.Children)
                      {
                          check = false;

                          Debug.Log("fetched Data: " + place.GetRawJsonValue());

                          LegacyModel m = JsonUtility.FromJson<LegacyModel>(place.GetRawJsonValue());
                          for (int z = 0; z < totalModels; z++)
                          {

                              if (modelNameArray[z].Equals(m.modelNamee) && savedArray[z])
                              {
                                  check = true;

                              }


                          }

                          if (check)
                          {
                              game.SetActive(true);
                              if (i == 0)
                              {
                                  objectArray[i] = game;

                                  btnsArray[i] = objectArray[i].GetComponent<Button>();
                                  int x = i;
                                  btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });
                              }

                              else
                              {
                                  objectArray[i] = Instantiate(game, content.transform);
                                  btnsArray[i] = objectArray[i].GetComponent<Button>();
                                  int x = i;
                                  btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });
                              }

                              picArray[i] = m.picPathh;
                              nameArray[i] = m.name;
                              modelName[i] = m.modelNamee;
                              storagePathArray[i] = m.storagePath ?? "";

                              Debug.Log("name" + m.name);
                              i++;
                          }
                          else
                          {
                              check = false;

                          }
                      }
                      if (totalModels > 0)
                          DownloadFileAsync();
                      else

                      {
                          game.SetActive(true);

                          GameObject textChild = game.transform.GetChild(1).gameObject;
                          textChild.GetComponent<TMP_Text>().text = "No Model";
                         
                          game.transform.GetChild(0).gameObject.SetActive(false);

                      }
                  }
              });

    }
    void btnClicks(int value)
    {
        PlayerPrefs.SetString("picLocation", $"{Application.persistentDataPath}/Files/" + picArray[value] + ".png");
        PlayerPrefs.SetString("productName", nameArray[value].Replace(".glb", ""));
        PlayerPrefs.SetString("modelName", modelName[value] + ".glb");
        PlayerPrefs.SetString("modelStoragePath", storagePathArray[value]);
        PlayerPrefs.SetString("modRating", ratingmod[value]);
        PlayerPrefs.SetString("givenAnswer", answer[value]);
        if (savedArray[value])
        PlayerPrefs.SetInt("modelSaved",1);
        else
        PlayerPrefs.SetInt("modelSaved", 0);
        SceneManager.LoadScene("UserModelDetails");
    }
    async System.Threading.Tasks.Task DownloadFileAsync()
    {
        for (int z = 0; z < i; z++)
        {
            Debug.Log("z=" + z);
            downloadPath = $"{Application.persistentDataPath}/Files/" + picArray[z] + ".png";

            // Set name text immediately regardless of thumbnail outcome
            var tmp = objectArray[z].GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = nameArray[z].Replace(".glb", "");

            // Disable all animators (loading spinners) at any depth
            foreach (var anim in objectArray[z].GetComponentsInChildren<Animator>(true))
                anim.enabled = false;

            var rawImg = objectArray[z].GetComponentInChildren<RawImage>();

            if (File.Exists(downloadPath))
            {
                Debug.Log("Found the same file locally, Loading!!!");
                if (rawImg != null) rawImg.texture = GetImage(downloadPath);
            }
            else if (!string.IsNullOrEmpty(picArray[z]))
            {
                StorageReference gsReference =
                 storage.GetReferenceFromUrl("gs://cornucopia-54b02.appspot.com/pics/" + picArray[z] + ".png");

                int capturedZ = z;
                string capturedPath = downloadPath;
                var task = gsReference.GetFileAsync(capturedPath).ContinueWithOnMainThread(task => {
                    if (!task.IsFaulted && !task.IsCanceled)
                    {
                        var ri = objectArray[capturedZ].GetComponentInChildren<RawImage>();
                        if (ri != null) ri.texture = GetImage(capturedPath);
                    }
                });
                await task;

            }

        }
    }

    public void backClick()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Admin");
    }
  

    // get texture stored in device if exists, if doesn't exists, return null
    private static Texture2D GetImage(string fileName)
    {


        try
        {
            //first check if texture exists , if exists, start fetching
            if (File.Exists(fileName))
            {
                byte[] bytes = File.ReadAllBytes(fileName);
                Texture2D texture = new Texture2D(1, 1);
                texture.LoadImage(bytes);
                Debug.Log("done");
                return texture;
            }
            Debug.Log("not");
            return null; // texture not found so return null
        }
        catch (Exception e)
        {
            Debug.Log("errorrrrr");
            Debug.Log(e);
            return null;
        }
    }
    public void homeClick()
    {
        SceneManager.LoadScene("Home");
    }
    public void profileClick()
    {
        SceneManager.LoadScene("Profile");
    }
    public void notificationClick()
    {
        SceneManager.LoadScene("Notification");
    }
}
