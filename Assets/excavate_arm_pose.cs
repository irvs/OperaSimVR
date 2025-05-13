using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.Core;
using System;

public class JointSubscriber : MonoBehaviour
{
    public bool ViaDB;
    public bool JointChengeSw;
    private double pos_of_swing_joint;
    private double pos_of_boom_joint;
    private double pos_of_arm_joint;
    private double pos_of_bucket_joint;
    private List<string> jointNames;
    private List<ArticulationBody> targetjoints;
    private List<string> targetjointNames;
    private double targetPos;
    private float dissconnect_timer;
    GameObject targetObject;
    public string PhysXSubscribeTopicName;
    public string AGXSubscribeTopicName;
    public string RealSubscribeTopicName;
    public string ViaDBSubscribeTopicName;
    private string SubscribeJointTopicName;
    private mode_selector mode;
    FieldMainManager SimORRealSelecter;
    public List<double> JointPositions;
    ROSConnection ros;

    void Start()
    {
        targetObject = this.gameObject;
        ros = ROSConnection.GetOrCreateInstance();
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
        {
            SubscribeJointTopicName = PhysXSubscribeTopicName;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")
        {
            SubscribeJointTopicName = AGXSubscribeTopicName;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
        {
            SubscribeJointTopicName = RealSubscribeTopicName;
        }
        if (ViaDB == true || SimORRealSelecter.ViaDB == true)
        {
            SubscribeJointTopicName = ViaDBSubscribeTopicName;
        }
        Debug.Log("check:joint_states_pub");
        // ROSコネクションへのサブスクライバーの登録
        ros.Subscribe<JointStateMsg>(SubscribeJointTopicName, Callback);
        Debug.Log("already:joint_states_pub");
        ///
    }
    void Update()
    {
        dissconnect_timer += Time.deltaTime;
    }

    void Callback(JointStateMsg msg)
    {
        //Debug.Log("joint_subscribe");
        mode = FindObjectOfType<mode_selector>();
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
            JointPositions[0] = msg.position[0];
            JointPositions[1] = msg.position[1];
            JointPositions[2] = msg.position[2];
            JointPositions[3] = msg.position[3];
            JointPositions[4] = msg.velocity[0];
            JointPositions[5] = msg.velocity[1];
            JointPositions[6] = msg.velocity[2];
            JointPositions[7] = msg.velocity[3];
            //
            if (JointChengeSw == true)
            {
                int j = 0;
                foreach (var joint in targetObject.GetComponentsInChildren<ArticulationBody>())
                {
                    if ((joint.isActiveAndEnabled) && (j <= 3))
                    {
                        var targetujoint = joint.GetComponent<UrdfJoint>();
                        if (targetujoint && !(targetujoint is UrdfJointFixed))
                        {
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
                            j += 1;
                        }
                    }
                }
            }
        }
    }
}