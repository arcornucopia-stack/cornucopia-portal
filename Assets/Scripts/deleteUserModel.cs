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

public class deleteUserModel : MonoBehaviour
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
    string[] UrateArray;
    int totalModels = 0;
    Button[] btnsArray;

    string[] picArray;
    string[] nameArray;
    string[] modName;
    bool[] savedArray;
    bool check = false;

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
                   UrateArray = new string[snapshot.ChildrenCount];
                   savedArray = new bool[snapshot.ChildrenCount];
                   foreach (DataSnapshot place in snapshot.Children)
                   {


                       Debug.Log("fetched Data: " + place.GetRawJsonValue());

                       LegacyModelRef m = JsonUtility.FromJson<LegacyModelRef>(place.GetRawJsonValue());
                       modelNameArray[i] = m.MName;
                       UrateArray[i] = m.Rating;
                       savedArray[i] = m.saved;
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
                      modName = new string[totalModels];
                      // model data
                      sentArray = new int[totalModels];
                      saveArray = new int[ totalModels];
                      yesArray = new int[totalModels];
                      noArray = new int[totalModels];
                      ratingArray = new string[totalModels];
                      foreach (DataSnapshot place in snapshot.Children)
                      {

                          check = false;
                          Debug.Log("fetched Data: " + place.GetRawJsonValue());

                          LegacyModel m = JsonUtility.FromJson<LegacyModel>(place.GetRawJsonValue());
                          for (int z = 0; z < totalModels; z++)
                          {

                              if (modelNameArray[z].Equals(m.modelNamee))
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
                                  GameObject childObject = objectArray[i].transform.GetChild(1).gameObject;
                                  btnsArray[i] = childObject.transform.GetChild(5).gameObject.GetComponent<Button>();
                                  int x = i;
                                  btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });
                                  Debug.Log("testtt0");
                              }

                              else
                              {
                                  objectArray[i] = Instantiate(game, content.transform);
                                  GameObject childObject = objectArray[i].transform.GetChild(1).gameObject;
                                  btnsArray[i] =  childObject.transform.GetChild(5).gameObject.GetComponent<Button>();
                                  int x = i;
                                  btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });
                                  Debug.Log("testtt1");
                              }

                              Debug.Log("testtt2" );
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
         Debug.Log("testtt3");
                                                                                for (int b = 0; b < i; b++)
                                                                                {
             Debug.Log("testtt4"+i+""+b);
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
                                                                                Debug.Log("testtt5"  + b);
                                                                            }
                                                                        });
                                                                                    await task3;

                                                                             }
        Debug.Log("testtt67" );
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
        if (totalModels > 0)
            DownloadFileAsync();
        else

        {
            game.SetActive(true);
            GameObject textChild = game.transform.GetChild(0).gameObject;
            textChild.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = "No Model";
            game.transform.GetChild(1).gameObject.SetActive(false);
            game.transform.GetChild(2).gameObject.SetActive(false);
            game.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.SetActive(false);
        }

    }

    void btnClicks(int value)
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
       // modelName m = new modelName(modName[value]);
       // string json = JsonUtility.ToJson(m);
        Debug.Log("click" + value);
       // Debug.Log("valuews:" + json);
        var task = reference.Child("cornucopia").Child("users").Child(Id).Child("models").Child(modName[value]).RemoveValueAsync();

        LegacyModelData d = new LegacyModelData(sentArray[value] -1, saveArray[value], yesArray[value], noArray[value], ratingArray[value]);
        string json2 = JsonUtility.ToJson(d);
        Debug.Log("valuews:" + json2);
        var task2 = reference.Child("cornucopia").Child("models").Child(modName[value]).Child("data").SetRawJsonValueAsync(json2);

        LegacyUserData ud = new LegacyUserData(Usent -1, Usaved, Urate);
        string json3 = JsonUtility.ToJson(ud);
        Debug.Log("valuews:" + json3);
        var task3 = reference.Child("cornucopia").Child("users").Child(Id).Child("userData").SetRawJsonValueAsync(json3);
        //  await task;
        SceneManager.LoadScene("deleteUserModel");
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
                GameObject childObject = objectArray[z].transform.GetChild(0).gameObject;
                GameObject child = childObject.transform.GetChild(0).gameObject;
                child.GetComponent<RawImage>().texture = GetImage(downloadPath);
                GameObject textChild = childObject.transform.GetChild(1).gameObject;
                textChild.GetComponent<TMP_Text>().text = nameArray[z].Replace(".glb", "");
               
               

                GameObject dataParent = objectArray[z].transform.GetChild(1).gameObject;
                GameObject sentChild = dataParent.transform.GetChild(0).gameObject;
                sentChild.GetComponent<TMP_Text>().text = "Sent";

                GameObject saveChild = dataParent.transform.GetChild(1).gameObject;
                GameObject yesChild = dataParent.transform.GetChild(2).gameObject;
                if (savedArray[z])
                {
                    saveChild.GetComponent<TMP_Text>().text = "Saved";
                    yesChild.GetComponent<TMP_Text>().text ="Answered";
                }
                else
                {
                    saveChild.GetComponent<TMP_Text>().text = "Not Saved";
                    yesChild.GetComponent<TMP_Text>().text = "Not Answered";
                }
                if (UrateArray[z] != "0.0" && UrateArray[z] != "0")
                {
                    GameObject noChild = dataParent.transform.GetChild(3).gameObject;
                    noChild.GetComponent<TMP_Text>().text = "Rated";
                }
               else 
                {
                    GameObject noChild = dataParent.transform.GetChild(3).gameObject;
                    noChild.GetComponent<TMP_Text>().text = "Not Rated";
                }

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
                        GameObject child = objectArray[z].transform.GetChild(0).gameObject;
                        child.GetComponent<RawImage>().texture = GetImage(downloadPath);
                        GameObject textChild = objectArray[z].transform.GetChild(1).gameObject;
                        textChild.GetComponent<TMP_Text>().text = nameArray[z].Replace(".glb", "");

                        GameObject dataParent = objectArray[z].transform.GetChild(1).gameObject;
                        GameObject sentChild = dataParent.transform.GetChild(0).gameObject;
                        sentChild.GetComponent<TMP_Text>().text = "Sent";

                        GameObject saveChild = dataParent.transform.GetChild(1).gameObject;
                        GameObject yesChild = dataParent.transform.GetChild(2).gameObject;
                        if (savedArray[z])
                        {
                            saveChild.GetComponent<TMP_Text>().text = "Saved";
                            yesChild.GetComponent<TMP_Text>().text = "Answered";
                        }
                        else
                        {
                            saveChild.GetComponent<TMP_Text>().text = "Not Saved";
                            yesChild.GetComponent<TMP_Text>().text = "Not Answered";
                        }
                        if (ratingArray[z] != "0.0" && ratingArray[z] != "0.0")
                        {
                            GameObject noChild = dataParent.transform.GetChild(3).gameObject;
                            noChild.GetComponent<TMP_Text>().text = "Rated";
                        }
                        else
                        {
                            GameObject noChild = dataParent.transform.GetChild(3).gameObject;
                            noChild.GetComponent<TMP_Text>().text = "Not Rated";
                        }
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
