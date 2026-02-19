using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cornucopia.Core.Models;

public class displayUsers : MonoBehaviour
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


    public void getData()
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        var task = FirebaseDatabase.DefaultInstance
          .GetReference("cornucopia")
            .Child("users")
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
                    i = 0;
                    // Do something with snapshot...
                    objectArray = new GameObject[snapshot.ChildrenCount];

                    nameArray = new string[snapshot.ChildrenCount];
                    btnsArray = new Button[snapshot.ChildrenCount];
                    emailArray = new string[snapshot.ChildrenCount];
                    idArray = new string[snapshot.ChildrenCount];
                    foreach (DataSnapshot place in snapshot.Children)
                    {

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

                        LegacyUserDetails m = JsonUtility.FromJson<LegacyUserDetails>(place.GetRawJsonValue());
                        nameArray[i] = m.name;
                        emailArray[i] = m.email;
                        idArray[i] = m.id;
                        GameObject textChild = objectArray[i].transform.GetChild(1).gameObject;
                        textChild.GetComponent<TMP_Text>().text = emailArray[i];

                        Debug.Log("name" + m.name);
                        i++;
                    }

                }
            });
    }
    void btnClicks(int value)
    {

        PlayerPrefs.SetString("Name", nameArray[value]);
        PlayerPrefs.SetString("Email", emailArray[value]);
        PlayerPrefs.SetString("Id", idArray[value]);
        SceneManager.LoadScene("userDetails");
    }
    public void backClick()
    {
        SceneManager.LoadScene("Admin");
    }
}



