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
    public float rot_offset_x = 0;
    public float rot_offset_y = 0;
    public float rot_offset_z = 0;
    private Vector3 rot_offset;
    private Vector3 chenged_orientation;
    public bool chenge_position_sw;

    private mood_selector mode;
    private VR_cont_2 VRcontroller;
    FieldMainManager SimORRealSelecter;
    Model_name model_name_space;
    ROSConnection ros;
    public Vector3 newPosition;
    public Quaternion newRotation;
    public GameObject SelectorObject;
    public GameObject VRControllerObject;

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
            ros.Subscribe<PoseStampedMsg>(RealSubscribeTopicName, Callback2);
        }
        Debug.Log("already:baselink/pose");
        //

    }

    /*
    void Update()
    {
        //Vector3 newPosition = new Vector3(((float)21420.911) - offset_x, ((float)68.5) - offset_y, ((float)14027.59 - offset_z));
        Vector3 newPosition = new Vector3(((float)21486.702) - ((float)21395.18), ((float)68.02) - 62, ((float)14065.63 - ((float)14034.45)));
        //x:21395.18 y:,z:14034.45
        Debug.Log(newPosition);

        Quaternion newRotation = new((float)0.01826 * (-1), (float)0.342, (float)0.00078, (float)0.9395 * (-1));
        rot_offset = new Vector3((float)rot_offset_x, (float)rot_offset_y, (float)rot_offset_z);
        chenged_orientation = newRotation.eulerAngles - rot_offset;
        

        targetObject.transform.position = newPosition + new Vector3(-36f, 0, 52f); //GameObject.Find("map_Reference point").transform.position;// -new Vector3(-65,0,50);new Vector3(55.24f, 6.3f, 63.6f);//
       targetObject.transform.eulerAngles = chenged_orientation;
    }
    */

    void Callback(PoseStampedMsg msg)
    {
        mode = GetComponent<mood_selector>();
        VRcontroller = GetComponent<VR_cont_2>();
        //Debug.Log(msg.pose.position);
        //Debug.Log(msg.pose.orientation);
        //
        //Vector3 newPosition = new Vector3(((float)msg.pose.position.y * (-1)+ offset_x), ((float)msg.pose.position.z)+offset_z, ((float)msg.pose.position.x)+ offset_y);
        Vector3 newPosition = new Vector3(((float)msg.pose.position.x) + offset_y, ((float)msg.pose.position.z) + offset_z, ((float)msg.pose.position.y + offset_x));
        Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
        rot_offset = new Vector3((float)rot_offset_x, (float)rot_offset_y, (float)rot_offset_z);
        chenged_orientation = newRotation.eulerAngles - rot_offset;
        //Debug.Log(newPosition);
        //Debug.Log(newRotation.eulerAngles);
        //
        //GameObject.Find("ic120").GetComponent<CharacterController>().enabled = false;
        targetObject.transform.position = newPosition - new Vector3(55.24f, 6.3f, 63.6f);// - GameObject.Find("map_zero_point").transform.position;// -new Vector3(-65,0,50);
        targetObject.transform.eulerAngles = chenged_orientation;
    }

    void Callback2(PoseStampedMsg msg)
    {
        mode = SelectorObject.GetComponent<mood_selector>();
        VRcontroller = VRControllerObject.GetComponent<VR_cont_2>();
        Debug.Log(mode + " : " + VRcontroller);

            //Debug.Log(msg.pose.position);
            //Debug.Log(msg.pose.orientation);
            //
            //Vector3 newPosition = new Vector3(((float)msg.pose.position.y * (-1)+ offset_x), ((float)msg.pose.position.z)+offset_z, ((float)msg.pose.position.x)+ offset_y);
            Vector3 newPosition = new Vector3(((float)msg.pose.position.x) - ((float)21395.18), ((float)msg.pose.position.z) - offset_y, ((float)msg.pose.position.y - ((float)14034.45)));
            // Vector3 newPosition = new Vector3(((float)14027) - offset_x, ((float)68.5) - offset_z, ((float)21420.9 - offset_y));
            Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
            rot_offset = new Vector3((float)rot_offset_x, (float)rot_offset_y, (float)rot_offset_z);
            chenged_orientation = newRotation.eulerAngles - rot_offset;
        //Debug.Log(newPosition);
        //Debug.Log(newRotation.eulerAngles);
        //
        if (mode.mode == 1 || VRcontroller.sw == 1) //Visual tool
        {
            if (chenge_position_sw == true)
            {
                //GameObject.Find("ic120").GetComponent<CharacterController>().enabled = false;
                targetObject.transform.position = newPosition + new Vector3(-36f, 0, 52f); //GameObject.Find("map_Reference point").transform.position;// -new Vector3(-65,0,50);new Vector3(55.24f, 6.3f, 63.6f);//
            }
            targetObject.transform.eulerAngles = chenged_orientation;
        }
    }


    void Callback1(OdometryMsg msg)
    {
        mode = FindObjectOfType<mood_selector>();
        VRcontroller = GetComponent<VR_cont_2>();
        if (mode.mode == 1 || VRcontroller.sw == 1) //Visual tool
        {
            model_name_space = GetComponent<Model_name>();
            offset_x = model_name_space.OffsetList[0];
            //  offset_y = model_name_space.OffsetList[1];
            offset_z = model_name_space.OffsetList[2];
            //Debug.Log(msg.pose.orientation);
            //
            newPosition = new Vector3(((float)msg.pose.pose.position.x + offset_x), ((float)msg.pose.pose.position.z) + offset_z, ((float)msg.pose.pose.position.y) + offset_y);
            newRotation = new((float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.y, (float)msg.pose.pose.orientation.w);
            if (mode.mode == 1) //Visual tool
            {
                //Debug.Log(newPosition);
                //Debug.Log(newRotation.eulerAngles);
                //
                // targetObject.GetComponent<Rigidbody>().isKinematic = false;
                targetObject.transform.position = newPosition - new Vector3(55.24f, 6.3f, 63.6f);
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
                rot_offset = new Vector3((float)rot_offset_x, (float)rot_offset_y, (float)rot_offset_z);
                chenged_orientation = newRotation.eulerAngles - rot_offset;
                targetObject.transform.rotation = Quaternion.Euler(chenged_orientation);
                targetObject.GetComponent<Rigidbody>().isKinematic = true;
                //
                //Debug.Log("rot_change_strange" + Real_Cyber_angle_diff);
            }
        }
    }
}