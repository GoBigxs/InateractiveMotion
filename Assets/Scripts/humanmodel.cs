using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class humanmodel : MonoBehaviour
{
    private UnityMqttReceiver mqttReceiver;

    void Start()
    {
        // Find the UnityMqttReceiver script attached to an object in the scene
        mqttReceiver = FindObjectOfType<UnityMqttReceiver>();

        if (mqttReceiver != null)
        {
            // Access the jointsArray
            if (mqttReceiver.JointsArray != null)
            {
                foreach (var joint in mqttReceiver.JointsArray)
                {
                    // Do something with each joint
                    Debug.Log(joint);
                }
            }
            else
            {
                Debug.LogError("JointsArray is null.");
            }
        }
        else
        {
            Debug.LogError("UnityMqttReceiver script not found in the scene.");
        }
    }
}
