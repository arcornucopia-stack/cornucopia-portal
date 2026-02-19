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

public class viewUserModels : MonoBehaviour
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
    int totalModels = 0;
    TMP_Text[] ratingsArray;
    string[] ratingString;
    string[] answerString;
    bool[] savedString;
    string[] picArray;
    string[] nameArray;
    string[] modName;
    bool check = false;
    // Start is called before the first frame update
    //model data

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
                   ratingString = new string[snapshot.ChildrenCount];
                   answerString = new string[snapshot.ChildrenCount];
                   savedString = new bool[snapshot.ChildrenCount];
                   foreach (DataSnapshot place in snapshot.Children)
                   {


                       Debug.Log("fetched Data: " + place.GetRawJsonValue());

                       LegacyModelRating m = JsonUtility.FromJson<LegacyModelRating>(place.GetRawJsonValue());
                       modelNameArray[i] = m.MName;
                       Debug.Log("rating" + m.Rating);
                       ratingString[i] = m.Rating;
                       answerString[i] = m.answer;
                       savedString[i] = m.saved;
                       
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
                      ratingsArray = new TMP_Text[totalModels];
                      modName = new string[totalModels];
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

                                /*  ratingsArray[i] = objectArray[i].transform.GetChild(2).gameObject.GetComponent<TMP_Text>();
                                 
                                  ratingsArray[i].text="Rating:"+ratingString[i];*/
                              }

                              else
                              {
                                  objectArray[i] = Instantiate(game, content.transform);
                                 /* ratingsArray[i] = objectArray[i].transform.GetChild(2).gameObject.GetComponent<TMP_Text>();

                                   ratingsArray[i].text = "Rating:" + ratingString[i];*/
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
                      if (totalModels > 0)
                          DownloadFileAsync();
                      else

                      {
                          game.SetActive(true);
                          GameObject childObject = game.transform.GetChild(0).gameObject;
                          GameObject textChild =childObject.transform.GetChild(1).gameObject;
                          textChild.GetComponent<TMP_Text>().text = "No Model";
                          game.transform.GetChild(1).gameObject.SetActive(false);
                          game.transform.GetChild(2).gameObject.SetActive(false);
                          childObject.transform.GetChild(0).gameObject.SetActive(false);
                        
                      }
                  }
              });

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
                if (savedString[z])
                {
                    saveChild.GetComponent<TMP_Text>().text = "Saved";
                    yesChild.GetComponent<TMP_Text>().text =answerString[z];
                }
                else
                {
                    saveChild.GetComponent<TMP_Text>().text = "Not Saved";
                    yesChild.GetComponent<TMP_Text>().text = "Not Answered";
                }
                if (ratingString[z] != "0.0" && ratingString[z] != "0")
                {
                    GameObject noChild = dataParent.transform.GetChild(3).gameObject;
                    noChild.GetComponent<TMP_Text>().text = ratingString[z]+" Rating";
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
                        if (savedString[z])
                        {
                            saveChild.GetComponent<TMP_Text>().text = "Saved";
                            yesChild.GetComponent<TMP_Text>().text = answerString[z];
                        }
                        else
                        {
                            saveChild.GetComponent<TMP_Text>().text = "Not Saved";
                            yesChild.GetComponent<TMP_Text>().text = "Not Answered";
                        }
                        if (ratingString[z] != "0.0" && ratingString[z] != "0")
                        {
                            GameObject noChild = dataParent.transform.GetChild(3).gameObject;
                            noChild.GetComponent<TMP_Text>().text = ratingString[z] + " Rating";
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
