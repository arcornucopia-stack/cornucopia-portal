using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using EasyUI.Dialogs;



// mailFrom must be Gmail or you have to change smtpServer properties to your mail provider
// mailFrom must have enabled "Allow less secure applications" in google account and I recommend use for it new mail account for safety
// https://myaccount.google.com/lesssecureapps
// mailTo can be from any mail provider


public class RateGame : MonoBehaviour {



    [Header("References")]
    public GameObject hideo;
    public GameObject hidet;
    // PanelAppRating
    public Button[] starButton;
    public GameObject mainPanel;
    string Id;
    string ModelName;
    int save;
    bool check;
    // PanelRateMarket

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

    bool ratechecking=false;
    string timezone;

    [HideInInspector] public int ratedApp; // rate stor value can be used for something after rating
    private void Start()
    {
        Id = PlayerPrefs.GetString("userId");
        ModelName=PlayerPrefs.GetString("modelName");
       ModelName= ModelName.Replace(".glb", "");
        Debug.Log("" + PlayerPrefs.GetString("modRating"));
        int ratep = int.Parse(PlayerPrefs.GetString("modRating").Substring(0,1));
        Debug.Log(""+ratep);
        if(ratep==0)
        {
            ratechecking = true;
        }
        else
        {
            hideo.SetActive(false);
            hidet.SetActive(false);
        }
        RateApplication(ratep);

    }

    public void Init(int gameOpenCounter)
    {
        
        // get is rated app value
        //bool isRated = PlayerPrefs.GetInt("isAppRated", 0) == 1 ? true : false;
        int ratep=int.Parse(PlayerPrefs.GetString("modRating"));
       

      
            ratedApp = 0;
            RateApplication(ratep);
            mainPanel.SetActive(true);
            
           
            
        
        
    }


    public void RateApplication(int rate)
    {
        ratedApp = rate;
        
        // enable stars equal than user rated
        for (int i=0; i < rate; i++)
        {
            foreach (Transform t in starButton[i].transform)
            {
                t.gameObject.SetActive(true);
            }
        }

        // enable stars greater than user rated
        for (int i = rate; i < starButton.Length; i++)
        {
            foreach (Transform t in starButton[i].transform)
            {
                t.gameObject.SetActive(false);
            }

        }
        Debug.Log("" + rate);
        Debug.Log(Id);
        Debug.Log(ModelName);
        if (rate > 0)
        {
            save = PlayerPrefs.GetInt("modelSaved");
            if (save == 1)
                check = true;
            else
                check = false;
            Debug.Log("dosra wala" + check);
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
            modelRating mr = new modelRating("" + rate, ModelName,check, PlayerPrefs.GetString("givenAnswer"));
            string json = JsonUtility.ToJson(mr);
            var task = reference.Child("cornucopia").Child("users").Child(Id).Child("models").Child(ModelName).SetRawJsonValueAsync(json);

            timezone = System.DateTime.UtcNow.ToString("dd MMMM yyyy");
            string usersemails=PlayerPrefs.GetString("userEmail");
            ratingTable rt = new ratingTable( usersemails,"" + rate, timezone);
            string json8 = JsonUtility.ToJson(rt);
            Debug.Log(usersemails);
            var task8 = reference.Child("cornucopia").Child("Ratings").Child(Id).Child(ModelName).SetRawJsonValueAsync(json8);

            PlayerPrefs.SetString("modRating", "" + rate);
            hideo.SetActive(false);
            hidet.SetActive(false);
            if (ratechecking)
            {
                GetData();
            }
            ratechecking = false;
            Debug.Log("doneeee");
        }
    }




    async System.Threading.Tasks.Task GetData()
    {
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        var task = FirebaseDatabase.DefaultInstance
                           .GetReference("cornucopia")
                           .Child("models").Child(ModelName).Child("data")
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
                                   data d = JsonUtility.FromJson<data>(snapshot.GetRawJsonValue());
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
                userData ud = JsonUtility.FromJson<userData>(snapshot.GetRawJsonValue());
                Usent = ud.sent;
                Usaved = ud.saved;
                Urate = ud.rated;

            }
        });
        await task4;
        Debug.Log("yahan tk chal giya hai");
        double avg=0.0;
        string gvalue;
        Debug.Log(ratingArray);
        Debug.Log(PlayerPrefs.GetString("modRating")); 
        if (ratingArray != "0" && ratingArray != "0.0")
        {
            avg = (double.Parse(ratingArray) + double.Parse(PlayerPrefs.GetString("modRating"))) / 2;
            Debug.Log(avg);

            avg = avg + 0.00001;
            Debug.Log(avg);
            gvalue = "" + avg;

            gvalue = gvalue.Substring(0, 3);
            Debug.Log("yahan tk");
        }
        else
        {
            avg = double.Parse(PlayerPrefs.GetString("modRating")) + 0.00;
            gvalue = "" + avg;
            Debug.Log(" giya hai"+avg);
        }
        Debug.Log(""+avg);
      
        Debug.Log("yahan"+gvalue);
        data d = new data(sentArray, saveArray , yesArray, noArray ,gvalue);
            string json2 = JsonUtility.ToJson(d);
            Debug.Log("valuews:" + json2);
            var task2 = reference.Child("cornucopia").Child("models").Child(ModelName).Child("data").SetRawJsonValueAsync(json2);
       
        userData ud = new userData(Usent, Usaved, Urate+1);
        string json3 = JsonUtility.ToJson(ud);
        Debug.Log("valuews:" + json3);
        var task3 = reference.Child("cornucopia").Child("users").Child(Id).Child("userData").SetRawJsonValueAsync(json3);
      

    }





  

 
  


    public class modelRating
    {
        public string Rating;
        public string MName;
        public bool saved;
        public string answer;
        public modelRating()
        {

        }
        public modelRating(string rating , string MName,bool saved, string answer)
        {
            this.MName = MName;
            this.Rating = rating;
            this.saved = saved;
            this.answer = answer;
        }
    }


    public class data
    {

        public int sent;
        public int saved;
        public int yes;
        public int no;
        public string rating;
        public data()
        {

        }
        public data(int sent, int saved, int yes, int no, string rating)
        {
            this.sent = sent;
            this.saved = saved;
            this.yes = yes;
            this.no = no;
            this.rating = rating;
        }

    }
    public class userData
    {
        public int sent;
        public int saved;
        public int rated;
        public userData()
        {

        }
        public userData(int sent, int saved, int rated)
        {
            this.sent = sent;
            this.saved = saved;
            this.rated = rated;
        }
    }

    public class ratingTable
    {
        public string UserEmail;
        public string RatingValue;
        public string RatingDate;

        public ratingTable()
        {

        }

        public ratingTable( string UserEmail,string RatingValue, string RatingDate)
        {
            this.UserEmail = UserEmail;
            this.RatingValue = RatingValue;
            this.RatingDate = RatingDate;
        }

    }
}
