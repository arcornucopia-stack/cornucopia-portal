using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.IO;

public class screenshothandeling : MonoBehaviour
{
    byte[] file;
    System.Guid guid;
  //  public GameObject popup;
    public Button back;
    public Button capturee;
    public GameObject ui;
    public void take()
    {
        if (back != null) back.gameObject.SetActive(false);
        if (capturee != null) capturee.gameObject.SetActive(false);
        if (ui != null) ui.SetActive(false);
        guid = System.Guid.NewGuid();

        StartCoroutine(TakeScreenshotAndShare());
    }
    public IEnumerator capture()
    {
        yield return new WaitForEndOfFrame();
        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();
        file = ss.EncodeToPNG();
        // Save the screenshot to Gallery/Photos
        NativeGallery.Permission permission = NativeGallery.SaveImageToGallery(ss, "Models", "Image.png", (success, path) => Debug.Log("Media save result: " + path));
        AndroidNativeFunctions.ShowToast(Application.persistentDataPath+"/Models/");
        Debug.Log(Application.persistentDataPath);
        Debug.Log("Permission result: " + permission);
        
        if (back != null) back.gameObject.SetActive(true);
        if (capturee != null) capturee.gameObject.SetActive(true);
    }

    private IEnumerator TakeScreenshotAndShare()
    {
        yield return new WaitForEndOfFrame();

        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();

        string filePath = Path.Combine(Application.temporaryCachePath, "shared img.png");
        File.WriteAllBytes(filePath, ss.EncodeToPNG());

        Destroy(ss);

        new NativeShare().AddFile(filePath)
            .SetSubject("Cornucopia").SetText("Cornucopia!").SetUrl("")
            .SetCallback((result, shareTarget) => Debug.Log("Share result: " + result + ", selected app: " + shareTarget))
            .Share();
        if (back != null) back.gameObject.SetActive(true);
        if (capturee != null) capturee.gameObject.SetActive(true);
        // Share on WhatsApp only, if installed (Android only)
        //if( NativeShare.TargetExists( "com.whatsapp" ) )
        //	new NativeShare().AddFile( filePath ).AddTarget( "com.whatsapp" ).Share();
    }

}
