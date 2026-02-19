using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.IO;
using UnityEngine.Networking;
using System;
using Firebase.Storage;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using EasyUI.Dialogs;
using Cornucopia.Core.Models;

public class modelDatabase1 : MonoBehaviour
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
    string[] picArray;
    string[] nameArray;
    string[] questionArray;
    Button[] btnsArray;
    string[] modelName;
    int[] sentArray;
    int[] saveArray;
    int[] yesArray;
    int[] noArray;
    string[] ratingArray;
   // public TMP_Text errorr;
    void Start()
    {
        
        game.SetActive(false);                                                  
        storage = FirebaseStorage.DefaultInstance;
        storageReference = storage.RootReference;
        downloadPath=$"{Application.persistentDataPath}/Files/";
        getData();


    }

    async System.Threading.Tasks.Task getData()
    {

        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        var task = FirebaseDatabase.DefaultInstance
            .GetReference("cornucopia")
              .Child("models")
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
                      objectArray = new GameObject[snapshot.ChildrenCount];
                      picArray =new string[snapshot.ChildrenCount];
                      nameArray = new string[snapshot.ChildrenCount];
                      questionArray = new string[snapshot.ChildrenCount];
                      btnsArray = new Button[snapshot.ChildrenCount];
                      modelName = new string[snapshot.ChildrenCount];
                      saveArray = new int[snapshot.ChildrenCount];
                      sentArray = new int[snapshot.ChildrenCount];
                      yesArray = new int[snapshot.ChildrenCount];
                      noArray = new int[snapshot.ChildrenCount];
                      ratingArray = new string[snapshot.ChildrenCount];
                      foreach (DataSnapshot place in snapshot.Children)
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
                          Debug.Log("fetched Data: " + place.GetRawJsonValue());

                          LegacyModel m = JsonUtility.FromJson<LegacyModel>(place.GetRawJsonValue());
                          
                          picArray [i]= m.picPathh;
                          nameArray[i] = m.name;
                          questionArray [i]= m.question;
                          modelName[i] = m.modelNamee;
                          

             
                        
                          Debug.Log("name" + m.name);
                          i++;
                          Debug.Log("i=" + i);
                      }
                   
                  }
              });
        await task;
        Debug.Log("i=000" + i);
        for (int b = 0; b < i; b++)
                                        {
            Debug.Log("i=" + i);
            Debug.Log("model name: " + modelName[b]);
                                     var task2 = FirebaseDatabase.DefaultInstance
                                 .GetReference("cornucopia")
                                   .Child("models").Child(modelName[b]).Child("data")
                                   .GetValueAsync().ContinueWithOnMainThread(task =>
                                   {
                                       if (task.IsFaulted || task.IsCanceled)
                                       {
                                       }
                                       else
                                       {
                                           snapshot = task.Result;
                                           Debug.Log("fetched Data: " + snapshot);
                                           
                                               Debug.Log("fetched Data: " + snapshot.GetRawJsonValue());

                                               LegacyModelData d = JsonUtility.FromJson<LegacyModelData>(snapshot.GetRawJsonValue());
                                               saveArray[b] = d.saved;
                                               sentArray[b] = d.sent;
                                               yesArray[b] = d.yes;
                                               noArray[b] = d.no;
                                               ratingArray[b] = d.rating;

                                           
                                          
                                       }

                                   });
            await task2;
            

        }
        
        Debug.Log("i=000256" + i);
        DownloadFileAsync();
    }
  void btnClicks(int value)
    {
        PlayerPrefs.SetString("picLocation", $"{Application.persistentDataPath}/Files/" + picArray[value] + ".png");
        PlayerPrefs.SetString("productName", nameArray[value].Replace(".glb", ""));
        PlayerPrefs.SetString("question", questionArray[value]);
        PlayerPrefs.SetString("modelName", modelName[value]+".glb");
        PlayerPrefs.SetInt("sent", sentArray[value]);
        PlayerPrefs.SetInt("save", saveArray[value]);
        PlayerPrefs.SetInt("yes", yesArray[value] );
        PlayerPrefs.SetInt("no", noArray[value] );
        PlayerPrefs.SetString("rating", ratingArray[value] );
        SceneManager.LoadScene("displayDetails");
    }
    async System.Threading.Tasks.Task DownloadFileAsync()
    {
        if(i==0)
        {
            game.SetActive(true);
            GameObject parentChild = game.transform.GetChild(0).gameObject;
            GameObject textChild = parentChild.transform.GetChild(1).gameObject;
            textChild.GetComponent<TMP_Text>().text = "No model Exist";
        }
        for (int z=0;z<i;z++)
        {
            Debug.Log("z="+z);
                  //GetFilePath(url); 
        downloadPath = $"{Application.persistentDataPath}/Files/" + picArray[z] + ".png";
           
            if (File.Exists(downloadPath))
            {
                
                Debug.Log("Found the same file locally, Loading!!!");

                GameObject parent = objectArray[z].transform.GetChild(0).gameObject;
                GameObject child = parent.transform.GetChild(0).gameObject;
                child.GetComponent<RawImage>().texture = GetImage(downloadPath);
                GameObject textChild= parent.transform.GetChild(1).gameObject;
                textChild.GetComponent<TMP_Text>().text = nameArray[z].Replace(".glb", "");

                GameObject dataParent = objectArray[z].transform.GetChild(1).gameObject;
                GameObject sentChild = dataParent.transform.GetChild(0).gameObject;
                sentChild.GetComponent<TMP_Text>().text =sentArray[z]+" sent";

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
                var task=   gsReference.GetFileAsync(downloadPath).ContinueWithOnMainThread(task => {
                    if (!task.IsFaulted && !task.IsCanceled)
                    {
                        Debug.Log("File downloaded.");
                        Debug.Log(downloadPath);
                        GameObject parent = objectArray[z].transform.GetChild(0).gameObject;
                        GameObject child = parent.transform.GetChild(0).gameObject;
                        child.GetComponent<RawImage>().texture = GetImage(downloadPath);
                        GameObject textChild = parent.transform.GetChild(1).gameObject;
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
              
            }
           
            }
    }

    public void backClick()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Admin");
    }
    void LoadImage(string path,GameObject current)
    {
        //  ResetSpaceShip();
        GameObject child = current.transform.GetChild(0).gameObject;
        child.GetComponent<RawImage>().texture=GetImage(downloadPath);
       
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

    IEnumerator GetFileRequest(string url, Action<UnityWebRequest> callback)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerFile(downloadPath);

            yield return req.SendWebRequest();

            callback(req);

        }
    }
}
