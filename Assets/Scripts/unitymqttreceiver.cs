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

public class UnityMqttReceiver : MonoBehaviour
{

    [SerializeField]public string LocalReceiveTopic = "sony/ui";
    private String msg;
    private MqttClient client;

    void Awake()
    {

        // Server Setting 
        // 127.0.0.1 for local server, 192.168.1.100 for on-site server
        // create client instance 
        // Create a new instance of MqttClient
	client = new MqttClient("tcp://127.0.0.1:1883");


        // register to message received 
        client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived; 
        
        string clientId = Guid.NewGuid().ToString(); 
        client.Connect(clientId); 
        
        // subscribe to the topic "/home/temperature" with QoS 2 
        client.Subscribe(new string[] { LocalReceiveTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE }); 
    }

	void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) 
	{ 
        msg = System.Text.Encoding.UTF8.GetString(e.Message);
	    
	    
        if (e.Topic == LocalReceiveTopic && msg != null)
        {
            Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message));
            Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message));
        }
	} 

}
