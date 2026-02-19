using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class displayDetails : MonoBehaviour
{
    public RawImage productImage;
    public TMP_Text productName;
    public TMP_Text questionText;
    public TMP_Text sentText;
    public TMP_Text saveText;
    public TMP_Text yesText;
    public TMP_Text noText;
    public TMP_Text ratingText;

    // Start is called before the first frame update
    void Start()
    {
        productImage.texture = GetImage(PlayerPrefs.GetString("picLocation"));
        productName.text = PlayerPrefs.GetString("productName");
        questionText.text = PlayerPrefs.GetString("question");
        sentText.text= PlayerPrefs.GetInt("sent")+" sent";
        saveText.text=PlayerPrefs.GetInt("save")+" saved";
       yesText.text= PlayerPrefs.GetInt("yes") + " yes";
       noText.text= PlayerPrefs.GetInt("no")+" no";
        ratingText.text=PlayerPrefs.GetString("rating")+ " rating";
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
      SceneManager.LoadScene("viewModel");
    }
    public void openAR()
    {
        PlayerPrefs.SetInt("ArNo", 3);
        SceneManager.LoadScene("UXManagerScene");
    }
    public void openModelUsers()
    {
        
        SceneManager.LoadScene("displayNModelUsers");
    }
}
