using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cornucopia.Core.Models;

public class displayUserDetails : MonoBehaviour
{
    public TMP_Text Uname;
    public TMP_Text Uemail;
    public TMP_Text sent;
    public TMP_Text save;
    public TMP_Text rate;
    DataSnapshot snapshot;
    // Start is called before the first frame update
    void Start()
    {
        Uname.text= PlayerPrefs.GetString("Name");
        Uemail.text= PlayerPrefs.GetString("Email");
        getData();
    }
    public void getData()
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        var task = FirebaseDatabase.DefaultInstance
          .GetReference("cornucopia")
            .Child("users").Child(PlayerPrefs.GetString("Id")).Child("userData")
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
                    Debug.Log("data:" + snapshot);
                    LegacyUserData d = JsonUtility.FromJson<LegacyUserData>(snapshot.GetRawJsonValue());
                    sent.text = d.sent + " sent |";
                    save.text = d.saved + " saved |";
                    rate.text = d.rated + " rated";
                }
            });

            }
   public void viewModel()
    {
        SceneManager.LoadScene("viewUserModel");
    }
    public void addModel()
    {
        SceneManager.LoadScene("addUserModel");
    }
    public void deleteModel()
    {
        SceneManager.LoadScene("deleteUserModel");
    }
    public void backClick()
    {
        SceneManager.LoadScene("displayUsers");
    }
    }
