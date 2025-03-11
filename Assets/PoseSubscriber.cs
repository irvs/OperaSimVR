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
    public bool ViaDB;
    public bool WorldToMap;
    public enum PoseMessageType { OdometryMsg, PoseStampedMsg }
    public PoseMessageType PoseMsgType;
    public GameObject targetObject;
    public string SimPhysXSubscribeTopicName;
    public string SimAGXSubscribeTopicName;
    public string RealSubscribeTopicName;
    public string ViaDBSubscribeTopicName;
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    public float rot_offset_x = 0;
    public float rot_offset_y = 0;
    public float rot_offset_z = 0;
    private Vector3 rot_offset;
    private Vector3 chenged_orientation;
    public bool chenge_position_sw;

    private mode_selector mode;
    FieldMainManager SimORRealSelecter;
    ROSConnection ros;
    public Vector3 newPosition;
    public Quaternion newRotation;
    public GameObject SelectorObject;
    Model_name MachineManager;
    private bool IsPapermachine;


    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        Debug.Log("check:baselink/pose");
        if (ViaDB == false)
        {
            // ROSコネクションへのサブスクライバーの登録
            SimORRealSelecter = FindObjectOfType<FieldMainManager>();
            if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
            {
                ros.Subscribe<PoseStampedMsg>(SimPhysXSubscribeTopicName, CallbackPS);
            }
            else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")
            {
                ros.Subscribe<OdometryMsg>(SimAGXSubscribeTopicName, Callback1);
            }
            else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
            {
                if (PoseMsgType.ToString() == "PoseStampedMsg")
                {
                    ros.Subscribe<PoseStampedMsg>(RealSubscribeTopicName, CallbackPS);
                }
                else if (PoseMsgType.ToString() == "OdometryMsg")
                {
                    ros.Subscribe<OdometryMsg>(RealSubscribeTopicName, CallbackOd);
                }
            }
        }
        else if (ViaDB == true)// || SimORRealSelecter == true)
        {
            if (PoseMsgType.ToString() == "PoseStampedMsg")
            {
                ros.Subscribe<PoseStampedMsg>(ViaDBSubscribeTopicName, CallbackPS);
            }
            else if (PoseMsgType.ToString() == "OdometryMsg")
            {
                ros.Subscribe<OdometryMsg>(ViaDBSubscribeTopicName, CallbackOd);
            }
        }
        MachineManager = GetComponent<Model_name>();
        if (MachineManager.ObjectTypeIsPaperMachine == true)
        {
            IsPapermachine = true;
        }
        Debug.Log("already:baselink/pose");
    }


    
    void Callback1(OdometryMsg msg)
    {
        mode = FindObjectOfType<mode_selector>();
        if (mode.mode == 1) //Visual tool
        {
            MachineManager = GetComponent<Model_name>();
            offset_x = MachineManager.OffsetList[0];
            //  offset_y = MachineManager.OffsetList[1];
            offset_z = MachineManager.OffsetList[2];
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

    void CallbackPS(PoseStampedMsg msg)
    {
       // mode = SelectorObject.GetComponent<mode_selector>();
        MachineManager = targetObject.GetComponent<Model_name>();

        Vector3 newPosition = new Vector3(((float)msg.pose.position.x), ((float)msg.pose.position.z) , ((float)msg.pose.position.y));
        Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
        PoseCheanger(newPosition, newRotation, MachineManager.offset_z, MachineManager.offset_y, MachineManager.offset_x, MachineManager.offset_adoptor_x, MachineManager.offset_adoptor_y, MachineManager.offset_adoptor_z);

        /*
        //Debug.Log(msg.pose.position);
        //Debug.Log(msg.pose.orientation);
        Vector3 newPosition = new Vector3(((float)msg.pose.position.x), ((float)msg.pose.position.z) - offset_y, ((float)msg.pose.position.y));
        if (WorldToMap == true)
        {
            newPosition = new Vector3(((float)msg.pose.position.x) - ((float)21395.18), ((float)msg.pose.position.z) - offset_y, ((float)msg.pose.position.y - ((float)14034.45)));
        }
        //
        Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
        rot_offset = new Vector3((float)rot_offset_x, (float)rot_offset_y, (float)rot_offset_z);
        chenged_orientation = newRotation.eulerAngles - rot_offset;

        //
        if (mode.mode == 1) //Visual tool
        {
            if (chenge_position_sw == true)
            {
                targetObject.transform.position = newPosition + new Vector3(GameObject.Find("map_Reference point").transform.position.x, 0, GameObject.Find("map_Reference point").transform.position.z); //GameObject.Find("map_Reference point").transform.position;// -new Vector3(-65,0,50);new Vector3(55.24f, 6.3f, 63.6f);//
            }
            targetObject.transform.eulerAngles = chenged_orientation;
        }
        */
    }

    void CallbackOd(OdometryMsg msg)
    {
        //  mode = SelectorObject.GetComponent<mode_selector>();
        MachineManager = targetObject.GetComponent<Model_name>();

        Vector3 newPosition = new Vector3(((float)msg.pose.pose.position.x), ((float)msg.pose.pose.position.z), ((float)msg.pose.pose.position.y));
        Quaternion newRotation = new((float)msg.pose.pose.orientation.y * (-1), (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.w * (-1));
        PoseCheanger(newPosition, newRotation, MachineManager.offset_z, MachineManager.offset_y, MachineManager.offset_x, MachineManager.offset_adoptor_x, MachineManager.offset_adoptor_y, MachineManager.offset_adoptor_z);

        /*
        //Debug.Log(msg.pose.position);
        //Debug.Log(msg.pose.orientation);
        Vector3 newPosition = new Vector3(((float)msg.pose.pose.position.x), ((float)msg.pose.pose.position.z) - offset_y, ((float)msg.pose.pose.position.y));
        if (WorldToMap == true)
        {
            newPosition = new Vector3(((float)msg.pose.pose.position.x) - ((float)21395.18), ((float)msg.pose.pose.position.z) - offset_y, ((float)msg.pose.pose.position.y - ((float)14034.45)));
        }
        //
        Quaternion newRotation = new((float)msg.pose.pose.orientation.y * (-1), (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.w * (-1));
        rot_offset = new Vector3((float)rot_offset_x, (float)rot_offset_y, (float)rot_offset_z);
        chenged_orientation = newRotation.eulerAngles - rot_offset;

        //
        if (mode.mode == 1) //Visual tool
        {
            if (chenge_position_sw == true)
            {
                targetObject.transform.position = newPosition + new Vector3(GameObject.Find("map_Reference point").transform.position.x, 0, GameObject.Find("map_Reference point").transform.position.z); //GameObject.Find("map_Reference point").transform.position;// -new Vector3(-65,0,50);new Vector3(55.24f, 6.3f, 63.6f);//
            }
            targetObject.transform.eulerAngles = chenged_orientation;
        }
        */
    }

    void PoseCheanger(Vector3 NewPosition, Quaternion NewRotation, float OffsetX, float OffsetY, float OffsetZ, float RotOffsetX, float RotOffsetY, float RotOffsetZ)
    {
        Vector3 ModifyPosition = new Vector3((NewPosition.x), (NewPosition.y) - OffsetY, (NewPosition.z));

        if (WorldToMap == true)
        {
            ModifyPosition = new Vector3((NewPosition.x) - OffsetX, (NewPosition.y) - OffsetY, (NewPosition.z - OffsetZ));
        }

        rot_offset = new Vector3(RotOffsetX, RotOffsetY, RotOffsetZ);
        chenged_orientation = NewRotation.eulerAngles - rot_offset;

        mode = SelectorObject.GetComponent<mode_selector>();
        if (mode.mode == 1 || (IsPapermachine == true && mode.mode == 2)) //Visual tool
        {
            if (chenge_position_sw == true)
            {
                targetObject.transform.position = ModifyPosition + new Vector3(GameObject.Find("map_Reference point").transform.position.x, 0, GameObject.Find("map_Reference point").transform.position.z);
            }
            targetObject.transform.eulerAngles = chenged_orientation;
        }
       // Debug.Log("pose changed");
    }

}