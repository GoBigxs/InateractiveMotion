// using UnityEngine;
// using UnityEngine.UI;

// public class Pen : MonoBehaviour
// {
//     [Header("Pen Properties")]
//     public Transform tip;                    // Reference to the transform representing the tip of the pen
//     public Material drawingMaterial;         // Material used for drawing lines
//     public Material tipMaterial;             // Material used for the tip of the pen
//     [Range(0.01f, 0.1f)]
//     public float penWidth = 0.01f;           // Width of the drawn line
//     public Color penColor = Color.red;       // Color of the pen
//     public float pointThreshold = 0.01f;     // Minimum distance between points to add to the line

//     public RawImage canvasImage;             // Reference to the RawImage component on the canvas

//     public int lineWidth;                // Width of the drawn line on the canvas

//     private LineRenderer currentDrawing;     // Reference to the current LineRenderer component
//     private Vector3 previousTipPosition;     // Previous position of the pen tip

//     private Texture2D canvasTexture;         // Texture used for drawing on the canvas
//     private Color[] canvasColors;            // Array to store colors of the canvas texture

//     private void Start()
//     {
//         tipMaterial.color = penColor;               // Set the tip color to the specified pen color
//         CreateNewLineRenderer();                     // Initialize the LineRenderer object
//         previousTipPosition = tip.position;         // Initialize the previous tip position

//         // Initialize the canvas texture and colors
//         canvasTexture = new Texture2D((int)canvasImage.rectTransform.rect.width, (int)canvasImage.rectTransform.rect.height);
//         canvasImage.texture = canvasTexture;
//         canvasColors = new Color[canvasTexture.width * canvasTexture.height];
//         ClearCanvas();
//     }

//     private void Update()
//     {
//         UpdateLinePosition();                        // Update the position of the line
//         UpdateCanvasTexture();                      // Update the canvas texture with the drawn lines
//     }

//     // Method to create a new LineRenderer object
//     private void CreateNewLineRenderer()
//     {
//         currentDrawing = new GameObject().AddComponent<LineRenderer>(); // Create a new GameObject with a LineRenderer component
//         currentDrawing.material = drawingMaterial;                      // Assign the drawing material to the LineRenderer
//         currentDrawing.startColor = currentDrawing.endColor = penColor; // Set the start and end color of the line to the pen color
//         currentDrawing.startWidth = currentDrawing.endWidth = penWidth; // Set the start and end width of the line
//         currentDrawing.positionCount = 1;                               // Set the initial position count to 1
//         currentDrawing.SetPosition(0, tip.position);                    // Set the initial position of the line to the tip position
//     }

//     // Method to update the position of the line based on the pen tip's movement
//     private void UpdateLinePosition()
//     {
//         float distance = Vector3.Distance(previousTipPosition, tip.position);
//         if (distance > pointThreshold)
//         {
//             currentDrawing.positionCount++;
//             currentDrawing.SetPosition(currentDrawing.positionCount - 1, tip.position);
//             previousTipPosition = tip.position;
//         }
//     }

//     // Method to update the canvas texture with the drawn lines// Method to update the canvas texture with the drawn lines
//     // // Method to update the canvas texture with the drawn lines
//     private void UpdateCanvasTexture()
//     {
//         ClearCanvas(); // Clear the canvas texture
//         foreach (var lineRenderer in FindObjectsOfType<LineRenderer>())
//         {
//             for (int i = 1; i < lineRenderer.positionCount; i++)
//             {
//                 Vector2 startPixelUV = WorldToCanvasPoint(lineRenderer.GetPosition(i - 1));
//                 Vector2 endPixelUV = WorldToCanvasPoint(lineRenderer.GetPosition(i));

//                 if (IsPenTouchingPaper(lineRenderer.GetPosition(i - 1)) && IsPenTouchingPaper(lineRenderer.GetPosition(i)))
//                 {
//                     DrawLine(startPixelUV, endPixelUV, lineWidth);
//                 }
//             }
//         }
//         canvasTexture.SetPixels(canvasColors);
//         canvasTexture.Apply();
//     }
//     // private void UpdateCanvasTexture()
//     // {
//     //     ClearCanvas(); // Clear the canvas texture
//     //     foreach (var lineRenderer in FindObjectsOfType<LineRenderer>())
//     //     {
//     //         Vector2? previousPixelUV = null;
//     //         for (int i = 0; i < lineRenderer.positionCount; i++)
//     //         {
//     //             Vector3 currentPosition = lineRenderer.GetPosition(i);
//     //             Vector2 pixelUV = WorldToCanvasPoint(currentPosition);
//     //             int textureX = Mathf.RoundToInt(pixelUV.x);
//     //             int textureY = Mathf.RoundToInt(pixelUV.y);
//     //             if (textureX >= 0 && textureX < canvasTexture.width && textureY >= 0 && textureY < canvasTexture.height)
//     //             {
//     //                 // Check if the pen is touching the paper (for example, by comparing the pen's position to a certain threshold)
//     //                 if (!IsPenTouchingPaper(currentPosition))
//     //                 {
//     //                     // If pen is not touching paper, don't draw on canvas
//     //                     continue;
//     //                 }

//     //                 canvasColors[textureY * canvasTexture.width + textureX] = penColor;

//     //                 // Connect previous point to current point with a line
//     //                 if (previousPixelUV != null)
//     //                 {
//     //                     Vector3 previousPosition = lineRenderer.GetPosition(i - 1);
//     //                     // Draw line only if the previous point is touching paper
//     //                     if (IsPenTouchingPaper(previousPosition))
//     //                     {
//     //                         DrawLine(WorldToCanvasPoint(previousPosition), pixelUV, lineWidth);
//     //                     }
//     //                 }
//     //                 previousPixelUV = pixelUV;
//     //             }
//     //         }
//     //     }
//     //     canvasTexture.SetPixels(canvasColors);
//     //     canvasTexture.Apply();
//     // }


//     // Method to check if the pen is touching the paper at a certain point
//     private bool IsPenTouchingPaper(Vector3 position)
//     {
//         // Check
//         return position.z >= 1.10f;
//     }


//     // // Method to draw a line on the canvas texture using Bresenham's line algorithm
//     // private void DrawLine(Vector2 start, Vector2 end)
//     // {
//     //     int startX = Mathf.RoundToInt(start.x);
//     //     int startY = Mathf.RoundToInt(start.y);
//     //     int endX = Mathf.RoundToInt(end.x);
//     //     int endY = Mathf.RoundToInt(end.y);

//     //     int dx = Mathf.Abs(endX - startX);
//     //     int dy = Mathf.Abs(endY - startY);
//     //     int sx = startX < endX ? 1 : -1;
//     //     int sy = startY < endY ? 1 : -1;
//     //     int err = dx - dy;

//     //     while (true)
//     //     {
//     //         if (startX == endX && startY == endY) break;
//     //         if (startX >= 0 && startX < canvasTexture.width && startY >= 0 && startY < canvasTexture.height)
//     //         {
//     //             canvasColors[startY * canvasTexture.width + startX] = penColor;
//     //         }
//     //         int e2 = 2 * err;
//     //         if (e2 > -dy)
//     //         {
//     //             err -= dy;
//     //             startX += sx;
//     //         }
//     //         if (e2 < dx)
//     //         {
//     //             err += dx;
//     //             startY += sy;
//     //         }
//     //     }
//     // }

//     // private void DrawLine(Vector2 start, Vector2 end, int lineWidth)
//     // {
//     //     // Iterate over each pixel between start and end points
//     //     for (int i = 0; i < lineWidth; i++)
//     //     {
//     //         // Calculate offset based on line width
//     //         Vector2 offset = new Vector2(-lineWidth / 2 + i, -lineWidth / 2 + i);

//     //         // Draw line segment with adjusted start and end points
//     //         DrawSegment(start + offset, end + offset);
//     //         Debug.Log("draw i: " + i);
//     //     }
//     // }
//     private void DrawLine(Vector2 start, Vector2 end, int lineWidth)
//     {
//         // Draw the first segment
//         DrawSegment(start, end, lineWidth);

//         // If the line width is greater than 1, draw additional segments to fill the gap
//         for (int i = 1; i < lineWidth; i++)
//         {
//             // Calculate the start and end points for the next segment
//             Vector2 adjustedStart = start + new Vector2(0, i);
//             Vector2 adjustedEnd = end + new Vector2(0, i);

//             // Draw the additional segment
//             DrawSegment(adjustedStart, adjustedEnd, lineWidth);
//         }
//     }


//     private void DrawSegment(Vector2 start, Vector2 end, int lineWidth)
//     {
//         // Calculate the delta values for the line
//         float dx = Mathf.Abs(end.x - start.x);
//         float dy = Mathf.Abs(end.y - start.y);
        
//         // Determine the sign for each axis
//         int sx = (start.x < end.x) ? 1 : -1;
//         int sy = (start.y < end.y) ? 1 : -1;

//         // Start position
//         float x = start.x;
//         float y = start.y;

//         // Error value for adjusting the next pixel position
//         float error = dx - dy;

//         // Iterate over the line and set pixels
//         for (int i = 0; i <= Mathf.Max(dx, dy); i++)
//         {
//             // Set the pixel at the current position
//             int pixelX = Mathf.RoundToInt(x);
//             int pixelY = Mathf.RoundToInt(y);

//             if (pixelX >= 0 && pixelX < canvasTexture.width && pixelY >= 0 && pixelY < canvasTexture.height)
//             {
//                 canvasColors[pixelY * canvasTexture.width + pixelX] = penColor;
//             }

//             // Calculate the next pixel position
//             float error2 = error * 2;
//             if (error2 > -dy)
//             {
//                 error -= dy;
//                 x += sx;
//             }
//             if (error2 < dx)
//             {
//                 error += dx;
//                 y += sy;
//             }
//         }
//     }




//     private void DrawSegment1(Vector2 start, Vector2 end)
//     {
//         // Convert start and end points to integers
//         int startX = Mathf.RoundToInt(start.x);
//         int startY = Mathf.RoundToInt(start.y);
//         int endX = Mathf.RoundToInt(end.x);
//         int endY = Mathf.RoundToInt(end.y);

//         // Calculate the difference between start and end points
//         int dx = Mathf.Abs(endX - startX);
//         int dy = Mathf.Abs(endY - startY);

//         // Determine the direction of the line
//         int sx = startX < endX ? 1 : -1;
//         int sy = startY < endY ? 1 : -1;

//         // Calculate error for line drawing
//         int err = dx - dy;

//         while (true)
//         {
//             // Plot the current point
//             if (startX >= 0 && startX < canvasTexture.width && startY >= 0 && startY < canvasTexture.height)
//             {
//                 canvasColors[startY * canvasTexture.width + startX] = penColor;
//             }

//             // Check if we reached the end point
//             if (startX == endX && startY == endY) 
//                 break;

//             // Calculate error for the next step
//             int e2 = 2 * err;

//             // Adjust the error and update the current point
//             if (e2 > -dy)
//             {
//                 err -= dy;
//                 startX += sx;
//             }
//             if (e2 < dx)
//             {
//                 err += dx;
//                 startY += sy;
//             }
//         }
//     }



//     // Method to convert world position to canvas position
//     private Vector2 WorldToCanvasPoint(Vector3 worldPosition)
//     {
//         // Scale the world position by 100 to match the canvas size
//         Vector3 scaledPosition = worldPosition * 100f;
//         Debug.Log("worldPosition: " + worldPosition + ", " + "scaledPosition: " + scaledPosition);


//         // Assuming your 3D world space is within a certain range, you can scale it to fit the 500x500 canvas
//         float scaledX = Mathf.InverseLerp(-1000f, 1000f, scaledPosition.x) * 512f;
//         float scaledY = Mathf.InverseLerp(-1000f, 1000f, scaledPosition.y) * 512f;
//         Debug.Log("scaledX: " + scaledX + ", " + "scaledY: " + scaledY);

//         return new Vector2(scaledX, scaledY);
//     }



//     // Method to clear the canvas texture
//     private void ClearCanvas()
//     {
//         for (int i = 0; i < canvasColors.Length; i++)
//         {
//             canvasColors[i] = Color.clear;
//         }
//     }

//     // Method to clear the current drawing and create a new LineRenderer
//     public void ResetDrawing()
//     {
//         Destroy(currentDrawing.gameObject);
//         CreateNewLineRenderer();
//     }
// }



// using UnityEngine;
// using UnityEngine.UI;
// using System.IO;

// public class CanvasImageSaver : MonoBehaviour
// {
//     public Pen pen; // Reference to the Pen script

//     private PenState previousPenState; // Previous state of the pen
//     private RawImage rawImage; // Reference to the RawImage component

//     private void Start()
//     {
//         previousPenState = pen.penState; // Initialize the previous pen state

//         // Find the RawImage component in children
//         rawImage = GetComponentInChildren<RawImage>();
//         if (rawImage == null)
//         {
//             Debug.LogError("RawImage component not found.");
//         }
//     }

//     private void Update()
//     {
//         // Check if the pen state has changed from touching to not touching
//         if (previousPenState == PenState.Touching && pen.penState == PenState.NotTouching)
//         {
//             SaveCanvasImage(); // Save the canvas image when the pen state changes
//         }

//         // Update the previous pen state
//         previousPenState = pen.penState;
//     }

//     private void SaveCanvasImage()
//     {
//         if (rawImage == null)
//         {
//             Debug.LogError("RawImage component not found.");
//             return;
//         }

//         // Create a temporary RenderTexture with the same dimensions as the RawImage
//         RenderTexture renderTexture = RenderTexture.GetTemporary(
//             (int)rawImage.rectTransform.rect.width,
//             (int)rawImage.rectTransform.rect.height,
//             0,
//             RenderTextureFormat.ARGB32
//         );

//         // Set the target RenderTexture for rendering
//         Graphics.SetRenderTarget(renderTexture);

//         // Render the RawImage to the RenderTexture
//         Graphics.Blit(rawImage.texture, renderTexture);

//         // Read the pixels from the RenderTexture
//         Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height);
//         texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
//         texture.Apply();

//         // Release the temporary RenderTexture
//         RenderTexture.ReleaseTemporary(renderTexture);

//         // Encode the texture as a PNG
//         byte[] bytes = texture.EncodeToPNG();

//         // Define the file path to save the image
//         string filePath = "/home/SONY/s7000036356/Documents/InteractiveMotion/Assets/Image/saved_image.png";

//         // Write the bytes to a file
//         File.WriteAllBytes(filePath, bytes);

//         Debug.Log("Image saved to: " + filePath);
//     }

// }
