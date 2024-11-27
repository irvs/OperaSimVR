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
    mood_selector selected_mode;
    public int sw = 0;
    int cmd_operation = 0;
    float movespeed = 0.01f;
    ROSConnection ros;
    public string topicName_cmd_vel = "zx200/tracks/cmd_vel";
    public string topicName_swing = "/zx200/swing/cmd";
    public string topicName_boom = "/zx200/boom/cmd";
    public string topicName_arm = "/zx200/arm/cmd";
    public string topicName_bucket = "/zx200/bucket/cmd";
    public float publishMessageFrequency = 0.5f;
    private float timeElapsed;
    float goalpose_swing = 0.0f;
    float goalpose_boom = -0.7f;
    float goalpose_arm = 1.57f;
    float goalpose_bucket = 0.785f;
    private List<ArticulationBody> targetjoints;
    private List<string> targetjointNames;
    private double targetPos;
    public enum JointContorollerModeOption { Velocity, Fource }

    public JointContorollerModeOption JointContorollerMode;
    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    int zerocounter = 0;
    float RFront = 0.0f;
    float RBack = 0.0f;
    float LFront = 0.0f;
    float LBack = 0.0f;
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
    public GameObject Player;
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
        if (sw == 1)
        {

            if (VRManager.Player_posi_mover_SW == 0 && sw != 1)
            {
                cmd_operation = 0;

            }
            else if (VRManager.Player_posi_mover_SW > 0 || sw == 1)
            {
                cmd_operation = 1;
                //Debug.Log("geton");

                selected_mode = FindObjectOfType<mood_selector>();


                OVRPlayerController scriptA = targetObject.GetComponent<OVRPlayerController>();
                if (scriptA != null)
                {
                    //Debug.Log("kaitennha" + scriptA.RotationRatchet);
                    scriptA.RotationRatchet = 0;
                    scriptA.RotationAmount = 0;
                    //Debug.Log("kaitennha" + scriptA.RotationRatchet);
                }
                //


                //
                if (Input.GetKey(KeyCode.Y))
                {
                    goalpose_swing += 0.005f;
                }
                if (Input.GetKey(KeyCode.H))
                {
                    goalpose_swing -= 0.005f;
                }
                if (Input.GetKey(KeyCode.U) && goalpose_boom <= 0.9594)
                {
                    goalpose_boom += 0.005f;
                }
                if (Input.GetKey(KeyCode.J) && goalpose_boom >= -1.2211)
                {
                    goalpose_boom -= 0.005f;
                }
                if (Input.GetKey(KeyCode.I) && goalpose_arm <= 2.5294)
                {
                    goalpose_arm += 0.005f;
                }
                if (Input.GetKey(KeyCode.K) && goalpose_arm >= 0.785)
                {
                    goalpose_arm -= 0.005f;
                }
                if (Input.GetKey(KeyCode.O) && goalpose_bucket <= 1.39555)
                {
                    goalpose_bucket += 0.005f;
                }
                if (Input.GetKey(KeyCode.L) && goalpose_bucket >= -1.2211)
                {
                    goalpose_bucket -= 0.005f;
                }

                //
                RBack = OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger);
                RFront = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);
                LBack = OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger);
                LFront = OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger);

                if (RFront >= 0.5 && LFront >= 0.5)
                {
                    linear.x = (RFront + LFront) / 2;
                }
                else if (RFront < 0.5 && LFront >= 0.5)
                {
                    angular.z = 0.8;
                }
                else if (RFront >= 0.5 && LFront < 0.5)
                {
                    angular.z = -0.8;
                }
                else
                {
                    linear.x = 0;
                    angular.x = 0;
                }


                //
                //twist
                Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                Vector2 stickR = movespeed * OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
                if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).y) > 0.3))
                {
                    if (goalpose_arm >= 0.785 && goalpose_arm <= 2.529)
                    {
                        goalpose_arm += -stickL.y;

                    }
                    else if (goalpose_arm <= 0.785 && -stickL.y > 0)
                    {
                        goalpose_arm += -stickL.y;
                    }
                    else if (goalpose_arm >= 2.529 && -stickL.y < 0)
                    {
                        goalpose_arm += -stickL.y;
                    }

                }
                if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).x) > 0.3))
                {
                    goalpose_swing += -0.3f * stickL.x;
                }
                if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).y) > 0.3))
                {
                    if (goalpose_boom >= -1.221 && goalpose_boom <= 0.9594)
                    {
                        goalpose_boom += 0.3f * stickR.y;
                    }
                    else if (goalpose_boom <= -1.221 && stickR.y > 0)
                    {
                        goalpose_boom += stickR.y;
                    }
                    else if (goalpose_boom >= 0.9594 && stickR.y < 0)
                    {
                        goalpose_boom += stickR.y;
                    }
                    Debug.Log("swingtest" + goalpose_boom);
                }
                if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).x) > 0.3))
                {
                    if (goalpose_bucket >= -1.221 && goalpose_bucket <= 1.3955)
                    {
                        goalpose_bucket += -1.5f * stickR.x;
                    }
                    else if (goalpose_bucket <= -1.221 && -stickR.x > 0)
                    {
                        goalpose_bucket += -stickR.x;
                    }
                    else if (goalpose_bucket >= 1.3955 && -stickR.x < 0)
                    {
                        goalpose_bucket += -stickR.x;
                    }

                }
                Debug.Log("swingtest" + goalpose_swing + ":" + -stickL.x);
                //
                //

                //
                //for twist
                if (linear.x == 0 && angular.z == 0)
                {
                    zerocounter += 1;
                }
                if ((zerocounter != 0 && linear.x != 0) | (zerocounter != 0 && angular.z != 0))
                {
                    zerocounter = 0;
                }
                //

                timeElapsed += Time.deltaTime;
                if (timeElapsed > publishMessageFrequency)
                {
                    Float64Msg angleMessage_swing = new Float64Msg
                    {
                        data = goalpose_swing // Set your joint angle here, e.g., transform.rotation.eulerAngles.x//
                    };
                    Float64Msg angleMessage_boom = new Float64Msg
                    {
                        data = goalpose_boom // Set your joint angle here, e.g., transform.rotation.eulerAngles.x//
                    };
                    Float64Msg angleMessage_arm = new Float64Msg
                    {
                        data = goalpose_arm // Set your joint angle here, e.g., transform.rotation.eulerAngles.x//
                    };
                    Float64Msg angleMessage_bucket = new Float64Msg
                    {
                        data = goalpose_bucket // Set your joint angle here, e.g., transform.rotation.eulerAngles.x//
                    };
                    //twist
                    TwistMsg Twist = new TwistMsg(
                       linear,
                       angular
                    );
                    //
                    Debug.Log(goalpose_swing + "  :  " + goalpose_boom + "  :  " + goalpose_arm + "  :  " + goalpose_bucket);

                    if (zerocounter <= 20)
                    {
                        // ros.Send(topicName_cmd_vel, Twist);
                    }

                    timeElapsed = 0;
                }
                List<float> msg = new List<float>();
                msg.Add(goalpose_swing);
                msg.Add(goalpose_boom);
                msg.Add(goalpose_arm);
                msg.Add(goalpose_bucket);
                msg.Add(0);

                //
                //private List<ArticulationBody> targetjoints;
                //private List<string> targetjointNames;
                //private double targetPos;
                //
                targetjoints = new List<ArticulationBody>();
                targetjointNames = new List<string>();
                //
                targetObject = GameObject.Find("zx200");
                //Debug.Log(msg.position[0]);
                //   pos_of_swing_joint = msg[0];
                //   pos_of_boom_joint = msg[1];
                //   pos_of_arm_joint = msg[2];
                //   pos_of_bucket_joint = msg[3];
                //   pos_of_end_joint = msg[4];
                //Debug.Log(pos_of_bucket_joint);
                //Debug.Log(msg.velocity[0]);
                //Debug.Log(msg.effort[0]);
                //
                int j = 0;
                foreach (var joint in targetObject.GetComponentsInChildren<ArticulationBody>())
                {
                    //Debug.Log(joint);
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
                            targetPos = msg[j];

                            var drive = joint.xDrive;//targetjoints[i].xDrive;


                            if (JointContorollerMode.ToString() == "velocity") 
                            {
                                //targetVelocity = 1.1f;
                                drive.driveType = ArticulationDriveType.Velocity;// = targetVelocity;
                                drive.targetVelocity = (float)targetPos;
                                joint.xDrive = drive;
                            }
                            else
                            {
                                drive.driveType = ArticulationDriveType.Force;
                                drive.target = (float)(targetPos * Mathf.Rad2Deg);
                                joint.xDrive = drive;//targetjoints[i].xDrive = drive;
                                                     //Debug.Log(j + "abcd" + joint + " " + targetPos);
                            }


                            //
                            /*
                            
                            */
                            j += 1;
                        }
                    }
                }


            }
        }
        /*
        if ((laiser.conum_zx200 > 0) && OVRInput.GetDown(OVRInput.RawButton.B) && (laiser.num == 1))
        {
            scriptA.RotationRatchet = 45;
            scriptA.RotationAmount = 0.5f;
            Debug.Log("kaitennha" + scriptA.RotationRatchet + scriptA.RotationAmount);
        }
        */
    }
    /*
    void Callback(JointStateMsg msg)
    {
        //
        //private List<ArticulationBody> targetjoints;
        //private List<string> targetjointNames;
        //private double targetPos;
        //
        targetjoints = new List<ArticulationBody>();
        targetjointNames = new List<string>();
        //
        targetObject = GameObject.Find("zx200");
        //Debug.Log(msg.position[0]);
        pos_of_swing_joint = msg.position[0];
        pos_of_boom_joint = msg.position[1];
        pos_of_arm_joint = msg.position[2];
        pos_of_bucket_joint = msg.position[3];
        pos_of_end_joint = msg.position[4];
        //Debug.Log(pos_of_bucket_joint);
        //Debug.Log(msg.velocity[0]);
        //Debug.Log(msg.effort[0]);
        //
        int j = 0;
        foreach (var joint in targetObject.GetComponentsInChildren<ArticulationBody>())
        {
            Debug.Log(joint);
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
                    drive.target = (float)(targetPos * Mathf.Rad2Deg);
                    joint.xDrive = drive;//targetjoints[i].xDrive = drive;
                    Debug.Log("abcd" + joint + " " + targetPos);
                    j += 1;
                }
            }
        }

    }*/
}

