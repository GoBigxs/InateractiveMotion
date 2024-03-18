using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using System.IO;
using System.Linq;
using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;

public class UnityMqttReceiver : MonoBehaviour
{

    [SerializeField]public string LocalReceiveTopic = "sony/ui";
    public GameObject controllerPrefab; 
    private String msg;
    public int consecutiveThreshold;
    private MqttClient client;
    private GameObject userController;
    private Dictionary<int, GameObject> controllers = new Dictionary<int, GameObject>(); // Store instantiated controllers by user ID

    private Dictionary<int, int> consecutiveCounts = new Dictionary<int, int>();

    private ConcurrentQueue<System.Action> mainThreadActions = new ConcurrentQueue<System.Action>();
    
    private Pen penRight;
    private Pen penLeft;
    //private int jointArrayCount = 0;
    void Awake()
    {

        // Server Setting 
        // 127.0.0.1 for local server, 192.168.1.100 for on-site server
        // create client instance 
        // Create a new instance of MqttClient
	    client = new MqttClient("127.0.0.1");


        // register to message received 
        client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived; 
        
        string clientId = Guid.NewGuid().ToString(); 
        client.Connect(clientId); 
        
        // subscribe to the topic "/home/temperature" with QoS 2 
        client.Subscribe(new string[] { LocalReceiveTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE }); 
    }

    void Update()
    {
        // Execute all queued actions on the main thread
        while (mainThreadActions.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }

	void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) 
	{ 
        msg = System.Text.Encoding.UTF8.GetString(e.Message);
	    JObject jsonObject = JsonConvert.DeserializeObject<JObject>(msg);
        //var jointsArray = jsonObject["joints"] as JArray;
        var usersArray = jsonObject["users"] as JArray;

        if (e.Topic == LocalReceiveTopic && msg != null)
        {
            // Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message));
            // Debug.Log(jointsArray.Count);

            mainThreadActions.Enqueue(() =>
            {
                // Create a list to keep track of user IDs present in the received message
                List<int> receivedUserIDs = new List<int>();
                // Iterate over each user in the JSON
                foreach (JObject user in usersArray)
                {
                    int id = (int)user["id"];
                    receivedUserIDs.Add(id); // Add the user ID to the list of received IDs
                    Debug.Log("Received User IDs: " + string.Join(", ", receivedUserIDs));
                    // Retrieve the joint positions array
                    JArray jointsArray = user["joints"] as JArray;

                    if (controllers.ContainsKey(id))
                    {
                        // Get the existing controller prefab associated with the user ID
                        userController = controllers[id];

                        penRight = userController.transform.Find("PenRight").GetComponent<Pen>();
                        penLeft = userController.transform.Find("PenLeft").GetComponent<Pen>();


                    }
                    else
                    {
                        
                        // Instantiate controller prefab
                        userController = Instantiate(controllerPrefab, Vector3.one, Quaternion.identity);
                        // Set the name of the instantiated prefab
                        userController.name = "User_" + id;
                        // Attach the controller to the dictionary with the user ID as the key
                        controllers.Add(id, userController);

                        penRight = userController.transform.Find("PenRight").GetComponent<Pen>();
                        penRight.InitializePen(id);
                        penLeft = userController.transform.Find("PenLeft").GetComponent<Pen>();
                        penLeft.InitializePen(id);



                    }

                    // Get the HumanController component attached to the GameObject
                    HumanController humanController = userController.GetComponent<HumanController>();
                    // Call the UpdateJointPositions method on the HumanController component
                    humanController.UpdateJointPositions(jointsArray);

                    Vector3 jointRight = humanController.GetJoint(7);

                    Vector3 jointLeft = humanController.GetJoint(4);


                    // Pen penRight = userController.transform.Find("PenRight").GetComponent<Pen>();
                    // Pen penLeft = userController.transform.Find("PenLeft").GetComponent<Pen>();

                    penRight.UpdateLinePosition(jointRight);
                    penRight.UpdateCanvasTexture();

                    penLeft.UpdateLinePosition(jointLeft);
                    penLeft.UpdateCanvasTexture();

                    // Reset consecutive count for the current ID
                    if (consecutiveCounts.ContainsKey(id))
                        consecutiveCounts[id] = 0;
                    else
                        consecutiveCounts.Add(id, 0);

                }
                // Create a copy of the keys in the controllers dictionary
                List<int> controllerKeys = new List<int>(controllers.Keys);

                // Debug log controllerKeys
                Debug.Log("Controller Keys: " + string.Join(", ", controllerKeys));

                // Iterate over the copied keys to update consecutive counts and destroy game objects if necessary
                foreach (var id in controllerKeys)
                {
                    if (!receivedUserIDs.Contains(id))
                    {
                        Debug.Log("MISSED ID: " + id);
                        
                        // Increment consecutive count for the ID

                        consecutiveCounts[id]++;


                        Debug.Log("Consecutive Counts: " + string.Join(", ", consecutiveCounts.Select(kv => kv.Key + ":" + kv.Value)));


                        // Check if consecutive count exceeds the desired threshold
                        if (consecutiveCounts[id] >= consecutiveThreshold)
                        {
                            // If the consecutive count reaches the threshold, destroy the associated prefab

                            Destroy(controllers[id]);

                            GameObject rightlineRender = GameObject.Find("Line_Right_" + id);
                            GameObject leftlineRender = GameObject.Find("Line_Left_" + id);
  
                            Destroy(rightlineRender);
                            Destroy(leftlineRender);

                            controllers.Remove(id); // Remove the entry from the dictionary
                            consecutiveCounts.Remove(id); // Remove the entry from the consecutive counts dictionary
                        }
                    }
                    else
                    {
                        // Reset consecutive count for the ID since it appeared in the received data
                        consecutiveCounts[id] = 0;
                    }
                }
                    
            });

        }

	} 

    public string GetLastMessage()
    {
        return msg;
    }

        // Example method to access a controller by ID
    public GameObject GetControllerByID(int id)
    {
        if (controllers.ContainsKey(id))
        {
            return controllers[id];
        }
        else
        {
            Debug.LogWarning($"Controller with ID {id} not found.");
            return null;
        }
    }

}