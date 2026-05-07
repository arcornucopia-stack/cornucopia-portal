using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;
using System;
using EasyUI.Dialogs;
using Cornucopia.Core.Models;

public class notification : MonoBehaviour
{
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
    string[] questionArray;
    string[] nameArray;
    Button[] btnsArray;
    string[] modelName;
    string[] storagePathArray;
    string[] ratingmod;
    string[] raintgmodarr;
    bool[] savedArray;
    bool check = false;
    int b = 0;
    // public TMP_Text errorr;
    void Start()
    {
        game.SetActive(false);
        
        if (PlayerPrefs.GetInt("notifyCount") < 0)
        {

            game.SetActive(false);
        }
        else
        {
            Id = PlayerPrefs.GetString("userId");
            storage = FirebaseStorage.DefaultInstance;
            storageReference = storage.RootReference;
            downloadPath = $"{Application.persistentDataPath}/Files/";
            getData();
        }

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
                   foreach (DataSnapshot place in snapshot.Children)
                   {


                       Debug.Log("fetched Data: " + place.GetRawJsonValue());

                       LegacyModelRef m = JsonUtility.FromJson<LegacyModelRef>(place.GetRawJsonValue());
                       modelNameArray[i] = m.MName;
                       savedArray[i] = m.saved;
                       ratingmod[i] = m.Rating;
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
                      questionArray = new string[totalModels];
                      nameArray = new string[totalModels];
                      btnsArray = new Button[totalModels];
                      modelName = new string[totalModels];
                      storagePathArray = new string[totalModels];
                      raintgmodarr = new string[totalModels];
                      foreach (DataSnapshot place in snapshot.Children)
                      {
                          check = false;

                          Debug.Log("fetched Data: " + place.GetRawJsonValue());

                          LegacyModel m = JsonUtility.FromJson<LegacyModel>(place.GetRawJsonValue());
                          for (int z = 0; z < totalModels; z++)
                          {

                              if (modelNameArray[z].Equals(m.modelNamee) && !savedArray[z])
                              {
                                  check = true;
                                  b = z;

                              }


                          }

                          if (check)
                          {
                              game.SetActive(true);
                              
                              if (i == 0 )
                              {
                                  objectArray[i] = game;

                             //     btnsArray[i] = objectArray[i].GetComponent<Button>();
                             //     int x = i;
                             //     btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });
                              }

                              else 
                              {
                                  objectArray[i] = Instantiate(game, content.transform);
                             //     btnsArray[i] = objectArray[i].GetComponent<Button>();
                             //     int x = i;
                             //     btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });
                              }
                              raintgmodarr[i] = ratingmod[b];
                              picArray[i] = m.picPathh;
                              questionArray[i] = m.question;
                              nameArray[i] = m.name;
                              modelName[i] = m.modelNamee;
                              storagePathArray[i] = m.storagePath ?? "";

                              // Attach button listener immediately — thumbnail is cosmetic only
                              btnsArray[i] = objectArray[i].GetComponent<Button>();
                              int x = i;
                              btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });

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
                      if(i==0)
                      {
                          game.SetActive(false);
                      }
                  }
              });

    }
    void btnClicks(int value)
    {
        PlayerPrefs.SetString("picLocation", $"{Application.persistentDataPath}/Files/" + picArray[value] + ".png");
        PlayerPrefs.SetString("modelQuestion", questionArray[value]);
        PlayerPrefs.SetString("productName", nameArray[value].Replace(".glb", ""));
        PlayerPrefs.SetString("modelName", modelName[value] + ".glb");
        PlayerPrefs.SetString("modelStoragePath", storagePathArray[value]);
        PlayerPrefs.SetString("modRating", raintgmodarr[value]);
        PlayerPrefs.SetInt("modelSaved", 0);

        Debug.Log("notification wala" + PlayerPrefs.GetInt("modelSaved"));
        SceneManager.LoadScene("NotifyModel");
    }
    void SetCardImage(int index, string path)
    {
        if (objectArray == null || index >= objectArray.Length || objectArray[index] == null) return;
        // Hide spinner images
        foreach (var img in objectArray[index].GetComponentsInChildren<UnityEngine.UI.Image>(true))
            img.enabled = false;
        // Set name
        foreach (var t in objectArray[index].GetComponentsInChildren<TMPro.TMP_Text>(true))
            t.text = nameArray[index].Replace(".glb", "");
        // Set thumbnail if available
        var tex = GetImage(path);
        if (tex != null)
        {
            var rawImage = objectArray[index].GetComponentInChildren<UnityEngine.UI.RawImage>();
            if (rawImage != null) rawImage.texture = tex;
        }
    }

    async System.Threading.Tasks.Task DownloadFileAsync()
    {
        for (int z = 0; z < i; z++)
        {
            Debug.Log("z=" + z);
            //GetFilePath(url); 
            downloadPath = $"{Application.persistentDataPath}/Files/" + picArray[z] + ".png";

            if (File.Exists(downloadPath))
            {
                Debug.Log("Found the same file locally, Loading!!!");
                SetCardImage(z, downloadPath);
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
                        Debug.Log("Thumbnail downloaded: " + capturedPath);
                        SetCardImage(capturedZ, capturedPath);
                    }
                    else
                    {
                        Debug.Log("Thumbnail not found for " + picArray[capturedZ] + " — skipping.");
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
    public void CollectiblesClick()
    {
        SceneManager.LoadScene("Collectibles");
    }
}
