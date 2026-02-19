using EasyUI.Dialogs;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cornucopia.Core.Models;

public class notifyModelUser : MonoBehaviour
{
    public RawImage productImage;
    public TMP_Text productName;
    public TMP_Text questionText;
    string Id;
    string ModelName;
    DataSnapshot snapshot;
    //model data
    int sentArray;
    int saveArray;
    int yesArray;
    int noArray;
    string ratingArray;

    //user data
    int Usent;
    int Usaved;
    int Urate;
    // Start is called before the first frame update
    void Start()
    {
        productImage.texture = GetImage(PlayerPrefs.GetString("picLocation"));
        productName.text = PlayerPrefs.GetString("productName");
        Id = PlayerPrefs.GetString("userId");
        questionText.text = PlayerPrefs.GetString("modelQuestion");
        ModelName = PlayerPrefs.GetString("modelName");
        ModelName = ModelName.Replace(".glb", "");
    }


    async System.Threading.Tasks.Task GetData()
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
                                    var task = FirebaseDatabase.DefaultInstance
                                                       .GetReference("cornucopia")
                                                       .Child("models").Child(PlayerPrefs.GetString("modelName").Replace(".glb", "")).Child("data")
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
                                                               sentArray = d.sent;
                                                               saveArray = d.saved;
                                                               yesArray = d.yes;
                                                               noArray = d.no;
                                                               ratingArray = d.rating;

                                                           }
                                                       });
        await task;

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
        if (PlayerPrefs.GetString("givenAnswer") == "yes")
        {
            LegacyModelData d = new LegacyModelData(sentArray, saveArray + 1, yesArray+1, noArray, ratingArray);
            string json2 = JsonUtility.ToJson(d);
            Debug.Log("valuews:" + json2);
            var task2 = reference.Child("cornucopia").Child("models").Child(PlayerPrefs.GetString("modelName").Replace(".glb", "")).Child("data").SetRawJsonValueAsync(json2);
        }
       else if (PlayerPrefs.GetString("givenAnswer") == "no")
        {
            LegacyModelData d = new LegacyModelData(sentArray, saveArray + 1, yesArray , noArray+1, ratingArray);
            string json2 = JsonUtility.ToJson(d);
            Debug.Log("valuews:" + json2);
            var task2 = reference.Child("cornucopia").Child("models").Child(PlayerPrefs.GetString("modelName").Replace(".glb", "")).Child("data").SetRawJsonValueAsync(json2);
        }
        LegacyUserData ud = new LegacyUserData(Usent, Usaved + 1, Urate);
        string json3 = JsonUtility.ToJson(ud);
        Debug.Log("valuews:" + json3);
        var task3 = reference.Child("cornucopia").Child("users").Child(Id).Child("userData").SetRawJsonValueAsync(json3);

        SceneManager.LoadScene("UserModelDetails");
    }



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
        SceneManager.LoadScene("Notification");
    }
    public void saveModel(string clicked)
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
      
        LegacyModelRating mr = new LegacyModelRating("0.0", ModelName, true,clicked);
        string json = JsonUtility.ToJson(mr);
        var task = reference.Child("cornucopia").Child("users").Child(Id).Child("models").Child(ModelName).SetRawJsonValueAsync(json);
        PlayerPrefs.SetInt("modelSaved", 1);
        PlayerPrefs.SetString("givenAnswer", clicked);
        PlayerPrefs.SetInt("notifyCount", PlayerPrefs.GetInt("notifyCount")-1);

        Debug.Log("doneeee");
        GetData();
    }
}
