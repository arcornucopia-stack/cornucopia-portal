using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class admin : MonoBehaviour
{
    // Start is called before the first frame update
    public Button upload;
    public Button model;
    public Button user;
    void Start()
    {
       
    }
    public void uploadModel()
    {
        SceneManager.LoadScene("UploadModel");
    }
    public void viewModel()
    {
        SceneManager.LoadScene("ViewModel");
    }
    public void viewUser()
    {
        SceneManager.LoadScene("displayUsers");
    }
    public void backClick()
    {
        SceneManager.LoadScene("FirebaseLogin");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
