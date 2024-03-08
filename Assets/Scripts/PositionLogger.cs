using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PositionLogger : MonoBehaviour
{
    private string logPath;
    private List<string> logBuffer = new List<string>();
    public float logWriteInterval = 5f; // Time in seconds between writes
    private float nextLogTime = 0f;
    private Vector3 lastPos;

    void Awake()
    {
        string dir = "/home/local/qinxin/Thesis/MasterThesis/MotionProcessor/Data_Unity";
        logPath = Path.Combine(dir, "positionLog.txt");
        Debug.Log(logPath);
        // Optionally clear the log file at the start of the session
        File.WriteAllText(logPath, "");
        lastPos = transform.position;
    }

    void Update()
    {
        // Example of logging a position; replace with your actual logging
        // if (transform.position != lastPos){
        LogPosition(transform.position);
        //     lastPos = transform.position;
        // }
        
        // Check if it's time to write the buffer to the file
        if (Time.time >= nextLogTime)
        {
            FlushLogBuffer();
            nextLogTime = Time.time + logWriteInterval;
        }
    }

    void LogPosition(Vector3 position)
    {
        // // Format the log string as desired
        // string logEntry = $"{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: Position - {position}\n";
        // // Add the log string to the buffer
        // logBuffer.Add(logEntry);

        // Format each component of the position to have 5 digits after the decimal point
        string formattedPosition = string.Format("({0}, {1}, {2})",
            position.x.ToString("F5"),
            position.y.ToString("F5"),
            position.z.ToString("F5"));

        // Format the log string as desired, including the formatted position
        string logEntry = $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}: Position - {formattedPosition}\n";

        // Add the log string to the buffer
        logBuffer.Add(logEntry);
    }

    void FlushLogBuffer()
    {
        // Write all buffered log entries to the file at once
        File.AppendAllLines(logPath, logBuffer);
        // Clear the buffer after writing
        logBuffer.Clear();
    }

    void OnDestroy()
    {
        // Ensure all remaining log entries are written to the file when the object is destroyed
        FlushLogBuffer();
    }
}
