using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CanvasImageSaver : MonoBehaviour
{
    public Pen[] pens; // Array of references to Pen scripts
    private PenState[] previousPenStates; // Array to store previous states of the pens
    private bool[] isTouchingPaper; // Array to track if each pen is touching paper
    private int[] touchCounts; // Array to store the number of times each pen touches the paper
    private RawImage rawImage; // Reference to the RawImage component

    private void Start()
    {
        // Initialize arrays based on the number of pens
        int numPens = pens.Length;
        previousPenStates = new PenState[numPens];
        isTouchingPaper = new bool[numPens];
        touchCounts = new int[numPens];

        // Initialize previous pen states
        for (int i = 0; i < numPens; i++)
        {
            previousPenStates[i] = pens[i].penState;
        }

        // Find the RawImage component in children
        rawImage = GetComponentInChildren<RawImage>();
        if (rawImage == null)
        {
            Debug.LogError("RawImage component not found.");
        }
    }

    private void Update()
    {
        // Update pen states and check for paper touch events for each pen
        for (int i = 0; i < pens.Length; i++)
        {
            if (pens[i].penState == PenState.Touching && previousPenStates[i] == PenState.NotTouching)
            {
                isTouchingPaper[i] = true;
                touchCounts[i]++;
            }
            else if (pens[i].penState == PenState.NotTouching && isTouchingPaper[i])
            {
                SaveCanvasImage(touchCounts[i]);
                isTouchingPaper[i] = false;
            }

            // Update previous pen state
            previousPenStates[i] = pens[i].penState;
        }
    }

    private void SaveCanvasImage(int count)
    {
        if (rawImage == null)
        {
            Debug.LogError("RawImage component not found.");
            return;
        }

        // Create a temporary RenderTexture with the same dimensions as the RawImage
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            (int)rawImage.rectTransform.rect.width,
            (int)rawImage.rectTransform.rect.height,
            0,
            RenderTextureFormat.ARGB32
        );

        // Set the target RenderTexture for rendering
        Graphics.SetRenderTarget(renderTexture);

        // Render the RawImage to the RenderTexture
        Graphics.Blit(rawImage.texture, renderTexture);

        // Read the pixels from the RenderTexture
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(renderTexture);

        // Encode the texture as a PNG
        byte[] bytes = texture.EncodeToPNG();

        // Define the file path to save the image with the touch count in the name
        string fileName = $"saved_image_{count}.png";
        string filePath = Path.Combine(Application.dataPath, "Image", fileName);

        // Write the bytes to a file
        File.WriteAllBytes(filePath, bytes);

        // Debug.Log($"Image {count} saved to: {filePath}");
    }
}
