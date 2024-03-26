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
    // // public Transform tip;                    // Reference to the transform representing the tip of the pen
    Coroutine drawing;

    public GameObject linePrefab;
    public Material drawingMaterial;         // Material used for drawing lines
    public Material tipMaterial;             // Material used for the tip of the pen
    [Range(0.01f, 0.1f)]
    public float penWidth = 0.01f;           // Width of the drawn line
    private Color penColor;       // Color of the pen
    public string side = "Left";

    private int userID;

    private LineRenderer currentDrawing;     // Reference to the current LineRenderer component
    private Vector3 previousTipPosition;     // Previous position of the pen tip
    public PenState penState { get; private set; } // Public property to access penState
    private Texture2D canvasTexture;         // Texture used for drawing on the canvas
    private Color[] canvasColors;            // Array to store colors of the canvas texture
    private Color[] canvasColorsOfUser;
    private int prevPositionCnt = 0;
    private DrawingManager DrawingManager; // Reference to the DrawingManager component
    private List<Color> colorList = new List<Color>();
    private Vector3 canvasPosition;
    private LineRenderer line;
    private int tempID, taskID;

    private bool curState = false;
    private bool prevState = false;
    private Vector3 startPixel;
    private Vector3 endPixel;
    private Vector2 startPixelCanvas, endPixelCanvas;



    public void InitializePen(int id)
    {

        userID =id;
        // Add colors to the list
        colorList.Add(Color.red);
        colorList.Add(Color.blue);
        colorList.Add(Color.green);
        colorList.Add(Color.yellow);
        colorList.Add(Color.cyan);
        // Get a random color from the list
        Color randomColor = GetRandomColor();

        // Do something with the random color (e.g., assign it to a variable)
        penColor = randomColor;

        tipMaterial.color = penColor;                // Set the tip color to the specified pen color
        Create3DLineRenderer(userID);                     // Initialize the LineRenderer object
        previousTipPosition = new Vector3(0.0f, 0.0f, 0.0f);          // Initialize the previous tip position
        // Initialize the canvas texture and colors
        // canvasTexture = new Texture2D((int)canvasImage.rectTransform.rect.width, (int)canvasImage.rectTransform.rect.height);
        // canvasImage.texture = canvasTexture;
        // canvasColors = new Color[canvasTexture.width * canvasTexture.height];

        canvasTexture = DrawingManager.Instance.GetCanvasTexture();
        canvasColors = DrawingManager.Instance.GetCanvasColors();
        canvasColorsOfUser = DrawingManager.Instance.GetCanvasColorsOfUser(userID);
        //Debug.Log("currentDrawing is null: " + (currentDrawing));

        canvasPosition = DrawingManager.Instance.GetCanvasWorldPosition();

    }



    // Method to create a new LineRenderer object
    private void Create3DLineRenderer(int id)
    {

        currentDrawing = new GameObject($"Line_{side}_" + id).AddComponent<LineRenderer>(); // Create a new GameObject with a LineRenderer component
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
        // float distance = Vector3.Distance(previousTipPosition, joint);
        //Debug.Log(previousTipPosition);
        // Debug.Log(distance);
        
        currentDrawing.positionCount++;
        currentDrawing.SetPosition(currentDrawing.positionCount - 1, joint);


        previousTipPosition = joint;
        prevPositionCnt = currentDrawing.positionCount - 1;

        if (currentDrawing.positionCount >= 2)
        {
            startPixel = currentDrawing.GetPosition(prevPositionCnt-1);
            endPixel = currentDrawing.GetPosition(prevPositionCnt);

            // startPixelCanvas = startPixel * 100f;
            // endPixelCanvas = endPixel * 100f;
        }

    }


    // Method to update the canvas texture with the drawn lines
    public void UpdateCanvasTexture(int userID)
    {
        int pointCnt = currentDrawing.positionCount;
        if (pointCnt >= 2 && pointCnt > prevPositionCnt) 
        {
            // startPixel = lineRenderer.GetPosition(prevPositionCnt-1);
            // endPixel = lineRenderer.GetPosition(pointCnt-1);

            Vector2 startPixelUV = WorldToCanvasPoint(startPixel);
            Vector2 endPixelUV = WorldToCanvasPoint(endPixel);
            // Debug.Log($"Line_{side}_ + {userID} " +  (drawing == null));
            if (IsPenTouchingPaper(startPixel) && IsPenTouchingPaper(endPixel))
            {
                curState = true;
                if (curState != prevState)
                {
                    StartLine();

                }
                // StartCoroutine(SendDataToServer(startPixelUV, endPixelUV, true, side, userID, tempID));

            }
            else
            {
                curState = false;
                if (curState != prevState)
                {
                    FinishLine();
                }
                StartCoroutine(SendDataToServer(startPixelUV, endPixelUV, false, side, userID, tempID));

                // Debug.Log($"Line_{side}_ + {userID} " + "finishline");
            }
            prevState = curState;

        }
    }
    
    private void StartLine()
    {
        if (drawing != null)
        {
            // Debug.Log($"Line_{side}_ + {userID} " + "StartLine coroutine stopped");
            //StopCoroutine(drawing);
        }
        tempID ++;
        drawing = StartCoroutine(Draw2DLine(tempID));
        // Debug.Log($"Line_{side}_ + {userID} " + "coroutine started"+ (drawing == null));

    }
    private void FinishLine()
    {
        if (drawing != null)
        {
            StopCoroutine(drawing);
            drawing = null;
            // Debug.Log($"Line_{side}_ + {userID} " + "coroutine FinishLine executed");

        }
    }

    IEnumerator Draw2DLine(int tempID)
    {
        GameObject line2d = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        line = line2d.GetComponent<LineRenderer>();
        line.name =  $"Line_User{userID}_{side}_" + tempID;
        //Debug.Log("Line created " + line.name);
        

        // Debug.Log($"Line_{side}_ + {userID} " + "line created");

            // Set line width
        line.startWidth = 2f; // Adjust the width as needed
        line.endWidth = 2f; // Adjust the width as needed

        // Set line color
        line.material.color = Color.red; // Adjust the color as needed


        line.positionCount = 0;
        while (true)
        {
            startPixelCanvas = WorldToCanvasPoint(startPixel);
            // Vector3 startPixel = lineRenderer.GetPosition(prevPositionCnt-1);
            Vector3 startPosition = new Vector3(startPixelCanvas.x, startPixelCanvas.y, canvasPosition.z);
            //Vector3 endPosition = new Vector3(endPixel.x, endPixel.y, canvasPosition.z);
            line.positionCount+= 1;
            line.SetPosition(line.positionCount - 1, startPosition);

            Vector2 startPixelUV = WorldToCanvasPoint(startPixel);
            Vector2 endPixelUV = WorldToCanvasPoint(endPixel);

            StartCoroutine(SendDataToServer(startPixelUV, endPixelUV, true, side, userID, tempID));

            //line.SetPosition(line.positionCount - 1, endPosition);
            // Debug.Log($"Line_{side}_ + {userID}_ " + line.positionCount + ": " + startPosition);
            yield return null;
        }
    }

    // Method to check if the pen is touching the paper at a certain point
    private bool IsPenTouchingPaper(Vector3 position)
    {
        if (position.z>=1.7f)
        {
            //Debug.Log("Point " + point + " is within the desired area.");
            // Return true if the point is within the area
            return true;
        }
        return false;
    }
    
    // Function to get a random color from the list
    private Color GetRandomColor()
    {
        if (colorList.Count == 0)
        {
            Debug.LogWarning("Color list is empty!");
            return Color.white;
        }

        int randomIndex = UnityEngine.Random.Range(0, colorList.Count);

        return colorList[randomIndex];
    }

   

    private IEnumerator SendDataToServer(Vector2 start, Vector2 end, bool penTouchingPaper,string side, int userID, int tempID)
    {
        // Define the URL of your Python server
        string serverURL = "http://localhost:5000/receive";

        // Create a data object to hold the start and end positions
        StartEndPositions data = new StartEndPositions();
        data.startX = start.x;
        data.startY = start.y;
        data.endX = end.x;
        data.endY = end.y;
        data.penTouchingPaper = penTouchingPaper;
        data.side = side;
        data.userID = userID;
        data.tempID = tempID;
        
        // Convert data object to JSON
        string jsonData = JsonUtility.ToJson(data);

        // Create a UnityWebRequest
        using (UnityWebRequest request = UnityWebRequest.Put(serverURL, jsonData))
        {
            // Set request headers
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Check for errors
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Debug.Log("Data sent successfully");
                ServerResponse response = JsonUtility.FromJson<ServerResponse>(request.downloadHandler.text);

                // Add info of the new task to the DataManager
                if (response.new_task)
                {
                    taskID = response.task_id;

                    Vector2 position = new Vector2(response.pos[0], response.pos[1]);
                    Data newData = new Data(response.user_id, response.side, position, response.size, response.line_ids);

                    // string lineName = $"Line_User{userID}_{side}_T" + tempID;
                    // string newLineName = $"Line_User{userID}_{side}_" + taskID;
                    //UpdateObjectName(lineName,newLineName);
                    
                    // Use the task_id from the response as the key to add Data to the DataManager
                    DataManager.AddData(response.task_id, newData);

                    

                    Debug.Log($"New Task: {response.new_task}, Task ID: {response.task_id}, Line IDs: {string.Join(", ", response.line_ids)}, User ID: {response.user_id}, Side: {response.side}, Position: [{response.pos[0]}, {response.pos[1]}], Size: {response.size}");
                }
                else
                {
                    Debug.Log($"New Task: {response.new_task}, Task ID: {response.task_id}, Line IDs: {string.Join(", ", response.line_ids)}, User ID: {response.user_id}, Side: {response.side}, Position: null, Size: {response.size}");
                }

            }
            else
            {
                // Log the error
                Debug.LogError(request.error);
            }
        }
    }

    // Method to convert world position to canvas position
    private Vector2 WorldToCanvasPoint(Vector3 worldPosition)
    {
        Vector3 scaledPosition = worldPosition * 100;

        // Assuming your 3D world space is within a certain range, you can scale it to fit the 500x500 canvas
        float scaledX = Mathf.InverseLerp(-250f, 250f, scaledPosition.x) * 600f;
        float scaledY = Mathf.InverseLerp(20f, 220f, scaledPosition.y) * 270f;

        return new Vector2(scaledX, scaledY);
    }

    private void UpdateObjectName(string originalName, string newName)
    {
    
        GameObject foundObject = GameObject.Find(originalName);
        if (foundObject == null)
        {
            Debug.Log("Object " + originalName + "not found");
        }
        else
        {
            Debug.Log("Changed " + originalName + "to " + newName);
            foundObject.name = newName;
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
        public bool penTouchingPaper;
        public string side;
        public int userID;
        public int tempID;
    }

    [System.Serializable]
    public class ServerResponse
    {
        public bool new_task;
        public int task_id;
        public int user_id;
        public int[] line_ids;
        public string side;
        public float[] pos; // pos: [x, y]
        public int size;
    }
  

}