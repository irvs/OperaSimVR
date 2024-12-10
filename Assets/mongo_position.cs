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
    private ROSConnection ros;
    public int sw;
    //private OdometryMsg odomMessage;
    private PoseStampedMsg odomMessage;

    public string robotName = "robot_name";
    public string WriterTopicName = "/ic120_db_write/pose";// Publish Message Topic Name


    private TwistMsg twist;
    private double previousTime = 0.0;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.5f;//50Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    public string WriteTargetObject;
    GameObject targetobject;
    Controller_manager SW_From_cont;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        odomMessage = new PoseStampedMsg();
        Debug.Log("check:baselink/pose");
        // ROSコネクションへのサブスクライバーの登録
        ros.RegisterPublisher<PoseStampedMsg>(WriterTopicName);
       // Debug.Log("already:baselink/pose");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        SW_From_cont = FindObjectOfType<Controller_manager>();
        WriteTargetObject = SW_From_cont.Machine_name;
       // targetobject= SW_From_cont.VehicletargetObject;
        if (sw == 1 || SW_From_cont.DB_pose_sw == true)
        {

             timeElapsed += Time.deltaTime;

            targetobject=GameObject.Find(WriteTargetObject);

            odomMessage.pose.position.x = GameObject.Find(WriteTargetObject).transform.position.z;
                odomMessage.pose.position.y = -targetobject.transform.position.x;
                //Vector3 newPosition = new Vector3((float)msg.pose.position.y * (-1), (float)msg.pose.position.z, (float)msg.pose.position.x);

                odomMessage.pose.orientation.w = targetobject.transform.rotation.w;
                odomMessage.pose.orientation.x = targetobject.transform.rotation.x;
                odomMessage.pose.orientation.y = targetobject.transform.rotation.y;
                odomMessage.pose.orientation.z = targetobject.transform.rotation.z;


                if (timeElapsed >= publishMessageInterval)
                {
                    odomMessage.header.frame_id = robotName;
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
