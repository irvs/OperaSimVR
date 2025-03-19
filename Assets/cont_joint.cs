using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using System;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Drawing;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.Core;
using RosMessageTypes.Nav;

public class cont_joint : MonoBehaviour
{
    Controller_manager VRManager;
    mode_selector selected_mode;
    FieldMainManager PoseVeloSelector;
    JointAnglePublisher FromVRJointController;
    public int sw = 0;
    int cmd_operation = 0;
    float movespeed = 0.01f;
    ROSConnection ros;
    /*
    public string topicName_cmd_vel = "zx200/tracks/cmd_vel";
    public string topicName_swing = "/zx200/swing/cmd";
    public string topicName_boom = "/zx200/boom/cmd";
    public string topicName_arm = "/zx200/arm/cmd";
    public string topicName_bucket = "/zx200/bucket/cmd";
    */
    public float publishMessageFrequency = 0.5f;
    private float timeElapsed;
    float goalpose_swing = 0.0f;
    float goalpose_boom = -0.7f;
    float goalpose_arm = 1.57f;
    float goalpose_bucket = 0.785f;
    private List<ArticulationBody> targetjoints;
    private List<string> targetjointNames;
    private double targetPos;
    public enum JointContorollerModeOption { Velocity, Position }

    public JointContorollerModeOption JointContorollerMode;
    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    private double pos_of_swing_joint;
    private double pos_of_boom_joint;
    private double pos_of_arm_joint;
    private double pos_of_bucket_joint;
    private double pos_of_end_joint;

    private float swing_upper_limit;
    private float swing_lower_limit;
    private float boom_upper_limit = 0.9594f;
    private float boom_lower_limit = -1.2211f;
    private float arm_upper_limit = 2.5294f;
    private float arm_lower_limit = 0.785f;
    private float bucket_upper_limit = 1.39555f;
    private float bucket_lower_limit = -1.2211f;
    private float joint_angular_upper_limit;
    private float joint_angular_lower_limit;


    //
    public GameObject targetPlayerObject;
    public GameObject targetObject;
    //
    //call back
    private JointStateMsg twist;
    //private double pos_of_swing_joint;
    //private double pos_of_boom_joint;
    //private double pos_of_arm_joint;
    //private double pos_of_bucket_joint;
    //private double pos_of_end_joint;
    private List<ArticulationBody> joints;
    private List<string> jointNames;
    float targetVelocity;
    //
    //private List<ArticulationBody> targetjoints;
    //private List<string> targetjointNames;
    //private double targetPos;

    void Start()
    {

    }
    void Update()
    {
        VRManager = FindObjectOfType<Controller_manager>();
        PoseVeloSelector = FindObjectOfType<FieldMainManager>();
        if (sw == 1)
        {

            if (VRManager.Player_posi_mover_SW > 0 || sw == 1)
            {
                selected_mode = FindObjectOfType<mode_selector>();
                
                OVRPlayerController scriptA = targetPlayerObject.GetComponent<OVRPlayerController>();
                if (scriptA != null)
                {
                    //Debug.Log("kaitennha" + scriptA.RotationRatchet);
                    scriptA.RotationRatchet = 0;
                    scriptA.RotationAmount = 0;
                    //Debug.Log("kaitennha" + scriptA.RotationRatchet);
                }
                //

                if(PoseVeloSelector.ForSimOrReal.ToString() == "ForSimPhysX")
                {
                    JointContorollerMode = JointContorollerModeOption.Position;
                }
                else if (PoseVeloSelector.ForSimOrReal.ToString() == "ForSimAGX")
                {
                    JointContorollerMode = JointContorollerModeOption.Velocity;
                }
                else if (PoseVeloSelector.ForSimOrReal.ToString() == "ForReal")
                {
                    JointContorollerMode = JointContorollerModeOption.Velocity;
                }

                timeElapsed += Time.deltaTime;
                if (timeElapsed > publishMessageFrequency)
                {
                    timeElapsed = 0;
                }
                /*
                List<float> msg = new List<float>();
                msg.Add(goalpose_swing);
                msg.Add(goalpose_boom);
                msg.Add(goalpose_arm);
                msg.Add(goalpose_bucket);
                msg.Add(0);
                */
                FromVRJointController = GetComponent<JointAnglePublisher>();
                //List<float> msg = new List<float>();
                //msg
                //List<float> msg = FromVRJointController.listOfJointCmdList[FromVRJointController.listOfJointCmdList.Count - 1];


                //
                //private List<ArticulationBody> targetjoints;
                //private List<string> targetjointNames;
                //private double targetPos;
                //
                targetjoints = new List<ArticulationBody>();
                targetjointNames = new List<string>();
                //
                //
                int j = 0;
                foreach (var joint in targetObject.GetComponentsInChildren<ArticulationBody>())
                {
                    //Debug.Log(joint);
                    if (joint.isActiveAndEnabled)
                    {
                        var targetujoint = joint.GetComponent<UrdfJoint>();
                        if (targetujoint && !(targetujoint is UrdfJointFixed))
                        {
                            targetjoints.Add(joint);
                            targetjointNames.Add(targetujoint.jointName);
                            //
                            if (FromVRJointController.listOfJointCmdList.Count - 1 >= 0)
                            {
                                targetPos = FromVRJointController.listOfJointCmdList[FromVRJointController.listOfJointCmdList.Count - 1][j];
                                Debug.Log(targetPos);
                                if (j == 2)
                                {
                                    targetPos = targetPos * 0.05;
                                }
                                targetPos = targetPos * 500.0f;
                                var drive = joint.xDrive;//targetjoints[i].xDrive;

                                if (JointContorollerMode.ToString() == "Velocity")
                                {
                                    //targetVelocity = 1.1f;
                                    drive.driveType = ArticulationDriveType.Velocity;// = targetVelocity;
                                    drive.targetVelocity = (float)targetPos;
                                    joint.xDrive = drive;
                                }
                                else if (JointContorollerMode.ToString() == "Position")
                                {
                                    drive.driveType = ArticulationDriveType.Force;
                                    drive.target = (float)(targetPos * Mathf.Rad2Deg);
                                    joint.xDrive = drive;//targetjoints[i].xDrive = drive;
                                }
                                j += 1;
                            }
                        }
                    }
                }
            }
        }
    }
}

