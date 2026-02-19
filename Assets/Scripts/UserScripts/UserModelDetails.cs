using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UserModelDetails : MonoBehaviour
{
    public RawImage productImage;
    public TMP_Text productName;
    

    // Start is called before the first frame update
    void Start()
    {
        productImage.texture = GetImage(PlayerPrefs.GetString("picLocation"));
        productName.text = PlayerPrefs.GetString("productName");
        
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
        SceneManager.LoadScene("Collectibles");
    }
    public void openAR()
    {
        PlayerPrefs.SetInt("ArNo", 2);
        SceneManager.LoadScene("UXManagerScene");
    }
}
