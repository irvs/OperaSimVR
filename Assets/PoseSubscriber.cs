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
public class PoseSubscriber : MonoBehaviour
{
    public bool SimORReal;
    public string robotName = "robot_name";
    public GameObject targetObject;
    public string Subscribe_topic_name = "subscribe_topic";
    public string RealSubscribeTopicName;
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    private mood_selector mode;
    FieldMainManager SimORRealSelecter;
    Model_name model_name_space;
    ROSConnection ros;
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        Debug.Log("check:baselink/pose");
        // ROSコネクションへのサブスクライバーの登録
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimulater")
        {
            SimORReal = false;
            ros.Subscribe<PoseStampedMsg>(Subscribe_topic_name, Callback);
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
        {
            SimORReal = true;
            ros.Subscribe<PoseStampedMsg>(RealSubscribeTopicName, Callback);
        }
        Debug.Log("already:baselink/pose");
        //

    }
    void Callback(PoseStampedMsg msg)
    {
        mode = FindObjectOfType<mood_selector>();

        if (mode.mood == 1) //Visual tool
        {
            model_name_space = FindObjectOfType<Model_name>();
            offset_x = model_name_space.OffsetList[0];
            offset_y = model_name_space.OffsetList[1];
            offset_z = model_name_space.OffsetList[2];
            //Debug.Log(msg.pose.orientation);
            //
            Vector3 newPosition = new Vector3(((float)msg.pose.position.y * (-1) + offset_x), ((float)msg.pose.position.z) + offset_z, ((float)msg.pose.position.x) + offset_y);
            Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
            //Debug.Log(newPosition);
            //Debug.Log(newRotation.eulerAngles);
            //
           // targetObject.GetComponent<Rigidbody>().isKinematic = false;
            targetObject.transform.position = newPosition;
            //
            /*
            float Real_Cyber_angle_diff = Vector3.SignedAngle(targetObject.transform.rotation * Vector3.forward, newRotation * Vector3.forward, Vector3.up);
            if (Math.Abs(Real_Cyber_angle_diff) >= 0.5)
            {
                targetObject.transform.position = new Vector3(0.0f, newPosition[1] + 0.5f, 0.0f);
                for (int i = 0; 2.5f * i < (Math.Abs(Real_Cyber_angle_diff)); i++)
                {
                    targetObject.transform.Rotate(0, 2.5f, 0);
                    Debug.Log("rot_change->"+i);
                }

            }*/
            //
            targetObject.transform.rotation = newRotation;
            targetObject.GetComponent<Rigidbody>().isKinematic = true;
            //
            //Debug.Log("rot_change_strange" + Real_Cyber_angle_diff);
        }

    }
}