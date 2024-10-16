using UnityEngine;
using System.Collections;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
public class vrcmdvelcontroller : MonoBehaviour
{
    public int control_mode = 0;
    public float Time_Delay = 5.0f;
    List<float> CMD_linear_list = new List<float>();
    List<float> CMD_anglar_list = new List<float>();
    List<Vector3> posi_list = new List<Vector3>();
    List<Vector3> rotation_list = new List<Vector3>();
    private Vector3 last_pose;
    private Vector3 last_rotation;
    private int last_time;
    private Vector3 diff_pose;
    private Vector3 diff_rot;
    private Quaternion rotation_for_list;
    public GameObject targetObject;
    //
    public float adopt_time = 1.0f;
    //
    public int key = 1;
    //
    public ParentObjectName laiser;
    int cmd_operation = 0;

    int publishersw = 0;
    int zerocounter = 0;
    float movespeed = 5.0f;
    public float linearspeed = 5.0f;
    public float rotspeed = 20.0f;
    //
    private double previousTime = 0.0;
    private double previousTime_adopt = 0.0;
    private float timeElapsed;
    private float timeElapsed_CMD;
    private float timeElapsed_Pose;
    private float timeElapsed_adopt;
    private float timeElapsed_adopt_starter = 0.0f;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.02f;//50Hz

    //Oculus Touch Information
    // public OVRInput.Controller controlL;
    // public OVRInput.Controller controlR;
    ROSConnection ros;
    private PoseStampedMsg twist;
    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    // Start is called before the first frame update
    void Start()
    {
        //
        laiser = FindObjectOfType<ParentObjectName>();
        if (laiser != null)
        {
            Debug.Log("Player's health is: " + laiser.num);
        }
        // start the ROS connection
        ros = ROSConnection.instance;
        ros.RegisterPublisher<TwistMsg>("ic120/tracks/cmd_vel");
        //cube = GetComponent<Rigidbody>();
        //
        //Debug.Log("aaaaaaaa");
        twist = new PoseStampedMsg();
        Debug.Log("check:baselink/pose");
        // ROSコネクションへのサブスクライバーの登録
        //ROSConnection.instance.Subscribe<TwistMsg>("/ic120/tracks/cmd_vel", Callback);
        ROSConnection.instance.Subscribe<PoseStampedMsg>("/ic120/base_link/pose", Callback);
        Debug.Log("already:baselink/pose");
    }
    // Update is called once per frame
    void Update()
    {
        //
        if (laiser != null && laiser.geton_ic120 == 1)
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
                timeElapsed_Pose += Time.deltaTime;
                timeElapsed_CMD += Time.deltaTime;
                timeElapsed_adopt_starter += Time.deltaTime;

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
                //
                linear.x = stickL.y;
                //Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                angular.z = -stickL.x;
                Debug.Log("x" + linear.x + "z" + angular.z);

                if (key == 1)
                {
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
                }

                //print(linear);
                //
                //
                //
                if (control_mode == 0)
                {

                    if (linear.x == 0 && angular.z == 0)
                    {
                        zerocounter += 1;
                    }
                    if ((zerocounter != 0 && linear.x != 0) | (zerocounter != 0 && angular.z != 0))
                    {
                        zerocounter = 0;
                    }
                }
                if (control_mode == 1)
                {
                    if (timeElapsed_CMD >= publishMessageInterval)
                    {
                        CMD_linear_list.Add(stickL.y);
                        CMD_anglar_list.Add(-stickL.x);

                    }
                    if (timeElapsed_Pose >= adopt_time)
                    {


                    }
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
                    ros.Send("ic120/tracks/cmd_vel", Twist);
                    timeElapsed = 0.0f;
                }
                previousTime = time;
            }//
        }//
    }

    void Callback(PoseStampedMsg msg)
    {

        timeElapsed_adopt += Time.deltaTime;
        //double time_adopt = Time.fixedTimeAsDouble;
        if (timeElapsed_adopt >= adopt_time)
        {
            posi_list.Add(GameObject.Find("ic120").transform.position);
            rotation_for_list = GameObject.Find("ic120").transform.rotation;
            rotation_list.Add(rotation_for_list.eulerAngles);


            Vector3 newPosition = new Vector3((float)msg.pose.position.y * (-1), (float)msg.pose.position.z, (float)msg.pose.position.x);
            Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
            Vector3 NewRotation = newRotation.eulerAngles;

            if (timeElapsed_adopt_starter >= Time_Delay) 
            {
                last_time = Mathf.RoundToInt(Time_Delay / adopt_time);
                last_pose = posi_list[posi_list.Count - last_time];
                last_rotation = rotation_list[rotation_list.Count - last_time];
                diff_pose = last_pose - newPosition;
                diff_rot = last_rotation - NewRotation;
                GameObject.Find("ic120").transform.position += diff_pose;
                GameObject.Find("ic120").transform.rotation = Quaternion.Euler(diff_rot + rotation_for_list.eulerAngles);
            }
            



            //double deltaTime_adopt = time_adopt - previousTime_adopt;
            timeElapsed_adopt = 0.0f;
        }
        //previousTime_adopt = time_adopt;
    }
}