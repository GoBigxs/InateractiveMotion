// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.Networking;
// using System.Collections;
 
// public class ImageDownloader : MonoBehaviour
// {
//     public string serverURL = "http://localhost:5000/get_image";
//     public RawImage imageDisplay;
 
//     IEnumerator Start()
//     {
//         while (true) 
//         {

//             UnityWebRequest www = UnityWebRequest.Get(serverURL);
//             yield return www.SendWebRequest();
    
//             if (www.result == UnityWebRequest.Result.Success)
//             {
//                 // Convert base64 string to byte array
//                 byte[] imageData = Convert.FromBase64String(www.downloadHandler.text);

//                 // Create texture from byte array
//                 Texture2D texture = new Texture2D(2, 2);
//                 texture.LoadImage(imageData);
    
//                 // Assign the texture to the RawImage component
//                 imageDisplay.texture = texture;
//             }
//             else
//             {
//                 Debug.LogError("Failed to load image: " + www.error);
//             }
//         }
//     }
// }