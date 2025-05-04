using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using Unity.Robotics.Core;
using System;
using static UnityEditor.PlayerSettings;
public class PoseSubscriber : MonoBehaviour
{
    public bool ViaDB;
    public bool WorldToMap;
    public enum PoseMessageType { OdometryMsg, PoseStampedMsg }
    public PoseMessageType PoseMsgType;
    GameObject targetObject;
    public string SimPhysXSubscribeTopicName;
    public string SimAGXSubscribeTopicName;
    public string RealSubscribeTopicName;
    public string ViaDBSubscribeTopicName;
    float offset_x = 0;
    float offset_y = 0;
    float offset_z = 0;
    float rot_offset_x = 0;
    float rot_offset_y = 0;
    float rot_offset_z = 0;
    private Vector3 rot_offset;
    private Vector3 chenged_orientation;
    public bool chenge_position_sw;

    private mode_selector mode;
    FieldMainManager SimORRealSelecter;
    ROSConnection ros;
    public Vector3 newPosition;
    public Quaternion newRotation;
    GameObject SelectorObject;
    Model_name MachineManager;
    private bool IsPapermachine;
    GameObject Reference;

    void Start()
    {
        targetObject = this.gameObject;
        SelectorObject = GameObject.Find("FieldManager");
        Reference = GameObject.Find("MapReferencePoint");
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
        MachineManager = targetObject.GetComponent<Model_name>();
        newPosition = new Vector3(((float)msg.pose.position.x), ((float)msg.pose.position.z) , ((float)msg.pose.position.y));
        newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
        PoseCheanger(newPosition, newRotation, MachineManager.Offset_z, MachineManager.Offset_y, MachineManager.Offset_x, MachineManager.OffsetRotation_x, MachineManager.OffsetRotation_y, MachineManager.OffsetRotation_z);
    }

    void CallbackOd(OdometryMsg msg)
    {
        MachineManager = targetObject.GetComponent<Model_name>();
        newPosition = new Vector3(((float)msg.pose.pose.position.x), ((float)msg.pose.pose.position.z), ((float)msg.pose.pose.position.y));
        newRotation = new((float)msg.pose.pose.orientation.y * (-1), (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.w * (-1));
        PoseCheanger(newPosition, newRotation, MachineManager.Offset_z, MachineManager.Offset_y, MachineManager.Offset_x, MachineManager.OffsetRotation_x, MachineManager.OffsetRotation_y, MachineManager.OffsetRotation_z);
    }

    void PoseCheanger(Vector3 NewPosition, Quaternion NewRotation, float OffsetX, float OffsetY, float OffsetZ, float RotOffsetX, float RotOffsetY, float RotOffsetZ)
    {
        Vector3 ModifyPosition = new Vector3((NewPosition.x), (NewPosition.y) - OffsetY, (NewPosition.z));
        Reference = GameObject.Find("MapReferencePoint");
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
                targetObject.transform.position = ModifyPosition + new Vector3(Reference.transform.position.x, 0, Reference.transform.position.z);
            }
            targetObject.transform.eulerAngles = chenged_orientation;
        }
       // Debug.Log("pose changed");
    }

    void Update()
    {
        //   Debug.Log(newPosition);
        //   Debug.Log( newRotation);
        //Debug.Log(targetObject);
       // Debug.Log(Reference.transform.position);
    }

}