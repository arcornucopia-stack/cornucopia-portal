using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class profile : MonoBehaviour
{
    public Text Uname;
    public Text Uemail;
    public GameObject notifyimage;
    public TMP_Text notify;
    void Start()
    {
        Uname.text =PlayerPrefs.GetString("userName");
        Uemail.text = PlayerPrefs.GetString("userEmail");
        if (PlayerPrefs.GetInt("notifyCount") > 0)
        {

            notifyimage.SetActive(true);
            notify.text = "" + PlayerPrefs.GetInt("notifyCount");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void logoutClick()
    {
        PlayerPrefs.SetString("userEmail","");
        PlayerPrefs.SetString("userName", "");
        PlayerPrefs.SetString("userId", "");
        PlayerPrefs.SetInt("login", 0);
        SceneManager.LoadScene("FirebaseLogin");
    }
    public void CollectiblesClick()
    {
        SceneManager.LoadScene("Collectibles");
    }
    public void notificationClick()
    {
        SceneManager.LoadScene("Notification");
    }
    public void homeClick()
    {
        SceneManager.LoadScene("Home");
    }
}
