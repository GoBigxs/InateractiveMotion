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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class UnityMqttReceiver : MonoBehaviour
{

    [SerializeField]public string LocalReceiveTopic = "sony/ui";
    public static string Msg { get; private set; }
    private MqttClient client;
    public JArray JointsArray { get; private set; }

    void Awake()
    {

        // Server Setting 
        // 127.0.0.1 for local server, 192.168.1.100 for on-site server
        // create client instance 
        client = new MqttClient("localhost");

        // register to message received 
        client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived; 
        
        string clientId = Guid.NewGuid().ToString(); 
        client.Connect(clientId); 
        
        // subscribe to the topic "/home/temperature" with QoS 2 
        client.Subscribe(new string[] { LocalReceiveTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE }); 
    }

	void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) 
	{ 
        Msg = System.Text.Encoding.UTF8.GetString(e.Message);
        // Deserialize the JSON string
        var jsonObject = JsonConvert.DeserializeObject<JObject>(Msg);

        JointsArray = jsonObject["joints"] as JArray;

        // Print out each joint
        for (int i = 0; i < JointsArray.Count; i++)
        {
            Console.WriteLine($"joint{i} = {JointsArray[i]}");
        }
        // Debug.Log("Type of jointsArray: " +jointsArray.GetType());

    }

    [Serializable]
    public class Data
    {
        public float[][] joints;
    }


}
