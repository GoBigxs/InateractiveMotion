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
    public HumanController controller;
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
                if (controller != null)
                {

                    // Iterate over each user in the JSON
                    foreach (JObject user in usersArray)
                    {
                        int id = (int)user["id"];

                        // Check if the dictionary already contains the key
                        if (controllers.ContainsKey(id))
                        {

                            // Retrieve the joint positions array
                            JArray jointsArray = user["joints"] as JArray;

                            // Get the existing controller prefab associated with the user ID
                            GameObject userController = controllers[id];

                            // Get the HumanController component attached to the GameObject
                            HumanController humanController = userController.GetComponent<HumanController>();

                            // Call the UpdateJointPositions method on the HumanController component
                            humanController.UpdateJointPositions(jointsArray);
                            // Get the joint positions from the controller
                            Vector3 jointRight = userController.GetComponent<HumanController>().GetJoint(7); // Assuming GetJointPosition returns the joint position
                            //Pen penRight = userController.transform.Find("PenRight").GetComponent<Pen>();

                            // Find penRight within the userController
                            GameObject penRightTransform = userController.transform.Find("PenRight").gameObject;
                            if (penRightTransform != null)
                            {
                                // Check if PenRight has a Pen component attached
                                Pen penRight = penRightTransform.GetComponent<Pen>();
                                if (penRight != null)
                                {
                                    // PenRight has a Pen component attached
                                    // You can proceed with using it
                                    penRight.UpdateLinePosition(jointRight);
                                    penRight.UpdateCanvasTexture(id);
                                }
                                else
                                {
                                    // PenRight does not have a Pen component attached
                                    Debug.LogWarning("PenRight does not have a Pen component attached.");
                                }
                            }
                            else
                            {
                                // PenRight object not found
                                Debug.LogWarning("PenRight object not found.");
                            }

                            // Update penRight position
                            // penRight.UpdateLinePosition(jointRight);
                            // penRight.UpdateCanvasTexture();
                        }
                        else
                        {
                            // Instantiate controller prefab
                            GameObject newController = Instantiate(controllerPrefab, Vector3.one, Quaternion.identity);

                            // Set the name of the instantiated prefab
                            newController.name = "User_" + id; // You can set any name format you prefer
                            Debug.Log("Created new user:" + newController.name);

                            // Set the joint positions of the new controller
                            JArray jointsArray = user["joints"] as JArray;
                            // Get the HumanController component attached to the new GameObject
                            HumanController humanController = newController.GetComponent<HumanController>();

                            // Call the UpdateJointPositions method on the HumanController component
                            humanController.UpdateJointPositions(jointsArray);

                            // Find penRight within the userController
                            GameObject penRightTransform = newController.transform.Find("PenRight").gameObject;

                            Pen penRight = penRightTransform.GetComponent<Pen>();

                            // Find penRight within the userController
                            GameObject penLeftTransform = newController.transform.Find("PenLeft").gameObject;

                            Pen penLeft = penRightTransform.GetComponent<Pen>();

                            // Check if both "penleft" and "penright" were found
                            if (penLeft != null && penRight != null)
                            {
                                // Do something with penLeft and penRight
                                Debug.Log("Found 'penleft' and 'penright' for Controller_" + id);

                                // Get the joint positions from the controller
                                Vector3 jointRight = newController.GetComponent<HumanController>().GetJoint(7); // Assuming GetJointPosition returns the joint position

                                penRight.UpdateLinePosition(jointRight);
                                penRight.UpdateCanvasTexture(id);

                            }
                            else
                            {
                                Debug.LogWarning($"'penleft' or 'penright' not found for Controller_{id}.");
                            }

                            // Attach the controller to the dictionary with the user ID as the key
                            controllers.Add(id, newController);

                        }

                    }
                    
                    // GameObject newController = Instantiate(controllerPrefab,Vector3.zero, Quaternion.identity);
                    // controllers.Add(1, newController);

                    // controller.UpdateJointPositions(jointsArray);
                    // //Debug.Log(jointsArray[7]);
                    // //Debug.Log("Enqueued jointArray #" + jointArrayCount); // Log the enqueue count
                    // //jointArrayCount++; // Increment the counter
                    // Pen penRight = newController.transform.Find("PenRight").GetComponent<Pen>();
                    // Pen penLeft = newController.transform.Find("PenLeft").GetComponent<Pen>();

                    // if (penRight != null && penLeft != null)
                    // {
                    //     Debug.Log("get both pen");
                    // }
                    // else
                    // {
                    //     // One or both Pen components were not found
                    //     // Handle the case where the components are missing or not properly attached
                    // }

                    // Vector3 jointRight = controller.GetJoint(7);
                    // // Log the type and position of penRight
                    // Debug.Log("PenRight Type: " + jointRight);
                    // Debug.Log("PenRight Position: " + penRight.transform.position);

                    // if (penRight != null)
                    // {
                    //     // Get the GameObject that the Pen component is attached to
                    //     GameObject penRightGameObject = penRight.gameObject;
                    //     if (penRightGameObject.activeSelf)
                    //     {
                    //         // The penRight GameObject is active
                    //         // Proceed with calling methods on its Pen component
                    //         penRight.GetComponent<Pen>().UpdateLinePosition(jointRight);
                    //         penRight.GetComponent<Pen>().UpdateCanvasTexture();
                    //     }
                    //     else
                    //     {
                    //         // The penRight GameObject is inactive
                    //         // You may choose to skip calling methods on its Pen component
                    //         Debug.LogWarning("penRight GameObject is inactive");
                    //     }
                    // }
                    // else
                    // {
                    //     // The Pen component on penRight is null
                    //     // Handle this case as appropriate
                    //     Debug.LogError("Pen component on penRight is null");
                    // }


                    // // Pass the joint positions data to the Pen script
                    // penRight.UpdateLinePosition(jointRight);
                    // penRight.UpdateCanvasTexture();

                    // Vector3 jointLeft = controller.GetJoint(4);
                    // penLeft.UpdateLinePosition(jointLeft);
                    // penLeft.UpdateCanvasTexture();


                }
                else
                {
                    Debug.LogError("JointPositionUpdater reference not set in UnityMqttReceiver.");
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
