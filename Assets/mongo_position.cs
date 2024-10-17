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

    //private OdometryMsg odomMessage;
    private PoseStampedMsg odomMessage;

    public string robotName = "robot_name";
    public string WriterTopicName = "/ic120_db_write/pose";// Publish Message Topic Name


    private TwistMsg twist;
    private double previousTime = 0.0;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 10.02f;//50Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    public string WriteTargetObject;
    public ControllerLay laiser;
    int Geton;

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

        laiser = FindObjectOfType<ControllerLay>();
        Geton = laiser.GetOn;
        WriteTargetObject = laiser.GetOnVehicle;

        if (Geton == 1)
        {

            timeElapsed += Time.deltaTime;

            double time = Time.fixedTimeAsDouble;
            double deltaTime = time - previousTime;

            odomMessage.pose.position.x = GameObject.Find(WriteTargetObject).transform.position.z;
            odomMessage.pose.position.y = -GameObject.Find("ic120").transform.position.x;
            //Vector3 newPosition = new Vector3((float)msg.pose.position.y * (-1), (float)msg.pose.position.z, (float)msg.pose.position.x);

            odomMessage.pose.orientation.w = GameObject.Find("ic120").transform.rotation.w;
            odomMessage.pose.orientation.x = GameObject.Find("ic120").transform.rotation.x;
            odomMessage.pose.orientation.y = GameObject.Find("ic120").transform.rotation.y;
            odomMessage.pose.orientation.z = GameObject.Find("ic120").transform.rotation.z;
            //Debug.Log(GameObject.Find("ic120").transform.position.x);
            // odomMessage.pose.covariance = new double[] { 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000.0 };

            //   odomMessage.twist.twist.linear.x = 0.0;
            //   odomMessage.twist.twist.linear.y = 0.0;
            //   odomMessage.twist.twist.linear.z = 0.0;

            //   odomMessage.twist.twist.angular.x = 0.0;
            //   odomMessage.twist.twist.angular.y = 0.0;
            //   odomMessage.twist.twist.angular.z = 0.0;

            //  odomMessage.twist.covariance = new double[] { 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000.0 };

            if (timeElapsed >= publishMessageInterval)
            {

                odomMessage.header.frame_id = robotName;
                odomMessage.header.stamp = new TimeStamp(Clock.time);
                //odomMessage.child_frame_id = childFrameName;

                ros.Publish(WriterTopicName, odomMessage);
                timeElapsed = 0.0f;
                // Debug.Log("pubpubpub");
            }


            previousTime = time;
        }
    }

}
