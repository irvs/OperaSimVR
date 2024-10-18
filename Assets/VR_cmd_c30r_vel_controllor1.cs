using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
public class vrcmdc30rvelcontroller : MonoBehaviour
{
    public ControllerLay laiser;
    int cmd_operation = 0;

    int publishersw = 0;
    int zerocounter = 0;
    float movespeed = 5.0f;
    public float linearspeed = 5.0f;
    public float rotspeed = 20.0f;
    //
    private double previousTime = 0.0;
    private float timeElapsed;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.02f;//50Hz

    //Oculus Touch Information
    // public OVRInput.Controller controlL;
    // public OVRInput.Controller controlR;
    ROSConnection ros;
    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    // Start is called before the first frame update
    void Start()
    {
        //
        laiser = FindObjectOfType<ControllerLay>();
        if (laiser != null)
        {
            Debug.Log("Player's health is: " + laiser.num);
        }
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>("c30r/tracks/cmd_vel");
        //cube = GetComponent<Rigidbody>();
    }
    // Update is called once per frame
    void Update()
    {
        //
        if (laiser != null && laiser.geton_c30r == 1)
        {
           // Debug.Log("get: " + laiser.conum_zx200);
            if (laiser.conum_zx200 == 0)
            {
                cmd_operation = 0;
                
            }
            else if (laiser.conum_zx200 > 0)
            {
                cmd_operation = 1;
                //Debug.Log("geton");
                //}
                // }

                //
                timeElapsed += Time.deltaTime;

                double time = Time.fixedTimeAsDouble;
                double deltaTime = time - previousTime;
                /*
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
                */
                //

                Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                linear.x = stickL.y;
                //
                //Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                angular.z = - stickL.x;
                Debug.Log("x" + linear.x + "z" + angular.z);

                /*
                if (birdseyeview == 1)
                {
                    //左ジョイスティックの情報取得
                    Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                    linear.x = stickL.x;
                }
                //
                //
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
                //Send untiy_odom to turtlebot_control
                TwistMsg Twist = new TwistMsg(
                       linear,
                       angular
                    );
                //
                //
                // Finally send the message to server_endpoint.py running in ROS
                if (zerocounter <= 20 && timeElapsed >= publishMessageInterval)
                {
                    Debug.Log("Publish");
                    ros.Publish("c30r/tracks/cmd_vel", Twist);
                    timeElapsed = 0.0f;
                }
                previousTime = time;
            }//
        }//
    }
}