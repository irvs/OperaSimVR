using PID_Controller;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.UrdfImporter;
using UnityEngine;



public class Mongo_pose_writer : MonoBehaviour
{
    public enum ONOFF { Off, On }
    public ONOFF OnOffSw;
    private ROSConnection ros;
    public bool IsReal;
    //private OdometryMsg odomMessage;
    private PoseStampedMsg odomMessage;

    public string WriterTopicName = "/db_write/pose";// Publish Message Topic Name

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.5f;//50Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    public string WriteTargetObject;
    GameObject targetobject;
    Controller_manager SW_From_cont;
    GameObject Reference;
    Model_name MachineManager;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        odomMessage = new PoseStampedMsg();
        // ROSコネクションへのサブスクライバーの登録
        ros.RegisterPublisher<PoseStampedMsg>(WriterTopicName);
        Reference = GameObject.Find("MapReferencePoint");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        SW_From_cont = FindObjectOfType<Controller_manager>();
        WriteTargetObject = SW_From_cont.Machine_name;
        if (OnOffSw.ToString() == "On" || SW_From_cont.DB_pose_sw == true)
        {
            timeElapsed += Time.deltaTime;
            targetobject = GameObject.Find(WriteTargetObject);
            MachineManager = targetobject.GetComponent<Model_name>();

            if (IsReal == false)
            {
                odomMessage.pose.position.x = targetobject.transform.position.z;
                odomMessage.pose.position.y = -targetobject.transform.position.x;

                odomMessage.pose.orientation.w = targetobject.transform.rotation.w;
                odomMessage.pose.orientation.x = targetobject.transform.rotation.x;
                odomMessage.pose.orientation.y = targetobject.transform.rotation.y;
                odomMessage.pose.orientation.z = targetobject.transform.rotation.z;
                //
            }
            else if (IsReal == true)
            {
                Vector3 ModifyPosition = targetobject.transform.position - new Vector3(Reference.transform.position.x, 0, Reference.transform.position.z);
                Vector2 WorldMachinePosition = new Vector2(ModifyPosition.x + MachineManager.Offset_z, ModifyPosition.z + MachineManager.Offset_x);
                Vector3 rot_offset = new Vector3(MachineManager.OffsetRotation_x, MachineManager.OffsetRotation_y, MachineManager.OffsetRotation_z);
                Quaternion chenged_orientation = Quaternion.Euler(targetobject.transform.eulerAngles + rot_offset);
                Quaternion NewRotation = new Quaternion(-chenged_orientation.z, -chenged_orientation.x, chenged_orientation.y, -chenged_orientation.w);
                odomMessage.pose.position.x = WorldMachinePosition.x;
                odomMessage.pose.position.y = WorldMachinePosition.y;

                odomMessage.pose.orientation.x = NewRotation.x;
                odomMessage.pose.orientation.y = NewRotation.y;
                odomMessage.pose.orientation.z = NewRotation.z;
                odomMessage.pose.orientation.w = NewRotation.w;
                Debug.Log(WorldMachinePosition + " , " + NewRotation);
            }

            if (timeElapsed >= publishMessageInterval)
            {
                odomMessage.header.frame_id = WriteTargetObject;
                odomMessage.header.stamp = new TimeStamp(Clock.time);
                //odomMessage.child_frame_id = childFrameName;

                ros.Publish(WriterTopicName, odomMessage);
                timeElapsed = 0.0f;
                Debug.Log(WriteTargetObject + " pose publish.");
                SW_From_cont.DB_pose_sw = false;
            }

        }
    }
}
