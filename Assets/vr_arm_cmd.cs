using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using System;
using RosMessageTypes.Geometry;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

public class JointAnglePublisher : MonoBehaviour
{
    Controller_manager VRManager;
    mood_selector selected_mode;
    public int sw = 0;
    public bool emergency;
    public int key = 0;
    public float linearspeed = 1.00f;
    public float rotspeed = 0.50f;
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
    private float frontback;
    private float rotation;
    float goalpose_swing = 0.0f;
    float goalpose_boom = -0.7f;
    float goalpose_arm = 1.57f;
    float goalpose_bucket = 0.785f;

    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    int zerocounter = 0;
    float RFront = 0.0f;
    float RBack = 0.0f;
    float LFront = 0.0f;
    float LBack = 0.0f;

    //
    public GameObject targetObject;

    void Start()
    {
        VRManager = FindObjectOfType<Controller_manager>();
        if (VRManager != null)
        {
            Debug.Log("Player's health is: " + VRManager.num);
        }
        //
        
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(topicName_cmd_vel);
        ros.RegisterPublisher<Float64Msg>(topicName_swing);
        ros.RegisterPublisher<Float64Msg>(topicName_boom);
        ros.RegisterPublisher<Float64Msg>(topicName_arm);
        ros.RegisterPublisher<Float64Msg>(topicName_bucket);

        

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
                // Debug.Log("Publish After Delay Time");
               // ros.Publish(topicName_swing, 0.0f);
               // ros.Publish(topicName_boom, 0.0f);
               // ros.Publish(topicName_arm, 0.0f);
               // ros.Publish(topicName_bucket, 0.0f);
                ros.Publish(topicName_cmd_vel, Twist);
                timeElapsed = 0.0f;
            }

        }

        else if (emergency == false)
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
                        if (Input.GetKey(KeyCode.DownArrow) )
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
                    //Debug.Log("swingtest" + goalpose_swing + ":" + -stickL.x);
                    //
                    //
                    //linear.x = stickL.y;
                    //angular.z = -stickL.x;
                    //Debug.Log("x" + linear.x + "z" + angular.z);

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
                        ros.Publish(topicName_swing, angleMessage_swing);
                        ros.Publish(topicName_boom, angleMessage_boom);
                        ros.Publish(topicName_arm, angleMessage_arm);
                        ros.Publish(topicName_bucket, angleMessage_bucket);
                        if (zerocounter <= 20)
                        {
                            ros.Publish(topicName_cmd_vel, Twist);
                            //Debug.Log("linear " + linear);
                        }

                        timeElapsed = 0;
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
}