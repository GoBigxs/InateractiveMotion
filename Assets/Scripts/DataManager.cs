using UnityEngine;
using System.Collections.Generic;
using System.Linq;
 
public class DataManager : MonoBehaviour
{
    // Queue to store dictionaries with task ID as key and data as value
    private static Queue<Dictionary<int, Data>> dataQueue = new Queue<Dictionary<int, Data>>();
 
    // Method to add data to the queue as a dictionary
    public static void AddData(int taskID, Data newData)
    {
        // Create a new dictionary for this data entry
        Dictionary<int, Data> dataEntry = new Dictionary<int, Data>();
        dataEntry.Add(taskID, newData);
 
        // Enqueue the dictionary to the queue
        dataQueue.Enqueue(dataEntry);
    }
 
    // Method to peek at the first element of the queue
    public static (int taskID, Data data) PeekFirstData()
    {
        // Check if the queue is not empty
        if (dataQueue.Count > 0)
        {
            // Peek at the first dictionary in the queue
            Dictionary<int, Data> dataEntry = dataQueue.Peek();
            KeyValuePair<int, Data> entry = dataEntry.First();

            int taskID = entry.Key;
            Data data = entry.Value;
            return (taskID, data);
 
            // Get the data associated with the task ID
            // foreach (var kvp in dataEntry)
            // {
            //     return kvp.Value;
            // }
        }
        else{
            //Debug.LogWarning("Data queue is empty.");
            return (-1, null);
        }
    }
 
    // Method to retrieve and remove data from the queue for a specific task ID
    public static void RetrieveAndRemoveData()
    {
        // // Check if the queue is not empty
        // if (dataQueue.Count > 0)
        // {
        //     // Dequeue the dictionary from the queue
        //     Dictionary<int, Data> dataEntry = dataQueue.Dequeue();
 
        //     // Check if the dictionary contains the task ID
        //     if (dataEntry.ContainsKey(taskID))
        //     {
        //         // Retrieve and remove the data from the dictionary
        //         return dataEntry[taskID];
        //     }
        //     else
        //     {
        //         Debug.LogWarning("No data found for task ID: " + taskID);
        //         return null;
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning("Data queue is empty.");
        //     return null;
        // }
        if (dataQueue.Count > 0)
        {
            Dictionary<int, Data> firstEntry = dataQueue.Dequeue();
            // Now, firstEntry contains the first item that was in the queue, and it has been removed from the queue.
        }
        else
        {
            //Debug.LogWarning("Attempted to dequeue from an empty queue.");
        }
    }

}

 
// Define a class to represent your data structure
public class Data
{
    public int userID;
    public string side;
    // public int taskID;
    public Vector2 position;
    public int size;
    public int[] lineIDs;
    // public Dictionary<int, int> lineIDsCnt;
 
    // Constructor
    public Data(int userID, string side, Vector2 position, int size, int[] lineIDs)
    {
        this.userID = userID;
        this.side = side;
        this.position = position;
        this.size = size;
        this.lineIDs = lineIDs;
        // this.lineIDsCnt = lineIDsCnt;
        // this.taskID = taskID;
    }
}