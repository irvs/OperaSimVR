using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using Unity.Robotics.Core;
using System;
using static UnityEditor.PlayerSettings;
//using MyStringMsg = RosMessageTypes.HelloInterfaces.MyStringMsg;
public class PapermachinePoseSubscriber : MonoBehaviour
{
    private PoseStampedMsg twist;
    public string robotName = "robot_name";
    public GameObject targetObject;
    public string Subscribe_topic_name = "subscribe_topic";
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    private mood_selector mode;
    ROSConnection ros;
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        //Debug.Log("aaaaaaaa");
        twist = new PoseStampedMsg();
        Debug.Log("check:baselink/pose");
        // ROSコネクションへのサブスクライバーの登録
        ros.Subscribe<PoseStampedMsg>(Subscribe_topic_name, Callback);
        Debug.Log("already:baselink/pose");
        //

    }
    void Callback(PoseStampedMsg msg)
    {
        mode = FindObjectOfType<mood_selector>();

        //if (mode.mood == 1) //Visual tool
        //{
            //Debug.Log("zxcvbnm");
            //Debug.Log(msg.pose.orientation);
            //
            Vector3 newPosition = new Vector3(((float)msg.pose.position.y * (-1) + offset_x), ((float)msg.pose.position.z) + offset_z, ((float)msg.pose.position.x) + offset_y);
            Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
            //Debug.Log(newPosition);
            //Debug.Log(newRotation.eulerAngles);
            //
         //   targetObject.GetComponent<Rigidbody>().isKinematic = false;
            targetObject.transform.position = newPosition;
            targetObject.transform.rotation = newRotation;
         //   targetObject.GetComponent<Rigidbody>().isKinematic = true;
            //
       // }

    }
}