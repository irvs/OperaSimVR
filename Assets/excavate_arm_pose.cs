using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.Core;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using System;
//using MyStringMsg = RosMessageTypes.HelloInterfaces.MyStringMsg;
public class JointSubscriber : MonoBehaviour
{
    private JointStateMsg twist;
    private double pos_of_swing_joint;
    private double pos_of_boom_joint;
    private double pos_of_arm_joint;
    private double pos_of_bucket_joint;
    private double pos_of_end_joint;
    private List<ArticulationBody> joints;
    private List<string> jointNames;
    //
    private List<ArticulationBody> targetjoints;
    private List<string> targetjointNames;
    private double targetPos;
    private float dissconnect_timer;
    public string robotName = "robot_name";
    public GameObject targetObject;
    public string Subscribe_topic_name = "subscribe_topic";
    public string RealSubscribeTopicName;
    private mood_selector mode;
    public bool SimORReal;
    FieldMainManager SimORRealSelecter;
    ROSConnection ros;

    void Start()
    {
        twist = new JointStateMsg();
        ros = ROSConnection.GetOrCreateInstance();
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
        {
            SimORReal = false;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")
        {
            SimORReal = true;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
        {
            SimORReal = true;
        }
        Debug.Log("check:joint_states_pub");
        // ROSコネクションへのサブスクライバーの登録
        //ROSConnection.instance.Subscribe<TwistMsg>("/ic120/tracks/cmd_vel", Callback);
        if (SimORReal == true)
        {
            ros.Subscribe<JointStateMsg>(RealSubscribeTopicName, Callback);
        }
        if (SimORReal == false)
        {
            ros.Subscribe<JointStateMsg>(Subscribe_topic_name, Callback);
        }
        Debug.Log("already:joint_states_pub");
        ///
    }
    void Update()
    {
        dissconnect_timer += Time.deltaTime;
    }
    void Callback(JointStateMsg msg)
    {
        //
        //Debug.Log("joint_subscribe");
        mode = FindObjectOfType<mood_selector>();
        dissconnect_timer = 0.0f;

        if (mode.mode == 1)//Visual tool
        {

            //
            targetjoints = new List<ArticulationBody>();
            targetjointNames = new List<string>();
            //
            pos_of_swing_joint = msg.position[0];
            pos_of_boom_joint = msg.position[1];
            pos_of_arm_joint = msg.position[2];
            pos_of_bucket_joint = msg.position[3];
            // pos_of_end_joint = msg.position[4];

            //
            int j = 0;
            foreach (var joint in targetObject.GetComponentsInChildren<ArticulationBody>())
            {
                // Debug.Log(joint);
                if (joint.isActiveAndEnabled)
                {
                    //Debug.Log("abbbbbbbbbbbbbb");
                    //Debug.Log(joint);
                    //Debug.Log(j);
                    var targetujoint = joint.GetComponent<UrdfJoint>();
                    if (targetujoint && !(targetujoint is UrdfJointFixed))
                    {
                        //Debug.Log("abbbbbbbbbbbbbb");
                        targetjoints.Add(joint);
                        targetjointNames.Add(targetujoint.jointName);
                        //
                        targetPos = msg.position[j];
                        var drive = joint.xDrive;//targetjoints[i].xDrive;
                        //
                        //if (drive.stiffness == 0)
                            drive.stiffness = 20000000;
                        //if (drive.damping == 0)
                            drive.damping = 10000000;
                        //if (drive.forceLimit == 0)
                            drive.forceLimit = 10000000;
                        //
                        drive.target = (float)(targetPos * Mathf.Rad2Deg);
                        joint.xDrive = drive;//targetjoints[i].xDrive = drive;
                       // Debug.Log("abcd" + joint + " " + targetPos);
                        j += 1;
                    }
                }
            }
            ///
            //
        }
    }
}