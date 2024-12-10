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
    public string SimPhysXSubscribeTopicName;
    public string SimAGXSubscribeTopicName;
    public string RealSubscribeTopicName;
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    private mood_selector mode;
    private VR_cont_2 VRcontroller;
    FieldMainManager SimORRealSelecter;
    Model_name model_name_space;
    ROSConnection ros;
    public Vector3 newPosition;
    public Quaternion newRotation;
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        Debug.Log("check:baselink/pose");
        // ROSコネクションへのサブスクライバーの登録
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
        {
            SimORReal = false;
            ros.Subscribe<PoseStampedMsg>(SimPhysXSubscribeTopicName, Callback);
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")
        {
            SimORReal = true;
            ros.Subscribe<OdometryMsg>(SimAGXSubscribeTopicName, Callback1);
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
        VRcontroller = FindObjectOfType<VR_cont_2>();
        if (mode.mood == 1 || VRcontroller.sw == 1) //Visual tool
        {
            model_name_space = FindObjectOfType<Model_name>();
            offset_x = model_name_space.OffsetList[0];
            //  offset_y = model_name_space.OffsetList[1];
            offset_z = model_name_space.OffsetList[2];
            //Debug.Log(msg.pose.orientation);
            //
            newPosition = new Vector3(((float)msg.pose.position.y * (-1) + offset_x), ((float)msg.pose.position.z) + offset_z, ((float)msg.pose.position.x) + offset_y);
            newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
            if (mode.mood == 1) 
            {
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

    void Callback1(OdometryMsg msg)
    {
        mode = FindObjectOfType<mood_selector>();
        VRcontroller = FindObjectOfType<VR_cont_2>();
        if (mode.mood == 1 || VRcontroller.sw == 1) //Visual tool
        {
            model_name_space = FindObjectOfType<Model_name>();
            offset_x = model_name_space.OffsetList[0];
            //  offset_y = model_name_space.OffsetList[1];
            offset_z = model_name_space.OffsetList[2];
            //Debug.Log(msg.pose.orientation);
            //
            newPosition = new Vector3(((float)msg.pose.pose.position.x + offset_x), ((float)msg.pose.pose.position.z) + offset_z, ((float)msg.pose.pose.position.y) + offset_y);
            newRotation = new((float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.y, (float)msg.pose.pose.orientation.w);
            if (mode.mood == 1) //Visual tool
            {
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
}