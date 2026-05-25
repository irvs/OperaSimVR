using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.Com3;
using System.Collections.Generic;
using Unity.Robotics.Core;
using Unity.Robotics.UrdfImporter;

public class JointAnglePublisher : MonoBehaviour
{
    Controller_manager VRManager;
    mode_selector selected_mode;
    public enum ONOFF { Off, On }
    public ONOFF OnOffSw;
    public enum JointContorollerModeOption { Velocity, Position }
    public JointContorollerModeOption JointContorollerMode;
    // public int sw = 0;
    public bool emergency;
    public bool SimORReal = false;
    public bool key;
    public float linearspeed = 1.00f;
    public float rotspeed = 0.50f;
    float movespeed = 0.01f;
    ROSConnection ros;
    JointSubscriber RealJointAngular;
    public string topicName_cmd_vel = "zx200/tracks/cmd_vel";
    public string topicname_joint = "/zx200/front_cmd";
    public string JointControlTopic = "/zx200/front_cmd/for_ROS";
    public string EmergencyTopicName;
    public float publishMessageInterval = 0.02f;//50Hz
    private float timeElapsed;
    private float sw_timeElapsed = 0.0f;
    private float frontback;
    private float rotation;
    private float dissconnect_timer;
    private int dissconnect_detecter = 0;
    List<float> JointCmdList = new List<float>();
    public List<List<double>> listOfJointPositionCmdList = new List<List<double>>();
    public List<List<double>> listOfJointVelocityCmdList = new List<List<double>>();
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
    JointControler JointController;
    //
    GameObject PlayertargetObject;
    //
    public float Time_Delay = 5.0f;
    private float timeElapsed_start = 0.0f;
    //
    float[] goalPose = new float[4];
    float[] velocity = new float[4];

    private readonly float[] minLimit = { -999f, -0.872f, 0.959f, -1.2211f };
    private readonly float[] maxLimit = { 999f, 0.174f, 2.35f, 1.3955f };

    private readonly float[] posSpeed = { 0.005f, 0.01f, 0.005f, -0.01f };
    private readonly float[] velSpeed = { -0.5f, 0.3f, -0.5f, -0.5f };

    private readonly KeyCode[] plusKeys =
    {KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O};

    private readonly KeyCode[] minusKeys =
    {KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L};


    [System.Serializable]
    public class JointState
    {
        public float swing_position;
        public float boom_position;
        public float arm_position;
        public float bucket_position;
        public float swing_velocity;
        public float boom_velocity;
        public float arm_velocity;
        public float bucket_velocity;
    }

    void Start()
    {
        VRManager = FindObjectOfType<Controller_manager>();
        selected_mode = FindObjectOfType<mode_selector>();
        ros = ROSConnection.GetOrCreateInstance();
        PlayertargetObject = GameObject.Find("OVRPlayerController");

        ros.RegisterPublisher<JointCmdMsg>(JointControlTopic);
        ros.RegisterPublisher<TwistMsg>(topicName_cmd_vel);
        ros.RegisterPublisher<BoolMsg>(EmergencyTopicName);
        //
        jointNames = new List<string> { "swing_joint", "boom_joint", "arm_joint", "bucket_joint", "bucket_end_joint" };
        positions = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 };
        velocities = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 };
        efforts = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 };
        JointPositions = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 };
        //List<double> jointCmd = new List<double> { 0.0, 0.0, 0.0, 0.0 };
        listOfJointVelocityCmdList.Add(efforts);
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
                velocity[0] = 0.0f;
                velocity[1] = 0.0f;
                velocity[2] = 0.0f;
                velocity[3] = 0.0f;
                velocities = new List<double> { 0.0, 0.0, 0.0, 0.0 };
                listOfJointVelocityCmdList.Add(velocities);
                string[] jointNamesArray = jointNames.ToArray();
                double[] positionsArray = positions.ToArray();
                double[] velocitiesArray = velocities.ToArray();
                double[] effortsArray = efforts.ToArray();

                JointCmdMsg JointCMD = new JointCmdMsg(
                    jointNamesArray,
                //    controltype,
                    positionsArray,
                    velocitiesArray,
                    effortsArray
                );
                ros.Publish(JointControlTopic, JointCMD);
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
            if (OnOffSw == ONOFF.On)
            {
                if (sw_timeElapsed >= publishMessageInterval * 50.0f)
                {
                }

                else if (VRManager.PlayerPoseMove_SW > 0 || OnOffSw == ONOFF.On)
                {
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
                    if (JointContorollerMode == JointContorollerModeOption.Position)
                    {   
                        if (key == true)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                float input = GetAxis(plusKeys[i], minusKeys[i]);

                                goalPose[i] += ClampVelocityByLimit((float)Jointposition[i], input, minLimit[i], maxLimit[i], posSpeed[i]);
                            }
                        }
                        
                        //key
                        if (key == true)
                        {
                            rotation = 0.0f;
                            rotation = GetAxis(KeyCode.LeftArrow, KeyCode.RightArrow);
                            frontback = 0.0f;
                            frontback = GetAxis(KeyCode.UpArrow, KeyCode.DownArrow);
                            linear.x = frontback;
                            angular.x = rotation;
                        }
                        //
                        //for joint
                        Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                        Vector2 stickR = movespeed * OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);

                        //swing
                        goalPose[0] += ClampVelocityByLimit((float)Jointposition[0], stickL.x, -999f, 999f, -0.3f);
                        //boom
                        goalPose[1] += ClampVelocityByLimit((float)Jointposition[1], stickR.y, -0.872f, 0.1749594f, 0.30f);
                        //arm
                        goalPose[2] += ClampVelocityByLimit((float)Jointposition[2], stickL.y, 0.959f, 2.35f, 1.00f);
                        //bucket
                        goalPose[3] += ClampVelocityByLimit((float)Jointposition[3], stickR.x, -1.2211f, 1.39555f, -1.5f);
                    }
                    ////////////////////////////////////////////////////////////
                    ////////////////////////////////////////////////////////////velosity
                    if (JointContorollerMode == JointContorollerModeOption.Velocity)
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

                        //swing
                        velocity[0] = ClampVelocityByLimit((float)Jointposition[1], stickL.x, -999f, 999f, -0.5f);
                        //boom
                        velocity[1] = ClampVelocityByLimit((float)Jointposition[1], stickR.y, -0.872f, 0.1749594f, 0.30f);
                        //arm
                        velocity[2] = ClampVelocityByLimit((float)Jointposition[2], stickL.y, 0.959f, 2.35f, -0.5f);
                        //bucket
                        velocity[3] = ClampVelocityByLimit((float)Jointposition[3], stickR.x, -1.2211f, 1.39555f, -0.5f);

                        if (key == true)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                float input = GetAxis(plusKeys[i], minusKeys[i]);

                                velocity[i] = ClampVelocityByLimit((float)Jointposition[i], input,minLimit[i], maxLimit[i], velSpeed[i]);
                            }
                        }



                        //////////////////////////////////////////////////
                        //////////////////////////////////////////////////twist
                        RBack = OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger);
                        RFront = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);
                        LBack = OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger);
                        LFront = OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger);

                        if (RFront >= 0.5 && LFront >= 0.5) { linear.x = (RFront + LFront) / 2; }
                        else if (RFront < 0.5 && LFront >= 0.5)
                        {
                            //(LFront - RFront)/2
                            angular.z = 0.3;
                        }
                        else if (RFront >= 0.5 && LFront < 0.5) { angular.z = -0.3; }
                        else
                        {
                            linear.x = 0;
                            angular.x = 0;
                        }
                        //key
                        if (key == true)
                        {
                            if (Input.GetKey(KeyCode.LeftArrow)) { rotation = rotspeed; }
                            if (Input.GetKeyUp(KeyCode.LeftArrow)) { rotation = 0; }
                            if (Input.GetKey(KeyCode.RightArrow)) { rotation = -rotspeed; }
                            if (Input.GetKeyUp(KeyCode.RightArrow)) { rotation = 0; }
                            if (Input.GetKey(KeyCode.UpArrow)) { frontback = linearspeed; }
                            if (Input.GetKeyUp(KeyCode.UpArrow)) { frontback = 0; }
                            if (Input.GetKey(KeyCode.DownArrow)) { frontback = -linearspeed; }
                            if (Input.GetKeyUp(KeyCode.DownArrow)) { frontback = 0; }
                            linear.x = frontback;
                            angular.x = rotation;
                        }

                        timeElapsed += Time.deltaTime;
                        sw_timeElapsed += Time.deltaTime;
                        JointController = GetComponent<JointControler>();

                        if (timeElapsed > publishMessageInterval * 20.0f)
                        {
                            if (selected_mode.mode == 2)
                            {
                                if (JointContorollerMode == JointContorollerModeOption.Position)
                                {
                                    JointController.JointTargets[0] = goalPose[0];
                                    JointController.JointTargets[1] = goalPose[1];
                                    JointController.JointTargets[2] = goalPose[2];
                                    JointController.JointTargets[3] = goalPose[3];
                                }
                                if (JointContorollerMode == JointContorollerModeOption.Velocity)
                                {
                                    JointController.JointTargets[0] = velocity[0];
                                    JointController.JointTargets[1] = velocity[1];
                                    JointController.JointTargets[2] = velocity[2];
                                    JointController.JointTargets[3] = velocity[3];
                                }
                            }

                            Debug.Log("goal_pose : " + goalPose[0] + "  :  " + goalPose[1] + "  :  " + goalPose[2] + "  :  " + goalPose[3]);
                            positions[0] = goalPose[0];
                            positions[1] = goalPose[1];
                            positions[2] = goalPose[2];
                            positions[3] = goalPose[3];
                            listOfJointPositionCmdList.Add(positions);
                            if (selected_mode.mode == 2 && timeElapsed_start > (Time_Delay + 5.0f) && listOfJointPositionCmdList.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) >= 0)
                            {
                                int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                                goalPose[0] = (float)listOfJointPositionCmdList[listOfJointPositionCmdList.Count - 1 - CMD_time][0];
                                goalPose[1] = (float)listOfJointPositionCmdList[listOfJointPositionCmdList.Count - 1 - CMD_time][1];
                                goalPose[2] = (float)listOfJointPositionCmdList[listOfJointPositionCmdList.Count - 1 - CMD_time][2];
                                goalPose[3] = (float)listOfJointPositionCmdList[listOfJointPositionCmdList.Count - 1 - CMD_time][3];
                            }

                            Debug.Log("velocity : " + velocity[0] + "  :  " + velocity[1] + "  :  " + velocity[2] + "  :  " + velocity[3]);
                            velocities[0] = velocity[0];
                            velocities[1] = velocity[1];
                            velocities[2] = velocity[2];
                            velocities[3] = velocity[3];
                            listOfJointVelocityCmdList.Add(velocities);
                            for (int i = 0; i < joints.Count; i++)
                            {
                                JointPositions[i] = joints[i].jointPosition[0];
                            }
                            listOfJointPositionList.Add(JointPositions);
                            if (selected_mode.mode == 2 && timeElapsed_start > (Time_Delay + 5.0f) && listOfJointVelocityCmdList.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) >= 0)
                            {
                                int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                                velocities[0] = listOfJointVelocityCmdList[listOfJointVelocityCmdList.Count - 1 - CMD_time][0];
                                velocities[1] = listOfJointVelocityCmdList[listOfJointVelocityCmdList.Count - 1 - CMD_time][1];
                                velocities[2] = listOfJointVelocityCmdList[listOfJointVelocityCmdList.Count - 1 - CMD_time][2];
                                velocities[3] = listOfJointVelocityCmdList[listOfJointVelocityCmdList.Count - 1 - CMD_time][3];
                            }
                            string[] jointNamesArray = jointNames.ToArray();
                            double[] positionsArray = positions.ToArray();
                            double[] velocitiesArray = velocities.ToArray();
                            double[] effortsArray = efforts.ToArray();

                            JointCmdMsg JointCMD = new JointCmdMsg(
                                jointNamesArray,
                                //     controltype,
                                positionsArray,
                                velocitiesArray,
                                effortsArray
                            );
                            ros.Publish(JointControlTopic, JointCMD);
                            timeElapsed = 0;


                            //twist
                            TwistMsg Twist = new TwistMsg(
                               linear,
                               angular
                            );
                            //
                            //Publish
                            ros.Publish(topicName_cmd_vel, Twist);
                            timeElapsed = 0;
                        }
                        ///for prev
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
                        }
                    }
                }
            }
        }
    }

    private float ClampVelocityByLimit(float position, float input, float min, float max, float speed)
    {
        if (Mathf.Abs(input) <= 0.3f)
            return 0f;

        float velocity = speed * input;

        bool movable =
            (position > min && position < max) ||
            (position <= min && velocity > 0) ||
            (position >= max && velocity < 0);

        return movable ? velocity : 0f;
    }

    private float GetAxis(KeyCode positive, KeyCode negative)
    {
        if (Input.GetKey(positive)) return 1f;
        if (Input.GetKey(negative)) return -1f;
        return 0f;
    }

}