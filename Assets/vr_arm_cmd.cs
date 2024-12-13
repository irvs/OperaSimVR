using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using System;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
//using RosMessageTypes.Com3;
using static UnityEngine.GraphicsBuffer;
using System.ComponentModel.Composition.Primitives;
using System.Runtime.Remoting.Messaging;
using UnityEngine.UIElements;
using Unity.Robotics.Core;
using UnityEngine.Analytics;
using Unity.Robotics.UrdfImporter;

public class JointAnglePublisher : MonoBehaviour
{
    Controller_manager VRManager;
    mood_selector selected_mode;
    public int sw = 0;
    public bool emergency;
    public bool SimORReal=false;
    public int key = 0;
    public float linearspeed = 1.00f;
    public float rotspeed = 0.50f;
    int cmd_operation = 0;
    float movespeed = 0.01f;
    ROSConnection ros;
    FieldMainManager SimORRealSelecter;
    public string topicName_cmd_vel = "zx200/tracks/cmd_vel";
    public string topicName_swing = "/zx200/swing/cmd";
    public string topicName_boom = "/zx200/boom/cmd";
    public string topicName_arm = "/zx200/arm/cmd";
    public string topicName_bucket = "/zx200/bucket/cmd";
    public string topicname_joint = "/zx200/front_cmd";
    public string Real_topicName_joint_velocity = "/zx200/front_cmd/for_ROS";
    public string SimPhysXSubscribeTopicName;
    public string SimAGXSubscribeTopicName;
    public string RealSubscribeTopicName;
    public string controller_swTopicName = "controller_sw_zx200";
    public float publishMessageFrequency = 0.5f;
    public float publishMessageInterval = 0.02f;//50Hz
    public GameObject targetObject;
    private float timeElapsed;
    private float sw_timeElapsed = 0.0f;
    private float frontback;
    private float rotation;
    private float dissconnect_timer;
    private int dissconnect_detecter = 0;
    float goalpose_swing = 0.0f;
    float goalpose_boom = -0.7f;
    float goalpose_arm = 1.57f;
    float goalpose_bucket = 0.0f;
    float velocity_of_swing = 0.0f;
    float velocity_of_boom = 0.0f;
    float velocity_of_arm = 0.0f;
    float velocity_of_bucket = 0.0f;
    List<float> JointCmdList = new List<float>();
    public List<List<double>> listOfJointCmdList = new List<List<double>>();
    List<List<double>> listOfJointPositionList = new List<List<double>>();
    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    int zerocounter = 0;
    float RFront = 0.0f;
    float RBack = 0.0f;
    float LFront = 0.0f;
    float LBack = 0.0f;
    private List<string> jointNames; 
    private List<double> positions; 
    private List<double> velocities; 
    private List<double> efforts;
    private List<double> JointPositions;
    private List<ArticulationBody> joints;
    private double[] Jointposition;
    //
    public GameObject PlayertargetObject;
    //
    public int control_mode = 0;
    List<float>Current_joint_CMD_List= new List<float>();
    public float Time_Delay = 5.0f;
    private float timeElapsed_start = 0.0f;
    //

    void Start()
    {
        VRManager = FindObjectOfType<Controller_manager>();
        ros = ROSConnection.GetOrCreateInstance();
        if (VRManager != null)
        {
            Debug.Log("Player's health is: " + VRManager.num);
        }
        //
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
        {
            SimORReal = false;
            ros.Subscribe<JointStateMsg>(SimPhysXSubscribeTopicName, Callback);
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")
        {
            SimORReal = true;
            ros.Subscribe<JointStateMsg>(SimAGXSubscribeTopicName, Callback);
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
        {
            SimORReal = true;
            ros.Subscribe<JointStateMsg>(RealSubscribeTopicName, Callback);
        }
        //


        ros.RegisterPublisher<BoolMsg>(controller_swTopicName);
        ros.RegisterPublisher<TwistMsg>(topicName_cmd_vel);
        ros.RegisterPublisher<Float64Msg>(topicName_swing);
        ros.RegisterPublisher<Float64Msg>(topicName_boom);
        ros.RegisterPublisher<Float64Msg>(topicName_arm);
        ros.RegisterPublisher<Float64Msg>(topicName_bucket);
        ros.RegisterPublisher<JointStateMsg>(Real_topicName_joint_velocity);
        //
        // ジョイント名、位置、速度、力の初期化
        jointNames = new List<string> { "swing_joint", "boom_joint", "arm_joint", "bucket_joint", "bucket_end_joint" }; 
        positions = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 }; 
        velocities = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 }; 
        efforts = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 };
        JointPositions = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 };
        //List<double> jointCmd = new List<double> { 0.0, 0.0, 0.0, 0.0 };
        listOfJointCmdList.Add(efforts);
        listOfJointPositionList.Add(efforts);

        joints = new List<ArticulationBody>();
        jointNames = new List<string>();
        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>())
        {
            if (joint.isActiveAndEnabled)
            {
                var ujoint = joint.GetComponent<UrdfJoint>();
                if (ujoint && !(ujoint is UrdfJointFixed))
                {
                    joints.Add(joint);
                    jointNames.Add(ujoint.jointName);
                }
            }
        }
}
    void Update()
    {

        if (emergency == true)
        {
            timeElapsed += Time.deltaTime;
            linear.x = 0.00f;
            angular.z = 0.00f;
            TwistMsg Twist = new TwistMsg(
              linear,
              angular
              );
            //Debug.Log("cont_mode1_read_list");
            //
            if (timeElapsed >= publishMessageFrequency / 2.0f)
            {
                //
                velocity_of_swing = 0.0f;
                velocity_of_boom = 0.0f;
                velocity_of_arm = 0.0f;
                velocity_of_bucket = 0.0f;
                velocities[0] = 0.0f;
                velocities[1] = 0.0f;
                velocities[2] = 0.0f;
                velocities[3] = 0.0f;
                listOfJointCmdList.Add(velocities);
                // List<double> を double[] に変換
                string[] jointNamesArray = jointNames.ToArray();
                double[] positionsArray = positions.ToArray();
                double[] velocitiesArray = velocities.ToArray();
                double[] effortsArray = efforts.ToArray();
                // 変換した配列を関数に渡す
                HeaderMsg header = new HeaderMsg(
                        new TimeStamp(Clock.time),
                        " "
                        );

                JointStateMsg JointCMD = new JointStateMsg(
                    header,
                    jointNamesArray,
                    positionsArray,
                    velocitiesArray,
                    effortsArray
                );
                ros.Publish(Real_topicName_joint_velocity, JointCMD);
                //
                ros.Publish(topicName_cmd_vel, Twist);
                timeElapsed = 0.0f;
            }

        }

        else if (emergency == false)
        {

            if (dissconnect_timer >= 3.0f)
            {
                dissconnect_detecter = 1;
                if (emergency == true)
                {
                    dissconnect_detecter = 2;
                }
                //emergency = true;
            }
            if (dissconnect_detecter == 1 && dissconnect_timer <= 1.0f)
            {
                dissconnect_detecter = 0;
                //emergency = false;
            }
            else if (dissconnect_detecter == 2 && dissconnect_timer <= 1.0f)
            {
                dissconnect_detecter = 0;
            }
            VRManager = FindObjectOfType<Controller_manager>();
            if (sw == 1)
            {
                if (sw_timeElapsed >= publishMessageInterval * 50.0f)
                {
                    // Debug.Log("Publish After Delay Time");
                    BoolMsg message = new BoolMsg(
                        true
                        );
                    ros.Publish(controller_swTopicName, message);
                    sw_timeElapsed = 0.0f;
                }

                if (VRManager.Player_posi_mover_SW == 0 && sw != 1)
                {
                    cmd_operation = 0;

                }
                else if (VRManager.Player_posi_mover_SW > 0 || sw == 1)
                {
                    cmd_operation = 1;
                    //Debug.Log("geton");

                    selected_mode = FindObjectOfType<mood_selector>();
                    dissconnect_timer += Time.deltaTime;
                    timeElapsed_start += Time.deltaTime;

                    OVRPlayerController scriptA = PlayertargetObject.GetComponent<OVRPlayerController>();
                    if (scriptA != null)
                    {
                        //Debug.Log("kaitennha" + scriptA.RotationRatchet);
                        scriptA.RotationRatchet = 0;
                        scriptA.RotationAmount = 0;
                        //Debug.Log("kaitennha" + scriptA.RotationRatchet);
                    }
                    /////////////////////////////////////////////position
                    /////////////////////////////////////////////
                    if (SimORReal == false)
                    {


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
                        if (Input.GetKey(KeyCode.I) && goalpose_arm <= 2.35)
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

                        //key
                        if (key == 1)
                        {

                            if (Input.GetKey(KeyCode.Space))
                            {
                                // OVRManager.display.RecenterPose();
                                frontback = 0.0f;
                                rotation = 0.0f;
                            }
                            //
                            if (Input.GetKey(KeyCode.LeftArrow))
                            {
                                rotation = rotspeed;
                            }
                            if (Input.GetKeyUp(KeyCode.LeftArrow))
                            {
                                rotation = 0;
                            }
                            if (Input.GetKey(KeyCode.RightArrow))
                            {
                                rotation = -rotspeed;
                            }
                            if (Input.GetKeyUp(KeyCode.RightArrow))
                            {
                                rotation = 0;
                            }
                            if (Input.GetKey(KeyCode.UpArrow))
                            {
                                frontback = linearspeed;
                            }
                            if (Input.GetKeyUp(KeyCode.UpArrow))
                            {
                                frontback = 0;
                            }
                            if (Input.GetKey(KeyCode.DownArrow))
                            {
                                frontback = -linearspeed;
                            }
                            if (Input.GetKeyUp(KeyCode.DownArrow))
                            {
                                frontback = 0;
                            }
                            linear.x = frontback;
                            angular.x = rotation;
                        }

                        //
                        //for joint
                        Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                        Vector2 stickR = movespeed * OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).y) > 0.3))
                        {
                            if (goalpose_arm >= 0.785 && goalpose_arm <= 2.35)
                            {
                                goalpose_arm += -stickL.y;

                            }
                            else if (goalpose_arm <= 0.785 && -stickL.y > 0)
                            {
                                goalpose_arm += -stickL.y;
                            }
                            else if (goalpose_arm >= 2.35 && -stickL.y < 0)
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
                    }
                    ////////////////////////////////////////////////////////////
                    ////////////////////////////////////////////////////////////velosity
                    if (SimORReal == true)
                    {
                        Jointposition = new double[joints.Count];
                        for (int i = 0; i < joints.Count; i++)
                        {
                            Jointposition[i] = joints[i].jointPosition[0];
                        }

                        //
                        //for joint
                        Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                        Vector2 stickR = movespeed * OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
                        //arm
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).y) > 0.3))
                        {
                            if (Jointposition[2] <= 2.35 && Jointposition[2] >= 0.959)
                            {
                                velocity_of_arm = -0.5f * stickL.y;

                            }
                            if (Jointposition[2] <= 0.959 || Jointposition[2] >= 2.35)
                            {
                                velocity_of_arm = 0.0f;
                            }
                            if (Jointposition[2] <= 0.959 && -stickL.y > 0)
                            {
                                velocity_of_arm = -0.5f * stickL.y;
                            }
                            else if (Jointposition[2] >= 2.35 && -stickL.y < 0)
                            {
                                velocity_of_arm = -0.5f * stickL.y;
                            }

                        }
                        else if (Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).y) > 0.3)
                        {
                            velocity_of_arm = 0.0f;
                        }
                        //swing
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).x) > 0.3))
                        {
                            velocity_of_swing = -0.5f * stickL.x;
                        }
                        else if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).x) <= 0.3))
                        {
                            velocity_of_swing = -0.0f;
                        }
                        //boom
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).y) > 0.3))
                        {
                            if (Jointposition[1] >= -0.872 && Jointposition[1] <= 0.1749594)
                            {
                                velocity_of_boom = 0.3f * stickR.y;
                            }
                            if (Jointposition[1] <= -0.872 || Jointposition[1] >= 0.1749594)
                            {
                                velocity_of_boom = 0.0f;
                            }
                            if (Jointposition[1] <= -0.872 && stickR.y > 0)
                            {
                                velocity_of_boom = 0.3f * stickR.y;
                            }
                            else if (Jointposition[1] >= 0.1749594 && stickR.y < 0)
                            {
                                velocity_of_boom = 0.3f * stickR.y;
                            }
                        }
                        else if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).y) <= 0.3))
                        {
                            velocity_of_boom = 0.0f;
                        }
                        //bucket
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).x) > 0.3))
                        {
                            if (Jointposition[3] >= -1.221 && Jointposition[3] <= 1.3955)
                            {
                                velocity_of_bucket = -0.5f * stickR.x;
                            }
                            if (Jointposition[3] <= -1.221 || Jointposition[3] >= 1.3955)
                            {
                                velocity_of_bucket = 0.0f;
                            }
                            if (Jointposition[3] <= -1.221 && -stickR.x > 0)
                            {
                                velocity_of_bucket = -0.5f * stickR.x;
                            }
                            else if (Jointposition[3] >= 1.3955 && -stickR.x < 0)
                            {
                                velocity_of_bucket = -0.5f * stickR.x;
                            }

                        }
                        else if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).x) <= 0.3))
                        {
                            velocity_of_bucket = 0.0f;
                        }

                        //swing
                        if (Input.GetKey(KeyCode.Y) && velocity_of_swing <= 1.0)
                        {
                            velocity_of_swing += 0.030f;
                        }
                        if (Input.GetKey(KeyCode.H) && velocity_of_swing >= -1.0)
                        {
                            velocity_of_swing -= 0.030f;
                        }
                        if (Input.GetKeyUp(KeyCode.Y) || Input.GetKeyUp(KeyCode.H))
                        {
                            velocity_of_swing = 0.0f;
                        }
                        //boom
                        if (Input.GetKey(KeyCode.U) && velocity_of_boom <= 1.0 && Jointposition[1] <= 0.1749594) 
                        {
                            velocity_of_boom = +0.030f;
                        }
                        if (Input.GetKey(KeyCode.J) && velocity_of_boom >= -1.0 && Jointposition[1] >= -0.872)
                        {
                            velocity_of_boom = -0.030f;
                        }
                        if (Jointposition[1] > 0.9594 || Jointposition[1] < -0.872)
                        {
                            velocity_of_boom = 0.0f;
                            if (Input.GetKey(KeyCode.U) && velocity_of_boom <= 1.0 && Jointposition[1] <= 0.1749594)
                            {
                                velocity_of_boom = +0.030f;
                            }
                            if (Input.GetKey(KeyCode.J) && velocity_of_boom >= -1.0 && Jointposition[1] >= -0.872)
                            {
                                velocity_of_boom = -0.030f;
                            }
                        }
                        if (Input.GetKeyUp(KeyCode.J) || Input.GetKeyUp(KeyCode.U))
                        {
                            velocity_of_boom = 0.0f;
                        }
                        //arm
                        if (Input.GetKey(KeyCode.I) && velocity_of_arm <= 1.0 && Jointposition[2] <= 2.5294)
                        {
                            velocity_of_arm += 0.030f;
                        }
                        if (Input.GetKey(KeyCode.K) && velocity_of_arm >= -1.0 && Jointposition[2] >= 0.959)
                        {
                            velocity_of_arm -= 0.030f;
                        }
                        if (Jointposition[2] > 2.5294 || Jointposition[2] < 0.959)
                        {
                            velocity_of_arm = 0.0f;
                            if (Input.GetKey(KeyCode.I) && velocity_of_arm <= 1.0 && Jointposition[2] <= 2.5294)
                            {
                                velocity_of_arm += 0.030f;
                            }
                            if (Input.GetKey(KeyCode.K) && velocity_of_arm >= -1.0 && Jointposition[2] >= 0.959)
                            {
                                velocity_of_arm -= 0.030f;
                            } 
                        }
                        if (Input.GetKeyUp(KeyCode.I) || Input.GetKeyUp(KeyCode.K))
                        {
                            velocity_of_arm = 0.0f;
                        }
                        //bucket
                        if (Input.GetKey(KeyCode.O) && velocity_of_bucket <= 1.0 && Jointposition[3] <= 1.39555)
                        {
                            velocity_of_bucket += 0.030f;
                        }
                        if (Input.GetKey(KeyCode.L) && velocity_of_bucket >= -1.0 && Jointposition[3] >= -1.2211)
                        {
                            velocity_of_bucket -= 0.030f;
                        }
                        if(Jointposition[3] > 1.39555 || Jointposition[3] < -1.2211)
                        {
                            velocity_of_bucket = 0.0f;
                            if (Input.GetKey(KeyCode.O) && velocity_of_bucket <= 1.0 && Jointposition[3] <= 1.39555)
                            {
                                velocity_of_bucket += 0.030f;
                            }
                            if (Input.GetKey(KeyCode.L) && velocity_of_bucket >= -1.0 && Jointposition[3] >= -1.2211)
                            {
                                velocity_of_bucket -= 0.030f;
                            }
                        }
                        if (Input.GetKeyUp(KeyCode.O) || Input.GetKeyUp(KeyCode.L))
                        {
                            velocity_of_bucket = 0.0f;
                        }



                        
                    }
                    //Debug.Log("swingtest" + goalpose_swing + ":" + -stickL.x);
                    //////////////////////////////////////////////////
                    //////////////////////////////////////////////////twist
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
                            //(LFront - RFront)/2
                            angular.z = 0.3;
                        }
                        else if (RFront >= 0.5 && LFront < 0.5)
                        {
                            angular.z = -0.3;
                        }
                        else
                        {
                            linear.x = 0;
                            angular.x = 0;
                        }

                    //key
                    if (key == 1)
                    {
                        if (Input.GetKey(KeyCode.Space))
                        {
                            // OVRManager.display.RecenterPose();
                            frontback = 0.0f;
                            rotation = 0.0f;
                        }
                        //
                        if (Input.GetKey(KeyCode.LeftArrow))
                        {
                            rotation = rotspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.LeftArrow))
                        {
                            rotation = 0;
                        }
                        if (Input.GetKey(KeyCode.RightArrow))
                        {
                            rotation = -rotspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.RightArrow))
                        {
                            rotation = 0;
                        }
                        if (Input.GetKey(KeyCode.UpArrow))
                        {
                            frontback = linearspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.UpArrow))
                        {
                            frontback = 0;
                        }
                        if (Input.GetKey(KeyCode.DownArrow))
                        {
                            frontback = -linearspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.DownArrow))
                        {
                            frontback = 0;
                        }
                        linear.x = frontback;
                        angular.x = rotation;
                    }

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
                    sw_timeElapsed += Time.deltaTime;

                    if (control_mode == 1 && selected_mode.mode == 2)
                    {
                        

                    }


                    if (timeElapsed > publishMessageFrequency)
                    {
                        if (SimORReal == false)
                        {
                            Debug.Log(goalpose_swing + "  :  " + goalpose_boom + "  :  " + goalpose_arm + "  :  " + goalpose_bucket);
                            positions[0] = goalpose_swing;
                            positions[1] = goalpose_boom;
                            positions[2] = goalpose_arm;
                            positions[3] = goalpose_bucket;
                            listOfJointCmdList.Add(positions);
                            if (control_mode == 1 && selected_mode.mode == 2 && timeElapsed_start > (Time_Delay + 5.0f) && listOfJointCmdList.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) >= 0)
                            {
                                int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                                goalpose_swing = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][0];
                                goalpose_boom = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][1];
                                goalpose_arm = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][2];
                                goalpose_bucket = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][3];

                            }

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

                            
                            //Publish
                            ros.Publish(topicName_swing, angleMessage_swing);
                            ros.Publish(topicName_boom, angleMessage_boom);
                            ros.Publish(topicName_arm, angleMessage_arm);
                            ros.Publish(topicName_bucket, angleMessage_bucket);
                            if (zerocounter <= 20)
                            {
                                ros.Publish(topicName_cmd_vel, Twist);
                            }

                            timeElapsed = 0;
                        }

                        else if (SimORReal == true)
                        {
                            Debug.Log(velocity_of_swing + "  :  " + velocity_of_boom + "  :  " + velocity_of_arm + "  :  " + velocity_of_bucket);
                            velocities[0] = velocity_of_swing;
                            velocities[1] = velocity_of_boom;
                            velocities[2] = velocity_of_arm;
                            velocities[3] = velocity_of_bucket;
                            listOfJointCmdList.Add(velocities);
                            for (int i = 0; i < joints.Count; i++)
                            {
                                JointPositions[i] = joints[i].jointPosition[0];

                            }
                            listOfJointPositionList.Add(JointPositions);
                            if (control_mode == 1 && selected_mode.mode == 2 && timeElapsed_start > (Time_Delay + 5.0f) && listOfJointCmdList.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) >= 0)
                            {
                                int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                                velocities[0] = listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][0];
                                velocities[1] = listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][1];
                                velocities[2] = listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][2];
                                velocities[3] = listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][3];

                            }
                            // List<double> を double[] に変換
                            string[] jointNamesArray = jointNames.ToArray();
                            double[] positionsArray = positions.ToArray();
                            double[] velocitiesArray = velocities.ToArray();
                            double[] effortsArray = efforts.ToArray();
                            // 変換した配列を関数に渡す
                            HeaderMsg header = new HeaderMsg(
                                    new TimeStamp(Clock.time),
                                    " "
                                    );

                            JointStateMsg JointCMD=new JointStateMsg(
                                header,
                                jointNamesArray,
                                positionsArray,
                                velocitiesArray,
                                effortsArray
                            );
                            ros.Publish(Real_topicName_joint_velocity, JointCMD);
                            /*
                            if (zerocounter <= 20)
                            {
                                ros.Publish(topicName_cmd_vel, Twist);
                            }
                            */
                            timeElapsed = 0;
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

    void Callback(JointStateMsg msg)
    {
        selected_mode = FindObjectOfType<mood_selector>();
        dissconnect_timer = 0.0f;
        if (selected_mode.mode == 2)//Visual tool
        {
            float pos_of_swing = (float)msg.position[0];
            float pos_of_boom = (float)msg.position[1];
            float pos_of_arm = (float)msg.position[2];
            float pos_of_bucket = (float)msg.position[3];
            float velo_of_swing = (float)msg.velocity[0];
            float velo_of_boom = (float)msg.velocity[1];
            float velo_of_arm = (float)msg.velocity[2];
            float velo_of_bucket = (float)msg.velocity[3];

            int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
            /*
            float AtTimePosi_swing = (float)listOfJointPositionList[listOfJointPositionList.Count - 1 - CMD_time][0];
            float AtTimePosi_boom = (float)listOfJointPositionList[listOfJointPositionList.Count - 1 - CMD_time][1];
            float AtTimePosi_arm = (float)listOfJointPositionList[listOfJointPositionList.Count - 1 - CMD_time][2];
            float AtTimePosi_bucket = (float)listOfJointPositionList[listOfJointPositionList.Count - 1 - CMD_time][3];
            float AtTimeVelo_swing = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][0];
            float AtTimeVelo_boom = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][1];
            float AtTimeVelo_arm = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][2];
            float AtTimeVelo_bucket = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][3];
            */

        }
    }
}