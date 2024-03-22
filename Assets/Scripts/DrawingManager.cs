using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;

public class DrawingManager : MonoBehaviour
{
    public static DrawingManager Instance { get; private set; }
    public RawImage canvasImage; // Assign this in the Unity Editor
    private Dictionary<int, Color[]> userDrawings = new Dictionary<int, Color[]>(); // Mapping between user IDs and their drawing data
    private Texture2D canvasTexture; // Single canvas texture for all users
    private Color[] canvasColors;    // Array to store colors of the canvas texture
    private RectTransform canvasRectTransform; // RectTransform of the Canvas


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCanvas();
            canvasRectTransform = canvasImage.GetComponent<RectTransform>(); // Get RectTransform component of the Canvas
        }
    }

    void InitializeCanvas()
    {
        canvasTexture = new Texture2D((int)canvasImage.rectTransform.rect.width, (int)canvasImage.rectTransform.rect.height);
        canvasImage.texture = canvasTexture;
        ClearCanvas();
    }

    public void ClearCanvas()
    {
        canvasColors = new Color[canvasTexture.width * canvasTexture.height];
        for (int i = 0; i < canvasColors.Length; i++)
        {
            canvasColors[i] = Color.clear;
        }
        canvasTexture.SetPixels(canvasColors);
        canvasTexture.Apply();
    }
    
    public Texture2D GetCanvasTexture()
    {

        return canvasTexture;

    }

    public Color[] GetCanvasColors()
    {
        return canvasColors;
    }

    public Color[] GetCanvasColorsOfUser(int userID)
    {
        if (!userDrawings.ContainsKey(userID))
        {
            Color[] colorsUser = new Color[(int)canvasImage.rectTransform.rect.width * (int)canvasImage.rectTransform.rect.height];
            userDrawings.Add(userID, colorsUser);
            return colorsUser;
        }
        else{
            return userDrawings[userID];
        }
    }

    public void UpdateCanvas(int id, Color[] colors, Color[] colorsUser)
    {
        canvasTexture.SetPixels(colors);
        canvasTexture.Apply();
        if (userDrawings.ContainsKey(id))
        {
            userDrawings[id] = colorsUser;
        }
        else
        {
            userDrawings.Add(id, colorsUser);
        }

    }

    // Function to remove user drawing and update canvas
    public void RemoveUserDrawing(int id)
    {
        if (userDrawings.ContainsKey(id))
        {
            // Get the drawing of the user
            Color[] userDrawing = userDrawings[id];

            // Iterate through the pixels of the user drawing
            for (int i = 0; i < userDrawing.Length; i++)
            {
                // Check if the pixel in the user drawing is not clear
                if (userDrawing[i] != Color.clear)
                {
                    // Calculate the position of the pixel on the canvas
                    int x = i % canvasTexture.width;
                    int y = i / canvasTexture.width;

                    // Set the corresponding pixel on the canvas to clear color
                    canvasColors[y * canvasTexture.width + x] = Color.clear;
                }

            }

            // Update the canvas texture with the modified colors
            canvasTexture.SetPixels(canvasColors);
            canvasTexture.Apply();

            // Remove the user drawing from the dictionary
            userDrawings.Remove(id);
        }
    }

    // Function to get the world position of the Canvas
    public Vector3 GetCanvasWorldPosition()
    {
        // Get the position of the Canvas in screen space
        Vector2 screenPosition = canvasRectTransform.position;

        // Convert screen space position to world space
        Vector3 worldPosition = Vector3.zero;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, screenPosition, null, out worldPosition);

        return worldPosition;
    }

}