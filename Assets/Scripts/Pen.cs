using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;

public enum PenState
{
    NotTouching,
    Touching
}



public class Pen : MonoBehaviour 
{
    [Header("Pen Properties")]
    // public Transform tip;                    // Reference to the transform representing the tip of the pen 
    public Material drawingMaterial;         // Material used for drawing lines
    public Material tipMaterial;             // Material used for the tip of the pen
    [Range(0.01f, 0.1f)]
    public float penWidth = 0.01f;           // Width of the drawn line
    public Color penColor = Color.red;       // Color of the pen

    public RawImage canvasImage;             // Reference to the RawImage component on the canvas

    public int lineWidth;                // Width of the drawn line on the canvas
    private LineRenderer currentDrawing;     // Reference to the current LineRenderer component
    private Vector3 previousTipPosition;     // Previous position of the pen tip
    public PenState penState { get; private set; } // Public property to access penState
    private Texture2D canvasTexture;         // Texture used for drawing on the canvas
    private Color[] canvasColors;            // Array to store colors of the canvas texture
    private int prevPositionCnt = 0;

    private void Start()
    {
        tipMaterial.color = penColor;                // Set the tip color to the specified pen color
        CreateNewLineRenderer();                     // Initialize the LineRenderer object
        previousTipPosition = new Vector3(0.0f, 0.0f, 0.0f);          // Initialize the previous tip position
        // Initialize the canvas texture and colors
        canvasTexture = new Texture2D((int)canvasImage.rectTransform.rect.width, (int)canvasImage.rectTransform.rect.height);
        canvasImage.texture = canvasTexture;
        canvasColors = new Color[canvasTexture.width * canvasTexture.height];
        ClearCanvas();
    }


    // private void Update()
    // {
    //     // UpdateLinePosition();                       // Update the position of the line
    //     // UpdatePenState();                           //Update the state of pen
    //     UpdateCanvasTexture();                      // Update the canvas texture with the drawn lines
    // }

        // Method to update the state of the pen based on whether it's touching the paper
    // private void UpdatePenS HumanController controller;tate()
    // {
    //     penState = IsPenTouchingPaper(tip.position) ? PenState.Touching : PenState.NotTouching;
    //     //Debug.Log("tip position : " + tip.position + "penstate : " + penState);
    // }

    // Method to create a new LineRenderer object
    private void CreateNewLineRenderer()
    {
        currentDrawing = new GameObject().AddComponent<LineRenderer>(); // Create a new GameObject with a LineRenderer component
        currentDrawing.material = drawingMaterial;                      // Assign the drawing material to the LineRenderer
        currentDrawing.startColor = currentDrawing.endColor = penColor; // Set the start and end color of the line to the pen color
        currentDrawing.startWidth = currentDrawing.endWidth = penWidth; // Set the start and end width of the line
        currentDrawing.positionCount = 0;                               // Set the initial position count to 1
        // currentDrawing.SetPosition(0, tip.position);                    // Set the initial position of the line to the tip position 
    }


    // Method to update the position of the line based on the pen tip's movement
    public void UpdateLinePosition(Vector3 joint)
    {
        // tip.position = joint;
        float distance = Vector3.Distance(previousTipPosition, joint);
        Debug.Log(joint);
        if (distance > 0.0f)
        {
            // Debug.Log(distance);
            currentDrawing.positionCount++;
            //Debug.Log(currentDrawing.positionCount);
            currentDrawing.SetPosition(currentDrawing.positionCount - 1, joint);
            // StartCoroutine(SendDataToServer(currentDrawing.positionCount - 1, previousTipPosition, tip.position, false));

            previousTipPosition = joint;
            prevPositionCnt = currentDrawing.positionCount - 1;
            
        }
    }


    // Method to update the canvas texture with the drawn lines
    public void UpdateCanvasTexture()
    {

        LineRenderer lineRenderer = currentDrawing; // Assuming you have only one LineRenderer
        int pointCnt = lineRenderer.positionCount;
        if (pointCnt >= 2 && pointCnt > prevPositionCnt) 
        {
            Vector3 startPixel = lineRenderer.GetPosition(prevPositionCnt-1);
            Vector3 endPixel = lineRenderer.GetPosition(pointCnt-1);

            Vector2 startPixelUV = WorldToCanvasPoint(startPixel);
            Vector2 endPixelUV = WorldToCanvasPoint(endPixel);

            if (IsPenTouchingPaper(startPixel) && IsPenTouchingPaper(endPixel))
            {
                StartCoroutine(SendDataToServer(prevPositionCnt, startPixel.z, endPixel.z, startPixelUV, endPixelUV, true));
                DrawLine(startPixelUV, endPixelUV, lineWidth);
            }
            else
            {
                StartCoroutine(SendDataToServer(prevPositionCnt, startPixel.z, endPixel.z, startPixelUV, endPixelUV, false));
            }


        }


        canvasTexture.SetPixels(canvasColors);
        canvasTexture.Apply();
    }


    // Method to check if the pen is touching the paper at a certain point
    private bool IsPenTouchingPaper(Vector3 position)
    {
        if (position.z>=1.9f)
        {
            //Debug.Log("Point " + point + " is within the desired area.");
            // Return true if the point is within the area
            return true;
        }
        return false;
    }


    private void DrawLine(Vector2 start, Vector2 end, int lineWidth)
    {
        // Calculate the direction vector of the line
        Vector2 direction = (end - start).normalized;

        // Calculate the perpendicular vector to the line
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        // Draw the first segment
        DrawSegment(start - perpendicular * lineWidth / 2f, end - perpendicular * lineWidth / 2f, lineWidth);

        // If the line width is greater than 1, draw additional segments to fill the gap
        for (int i = 1; i < lineWidth; i++)
        {
            float offset = (float)i / (float)(lineWidth - 1) * lineWidth; // Offset along the perpendicular vector
            // Calculate the adjusted start and end points
            Vector2 adjustedStart = start - perpendicular * offset;
            Vector2 adjustedEnd = end - perpendicular * offset;

            // Draw the additional segment
            DrawSegment(adjustedStart, adjustedEnd, lineWidth);
        }
    }

    private IEnumerator SendDataToServer(int i, float z1, float z2, Vector2 start, Vector2 end, bool penTouchingPaper)
    {
        // Define the URL of your Python server
        string serverURL = "http://localhost:5000/receive";

        // Create a data object to hold the start and end positions
        StartEndPositions data = new StartEndPositions();
        data.i = i;
        data.z1 = z1;
        data.z2 = z2;
        data.startX = start.x;
        data.startY = start.y;
        data.endX = end.x;
        data.endY = end.y;
        data.penTouchingPaper = penTouchingPaper;
        

        // Convert data object to JSON
        string jsonData = JsonUtility.ToJson(data);

        // Debug and print the JSON data
        //Debug.Log("JSON Data: " + jsonData);

        // Create a UnityWebRequest
        using (UnityWebRequest request = UnityWebRequest.Put(serverURL, jsonData))
        {
            // Set request headers
            request.SetRequestHeader("Content-Type", "application/json");

            // Set timeout (in seconds)
            //request.timeout = 15; // Adjust timeout duration as needed

            // Send the request
            yield return request.SendWebRequest();

            // Check for errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                // Log the error
                Debug.LogError(request.error);
                // // Retry the request after a delay
                // yield return new WaitForSeconds(1); // Wait for 1 second (for example)
                // StartCoroutine(SendDataToServer(start, end, penTouchingPaper));
                // yield break; // Exit the coroutine
            }
            else
            {
                Debug.Log("Data sent successfully");
                Debug.Log(request.downloadHandler.text); // Log the response from the server
            }
        }
    }


    // Define a class to hold start and end positions
    [System.Serializable]
    public class StartEndPositions
    {
        public float startX;
        public float startY;
        public float endX;
        public float endY;
        public int i;
        public float z1;
        public float z2;
        public bool penTouchingPaper;
    }

    private void DrawSegment(Vector2 start, Vector2 end, int lineWidth)
    {
        // Calculate the delta values for the line
        float dx = Mathf.Abs(end.x - start.x);
        float dy = Mathf.Abs(end.y - start.y);

        // Determine the sign for each axis
        int sx = (start.x < end.x) ? 1 : -1;
        int sy = (start.y < end.y) ? 1 : -1;

        // Start position
        float x = start.x;
        float y = start.y;

        // Error value for adjusting the next pixel position
        float error = dx - dy;

        // Iterate over the line and set pixels
        for (int i = 0; i <= Mathf.Max(dx, dy); i++)
        {
            // Set the pixels along the line for the given line width
            for (int j = -lineWidth / 2; j <= lineWidth / 2; j++)
            {
                int pixelX = Mathf.RoundToInt(x);
                int pixelY = Mathf.RoundToInt(y) + j;

                if (pixelX >= 0 && pixelX < canvasTexture.width && pixelY >= 0 && pixelY < canvasTexture.height)
                {
                    canvasColors[pixelY * canvasTexture.width + pixelX] = penColor;
                }
            }

            // Calculate the next pixel position
            float error2 = error * 2;
            if (error2 > -dy)
            {
                error -= dy;
                x += sx;
            }
            if (error2 < dx)
            {
                error += dx;
                y += sy;
            }
        }
    }

    // Method to convert world position to canvas position
    private Vector2 WorldToCanvasPoint(Vector3 worldPosition)
    {

        // Scale the world position by 100 to match the canvas size
        Vector3 scaledPosition = worldPosition * 100;
        //Debug.Log("world: " + worldPosition + ", scaled: "+ scaledPosition);
        //Debug.Log("worldPosition: " + worldPosition + ", " + "scaledPosition: " + scaledPosition);


        // Assuming your 3D world space is within a certain range, you can scale it to fit the 500x500 canvas
        float scaledX = Mathf.InverseLerp(-250f, 250f, scaledPosition.x) * 600f;
        float scaledY = Mathf.InverseLerp(20f, 220f, scaledPosition.y) * 270f;
        //float scaledX = scaledPosition.x;
        //float scaledY = scaledPosition.y;
        // Debug.Log("scaledX: " + scaledX + ", " + "scaledY: " + scaledY);

        return new Vector2(scaledX, scaledY);
    }

    // Method to clear the canvas texture
    private void ClearCanvas()
    {
        for (int i = 0; i < canvasColors.Length; i++)
        {
            canvasColors[i] = Color.clear;
        }
    }

    // Method to clear the current drawing and create a new LineRenderer
    public void ResetDrawing()
    {
        Destroy(currentDrawing.gameObject);
        CreateNewLineRenderer();
    }
}