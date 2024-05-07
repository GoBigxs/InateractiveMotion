using UnityEngine;
using UnityEngine.UI;

public class LoadLocalImage : MonoBehaviour
{
    public RawImage rawImage;
    public string imagePath; // Path to the image file

    void Start()
    {
        LoadImageFromFile(imagePath);
    }

    void LoadImageFromFile(string path)
    {
        // Load the image from the file system
        byte[] imageData = System.IO.File.ReadAllBytes(path);
        
        // Create a new texture and load the image data into it
        Texture2D texture = new Texture2D(2, 2);
        bool success = texture.LoadImage(imageData);
        
        if (success)
        {
            // Apply the texture to the RawImage component
            rawImage.texture = texture;
        }
        else
        {
            Debug.LogError("Failed to load image from path: " + path);
        }
    }
}
