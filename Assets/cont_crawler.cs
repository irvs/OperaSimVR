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
    public float linearspeed = 1.0f;
    public float rotspeed = 1.0f;


    public List<GameObject> leftWheels;
    public List<GameObject> rightWheels;

    ///private OdometryMsg odomMessage;

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

    private double tread_half = 2.0;
    private double previousTime = 0.0;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.02f;//50Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;
    private float timeElapsed_for_pub;
    //  vrcmdvelcontroller controller_cmd;
    VRCrawlerOp cont_VR_2_cmd;
    Vector3 crowlarvelocity;
    float crowlarspeed;
    private Rigidbody rb;
    Recorder CMD_Recorder;//cmd record play
    private long PlayDeltaTime;//cmd record play
    public int RecordCounter = 0;//cmd record play
    private double cmdLinearVelForPlay;//cmd record play
    private double cmdAngularVelForPlay;//cmd record play

    // Start is called before the first frame update
    void Start()
    {
        leftWheelColliders = new List<WheelCollider>();
        rightWheelColliders = new List<WheelCollider>();

        leftWheelControllers = new List<PID>();
        rightWheelControllers = new List<PID>();

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

        rb = GetComponent<Rigidbody>();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        VRCrawlerOp VRdirective = GetComponent<VRCrawlerOp>();
        if (VRdirective.RecordPlaySw == false)
        {
            /* Calculate velocity command value based on inverse kinematics */
            //   controller_cmd = FindObjectOfType<vrcmdvelcontroller>();
            cont_VR_2_cmd = GetComponent<VRCrawlerOp>();
            Recorder CMD_Recorder = GetComponent<Recorder>();
            ///var cmdLinearVel = twist.linear.x;
            var cmdLinearVel = (double)(VRdirective.CMD_linear_list_for_cyber[VRdirective.CMD_linear_list_for_cyber.Count - 1] * linearspeed);
            var cmdAngularVel = (double)(VRdirective.CMD_anglar_list_for_cyber[VRdirective.CMD_anglar_list_for_cyber.Count - 1] * rotspeed);

            ///
            /*
            if (cmdLinearVelForPlay > 0.1 && crowlarspeed < 1.0)
            {

                cmdLinearVelForPlay = 3.0f;
            }
            else if (cmdLinearVelForPlay < -0.1 && crowlarspeed < 1.0)
            {
                cmdLinearVelForPlay = -3.0f;
            }
            */

            CrawlerOperater(cmdLinearVel, cmdAngularVel);

            
        }
    }

    
    void Update()
    {
        // Rigidbody�̑��x���擾
        crowlarvelocity = rb.velocity;

        // �I�u�W�F�N�g�̑��x��\��
        crowlarspeed = crowlarvelocity.magnitude;
        //Debug.Log("Speed: " + crowlarspeed);


        VRCrawlerOp VRdirective = GetComponent<VRCrawlerOp>();
        if (VRdirective.RecordPlaySw == true)
        {
            /* Calculate velocity command value based on inverse kinematics */
            cont_VR_2_cmd = GetComponent<VRCrawlerOp>();
            Recorder CMD_Recorder = GetComponent<Recorder>();
            //
            DateTime currentTime = DateTime.Now;
            long timestamp = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

            if (RecordCounter == 0)
            {
                PlayDeltaTime = timestamp - CMD_Recorder.TimeStampList[0];
            }
            if (timestamp - CMD_Recorder.TimeStampList[RecordCounter] >= PlayDeltaTime && RecordCounter < (CMD_Recorder.RecordList).Count - 1)
            {
                RecordCounter += 1;
                cmdLinearVelForPlay = (double)(CMD_Recorder.RecordList[RecordCounter][1] * VRdirective.adapter1 + VRdirective.adapter2) * linearspeed;
                cmdAngularVelForPlay = (double)(CMD_Recorder.RecordList[RecordCounter][2] + VRdirective.rotadapter) * rotspeed;
                if (Math.Abs(CMD_Recorder.RecordList[RecordCounter][1] ) <= 0.001)
                {
                    cmdLinearVelForPlay = 0.0f;
                    cmdAngularVelForPlay = 0.0f;
                }
                if (Math.Abs(CMD_Recorder.RecordList[RecordCounter][2]) <= 0.0000000000000000001) 
                {
                  //  cmdAngularVelForPlay = 0.0f;
                }
            }
            if (RecordCounter == (CMD_Recorder.RecordList).Count - 1)
            {
                //  RecordCounter = 0;
            }
            Debug.Log("PublishingLinear" + cmdLinearVelForPlay + " : " + VRdirective.adapter1 + " : " + VRdirective.adapter2 + " : " + VRdirective.rotadapter);
            Debug.Log("PublishingAngular" + cmdAngularVelForPlay);

            CrawlerOperater(cmdLinearVelForPlay, cmdAngularVelForPlay);


        }

    }

    void CrawlerOperater(double cmdLinearDirective, double cmdAngularDirective)
    {
        //const float speed = 100.0f;
        float leftVelCmd = 0.0f; // Velocity Command for Left Track
        float rightVelCmd = 0.0f; // Velocity Command for Right Track
        double leftVelMes = 0.0; // Measured Velocity from Left Track
        double rightVelMes = 0.0; // Measured Velocity from Right Track

        double time = Time.fixedTimeAsDouble;
        double deltaTime = time - previousTime;

        double leftTrackVel = 2.0 * Math.PI * leftMiddleWheel.rpm / 60.0; // Unit is [rad/s]
        double rightTrackVel = 2.0 * Math.PI * rightMiddleWheel.rpm / 60.0; // Unit is [rad/s]

        /* To Get Track's Radius use wheel collider parameter*/
        double leftTrackRadius = leftMiddleWheel.radius;
        double rightTrackRadius = rightMiddleWheel.radius;

        /* velocity =  angular velocity[rad/s] * radius[m] */
        leftVelMes = leftTrackVel * leftTrackRadius; // Unit is [m/s]
        rightVelMes = rightTrackVel * rightTrackRadius; // Unit is [m/s]


        cmdLinearDirective = Math.Min(cmdLinearDirective, maxLinearVelocity);
        cmdLinearDirective = Math.Max(cmdLinearDirective, -maxLinearVelocity);

        cmdAngularDirective = Math.Min(cmdAngularDirective, maxAngularVelocity);
        cmdAngularDirective = Math.Max(cmdAngularDirective, -maxAngularVelocity);
        leftVelCmd = (float) (cmdLinearDirective - tread_half* cmdAngularDirective); // Unit is [m/s]
        rightVelCmd = (float) (cmdLinearDirective + tread_half* cmdAngularDirective); // Unit is [m/s]


        /* Set targetVelocity in xDrive in wheels */
        var ts = TimeSpan.FromSeconds(deltaTime);
        for (var i = 0; i<leftWheelColliders.Count; i++)
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
}

