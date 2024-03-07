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
    public Pen pen;
    private ConcurrentQueue<System.Action> mainThreadActions = new ConcurrentQueue<System.Action>();
    private int jointArrayCount = 0;
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
	    var jsonObject = JsonConvert.DeserializeObject<JObject>(msg);
        var jointsArray = jsonObject["joints"] as JArray;

        if (e.Topic == LocalReceiveTopic && msg != null)
        {
            // Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message));
            // Debug.Log(jointsArray.Count);

            mainThreadActions.Enqueue(() =>
            {
                if (controller != null)
                {
                    controller.UpdateJointPositions(jointsArray);
                    //Debug.Log(jointsArray[7]);
                    //Debug.Log("Enqueued jointArray #" + jointArrayCount); // Log the enqueue count
                    //jointArrayCount++; // Increment the counter
                    // Check if the pen reference is not null
                    if (pen != null)
                    {
                        Vector3 joint =  new Vector3(
                        (float)jointsArray[7][0],
                        (float)jointsArray[7][1],
                        (float)jointsArray[7][2]
                        );
                        // Pass the joint positions data to the Pen script
                        pen.UpdateLinePosition(joint);

                    }

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

}
