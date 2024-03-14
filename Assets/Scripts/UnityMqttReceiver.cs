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
    private String msg;
    private MqttClient client;
    private GameObject userController;
    public GameObject controllerPrefab;

    private Dictionary<int, GameObject> controllers = new Dictionary<int, GameObject>(); // Store instantiated controllers by user ID

    private ConcurrentQueue<System.Action> mainThreadActions = new ConcurrentQueue<System.Action>();
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

                // Iterate over each user in the JSON
                foreach (JObject user in usersArray)
                {
                    int id = (int)user["id"];
                    // Retrieve the joint positions array
                    JArray jointsArray = user["joints"] as JArray;

                    if (controllers.ContainsKey(id))
                    {
                        // Get the existing controller prefab associated with the user ID
                        userController = controllers[id];

                    }
                    else
                    {
                        // Instantiate controller prefab
                        userController = Instantiate(controllerPrefab, Vector3.one, Quaternion.identity);
                        // Set the name of the instantiated prefab
                        userController.name = "User_" + id;
                        // Attach the controller to the dictionary with the user ID as the key
                        controllers.Add(id, userController);
                    }

                    // Get the HumanController component attached to the GameObject
                    HumanController humanController = userController.GetComponent<HumanController>();
                    // Call the UpdateJointPositions method on the HumanController component
                    humanController.UpdateJointPositions(jointsArray);

                    Vector3 jointRight = humanController.GetJoint(7);

                    Vector3 jointLeft = humanController.GetJoint(4);

                    // Find penRight within the userController
                    GameObject penRightTransform = userController.transform.Find("PenRight").gameObject;
                    GameObject penLeftTransform = userController.transform.Find("PenLeft").gameObject;


                    // Check if PenRight has a Pen component attached
                    Pen penRight = penRightTransform.GetComponent<Pen>();
                    Pen penLeft = penLeftTransform.GetComponent<Pen>();

                    penRight.UpdateLinePosition(jointRight);
                    penRight.UpdateCanvasTexture(id);

                    penLeft.UpdateLinePosition(jointLeft);
                    penLeft.UpdateCanvasTexture(id);

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
