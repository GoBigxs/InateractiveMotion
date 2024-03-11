using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
 
public class ImageDownloader : MonoBehaviour
{
    public string serverURL = "http://localhost:5000/get_image";
    public RawImage imageDisplay;
 
    IEnumerator Start()
    {
        while (true) 
        {

            UnityWebRequest www = UnityWebRequest.Get(serverURL);
            yield return www.SendWebRequest();
    
            if (www.result == UnityWebRequest.Result.Success)
            {
                // Load the image from binary data
                Texture2D texture = new Texture2D(1, 1);
                texture.LoadImage(www.downloadHandler.data);
    
                // Assign the texture to the RawImage component
                imageDisplay.texture = texture;
            }
            else
            {
                Debug.LogError("Failed to load image: " + www.error);
            }
        }
    }
}