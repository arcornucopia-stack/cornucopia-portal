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
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cornucopia.Core.Models;

public class addUserModel : MonoBehaviour
{
    string Id;
    public GameObject game;
    public GameObject content;
    string downloadPath;
    FirebaseStorage storage;
    DataSnapshot snapshot;
    int i;
    GameObject[] objectArray;
    string[] modelNameArray;
    int totalModels=0;
    Button[] btnsArray;

    string[] picArray;
    string[] nameArray;
    string[] modName;
    bool check=false;

    //model data
    int[] sentArray;
    int[] saveArray;
    int[] yesArray;
    int[] noArray;
    string[] ratingArray;

    //user data
    int Usent;
    int Usaved;
    int Urate;
    // Start is called before the first frame update
    void Start()
    {
        Id = PlayerPrefs.GetString("Id");
        game.SetActive(false);
        
        storage = FirebaseStorage.DefaultInstance;
      
        downloadPath = $"{Application.persistentDataPath}/Files/";
        GetData();
    }

    async System.Threading.Tasks.Task GetData()
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;

        var task = FirebaseDatabase.DefaultInstance
         .GetReference("cornucopia")
           .Child("users").Child(Id).Child("models")
           .GetValueAsync().ContinueWithOnMainThread(task => {
               if (task.IsFaulted || task.IsCanceled)
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

                   foreach (DataSnapshot place in snapshot.Children)
                   {


                       Debug.Log("fetched Data: " + place.GetRawJsonValue());

                       LegacyModelRating m = JsonUtility.FromJson<LegacyModelRating>(place.GetRawJsonValue());
                       modelNameArray[i] = m.MName;
                      
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
                      objectArray = new GameObject[snapshot.ChildrenCount - totalModels];
                      picArray = new string[snapshot.ChildrenCount-totalModels];
                      nameArray = new string[snapshot.ChildrenCount- totalModels];
                      btnsArray = new Button[snapshot.ChildrenCount- totalModels];
                      modName = new string[snapshot.ChildrenCount- totalModels];

                      // model data
                      sentArray=new int[snapshot.ChildrenCount - totalModels];
                      saveArray = new int[snapshot.ChildrenCount - totalModels];
                      yesArray = new int[snapshot.ChildrenCount - totalModels];
                      noArray = new int[snapshot.ChildrenCount - totalModels];
                      ratingArray = new string[snapshot.ChildrenCount - totalModels];


                      foreach (DataSnapshot place in snapshot.Children)
                      {

                          
                          Debug.Log("fetched Data: " + place.GetRawJsonValue());

                          LegacyModel m = JsonUtility.FromJson<LegacyModel>(place.GetRawJsonValue());
                          for(int z=0;z<totalModels; z++)
                          {
                              
                              if (modelNameArray[z].Equals(m.modelNamee))
                              {
                                  check = true;
                                  
                              }

                          }

                          if (!check)
                          {
                              game.SetActive(true);

                              if (i == 0)
                              {
                                  objectArray[i] = game;
                                  GameObject childObject = objectArray[i].transform.GetChild(1).gameObject;
                                  btnsArray[i] = childObject.transform.GetChild(6).gameObject.GetComponent<Button>();
                                  int x = i;
                                  btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });
                              }

                              else
                              {
                                  objectArray[i] = Instantiate(game, content.transform);
                                  GameObject childObject = objectArray[i].transform.GetChild(1).gameObject;
                                  btnsArray[i] = childObject.transform.GetChild(6).gameObject.GetComponent<Button>();
                                  int x = i;
                                  btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });
                              }


                              picArray[i] = m.picPathh;
                              nameArray[i] = m.name;
                              modName[i] = m.modelNamee;

                              Debug.Log("name" + m.name);
                              i++;
                          }
                          else
                          {
                              check = false;
                             
                          }

                      }
                      
                  }
              });
        await task2;
                                                    for(int b=0;b<i;b++)
                                                    {
                                                                    var task3 = FirebaseDatabase.DefaultInstance
                                                        .GetReference("cornucopia")
                                                        .Child("models").Child(modName[b]).Child("data")
                                                        .GetValueAsync().ContinueWithOnMainThread(task =>
                                                        {
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
                                                                LegacyModelData d = JsonUtility.FromJson<LegacyModelData>(snapshot.GetRawJsonValue());
                                                                sentArray[b] = d.sent;
                                                                saveArray[b] = d.saved;
                                                                yesArray[b] = d.yes;
                                                                noArray[b] = d.no;
                                                                ratingArray[b] = d.rating;

                                                            }
                                                        });
                                                await task3;

                                                     }
        var task4 = FirebaseDatabase.DefaultInstance
          .GetReference("cornucopia")
           .Child("users").Child(Id).Child("userData")
          .GetValueAsync().ContinueWithOnMainThread(task =>
          {
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
                  LegacyUserData ud = JsonUtility.FromJson<LegacyUserData>(snapshot.GetRawJsonValue());
                  Usent = ud.sent;
                 Usaved = ud.saved;              
                  Urate = ud.rated;

              }
          });
        await task4;
        if (snapshot.ChildrenCount - totalModels > 0)
            DownloadFileAsync();
        else

        {
            game.SetActive(true);
            GameObject textChild = game.transform.GetChild(0).gameObject;
            textChild.transform.GetChild(1).gameObject. GetComponent<TMP_Text>().text = "No Model";
            game.transform.GetChild(1).gameObject.SetActive(false);
            game.transform.GetChild(2).gameObject.SetActive(false);
            game.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.SetActive(false);

        }
    }

   void btnClicks(int value)
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        LegacyModelRating m = new LegacyModelRating("0", modName[value], false, "pending");
        string json = JsonUtility.ToJson(m);
        Debug.Log("click" + value);
        Debug.Log("valuews:" + json);
       var task= reference.Child("cornucopia").Child("users").Child(Id).Child("models").Child(modName[value]).SetRawJsonValueAsync(json);

        LegacyModelData d = new LegacyModelData(sentArray[value]+1,saveArray[value], yesArray[value], noArray[value], ratingArray[value]);
        string json2 = JsonUtility.ToJson(d);
        Debug.Log("valuews:" + json2);
        var task2 = reference.Child("cornucopia").Child("models").Child(modName[value]).Child("data").SetRawJsonValueAsync(json2);

        LegacyUserData ud = new LegacyUserData(Usent+1, Usaved,Urate);
        string json3 = JsonUtility.ToJson(ud);
        Debug.Log("valuews:" + json3);
        var task3 = reference.Child("cornucopia").Child("users").Child(Id).Child("userData").SetRawJsonValueAsync(json3);
        //  await task;
        SceneManager.LoadScene("addUserModel");
    }




    async System.Threading.Tasks.Task DownloadFileAsync()
    {
        if (i == 0)
        {
            GameObject childObject = game.transform.GetChild(0).gameObject;
            GameObject textChild = childObject.transform.GetChild(1).gameObject;
            textChild.GetComponent<TMP_Text>().text = "No model Exist";
            GameObject childObject2 = game.transform.GetChild(1).gameObject;
            childObject2.SetActive(false);
        }
        for (int z = 0; z < i; z++)
        {
            Debug.Log("z=" + z);
            //GetFilePath(url); 
            downloadPath = $"{Application.persistentDataPath}/Files/" + picArray[z] + ".png";

            if (File.Exists(downloadPath))
            {

                Debug.Log("Found the same file locally, Loading!!!");
                GameObject childObject = objectArray[z].transform.GetChild(0).gameObject;
                GameObject child =childObject.transform.GetChild(0).gameObject;
                child.GetComponent<RawImage>().texture = GetImage(downloadPath);
                GameObject textChild = childObject.transform.GetChild(1).gameObject;
                textChild.GetComponent<TMP_Text>().text = nameArray[z].Replace(".glb", "");

                GameObject dataParent = objectArray[z].transform.GetChild(1).gameObject;
                GameObject sentChild = dataParent.transform.GetChild(0).gameObject;
                sentChild.GetComponent<TMP_Text>().text = sentArray[z] + " sent";

                GameObject saveChild = dataParent.transform.GetChild(1).gameObject;
                saveChild.GetComponent<TMP_Text>().text = saveArray[z] + " save";

                GameObject yesChild = dataParent.transform.GetChild(2).gameObject;
                yesChild.GetComponent<TMP_Text>().text = yesArray[z] + " yes";

                GameObject noChild = dataParent.transform.GetChild(3).gameObject;
                noChild.GetComponent<TMP_Text>().text = noArray[z] + " no";

                GameObject ratingChild = dataParent.transform.GetChild(4).gameObject;
                ratingChild.GetComponent<TMP_Text>().text = ratingArray[z] + " rating";
            }
            else
            {
                StorageReference gsReference =
                 storage.GetReferenceFromUrl("gs://cornucopia-54b02.appspot.com/pics/" + picArray[z] + ".png");
       

                // Download to the local filesystem
                var task = gsReference.GetFileAsync(downloadPath).ContinueWithOnMainThread(task => {
                    if (!task.IsFaulted && !task.IsCanceled)
                    {
                        Debug.Log("File downloaded.");
                        Debug.Log(downloadPath);
                        GameObject childObject = objectArray[z].transform.GetChild(0).gameObject;
                        GameObject child = childObject.transform.GetChild(0).gameObject;
                        child.GetComponent<RawImage>().texture = GetImage(downloadPath);
                        GameObject textChild = childObject.transform.GetChild(1).gameObject;
                        textChild.GetComponent<TMP_Text>().text = nameArray[z].Replace(".glb", "");

                        GameObject dataParent = objectArray[z].transform.GetChild(1).gameObject;
                        GameObject sentChild = dataParent.transform.GetChild(0).gameObject;
                        sentChild.GetComponent<TMP_Text>().text = sentArray[z] + " sent";

                        GameObject saveChild = dataParent.transform.GetChild(1).gameObject;
                        saveChild.GetComponent<TMP_Text>().text = saveArray[z] + " save";

                        GameObject yesChild = dataParent.transform.GetChild(2).gameObject;
                        yesChild.GetComponent<TMP_Text>().text = yesArray[z] + " yes";

                        GameObject noChild = dataParent.transform.GetChild(3).gameObject;
                        noChild.GetComponent<TMP_Text>().text = noArray[z] + " no";

                        GameObject ratingChild = dataParent.transform.GetChild(4).gameObject;
                        ratingChild.GetComponent<TMP_Text>().text = ratingArray[z] + " rating";
                    }
                    else
                    {
                        DialogUI.Instance
                                               .SetTitle("Error")
                                               .SetMessage("Error getting data")
                                               .SetButtonColor(DialogButtonColor.Black)
                                               .OnClose(() => Debug.Log("Closed 1"))
                                               .Show();
                    }

                });
                await task;
             //   errorr.text = "" + downloadPath;
            }

        }
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

    public void backClick()
    {
        SceneManager.LoadScene("userDetails");
    }




}
