using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cornucopia.Core.Models;

public class displayModelUsers : MonoBehaviour
{
  


public GameObject game;
public GameObject content;
DataSnapshot snapshot;
int i;
GameObject[] objectArray;
string[] nameArray;
string[] emailArray;
string[] idArray;
Button[] btnsArray;
// Start is called before the first frame update
void Start()
{
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
                foreach (DataSnapshot place in snapshot.Children)
                {
                    LegacyUserDetails m = JsonUtility.FromJson<LegacyUserDetails>(place.GetRawJsonValue());
                    nameArray[i] = m.name;
                    emailArray[i] = m.email;
                    idArray[i] = m.id;

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
                                                                    
                                                                   LegacyModelRef m = JsonUtility.FromJson<LegacyModelRef>(place.GetRawJsonValue());
                                                                    
                                                                   if (m.MName == PlayerPrefs.GetString("modelName").Replace(".glb", ""))
                                                                   {


                                               if (i == 0)
                                               {
                                                   objectArray[i] = game;


                                               }

                                               else
                                               {
                                                   objectArray[i] = Instantiate(game, content.transform);

                                               }
                                               GameObject textChild = objectArray[i].transform.GetChild(1).gameObject;
                                                                       textChild.GetComponent<TMP_Text>().text = emailArray[i];

                                               GameObject rateChild = objectArray[i].transform.GetChild(2).gameObject;
                                               rateChild.GetComponent<TMP_Text>().text = m.Rating;
                                               i++;
                                                                   }
                                                                   Debug.Log("name" + m.MName);

                                                               }

                                                           }
                                                       });


                    await task2;
                  
                    Debug.Log("fetched Data: " + place.GetRawJsonValue());



                }

            }
        });
}

public void backClick()
{
    SceneManager.LoadScene("displayDetails");
}



}
