using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;
using Newtonsoft.Json.Linq;


public class HumanController : MonoBehaviour
{
    // Assuming this script is attached to the 'human' GameObject
    // // which has the 14 joint sub-GameObjects as children.
    // private int jointArrayCount = 1;

    // Method to call when a new JSON message is received
    public void UpdateJointPositions(JArray joints)
    {

        for (int i = 0; i < joints.Count; i++)
        {
            // Construct the name of the joint GameObject
            string jointName = $"joint{i}";
            //Debug.Log("i: " + i + ", " + joints[i][0]+ ", " + joints[i][1]+ ", " + joints[i][2]);

 
            // Find the joint GameObject as a child of 'human'
            Transform jointTransform = transform.Find(jointName);
 
            if (jointTransform != null)
            {

                Quaternion rotationQuaternion = Quaternion.Euler(-90f, 235f, 180f);
                // Update the position of the joint GameObject
                Vector3 newPosition = rotationQuaternion * new Vector3(
                    (float)joints[i][0],
                    (float)joints[i][1],
                    (float)joints[i][2]
                );
                newPosition.x = -newPosition.x;
                //Debug.Log(newPosition);

                // newPosition1 is only for Debug purpose
                Vector3 newPosition1 = new Vector3(
                    (float)joints[i][0],
                    (float)joints[i][1],
                    (float)joints[i][2]
                );
                // =======================================

                jointTransform.position = newPosition;
                // Log the position when i = 7
                // if (i == 7)
                // {
                //     Debug.Log("Position when i = 7: " + newPosition);
                //     Debug.Log("Enqueued jointArray #" + jointArrayCount); // Log the enqueue count
                //     jointArrayCount++; // Increment the counter
                // }


            }
            else
            {
                Debug.LogWarning($"Joint '{jointName}' not found.");
            }
        }
    }

    public Vector3 GetJoint(int i)
    {
        string jointName = $"joint{i}";
        Transform jointTransform = transform.Find(jointName);
        return jointTransform.position;
    }

    // Define a class to match the structure of your JSON data
    [Serializable]
    public class Joint
    {
        public float[] coords;
    }

    [Serializable]
    public class JointsData{
        public List<float> cursor_l;
        public List<float> cursor_r;
        public Joint[] joints;
        public string user;
        JointsData(List<float> cursor_l, List<float> cursor_r, Joint[] joints, string user)
        {
            this.cursor_l = cursor_l;
            this.cursor_r = cursor_r;
            this.joints = joints;
            this.user = user;
        }
    }

}
