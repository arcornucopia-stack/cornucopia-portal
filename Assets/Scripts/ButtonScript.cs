using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void backClick()
    {
        if (PlayerPrefs.GetInt("ArNo") == 1)
        {
            SceneManager.LoadScene("NotifyModel");
        }
        else if (PlayerPrefs.GetInt("ArNo") == 2)
        {
            SceneManager.LoadScene("UserModelDetails");
        }
        else 
        {
            SceneManager.LoadScene("displayDetails");
        }
    }
 
}
