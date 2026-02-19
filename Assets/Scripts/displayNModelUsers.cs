using EasyUI.Dialogs;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cornucopia.Core.Models;

public class displayNModelUsers : MonoBehaviour
{
  


public GameObject game;
public GameObject content;
DataSnapshot snapshot;
int i;
    bool check=false;
GameObject[] objectArray;
string[] nameArray;
string[] emailArray;
string[] idArray;
Button[] btnsArray;

//model data
    int[] sentArray;
    int[] saveArray;
    int[] yesArray;
    int[] noArray;
    string[] ratingArray;

    //user data
    int[] Usent;
    int[] Usaved;
    int[] Urate;
    // Start is called before the first frame update
    void Start()
{
        game.SetActive(false);
    getData();
}


    async System.Threading.Tasks.Task getData()
{
    DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
    var task = FirebaseDatabase.DefaultInstance
      .GetReference("cornucopia")
        .Child("users")
        .GetValueAsync().ContinueWithOnMainThread(async task => {
            if (task.IsFaulted)
            {
                Debug.Log("error getting data");
                snapshot = null;
            }
            else if (task.IsCompleted)
            {
                snapshot = task.Result;
                Debug.Log("count:" + snapshot.ChildrenCount);
                i = 0;
                    // Do something with snapshot...
                    objectArray = new GameObject[snapshot.ChildrenCount];

                nameArray = new string[snapshot.ChildrenCount];
                btnsArray = new Button[snapshot.ChildrenCount];
                emailArray = new string[snapshot.ChildrenCount];
                idArray = new string[snapshot.ChildrenCount];

                Usent = new int[snapshot.ChildrenCount];
                Usaved = new int[snapshot.ChildrenCount];
                Urate = new int[snapshot.ChildrenCount];

                foreach (DataSnapshot place in snapshot.Children)
                {
                    LegacyUserDetails m = JsonUtility.FromJson<LegacyUserDetails>(place.GetRawJsonValue());
                    nameArray[i] = m.name;
                    emailArray[i] = m.email;
                    idArray[i] = m.id;
                    check = false;
                  var task2=  FirebaseDatabase.DefaultInstance
                             .GetReference("cornucopia")
                               .Child("users").Child(m.id).Child("models")
                               .GetValueAsync().ContinueWithOnMainThread(task => {
                                                           if (task.IsFaulted)
                                                           {
                                                               Debug.Log("error getting data");
                                                               snapshot = null;
                                                           }
                                                           else if (task.IsCompleted)
                                                           {
                                                               snapshot = task.Result;
                                                               Debug.Log("count:" + snapshot.ChildrenCount);

                                       // Do something with snapshot...

                                      
                                       foreach (DataSnapshot place in snapshot.Children)
                                                               {

                                                                   Debug.Log("fetched Data: " + place.GetRawJsonValue());
                                                                    
                                                                   LegacyModelRating m = JsonUtility.FromJson<LegacyModelRating>(place.GetRawJsonValue());
                                                                    
                                                                   if (m.MName == PlayerPrefs.GetString("modelName").Replace(".glb", ""))
                                                                   {

                                                                         check = true;
                                               
                                                                   }
                                                                   Debug.Log("name" + m.MName);

                                                               }

                                                           }
                                                       });


                    await task2;
                  
                    Debug.Log("fetched Data: " + place.GetRawJsonValue());
                    if (check == false)
                    {
                        game.SetActive(true);
                        if (i == 0)
                        {
                            objectArray[i] = game;
                            GameObject childObject = objectArray[i].transform.GetChild(2).gameObject;
                            childObject.SetActive(true);
                            btnsArray[i] = childObject.transform.GetChild(0).gameObject.GetComponent<Button>();
                            int x = i;
                            btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });

                        }

                        else
                        {
                            objectArray[i] = Instantiate(game, content.transform);
                            GameObject childObject = objectArray[i].transform.GetChild(2).gameObject;
                            childObject.SetActive(true);
                            btnsArray[i] = childObject.transform.GetChild(0).gameObject.GetComponent<Button>();
                            int x = i;
                            btnsArray[i].onClick.AddListener(delegate { btnClicks(x); });

                        }
                        GameObject parentObject = objectArray[i].transform.GetChild(0).gameObject;
                        GameObject textChild = parentObject.transform.GetChild(1).gameObject;
                        textChild.GetComponent<TMP_Text>().text = emailArray[i];
                                                    var task4 = FirebaseDatabase.DefaultInstance
                                   .GetReference("cornucopia")
                                    .Child("users").Child(idArray[i]).Child("userData")
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
                                           Usent[i] = ud.sent;
                                           Usaved[i] = ud.saved;
                                           Urate[i] = ud.rated;
                                           GameObject parentObject = objectArray[i].transform.GetChild(1).gameObject;
                                           GameObject sentChild = parentObject.transform.GetChild(0).gameObject;
                                           sentChild.GetComponent<TMP_Text>().text =ud.sent+" sent |";
                                           GameObject saveChild = parentObject.transform.GetChild(1).gameObject;
                                           saveChild.GetComponent<TMP_Text>().text = ud.saved + " saved |";
                                           GameObject rateChild = parentObject.transform.GetChild(2).gameObject;
                                           rateChild.GetComponent<TMP_Text>().text = ud.rated + " rated";
                                       }
                                   });
                                                    await task4;
                        //GameObject rateChild = objectArray[i].transform.GetChild(2).gameObject;
                       // rateChild.GetComponent<TMP_Text>().text = m.Rating;
                        i++;
                    }
                }

            }
        });
}

public void backClick()
{
    SceneManager.LoadScene("displayDetails");
}


    async System.Threading.Tasks.Task btnClicks(int value)
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        LegacyModelRating m = new LegacyModelRating("0", PlayerPrefs.GetString("modelName").Replace(".glb", ""), false, "pending");
        string json = JsonUtility.ToJson(m);
        Debug.Log("click" + value);
        Debug.Log("valuews:" + json);
        var task = reference.Child("cornucopia").Child("users").Child(idArray[value]).Child("models").Child(PlayerPrefs.GetString("modelName").Replace(".glb", "")).SetRawJsonValueAsync(json);


        var task10 = FirebaseDatabase.DefaultInstance
                             .GetReference("cornucopia")
                               .Child("models").Child(PlayerPrefs.GetString("modelName").Replace(".glb", "")).Child("data")
                               .GetValueAsync().ContinueWithOnMainThread(task =>
                               {
                                   if (task.IsFaulted)
                                   {
                                       Debug.Log("error getting data");
                                       snapshot = null;
                                   }
                                   else if (task.IsCompleted)
                                   {
                                       snapshot = task.Result;
                                       Debug.Log("count:" + snapshot.ChildrenCount);
                                       LegacyModelData da = JsonUtility.FromJson<LegacyModelData>(snapshot.GetRawJsonValue());
                                       LegacyModelData d = new LegacyModelData(da.sent + 1, da.saved, da.yes, da.no, da.rating);
                                       string json2 = JsonUtility.ToJson(d);
                                       Debug.Log("valuews:" + json2);
                                       var task2 = reference.Child("cornucopia").Child("models").Child(PlayerPrefs.GetString("modelName").Replace(".glb", "")).Child("data").SetRawJsonValueAsync(json2);
                                   }
                               });
        await task10;
        LegacyUserData ud = new LegacyUserData(Usent[value] + 1, Usaved[value], Urate[value]);
        string json3 = JsonUtility.ToJson(ud);
        Debug.Log("valuews:" + json3);
        var task3 = reference.Child("cornucopia").Child("users").Child(idArray[value]).Child("userData").SetRawJsonValueAsync(json3);
        //  await task;
        SceneManager.LoadScene("displayNModelUsers");
    }
}
