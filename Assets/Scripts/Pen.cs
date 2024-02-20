using UnityEngine;
using UnityEngine.UI;


public enum PenState
{
    NotTouching,
    Touching
}

public class Pen : MonoBehaviour
{
    [Header("Pen Properties")]
    public Transform tip;                    // Reference to the transform representing the tip of the pen 
    public Material drawingMaterial;         // Material used for drawing lines
    public Material tipMaterial;             // Material used for the tip of the pen
    [Range(0.01f, 0.1f)]
    public float penWidth = 0.01f;           // Width of the drawn line
    public Color penColor = Color.red;       // Color of the pen
    public float pointThreshold = 0.01f;     // Minimum distance between points to add to the line

    public RawImage canvasImage;             // Reference to the RawImage component on the canvas

    public int lineWidth;                // Width of the drawn line on the canvas

    private LineRenderer currentDrawing;     // Reference to the current LineRenderer component
    private Vector3 previousTipPosition;     // Previous position of the pen tip
    public PenState penState { get; private set; } // Public property to access penState

    private Texture2D canvasTexture;         // Texture used for drawing on the canvas
    private Color[] canvasColors;            // Array to store colors of the canvas texture

    private void Start()
    {
        tipMaterial.color = penColor;               // Set the tip color to the specified pen color
        CreateNewLineRenderer();                     // Initialize the LineRenderer object
        previousTipPosition = tip.position;         // Initialize the previous tip position

        // Initialize the canvas texture and colors
        canvasTexture = new Texture2D((int)canvasImage.rectTransform.rect.width, (int)canvasImage.rectTransform.rect.height);
        canvasImage.texture = canvasTexture;
        canvasColors = new Color[canvasTexture.width * canvasTexture.height];
        ClearCanvas();
    }


    private void Update()
    {
        UpdatePenState();                            //Update the state of pen
        UpdateLinePosition();                        // Update the position of the line
        UpdateCanvasTexture();                      // Update the canvas texture with the drawn lines
    }

        // Method to update the state of the pen based on whether it's touching the paper
    private void UpdatePenState()
    {
        if (IsPenTouchingPaper(tip.position))
        {
            penState = PenState.Touching;
            Debug.Log(penState);
        }
        else
        {
            penState = PenState.NotTouching;
            Debug.Log(penState);
        }
    }

    // Method to create a new LineRenderer object
    private void CreateNewLineRenderer()
    {
        currentDrawing = new GameObject().AddComponent<LineRenderer>(); // Create a new GameObject with a LineRenderer component
        currentDrawing.material = drawingMaterial;                      // Assign the drawing material to the LineRenderer
        currentDrawing.startColor = currentDrawing.endColor = penColor; // Set the start and end color of the line to the pen color
        currentDrawing.startWidth = currentDrawing.endWidth = penWidth; // Set the start and end width of the line
        currentDrawing.positionCount = 1;                               // Set the initial position count to 1
        currentDrawing.SetPosition(0, tip.position);                    // Set the initial position of the line to the tip position
    }

    // Method to update the position of the line based on the pen tip's movement
    private void UpdateLinePosition()
    {
        float distance = Vector3.Distance(previousTipPosition, tip.position);
        if (distance > pointThreshold)
        {
            currentDrawing.positionCount++;
            currentDrawing.SetPosition(currentDrawing.positionCount - 1, tip.position);
            previousTipPosition = tip.position;
        }
    }

    // Method to update the canvas texture with the drawn lines
    private void UpdateCanvasTexture()
    {
        ClearCanvas(); // Clear the canvas texture
        foreach (var lineRenderer in FindObjectsOfType<LineRenderer>())
        {
            for (int i = 1; i < lineRenderer.positionCount; i++)
            {
                Vector2 startPixelUV = WorldToCanvasPoint(lineRenderer.GetPosition(i - 1));
                Vector2 endPixelUV = WorldToCanvasPoint(lineRenderer.GetPosition(i));

                if (IsPenTouchingPaper(lineRenderer.GetPosition(i - 1)) && IsPenTouchingPaper(lineRenderer.GetPosition(i)))
                {
                    DrawLine(startPixelUV, endPixelUV, lineWidth);
                }
            }
        }
        canvasTexture.SetPixels(canvasColors);
        canvasTexture.Apply();
    }


    // Method to check if the pen is touching the paper at a certain point
    private bool IsPenTouchingPaper(Vector3 position)
    {
        return position.z >= 1.20f;
    }


    private void DrawLine(Vector2 start, Vector2 end, int lineWidth)
    {
        // Draw the first segment
        DrawSegment(start, end);

        // If the line width is greater than 1, draw additional segments to fill the gap
        for (int i = 1; i < lineWidth; i++)
        {
            // Calculate the start and end points for the next segment
            Vector2 adjustedStart = start + new Vector2(0, i);
            Vector2 adjustedEnd = end + new Vector2(0, i);

            // Draw the additional segment
            DrawSegment(adjustedStart, adjustedEnd);
        }
    }


    private void DrawSegment(Vector2 start, Vector2 end)
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
            // Set the pixel at the current position
            int pixelX = Mathf.RoundToInt(x);
            int pixelY = Mathf.RoundToInt(y);

            if (pixelX >= 0 && pixelX < canvasTexture.width && pixelY >= 0 && pixelY < canvasTexture.height)
            {
                canvasColors[pixelY * canvasTexture.width + pixelX] = penColor;
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
        Vector3 scaledPosition = worldPosition * 100f;
        // Debug.Log("worldPosition: " + worldPosition + ", " + "scaledPosition: " + scaledPosition);


        // Assuming your 3D world space is within a certain range, you can scale it to fit the 500x500 canvas
        float scaledX = Mathf.InverseLerp(-1000f, 1000f, scaledPosition.x) * 1300f-350f;
        float scaledY = Mathf.InverseLerp(-1000f, 1000f, scaledPosition.y) * 1300f-550f;
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
