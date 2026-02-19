using EasyUI.Dialogs;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cornucopia.Core.Models;

public class Home : MonoBehaviour
{
    [SerializeField]
    private Text messageText;
    string Id;
    DataSnapshot snapshot;
    int i;
    int totalModels = 0;
    string[] modelNameArray;
    bool[] savedArray;
    bool check = false;
    public GameObject notifyimage;
    public TMP_Text notify;   // Start is called before the first frame update
    void Start()
    {

        PlayerPrefs.SetInt("notifyCount", 0);
        notifyimage.SetActive(false);
        ShowMessage();
        Id = PlayerPrefs.GetString("userId");
        getData();
    }

    private void ShowMessage()
    {
        messageText.text = string.Format("{0}", PlayerPrefs.GetString("userName"));
    }
    public void CollectiblesClick()
    {
        SceneManager.LoadScene("Collectibles");
    }
    public void notificationClick()
    {
        SceneManager.LoadScene("Notification");
    }
    public void profileClick()
    {
        SceneManager.LoadScene("Profile");
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

                   foreach (DataSnapshot place in snapshot.Children)
                   {


                       Debug.Log("fetched Data: " + place.GetRawJsonValue());

                       LegacyModelRef m = JsonUtility.FromJson<LegacyModelRef>(place.GetRawJsonValue());
                       modelNameArray[i] = m.MName;
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
                                

                              }


                          }

                          if (check)
                          {

                              

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
        Debug.Log("0"+i);
        if (i>0)
        {
            PlayerPrefs.SetInt("notifyCount", i);
            notifyimage.SetActive(true);
            notify.text = ""+i;
        }
    }

}
