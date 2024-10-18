using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using System;
using RosMessageTypes.Geometry;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Drawing;

public class JointAnglePublisher : MonoBehaviour
{
    public ControllerLay laiser;
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
        laiser = FindObjectOfType<ControllerLay>();
        if (laiser != null)
        {
            Debug.Log("Player's health is: " + laiser.num);
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
        OVRPlayerController scriptA = targetObject.GetComponent<OVRPlayerController>();
        if (laiser != null && laiser.geton_zx200 == 1)
        {
             
            //Debug.Log("get: " + laiser.conum_zx200);
           // Debug.Log("laiser: " + laiser.conum_zx200 + " : " + laiser.geton_zx200);
            if (laiser.conum_zx200 == 0)
            {
                cmd_operation = 0;

            }
            else if (laiser.conum_zx200 > 0)
            {
                cmd_operation = 1;
                //Debug.Log("geton_zx200");
                //}
                // }
                //

                //GameObject.Find("OVRPlayerController").transform.rotation = (GameObject.Find(parentObjectName + "_cam").transform.rotation);
                
                if (scriptA != null)
                {
                    //Debug.Log("kaitennha" + scriptA.RotationRatchet);
                    scriptA.RotationRatchet = 0;
                    scriptA.RotationAmount = 0;
                    //Debug.Log("kaitennha" + scriptA.RotationRatchet);
                }
                //

                /*
                //
                if (Input.GetKey(KeyCode.Y))
                {
                    //Debug.Log("get.H");
                    goalpose_swing += 0.005f;
                }
                if (Input.GetKey(KeyCode.H))
                {
                    // Debug.Log("get.G");
                    goalpose_swing -= 0.005f;
                }
                if (Input.GetKey(KeyCode.U) && goalpose_boom <= 0.9594)
                {
                    //Debug.Log("get.H");
                    goalpose_boom += 0.005f;
                }
                if (Input.GetKey(KeyCode.J) && goalpose_boom >= -1.2211)
                {
                    // Debug.Log("get.G");
                    goalpose_boom -= 0.005f;
                }
                if (Input.GetKey(KeyCode.I) && goalpose_arm <= 2.5294)
                {
                    //Debug.Log("get.H");
                    goalpose_arm += 0.005f;
                }
                if (Input.GetKey(KeyCode.K) && goalpose_arm >= 0.785)
                {
                    // Debug.Log("get.G");
                    goalpose_arm -= 0.005f;
                }
                if (Input.GetKey(KeyCode.O) && goalpose_bucket <= 1.39555)
                {
                    //Debug.Log("get.H");
                    goalpose_bucket += 0.005f;
                }
                if (Input.GetKey(KeyCode.L) && goalpose_bucket >= -1.2211)
                {
                    // Debug.Log("get.G");
                    goalpose_bucket -= 0.005f;
                }
                */
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
                    angular.z =0.8;
                }
                else if (RFront >= 0.5 && LFront < 0.5)
                {
                    angular.z = -0.8;
                }


                //
                //twist
                Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                Vector2 stickR = movespeed * OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
                if (Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).y) >0.3)
                {
                    goalpose_arm += -stickL.y;
                }
                if (Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.LThumbstick)).x) > 0.3)
                {
                    goalpose_swing += -stickL.x;
                }
                if (Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).y) > 0.3)
                {
                    goalpose_boom += 0.1f * stickR.y;
                }
                if (Mathf.Abs((OVRInput.Get(OVRInput.RawAxis2D.RThumbstick)).x) > 0.3)
                {
                    goalpose_bucket += -stickR.x;
                }
                Debug.Log("swingtest" + goalpose_swing + ":" + -stickL.x);
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
                    }
                    
                    timeElapsed = 0;
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