using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using Unity.Robotics.Core;
using System;
using PID_Controller;

public class cont_crowlar : MonoBehaviour
{
    // <summary>
    // 
    // </summary>
    int publishersw = 0;
    int zerocounter = 0;
    public float linearspeed = 1.0f;
    public float rotspeed = 1.0f;
    //Oculus Touch Information
    // public OVRInput.Controller controlL;
    // public OVRInput.Controller controlR;
    ///ROSConnection ros;
    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    // Start is called before the first frame update
    // 
    // </summary>
    private ROSConnection ros;

    public List<GameObject> leftWheels;
    public List<GameObject> rightWheels;

    ///private OdometryMsg odomMessage;

    public string robotName = "robot_name";
    public string TwistTopicName = "robot_name/tracks/cmd_vel"; // Subscribe Messsage Topic Name
    public string CMDTopicName = "ic120/tracks/cmd_vel";
    ///public string OdomTopicName = "robot_name/odom"; // Publish Message Topic Name
    ///public string childFrameName = "robot_name/base_link";
    ///public double treadCollectionFactor = 2.0; // Factor Collecting Yaw angle of base_link. This Parameter is multiplied to tread to calculate angular velocity based on Vehicle's Kinematics.
    private List<WheelCollider> leftWheelColliders;
    private List<WheelCollider> rightWheelColliders;
    private WheelCollider leftMiddleWheel;
    private WheelCollider rightMiddleWheel;

    public double pGain = 100.0;
    public double iGain = 0.0;
    public double dGain = 0.0;
    public double torqueLimit = 1000.0;//1000.0
    public float brakeTorque = 10000.0F;//10000.0F
    public double maxLinearVelocity = 3.00;  // unit is m/sec
    public double maxAngularVelocity = Math.PI * 2.0 * 5.0 / 360.0;  // unit is rad/sec
    private List<PID> leftWheelControllers;
    private List<PID> rightWheelControllers;

    private TwistMsg twist;
    private double tread_half = 2.0;
    private double previousTime = 0.0;
    private double previousTime_for_pub = 0.0;
    //private double yaw = 0.0;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.02f;//50Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;
    private float timeElapsed_for_pub;
    vrcmdvelcontroller controller_cmd;
    VR_cont_2 cont_VR_2_cmd;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(CMDTopicName);
        leftWheelColliders = new List<WheelCollider>();
        rightWheelColliders = new List<WheelCollider>();
        twist = new TwistMsg();

        leftWheelControllers = new List<PID>();
        rightWheelControllers = new List<PID>();

        ///odomMessage = new OdometryMsg();
        ///odomMessage.header = new HeaderMsg();
        ///odomMessage.header.stamp = new TimeMsg();

        /* Get ArticulationBody-type Components in Left Wheels and Set Parameters for xDrive in each Component */
        foreach (GameObject left in leftWheels)
        {
            var body = left.GetComponent<WheelCollider>();
            body.ConfigureVehicleSubsteps(5f, 100, 100);
            leftWheelColliders.Add(body);
            Debug.Log("Check left!");

            /* Get ArticulationBody-type Component named "left_middle_wheel_link" */
            if (left.name == "left_middle_wheel_link")
            {
                leftMiddleWheel = body;
            }
            leftWheelControllers.Add(new PID(pGain, iGain, dGain, 1, torqueLimit, -torqueLimit));
        }
        /* Get ArticulationBody-type Components in Right Wheels and Set Parameters for xDrive in each Component */
        foreach (GameObject right in rightWheels)
        {
            var body = right.GetComponent<WheelCollider>();
            body.ConfigureVehicleSubsteps(5f, 100, 100);
            rightWheelColliders.Add(body);
            Debug.Log("Check right!");

            /* Get ArticulationBody-type Component named "right_middle_wheel_link" */
            if (right.name == "right_middle_wheel_link")
            {
                rightMiddleWheel = body;
            }
            rightWheelControllers.Add(new PID(pGain, iGain, dGain, 1, torqueLimit, -torqueLimit));
        }
        tread_half = Mathf.Abs(leftWheels[0].transform.localPosition.x - rightWheels[0].transform.localPosition.x) / 2;

        Debug.Log("DiffDriveController starts!!");
       /// ros.Subscribe<TwistMsg>(TwistTopicName, ExecuteTwist); //Register Subscriber
       /// ros.RegisterPublisher<OdometryMsg>(OdomTopicName); //Register Publisher
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //const float speed = 100.0f;
        float leftVelCmd = 0.0f; // Velocity Command for Left Track
        float rightVelCmd = 0.0f; // Velocity Command for Right Track
        double leftVelMes = 0.0; // Measured Velocity from Left Track
        double rightVelMes = 0.0; // Measured Velocity from Right Track

        timeElapsed += Time.deltaTime;

        double time = Time.fixedTimeAsDouble;
        double deltaTime = time - previousTime;

        double leftTrackVel = 2.0 * Math.PI * leftMiddleWheel.rpm / 60.0; // Unit is [rad/s]
        double rightTrackVel = 2.0 * Math.PI * rightMiddleWheel.rpm / 60.0; // Unit is [rad/s]
        // Debug.Log("LeftTrackRPM:" + leftMiddleWheel.rpm);
        // Debug.Log("RightTrackRPM:" + rightMiddleWheel.rpm);
        // Debug.Log("LeftTrackVelocity:" + leftTrackVel);
        // Debug.Log("RightTrackVelocity:" + rightTrackVel);

        /* To Get Track's Radius use wheel collider parameter*/
        double leftTrackRadius = leftMiddleWheel.radius;
        double rightTrackRadius = rightMiddleWheel.radius;
        // Debug.Log("LeftTrackRadius:"+leftTrackRadius);
        // Debug.Log("RightTrackRadius:"+rightTrackRadius);

        /* velocity =  angular velocity[rad/s] * radius[m] */
        leftVelMes = leftTrackVel * leftTrackRadius; // Unit is [m/s]
        rightVelMes = rightTrackVel * rightTrackRadius; // Unit is [m/s]
        // Debug.Log("LeftJointVelocity:"+leftVelMes);
        // Debug.Log("RightJointVelocity:"+rightVelMes);

        ///double linearVel = 0.0;
        ///double angularVel = 0.0;

        /* Calculate linear and angular velocity based on kinematics */
        ///linearVel = (rightVelMes + leftVelMes) / 2.0;
        ///angularVel = (rightVelMes - leftVelMes) / (2.0 * tread_half * treadCollectionFactor);
        // Debug.Log("LinearVelocity:"+linearVel);
        // Debug.Log("AngularVelocity:"+angularVel);
        // Debug.Log("tread_half:"+tread_half);
        // Debug.Log("deltaTime:"+deltaTime);

        ///yaw += angularVel * deltaTime;
        /* Normalize Yaw [batween -PI and +PI] */
        ///if (Mathf.Abs((float)yaw) > Mathf.PI)
        ///{
        ///    yaw -= (double)(2 * Mathf.PI * Mathf.Sign((float)yaw));
        ///}

        ///odomMessage.pose.pose.position.x += linearVel * (double)Mathf.Cos((float)yaw) * deltaTime;
        ///odomMessage.pose.pose.position.y += linearVel * (double)Mathf.Sin((float)yaw) * deltaTime;

        // Debug.Log("x:"+odomMessage.pose.pose.position.x);
        // Debug.Log("y:"+odomMessage.pose.pose.position.y);
        // Debug.Log("yaw:"+yaw);

        /// Quaternion rotation = Quaternion.Euler(0, 0, (float)(yaw * 180.0 / (double)Mathf.PI));

        ///odomMessage.pose.pose.orientation.w = rotation.w;
        ///odomMessage.pose.pose.orientation.x = rotation.x;
        ///odomMessage.pose.pose.orientation.y = rotation.y;
        ///odomMessage.pose.pose.orientation.z = rotation.z;

        ///odomMessage.pose.covariance = new double[] { 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000.0 };

        ///odomMessage.twist.twist.linear.x = linearVel;
        ///odomMessage.twist.twist.linear.y = 0.0;
        ///odomMessage.twist.twist.linear.z = 0.0;

        ///   odomMessage.twist.twist.angular.x = 0.0;
        ///   odomMessage.twist.twist.angular.y = 0.0;
        ///   odomMessage.twist.twist.angular.z = angularVel;

        ///   odomMessage.twist.covariance = new double[] { 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000.0 };

        /// if (timeElapsed >= publishMessageInterval)
        /// {
        ///odomMessage.header.frame_id = robotName + "_tf/odom";
        /// odomMessage.header.stamp = new TimeStamp(Clock.time);
        /// odomMessage.child_frame_id = childFrameName;

        /// ros.Publish(OdomTopicName, odomMessage);
        ///  timeElapsed = 0.0f;
        ///}

        /* Calculate velocity command value based on inverse kinematics */
        //
        controller_cmd = FindObjectOfType<vrcmdvelcontroller>();
        cont_VR_2_cmd = FindObjectOfType<VR_cont_2>();
        VR_cont_2 VRdirective = GetComponent<VR_cont_2>();
        //
        ///var cmdLinearVel = twist.linear.x;
        //var cmdLinearVel = linear.x;
        //var cmdLinearVel = (double)(controller_cmd.CMD_linear_list[controller_cmd.CMD_linear_list.Count - 1]* linearspeed);
        //var cmdLinearVel = (double)(cont_VR_2_cmd.CMD_linear_list_for_cyber[cont_VR_2_cmd.CMD_linear_list_for_cyber.Count - 1] * linearspeed);
        var cmdLinearVel = (double)(VRdirective.CMD_linear_list_for_cyber[VRdirective.CMD_linear_list_for_cyber.Count - 1] * linearspeed);
        //Debug.Log("contcrawler"+ cmdLinearVel);
        //

        //
        cmdLinearVel = Math.Min(cmdLinearVel, maxLinearVelocity);
        cmdLinearVel = Math.Max(cmdLinearVel, -maxLinearVelocity);
        ///var cmdAngularVel = twist.angular.z;
        //var cmdAngularVel = angular.z;
        //var cmdAngularVel = (double)(cont_VR_2_cmd.CMD_anglar_list_for_cyber[cont_VR_2_cmd.CMD_anglar_list_for_cyber.Count - 1] * rotspeed);
        var cmdAngularVel = (double)(VRdirective.CMD_anglar_list_for_cyber[VRdirective.CMD_anglar_list_for_cyber.Count - 1] * rotspeed);
        cmdAngularVel = Math.Min(cmdAngularVel, maxAngularVelocity);
        cmdAngularVel = Math.Max(cmdAngularVel, -maxAngularVelocity);
        leftVelCmd = (float)(cmdLinearVel - tread_half * cmdAngularVel); // Unit is [m/s]
        rightVelCmd = (float)(cmdLinearVel + tread_half * cmdAngularVel); // Unit is [m/s]
        // Debug.Log("LeftJointVelocityCommand:" + leftVelCmd);
        // Debug.Log("RightJointVelocityCommand:" + rightVelCmd);

        /* Set targetVelocity in xDrive in wheels */
        var ts = TimeSpan.FromSeconds(deltaTime);
        for (var i = 0; i < leftWheelColliders.Count; i++)
        {
            var left = leftWheelColliders[i];
            var pid = leftWheelControllers[i];
            var v = (float)pid.PID_iterate(leftVelCmd, leftVelMes, ts);
            if (Math.Abs(leftVelCmd) < 0.001)
            {
                left.brakeTorque = brakeTorque;
                left.motorTorque = 0.0F;
            }
            else
            {
                left.brakeTorque = 0.0F;
                left.motorTorque = v;
            }
            //Debug.Log("LeftJointVelocityPID:" + v);
        }
        for (var i = 0; i < rightWheelColliders.Count; i++)
        {
            var right = rightWheelColliders[i];
            var pid = rightWheelControllers[i];
            var v = (float)pid.PID_iterate(rightVelCmd, rightVelMes, ts);
            if (Math.Abs(rightVelCmd) < 0.001)
            {
                right.brakeTorque = brakeTorque;
                right.motorTorque = 0.0F;
            }
            else
            {
                right.brakeTorque = 0.0F;
                right.motorTorque = v;
            }
            //Debug.Log("RightJointVelocityPID:" + v);
        }

        //Debug.Log("LeftJointVelocityDiff:" + (leftVelCmd - leftTrackVel));
        //Debug.Log("RightJointVelocityDiff:" + (rightVelCmd - rightTrackVel));

        previousTime = time;
    }

    ///void ExecuteTwist(TwistMsg msg)
    ///{
    ///    twist = msg;
        //Debug.Log("Linear Velocity:"+twist.linear.x);
        //Debug.Log("Angular Velocity:"+twist.angular.z);
    ///}
    void Update()
    {
        timeElapsed_for_pub += Time.deltaTime;

        double time_for_pub = Time.fixedTimeAsDouble;
        double deltaTime_for_pub = time_for_pub - previousTime_for_pub;
        //初期化
        if (Input.GetKey(KeyCode.Space))
        {
            // OVRManager.display.RecenterPose();
            linear.x = 0;
            angular.z = 0;
        }
        //
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            angular.z = rotspeed;
            //angular.z = angular.z + 0.1;
            // Debug.Log("左アナログスティックを上に倒した");
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            angular.z = 0;
            //angular.z = angular.z + 0.1;
            // Debug.Log("左アナログスティックを上に倒した");
        }
        // 右に移動
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            angular.z = -rotspeed;
            //angular.z = angular.z + (-0.1);
            // Debug.Log("右アナログスティックを上に倒した");
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            angular.z = 0;
            //angular.z = angular.z + (-0.1);
            // Debug.Log("右アナログスティックを上に倒した");
        }
        // 前に移動
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            linear.x = linearspeed;
            //linear.x = linear.x + 0.1;
        }

        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            linear.x = 0;
            //linear.x = linear.x + 0.1;
        }
        // 後ろに移動
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            linear.x = -linearspeed;
            //linear.x = linear.x + (-0.1);
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            linear.x = 0;
            //linear.x = linear.x + (-0.1);
        }
        //
        /*
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickUp))
        {
            Debug.Log("左アナログスティックを上に倒した");
            linear.x = linear.x + 0.1;
        }
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickDown))
        {
            Debug.Log("左アナログスティックを下に倒した");
            linear.x = linear.x + (-0.1);
        }
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickLeft))
        {
            Debug.Log("左アナログスティックを左に倒した");
            angular.z = angular.z + 0.1;
        }
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickRight))
        {
            Debug.Log("左アナログスティックを右に倒した");
            angular.z = angular.z + (-0.1);
        }*/
        //print(linear);
        //
        if (linear.x == 0 && angular.z == 0)
        {
            zerocounter += 1;
        }
        if ((zerocounter != 0 && linear.x != 0) | (zerocounter != 0 && angular.z != 0))
        {
            zerocounter = 0;
        }
        //
        if (publishersw == 1)
        {
            //Send untiy_odom to turtlebot_control
            TwistMsg Twist = new TwistMsg(
                   linear,
                   angular
                );
            //

            //
            // Finally send the message to server_endpoint.py running in ROS
            if (zerocounter <= 20 && timeElapsed_for_pub >= publishMessageInterval)
            {
                Debug.Log("Publish");
                ros.Publish(CMDTopicName, Twist);
                timeElapsed_for_pub = 0.0f;
            }
            previousTime_for_pub = time_for_pub;

        }
    }
}
