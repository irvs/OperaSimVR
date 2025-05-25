using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using System.Collections.Generic;
using Unity.Robotics.Core;
using Unity.Robotics.UrdfImporter;

public class JointAnglePublisher : MonoBehaviour
{
    Controller_manager VRManager;
    mode_selector selected_mode;
    public enum ONOFF { Off, On }
    public ONOFF OnOffSw;
   // public int sw = 0;
    public bool emergency;
    public bool SimORReal = false;
    public bool key;
    public float linearspeed = 1.00f;
    public float rotspeed = 0.50f;
    float movespeed = 0.01f;
    ROSConnection ros;
    FieldMainManager SimORRealSelecter;
    JointSubscriber RealJointAngular;
    public string topicName_cmd_vel = "zx200/tracks/cmd_vel";
    public string topicName_swing = "/zx200/swing/cmd";
    public string topicName_boom = "/zx200/boom/cmd";
    public string topicName_arm = "/zx200/arm/cmd";
    public string topicName_bucket = "/zx200/bucket/cmd";
    public string topicname_joint = "/zx200/front_cmd";
    public string Real_topicName_joint_velocity = "/zx200/front_cmd/for_ROS";
    public string controller_swTopicName = "controller_sw_zx200";
    public string EmergencyTopicName;
    public float publishMessageInterval = 0.02f;//50Hz
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
    JointControl JointController;
    //
    GameObject PlayertargetObject;
    //
    public float Time_Delay = 5.0f;
    private float timeElapsed_start = 0.0f;
    //

    void Start()
    {
        VRManager = FindObjectOfType<Controller_manager>();
        ros = ROSConnection.GetOrCreateInstance();
        PlayertargetObject = GameObject.Find("OVRPlayerController");
        //
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        //
        if ((SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")|| (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal"))
        {
            ros.RegisterPublisher<JointStateMsg>(Real_topicName_joint_velocity);
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
        {
            ros.RegisterPublisher<Float64Msg>(topicName_swing);
            ros.RegisterPublisher<Float64Msg>(topicName_boom);
            ros.RegisterPublisher<Float64Msg>(topicName_arm);
            ros.RegisterPublisher<Float64Msg>(topicName_bucket);
        }
        ros.RegisterPublisher<BoolMsg>(controller_swTopicName);
        ros.RegisterPublisher<TwistMsg>(topicName_cmd_vel);
        ros.RegisterPublisher<BoolMsg>(EmergencyTopicName);
        //
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
            BoolMsg EMGmessage = new BoolMsg(
              true
              );
            //
            if (timeElapsed >= publishMessageInterval * 20.0f)
            {
                ros.Publish(EmergencyTopicName, EMGmessage);
                //
                velocity_of_swing = 0.0f;
                velocity_of_boom = 0.0f;
                velocity_of_arm = 0.0f;
                velocity_of_bucket = 0.0f;
                velocities = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                listOfJointCmdList.Add(velocities);
                string[] jointNamesArray = jointNames.ToArray();
                double[] positionsArray = positions.ToArray();
                double[] velocitiesArray = velocities.ToArray();
                double[] effortsArray = efforts.ToArray();
                
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
            if (OnOffSw.ToString() == "On")
            {
                if (sw_timeElapsed >= publishMessageInterval * 50.0f)
                {
                    BoolMsg message = new BoolMsg(true);
                    ros.Publish(controller_swTopicName, message);
                    sw_timeElapsed = 0.0f;
                }

                else if (VRManager.PlayerPoseMove_SW > 0 || OnOffSw.ToString() == "On")
                {
                    selected_mode = FindObjectOfType<mode_selector>();
                    dissconnect_timer += Time.deltaTime;
                    timeElapsed_start += Time.deltaTime;

                    OVRPlayerController scriptA = PlayertargetObject.GetComponent<OVRPlayerController>();
                    if (scriptA != null)
                    {
                        scriptA.RotationRatchet = 0;
                        scriptA.RotationAmount = 0;
                    }
                    /////////////////////////////////////////////position
                    /////////////////////////////////////////////
                    if (SimORReal == false)
                    {
                        if (Input.GetKey(KeyCode.Y)){goalpose_swing += 0.005f;}
                        if (Input.GetKey(KeyCode.H)){goalpose_swing -= 0.005f;}
                        if (Input.GetKey(KeyCode.U) && goalpose_boom <= 0.9594){goalpose_boom += 0.01f;}
                        if (Input.GetKey(KeyCode.J) && goalpose_boom >= -1.2211){goalpose_boom -= 0.01f;}
                        if (Input.GetKey(KeyCode.I) && goalpose_arm <= 2.35){goalpose_arm += 0.005f;}
                        if (Input.GetKey(KeyCode.K) && goalpose_arm >= 0.785){goalpose_arm -= 0.005f;}
                        if (Input.GetKey(KeyCode.O) && goalpose_bucket <= 1.39555){goalpose_bucket += 0.01f;}
                        if (Input.GetKey(KeyCode.L) && goalpose_bucket >= -1.2211){goalpose_bucket -= 0.01f;}
                        //key
                        if (key == true)
                        {
                            if (Input.GetKey(KeyCode.LeftArrow)){rotation = rotspeed;}
                            if (Input.GetKey(KeyCode.RightArrow)){rotation = -rotspeed;}
                            if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow)){rotation = 0;}
                            if (Input.GetKey(KeyCode.UpArrow)){frontback = linearspeed;}
                            if (Input.GetKey(KeyCode.DownArrow)){frontback = -linearspeed;}
                            if (Input.GetKeyUp(KeyCode.UpArrow)|| Input.GetKeyUp(KeyCode.DownArrow)) {frontback = 0;}
                            linear.x = frontback;
                            angular.x = rotation;
                        }
                        //
                        //for joint
                        Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                        Vector2 stickR = movespeed * OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).x) > 0.3))
                        {
                            goalpose_swing += -0.3f * stickL.x;
                        }
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).y) > 0.3))
                        {
                            if (goalpose_arm >= 0.785 && goalpose_arm <= 2.35){goalpose_arm += -stickL.y;}
                            else if (goalpose_arm <= 0.785 && -stickL.y > 0){goalpose_arm += -stickL.y;}
                            else if (goalpose_arm >= 2.35 && -stickL.y < 0){goalpose_arm += -stickL.y;}
                        }
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).y) > 0.3))
                        {
                            if (goalpose_boom >= -1.221 && goalpose_boom <= 0.9594){goalpose_boom += 0.3f * stickR.y;}
                            else if (goalpose_boom <= -1.221 && stickR.y > 0){goalpose_boom += stickR.y;}
                            else if (goalpose_boom >= 0.9594 && stickR.y < 0){goalpose_boom += stickR.y;}
                        }
                        if ((Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).x) > 0.3))
                        {
                            if (goalpose_bucket >= -1.221 && goalpose_bucket <= 1.3955){goalpose_bucket += -1.5f * stickR.x;}
                            else if (goalpose_bucket <= -1.221 && -stickR.x > 0){goalpose_bucket += -stickR.x;}
                            else if (goalpose_bucket >= 1.3955 && -stickR.x < 0){goalpose_bucket += -stickR.x;}
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
                    //////////////////////////////////////////////////
                    //////////////////////////////////////////////////twist
                    RBack = OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger);
                    RFront = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);
                    LBack = OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger);
                    LFront = OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger);

                    if (RFront >= 0.5 && LFront >= 0.5){linear.x = (RFront + LFront) / 2;}
                    else if (RFront < 0.5 && LFront >= 0.5)
                    {
                        //(LFront - RFront)/2
                        angular.z = 0.3;
                    }
                    else if (RFront >= 0.5 && LFront < 0.5){angular.z = -0.3;}
                    else
                    {
                        linear.x = 0;
                        angular.x = 0;
                    }
                    //key
                    if (key == true)
                    {
                        if (Input.GetKey(KeyCode.LeftArrow)){rotation = rotspeed;}
                        if (Input.GetKeyUp(KeyCode.LeftArrow)){rotation = 0;}
                        if (Input.GetKey(KeyCode.RightArrow)){rotation = -rotspeed;}
                        if (Input.GetKeyUp(KeyCode.RightArrow)){rotation = 0;}
                        if (Input.GetKey(KeyCode.UpArrow)){frontback = linearspeed;}
                        if (Input.GetKeyUp(KeyCode.UpArrow)){frontback = 0;}
                        if (Input.GetKey(KeyCode.DownArrow)){frontback = -linearspeed;}
                        if (Input.GetKeyUp(KeyCode.DownArrow)){frontback = 0;}
                        linear.x = frontback;
                        angular.x = rotation;
                    }

                    timeElapsed += Time.deltaTime;
                    sw_timeElapsed += Time.deltaTime;
                    JointController = GetComponent<JointControl>();
                    
                    if (timeElapsed > publishMessageInterval * 20.0f)
                    {
                        if (selected_mode.mode == 2)
                        {
                            if (SimORReal == false)
                            {
                                JointController.JointTargets[0] = goalpose_swing;
                                JointController.JointTargets[1] = goalpose_boom;
                                JointController.JointTargets[2] = goalpose_arm;
                                JointController.JointTargets[3] = goalpose_bucket;
                            }
                            if (SimORReal == true)
                            {
                                JointController.JointTargets[0] = velocity_of_swing;
                                JointController.JointTargets[1] = velocity_of_boom;
                                JointController.JointTargets[2] = velocity_of_arm;
                                JointController.JointTargets[3] = velocity_of_bucket;
                            }
                        }
                        if (SimORReal == false)
                        {
                            Debug.Log("goal_pose : " + goalpose_swing + "  :  " + goalpose_boom + "  :  " + goalpose_arm + "  :  " + goalpose_bucket);
                            positions[0] = goalpose_swing;
                            positions[1] = goalpose_boom;
                            positions[2] = goalpose_arm;
                            positions[3] = goalpose_bucket;
                            listOfJointCmdList.Add(positions);
                            if (selected_mode.mode == 2 && timeElapsed_start > (Time_Delay + 5.0f) && listOfJointCmdList.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) >= 0)
                            {
                                int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                                goalpose_swing = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][0];
                                goalpose_boom = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][1];
                                goalpose_arm = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][2];
                                goalpose_bucket = (float)listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][3];
                            }

                            Float64Msg angleMessage_swing = new Float64Msg
                            {data = goalpose_swing};
                            Float64Msg angleMessage_boom = new Float64Msg
                            {data = goalpose_boom};
                            Float64Msg angleMessage_arm = new Float64Msg
                            {data = goalpose_arm};
                            Float64Msg angleMessage_bucket = new Float64Msg
                            {data = goalpose_bucket};
                            
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
                            ros.Publish(topicName_cmd_vel, Twist);
                            timeElapsed = 0;
                        }

                        else if (SimORReal == true)
                        {
                            Debug.Log("velocity : " + velocity_of_swing + "  :  " + velocity_of_boom + "  :  " + velocity_of_arm + "  :  " + velocity_of_bucket);
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
                            if (selected_mode.mode == 2 && timeElapsed_start > (Time_Delay + 5.0f) && listOfJointCmdList.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) >= 0)
                            {
                                int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                                velocities[0] = listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][0];
                                velocities[1] = listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][1];
                                velocities[2] = listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][2];
                                velocities[3] = listOfJointCmdList[listOfJointCmdList.Count - 1 - CMD_time][3];
                            }
                            string[] jointNamesArray = jointNames.ToArray();
                            double[] positionsArray = positions.ToArray();
                            double[] velocitiesArray = velocities.ToArray();
                            double[] effortsArray = efforts.ToArray();
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
                            timeElapsed = 0;
                        }
                    }
                    ///for prev
                    selected_mode = FindObjectOfType<mode_selector>();
                    dissconnect_timer = 0.0f;
                    if (selected_mode.mode == 2)//Visual tool
                    {
                        RealJointAngular = GetComponent<JointSubscriber>();
                        float pos_of_swing = (float)RealJointAngular.JointPositions[0];
                        float pos_of_boom = (float)RealJointAngular.JointPositions[1];
                        float pos_of_arm = (float)RealJointAngular.JointPositions[2];
                        float pos_of_bucket = (float)RealJointAngular.JointPositions[3];
                        float velo_of_swing = (float)RealJointAngular.JointPositions[4];
                        float velo_of_boom = (float)RealJointAngular.JointPositions[5];
                        float velo_of_arm = (float)RealJointAngular.JointPositions[6];
                        float velo_of_bucket = (float)RealJointAngular.JointPositions[7];

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
        }
    }
}