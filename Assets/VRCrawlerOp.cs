using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;
using System.Drawing.Printing;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

public class VR_cont_2 : MonoBehaviour
{
    public int sw = 0;
    private int prev_sw = 0;
    public bool RecordPlaySw;//cmd record play
    public int control_mode = 0;
    public bool emergency;
    public int SimORReal;
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    public float rot_offset_x = 0;
    public float rot_offset_y = 0;
    public float rot_offset_z = 0;
    public string SimPhysXPublishTopicName;
    public string SimAGXPublishTopicName;
    public string RealPublishTopicName;
    private string SRPublishTopicName;
    public string SimPhysXSubscribeTopicName;
    public string SimAGXSubscribeTopicName;
    public string RealSubscribeTopicName;
    private string SRSubscribeTopicName;
    public string controller_swTopicName = "controller_sw";
    private string controller_sw_return_TopicName;
    public string EmergencyTopicName;
    public float Time_Delay = 5.0f;
    public List<float> CMD_linear_list_for_cyber = new List<float>();
    public List<float> CMD_linear_list = new List<float>();
    public List<float> CMD_anglar_list_for_cyber = new List<float>();
    public List<float> CMD_anglar_list = new List<float>();
    List<string> CMD_time_list = new List<string>();
    List<Vector3> posi_list = new List<Vector3>();
    List<float> posi_list_z = new List<float>();/////////////////////////////////////
    List<Vector3> rotation_list = new List<Vector3>();
    List<Vector3> real_posi_list = new List<Vector3>();
    List<Vector3> real_rotation_list = new List<Vector3>();
    List<float> real_posi_length_list = new List<float>();
    List<float> real_posi_length_list_x = new List<float>();
    List<float> real_posi_length_list_z = new List<float>();
    List<float> cyber_posi_length_list = new List<float>();
    List<float> Real_Cyber_future_length_pose_compare = new List<float>();
    List<float> Real_Cyber_future_length_anglar_compare = new List<float>();
    List<float> real_diff_anglar_list = new List<float>();
    private Vector3 last_pose;
    private Vector3 last_rotation;
    private int last_time;
    private Vector3 diff_pose;
    private Vector3 diff_rot;
    private Quaternion rotation_for_list;
    private float real_anglar_length = 0.0f;
    public GameObject targetObject;
    public bool synchronization_sw;
    private float zerotime;
    //
    public float adopt_time = 1.0f;
    public float intervalInMilliseconds = 1000.0f; //    ԊԊu i ~   b j
    private DateTime nextActionTime;
    //
    public int key = 1;
    public bool TimeSynchronize;
    //
    Controller_manager VRManager;
    FieldMainManager SimORRealSelecter;
    Model_name model_name_space;
    mode_selector selected_mode;
    cont_crowlar crawler_controllor;
    PoseSubscriber RealPosition;
    int cmd_operation = 0;
    public float adapter1 = 1.0f;
    public float adapter2 = 0.0f;
    public float rotadapter = 0.0f;
    public int linear_or_rot = 0;
    private int moover_sw = 1;
    int publishersw = 0;
    int zerocounter = 0;
    float movespeed = 5.0f;
    public float linearspeed = 1.00f;
    public float rotspeed = 0.50f;
    //
    private double previousTime = 0.0;
    private double previousTime_adopt = 0.0;
    private float timeElapsed;
    private float timeElapsed_CMD = 0.0f;
    private float timeElapsed_Pose = 0.0f;
    private float timeElapsed_adopt = 0.0f;
    private float timeElapsed_adopt_starter = 0.0f;
    private float timeElapsed_start = 0.0f;
    private float sw_timeElapsed = 0.0f;
    private float dissconnect_timer;
    private int starter_acsel = 0;
    private float acsel = 0.0f;
    private float vel_linear_acceleration;
    public float max_lnear_accelaration = 2.5f;
    private float max_lnear_accel_per_pub;
    public float max_lnear_deceleration = -2.5f;
    private float max_lnear_deceleration_per_pub;
    private float vel_angular_acceleration;
    public float max_angular_accelaration = 3.2f;
    private float max_angular_accel_per_pub;
    public float max_angular_deceleration = -3.2f;
    private float max_angular_deceleration_per_pub;
    private float diff_pose_distance;
    private float side_diff = 0.0f;
    //
    private float point_theta;
    private float point_distance;
    //
    private float real_pose_length = 0.0f;
    private float real_pose_length_x = 0.0f;
    private float real_pose_length_z = 0.0f;
    private float Real_Cyber_future_length_pose = 0.0f;
    private float Real_Cyber_future_anglar_diff = 0.0f;
    private float side_anglar;

    private float cyber_pose_length = 0.0f;
    private int counter;
    private bool unconfined = true;
    public float publishMessageInterval = 0.02f;//50Hz
    private int dissconnect_detecter = 0;
    private Vector3 newPosition;
    private Quaternion newRotation;
    private int prev_control_mode;
    ROSConnection ros;
    // private PoseStampedMsg twist;
    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    private mode_selector mode;
    public float Margin = 0.2f;
    public float Angular_Margin = 0.2f;
    private float Stop_time = 0.0f;
    private float frontback = 0.0f;
    private float rotation = 0.0f;
    private long real_unix_time;
    private System.DateTime real_now_time;

    //public enum SimOrRealOption { ForSimPhysX, ForSimAGX, ForReal }

    //public SimOrRealOption ForPhysXorAGXorReal;

    // Start is called before the first frame update
    void Start()
    {
        //
        VRManager = FindObjectOfType<Controller_manager>();
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
        {
            SimORReal = 0;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")
        {
            SimORReal = 1;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
        {
            SimORReal = 2;
        }

        if (VRManager != null)
        {
            Debug.Log("Player's health is: " + VRManager.num);
        }
        // start the ROS connection
        Debug.Log("check:baselink/pose");
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<BoolMsg>(controller_swTopicName);
        ros.RegisterPublisher<BoolMsg>(EmergencyTopicName);
        controller_sw_return_TopicName = controller_swTopicName + "_return";
        ros.Subscribe<BoolMsg>(controller_sw_return_TopicName, SW_Callback);
        if (SimORReal == 2)
        {
            SRPublishTopicName = RealPublishTopicName;
            SRSubscribeTopicName = RealSubscribeTopicName;
            ros.Subscribe<PoseStampedMsg>(SRSubscribeTopicName, Callback);
        }
        else if (SimORReal == 1)
        {
            SRPublishTopicName = SimAGXPublishTopicName;
            SRSubscribeTopicName = SimAGXSubscribeTopicName;
            ros.Subscribe<OdometryMsg>(SRSubscribeTopicName, Callback1);
        }
        else if (SimORReal == 0)
        {
            SRPublishTopicName = SimPhysXPublishTopicName;
            SRSubscribeTopicName = SimPhysXSubscribeTopicName;
            ros.Subscribe<PoseStampedMsg>(SRSubscribeTopicName, Callback);
        }
        ros.RegisterPublisher<TwistMsg>(SRPublishTopicName);
        //
        //   twist = new PoseStampedMsg();


        Debug.Log("already:baselink/pose");
        //
        nextActionTime = DateTime.Now.AddMilliseconds(intervalInMilliseconds);
        //
        CMD_linear_list.Add(0.0f);
        CMD_linear_list.Add(0.0f);
        CMD_linear_list.Add(0.0f);
        CMD_linear_list.Add(0.0f);
        CMD_linear_list.Add(0.0f);
        CMD_linear_list.Add(0.0f);
        CMD_linear_list_for_cyber.Add(0.0f);
        CMD_anglar_list.Add(0.0f);
        CMD_anglar_list_for_cyber.Add(0.0f);
        real_rotation_list.Add(new Vector3(0.0f, 0.0f, 0.0f));
        real_posi_list.Add(new Vector3(0.0f, 0.0f, 0.0f));
        posi_list.Add(new Vector3(0.0f, 0.0f, 0.0f));
        Real_Cyber_future_length_anglar_compare.Add(0.0f);
        max_lnear_accel_per_pub = publishMessageInterval * max_lnear_accelaration;
        max_lnear_deceleration_per_pub = publishMessageInterval * max_lnear_deceleration;
        max_angular_accel_per_pub = publishMessageInterval * max_angular_accelaration;
        max_angular_deceleration_per_pub = publishMessageInterval * max_angular_deceleration;
    }
    // Update is called once per frame
    void Update()
    {
        //for test
        //
        //CMD_Calculator2(new Vector3(0.0f, 0.0f, 0.0f), new Quaternion(0.0f, 0.0f, 0.0f, 0.0f), new DateTime ((long)0.05f));
        //
        //
        //
        VRManager = FindObjectOfType<Controller_manager>();

        if (prev_sw == 1 && sw == 0)
        {
            sw_timeElapsed += Time.deltaTime;
            if (sw_timeElapsed >= publishMessageInterval * 50.0f)
            {
                // Debug.Log("Publish After Delay Time");
                BoolMsg message = new BoolMsg(
                    false
                    );
                ros.Publish(controller_swTopicName, message);
                sw_timeElapsed = 0.0f;
                timeElapsed = 0.0f;
                unconfined = true;
            }

        }
        if (emergency == true || VRManager.emergency_sw == true)
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
            //Debug.Log("cont_mode1_read_list");
            //
            if (timeElapsed >= publishMessageInterval / 2.0f)
            {
                // Debug.Log("Publish After Delay Time");
                ros.Publish(EmergencyTopicName, EMGmessage);
                ros.Publish(SRPublishTopicName, Twist);
                timeElapsed = 0.0f;
            }
            CMD_linear_list_for_cyber.Clear();
            CMD_anglar_list_for_cyber.Clear();
            CMD_linear_list.Clear();
            CMD_anglar_list.Clear();
            CMD_linear_list_for_cyber.Add(0.00f);
            CMD_anglar_list_for_cyber.Add(0.00f);
            CMD_linear_list.Add(0.00f);
            CMD_linear_list.Add(0.0f);
            CMD_linear_list.Add(0.0f);
            CMD_linear_list.Add(0.0f);
            CMD_linear_list.Add(0.0f);
            CMD_linear_list.Add(0.0f);
            CMD_linear_list.Add(0.0f);
            CMD_anglar_list.Add(0.00f);
            linear_or_rot = 0;
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

            //VRManager = FindObjectOfType<Controller_manager>();

            if (sw == 1 && RecordPlaySw == false)
            {
                prev_sw = 1;
                // Debug.Log("get: " + laiser.conum_zx200);
                if (sw_timeElapsed >= publishMessageInterval * 50.0f && unconfined == true)
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

                    selected_mode = FindObjectOfType<mode_selector>();
                    //
                    timeElapsed += Time.deltaTime;
                    timeElapsed_Pose += Time.deltaTime;
                    sw_timeElapsed += Time.deltaTime;

                    if (selected_mode.mode == 2)
                    {
                        timeElapsed_CMD += Time.deltaTime;
                        timeElapsed_adopt_starter += Time.deltaTime;
                        timeElapsed_start += Time.deltaTime;
                        zerotime += Time.deltaTime;
                        Stop_time += Time.deltaTime;
                        dissconnect_timer += Time.deltaTime;
                    }


                    Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                    if (linear_or_rot == 0)
                    {
                        if (Math.Abs(stickL.y) > 0.2)
                        {
                            linear_or_rot = 1;
                            Debug.Log("linear");
                        }
                        else if (Math.Abs(stickL.x) > 0.2)
                        {
                            linear_or_rot = 2;
                            Debug.Log("angular");
                        }
                    }
                    //
                    if (linear_or_rot == 1 || selected_mode.mode == 1 || control_mode == 0)
                    {
                        frontback = stickL.y;
                    }
                    else if (linear_or_rot == 2)
                    {
                        frontback = 0.0f;
                    }
                    if (linear_or_rot == 2 || selected_mode.mode == 1 || control_mode == 0)
                    {
                        rotation = -stickL.x;
                    }   
                    else if (linear_or_rot == 1)
                    {
                        rotation = 0.0f;
                    }

                    //Debug.Log("x" + linear.x + "z" + angular.z);

                    if (key == 1)
                    {
                        if (linear_or_rot == 0)
                        {
                            if ((Input.GetKey(KeyCode.UpArrow)) || (Input.GetKey(KeyCode.DownArrow)))
                            {
                                linear_or_rot = 1;
                            }
                            if ((Input.GetKey(KeyCode.LeftArrow)) || (Input.GetKeyUp(KeyCode.RightArrow)))
                            {
                                linear_or_rot = 2;
                            }
                        }

                        if (Input.GetKey(KeyCode.Space))
                        {
                            // OVRManager.display.RecenterPose();
                            frontback = 0.0f;
                            rotation = 0.0f;
                            adapter1 = 0.0f;
                            adapter2 = 0.0f;
                            Debug.Log("adapter reset at 411");
                        }
                        //
                        if (Input.GetKey(KeyCode.LeftArrow) && linear_or_rot == 2 || Input.GetKey(KeyCode.LeftArrow) && selected_mode.mode == 1 || Input.GetKey(KeyCode.LeftArrow) && control_mode == 0)
                        {
                            rotation = rotspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.LeftArrow))
                        {
                            rotation = 0;
                        }
                        if (Input.GetKey(KeyCode.RightArrow) && linear_or_rot == 2 || Input.GetKey(KeyCode.RightArrow) && selected_mode.mode == 1 || Input.GetKey(KeyCode.RightArrow) && control_mode == 0)
                        {
                            rotation = -rotspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.RightArrow))
                        {
                            rotation = 0;
                        }
                        if (Input.GetKey(KeyCode.UpArrow) && linear_or_rot == 1 || Input.GetKey(KeyCode.UpArrow) && selected_mode.mode == 1 || Input.GetKey(KeyCode.UpArrow) && control_mode == 0)
                        {
                            frontback = linearspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.UpArrow))
                        {
                            frontback = 0;
                        }
                        if (Input.GetKey(KeyCode.DownArrow) && linear_or_rot == 1 || Input.GetKey(KeyCode.DownArrow) && selected_mode.mode == 1 || Input.GetKey(KeyCode.DownArrow) && control_mode == 0)
                        {
                            frontback = -linearspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.DownArrow))
                        {
                            frontback = 0;
                        }
                    }
                    // Debug.Log("x" + frontback + "z" + rotation);
                    //
                    if (control_mode == 0)
                    {
                        moover_sw = 1;
                        if (prev_control_mode != control_mode) 
                        {
                            emergency = true;
                            prev_control_mode = 0;
                        }
                        

                        if (linear.x == 0 && angular.z == 0)
                        {
                            zerocounter += 1;
                        }
                        if ((zerocounter != 0 && frontback != 0) | (zerocounter != 0 && rotation != 0))
                        {
                            zerocounter = 0;
                        }
                        //
                        //
                        // Finally send the message to server_endpoint.py running in ROS
                        if (zerocounter <= 20 && timeElapsed >= publishMessageInterval)
                        {
                            vel_linear_acceleration = (frontback - CMD_linear_list[CMD_linear_list.Count - 1]) / (publishMessageInterval);
                            if (vel_linear_acceleration > max_lnear_accel_per_pub && frontback >= (CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub))
                            {
                                frontback = CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub;
                            }
                            else if (vel_linear_acceleration < max_lnear_deceleration_per_pub && frontback <= (CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub))
                            {
                                frontback = CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_deceleration_per_pub;
                            }
                            vel_angular_acceleration = (rotation - CMD_anglar_list[CMD_anglar_list.Count - 1]) / (publishMessageInterval);
                            if (vel_angular_acceleration > max_angular_accel_per_pub && rotation >= (CMD_anglar_list[CMD_anglar_list.Count - 1] + max_angular_accel_per_pub))
                            {
                                rotation = CMD_anglar_list[CMD_anglar_list.Count - 1] + max_angular_accel_per_pub;
                            }
                            else if (vel_angular_acceleration < max_angular_deceleration_per_pub && rotation <= (CMD_anglar_list[CMD_anglar_list.Count - 1] + max_angular_accel_per_pub))
                            {
                                rotation = CMD_anglar_list[CMD_anglar_list.Count - 1] + max_angular_deceleration_per_pub;
                            }
                            CMD_linear_list.Add(frontback);
                            CMD_linear_list_for_cyber.Add(frontback);
                            CMD_anglar_list.Add(rotation);
                            CMD_anglar_list_for_cyber.Add(rotation);
                            linear.x = frontback;
                            angular.z = rotation;
                            //Send untiy_odom to turtlebot_control
                            TwistMsg Twist = new TwistMsg(
                              linear,
                              angular
                              );
                            //  Debug.Log("linear " + linear.x);
                            //   Debug.Log("anglar" + angular.z);
                            //  Debug.Log("Publish On Time");
                            ros.Publish(SRPublishTopicName, Twist);
                            timeElapsed = 0.0f;
                        }
                        //   previousTime = time;

                    }

                    if (control_mode == 1 && selected_mode.mode == 2)
                    {
                        if (prev_control_mode != control_mode)
                        {
                            emergency = true;
                            prev_control_mode = 1;
                        }

                        //Debug.Log("cont_mode1");
                        if (linear_or_rot == 1 && frontback != 0 && moover_sw == 1)
                        {
                            zerotime = 0.0f;
                        }
                        if (linear_or_rot == 2 && rotation != 0 && moover_sw == 1)
                        {
                            zerotime = 0.0f;
                        }
                        if (frontback == 0 && rotation == 0)
                        {
                            adapter1 = 1.0f;
                            adapter2 = 0.0f;
                            rotadapter = 0.0f;
                            Debug.Log("adapter reset at 532");
                        }

                        if (zerotime >= Time_Delay + 3.0f && synchronization_sw == true)
                        {
                            moover_sw = 0;
                            frontback = 0.0f;
                            rotation = 0.0f;
                            adapter1 = 1.0f;
                            adapter2 = 0.0f;
                            rotadapter = 0.0f;
                            linear_or_rot = 0;
                            Debug.Log("adapter reset at 543");
                            if (zerotime >= Time_Delay)
                            {
                                moover_sw = 2;
                            }

                        }

                        if (moover_sw != 1)
                        {
                            frontback = 0.0f;
                            rotation = 0.0f;
                        }
                        if (timeElapsed_CMD >= publishMessageInterval)
                        {
                            vel_linear_acceleration = (frontback - CMD_linear_list[CMD_linear_list.Count - 1]) / (publishMessageInterval);

                            if (vel_linear_acceleration > max_lnear_accel_per_pub && frontback >= (CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub))
                            {
                                //Debug.Log(Mathf.Abs(frontback) + "  a  " + (CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub));
                                frontback = CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub;
                                //Debug.Log("accel");
                            }
                            else if (vel_linear_acceleration < max_lnear_deceleration_per_pub && frontback <= (CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub))
                            {
                                frontback = CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_deceleration_per_pub;
                            }
                            vel_angular_acceleration = (rotation - CMD_anglar_list[CMD_anglar_list.Count - 1]) / (publishMessageInterval);
                            if (vel_angular_acceleration > max_angular_accel_per_pub && rotation >= (CMD_anglar_list[CMD_anglar_list.Count - 1] + max_angular_accel_per_pub))
                            {
                                rotation = CMD_anglar_list[CMD_anglar_list.Count - 1] + max_angular_accel_per_pub;
                            }
                            else if (vel_angular_acceleration < max_angular_deceleration_per_pub && rotation <= (CMD_anglar_list[CMD_anglar_list.Count - 1] + max_angular_accel_per_pub))
                            {
                                rotation = CMD_anglar_list[CMD_anglar_list.Count - 1] + max_angular_deceleration_per_pub;
                            }
                            //if (CMD_linear_list.Count >= 1 && CMD_linear_list[CMD_linear_list.Count - 1] == 0 && frontback >= 3 )
                            //{
                            //    starter_acsel = 1;
                            //    frontback = 0.5f;
                            //}
                            CMD_linear_list.Add(frontback);
                            CMD_linear_list_for_cyber.Add(frontback * adapter1 + adapter2);
                            CMD_anglar_list.Add(rotation);
                            CMD_anglar_list_for_cyber.Add(rotation + rotadapter);
                            timeElapsed_CMD = 0.0f;

                            //TodayNow = DateTime.Now;

                            //CMD_time_list.Add(DateTime.Now.ToLongTimeString());
                            //Debug.Log("cont_mode1_add_list");

                        }
                        if (timeElapsed_Pose >= adopt_time)
                        {


                        }
                        if (timeElapsed_start > (Time_Delay + 5.0f) && CMD_linear_list.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) - 1 >= 0 && CMD_anglar_list.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) - 1 >= 0)
                        {
                            //
                            int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                            linear.x = CMD_linear_list[CMD_linear_list.Count - CMD_time - 1];
                            angular.z = CMD_anglar_list[CMD_anglar_list.Count - CMD_time - 1];
                            //Debug.Log(CMD_time_list[CMD_time_list.Count - (CMD_time)]);
                            TwistMsg Twist = new TwistMsg(
                              linear,
                              angular
                              );
                            //Debug.Log("cont_mode1_read_list");
                            //
                            if (timeElapsed >= publishMessageInterval)
                            {
                                // Debug.Log("Publish After Delay Time");
                                ros.Publish(SRPublishTopicName, Twist);
                                timeElapsed = 0.0f;
                            }
                            //      previousTime = time;


                        }
                        RealPosition = GetComponent<PoseSubscriber>();
                       // Debug.Log(RealPosition);
                      //  Debug.Log(RealPosition.newPosition);
                        newPosition = RealPosition.newPosition;
                        newRotation = RealPosition.newRotation;
                        
                    }

                }//
            }//
        }
        selected_mode = FindObjectOfType<mode_selector>();
        if (selected_mode.mode == 2 && RecordPlaySw == true)
        {
            timeElapsed_CMD += Time.deltaTime;
            timeElapsed_adopt_starter += Time.deltaTime;
            timeElapsed_start += Time.deltaTime;
            zerotime += Time.deltaTime;
            Stop_time += Time.deltaTime;
            dissconnect_timer += Time.deltaTime;
        }
    }

    DateTime UnixTimeToDateTime(long unixTime)
    {
        // Unixエポックは1970年1月1日 00:00:00 UTCからの秒数なので、それを基にDateTimeを作成
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddSeconds(unixTime).ToLocalTime(); // ローカルタイムに変換
    }
    void Callback1(OdometryMsg msg)
    {
        mode = FindObjectOfType<mode_selector>();
        dissconnect_timer = 0.0f;

        if (mode.mode == 2 && control_mode == 1 && sw == 1) //Controll mode (Pose modify)
        {
            model_name_space = GetComponent<Model_name>();
            RealPosition = GetComponent<PoseSubscriber>();
        //    offset_x = model_name_space.OffsetList[0];
         //   offset_y = model_name_space.OffsetList[1];
        //    offset_z = model_name_space.OffsetList[2];
            //Debug.Log("moooovercallback");
            DateTime currentTime = DateTime.Now;
            timeElapsed_adopt += Time.deltaTime;
            if (currentTime >= nextActionTime)//(timeElapsed_adopt >= adopt_time)
            {
                //Debug.Log("mooooverget");
                posi_list.Add(targetObject.transform.position);
                //posi_list_z.Add(GameObject.Find("ic120").transform.position.z);
                rotation_for_list = targetObject.transform.rotation;
                rotation_list.Add(rotation_for_list.eulerAngles);

                if (TimeSynchronize == true)
                {
                    real_unix_time = msg.header.stamp.sec;
                    real_now_time = UnixTimeToDateTime(real_unix_time);
                    //Debug.Log(real_now_time);
                }
                //////////////
                //////////////
                //       newPosition = new Vector3(((float)msg.pose.pose.position.x + offset_x), ((float)msg.pose.pose.position.z) + offset_z, ((float)msg.pose.pose.position.y) + offset_y);
                //       newRotation = new((float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.y, (float)msg.pose.pose.orientation.w);
                //       NewRotation = newRotation.eulerAngles;
                //////////////
                //////////////
                newPosition = RealPosition.newPosition + new Vector3(-36f, 0, 52f);
                newRotation = RealPosition.newRotation;
                Debug.Log(newPosition);
                //////////////
                //////////////
                ///
                CMD_Calculator(newPosition, newRotation, currentTime, timeElapsed_adopt_starter);
                ///
                /////////////
                /////////////

            }
        }
    }

    void Callback(PoseStampedMsg msg)
    {
       // Debug.Log("callback");
        mode = FindObjectOfType<mode_selector>();
        dissconnect_timer = 0.0f;

        if (mode.mode == 2 && control_mode == 1 && sw == 1) //Controll mode (Pose modify)
        {
            model_name_space = targetObject.GetComponent<Model_name>();
            RealPosition = targetObject.GetComponent<PoseSubscriber>();
          //  offset_x = model_name_space.OffsetList[0];
          //  offset_y = model_name_space.OffsetList[1];
           // offset_z = model_name_space.OffsetList[2];
            //Debug.Log("moooovercallback");
            DateTime currentTime = DateTime.Now;
            timeElapsed_adopt += Time.deltaTime;
            //double time_adopt = Time.fixedTimeAsDouble;
            //Debug.Log(timeElapsed_adopt);
            Debug.Log(RealPosition.newPosition);
            if (currentTime >= nextActionTime)//(timeElapsed_adopt >= adopt_time)
            {
                //Debug.Log("mooooverget");
                posi_list.Add(targetObject.transform.position);
                //posi_list_z.Add(GameObject.Find("ic120").transform.position.z);
                rotation_for_list = targetObject.transform.rotation;
                rotation_list.Add(rotation_for_list.eulerAngles);

                if (TimeSynchronize == true)
                {
                    real_unix_time = msg.header.stamp.sec;
                    real_now_time = UnixTimeToDateTime(real_unix_time);
                    //Debug.Log(real_now_time);
                }

                Vector3 newPosition = new Vector3(((float)msg.pose.position.x) - ((float)21395.18), ((float)msg.pose.position.z) - offset_y, ((float)msg.pose.position.y - ((float)14034.45)));
                Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
                Vector3 rot_offset = new Vector3((float)rot_offset_x, (float)rot_offset_y, (float)rot_offset_z);
                Vector3 chenged_orientation = newRotation.eulerAngles - rot_offset;
                Quaternion newRotationQuo = Quaternion.Euler(chenged_orientation);
                Vector3 newPositionChanged = newPosition + new Vector3(-36f, 0, 52f);
                //////////////
                //////////////
                //   newPosition = RealPosition.newPosition;
                //   newRotation = RealPosition.newRotation;
                //////////////
                //////////////
                ///
                CMD_Calculator(newPositionChanged, newRotationQuo, currentTime, timeElapsed_adopt_starter);
                /// 
                //////////////
                /////////////
                
                timeElapsed_adopt = 0.0f;
            }
            //previousTime_adopt = time_adopt;
        }
    }

    void CMD_Calculator(Vector3 RealPosition, Quaternion realRotation, DateTime NowTime, float TimeElapsed_adopt_starter_param)
    {
       // Debug.Log("CMD_Calculator");
        real_posi_list.Add(RealPosition);
        Vector3 RealRotation = realRotation.eulerAngles;
        //real_rotation_list.Add(RealRotation);
        real_rotation_list.Add(realRotation * Vector3.forward);
        //real_diff_anglar_list.Add(real_rotation_list[real_rotation_list.Count - 1][1] - real_rotation_list[real_rotation_list.Count - 2][1]);
        real_diff_anglar_list.Add(Vector3.SignedAngle(real_rotation_list[real_rotation_list.Count - 2], real_rotation_list[real_rotation_list.Count - 1], Vector3.up));
        real_posi_length_list.Add(Vector3.Distance(real_posi_list[real_posi_list.Count - 1], real_posi_list[real_posi_list.Count - 2]));//実機の前フレームからの進んだ距離
        real_posi_length_list_x.Add((real_posi_list[real_posi_list.Count - 1][0]) - (real_posi_list[real_posi_list.Count - 2][0]));//world座標のx方向に進んだ距離
        real_posi_length_list_z.Add((real_posi_list[real_posi_list.Count - 1][2]) - (real_posi_list[real_posi_list.Count - 2][2]));//world座標のz方向に進んだ距離
        cyber_posi_length_list.Add(Vector3.Distance(posi_list[posi_list.Count - 1], posi_list[posi_list.Count - 2]));//サイバー空間のモデルの進んだ距離

        Debug.Log("Real : "+ RealPosition+" : " + targetObject.transform.position);

        if (TimeElapsed_adopt_starter_param >= Time_Delay)
        {
            //Debug.Log("CMD_Calculator");
            last_time = Mathf.RoundToInt(Time_Delay / (intervalInMilliseconds / 1000));//ラグ時間前のリストの数
            if (((linear_or_rot == 1) && ((real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2])) < 1.3 && ((real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2])) > 0.7))//|| (RecordPlaySw == true))
            {
               // Debug.Log("CMD_Calculator");
                real_pose_length = 0.0f;
                real_pose_length_x = 0.0f;
                real_pose_length_z = 0.0f;
                cyber_pose_length = 0.0f;
                counter = 0;
                for (int i = real_posi_length_list.Count - 1; i >= (real_posi_length_list.Count - last_time - 1); i--)
                {
                    real_pose_length = real_pose_length + real_posi_length_list[i];//実空間の重機のラグ時間に進んだ距離
                    real_pose_length_x = real_pose_length_x + real_posi_length_list_x[i];
                    real_pose_length_z = real_pose_length_z + real_posi_length_list_z[i];
                    cyber_pose_length = cyber_pose_length + cyber_posi_length_list[i];
                    counter += 1;
                }
                //real_pose_length = real_pose_length / (counter);
                //real_pose_length_x = real_pose_length_x / (counter);
                //real_pose_length_z = real_pose_length_z / (counter);
                cyber_pose_length = cyber_pose_length / (counter);

                Vector3 Real_future_pose = new Vector3(RealPosition[0] + real_pose_length_x, 0.0f, RealPosition[2] + real_pose_length_z);
                Vector2 Real_Cyber_future_diff_pose = new Vector2((posi_list[posi_list.Count - 1][0]) - Real_future_pose[0], (posi_list[posi_list.Count - 1][2]) - Real_future_pose[2]);//サイバー空間の重機のモデルと実空間の重機の位置の差
                // Real_Cyber_future_length_pose = Vector2.Dot(new Vector2((float)Math.Sin(-RealRotation[1]), (float)Math.Cos(-RealRotation[1])), Real_Cyber_future_diff_pose);
                Vector3 Real_forwardVector = realRotation * Vector3.forward; // Z軸方向
                Vector3 Real_forwardVector_normal = Real_forwardVector.normalized;
                //Real_Cyber_future_length_pose = Vector2.Dot(new Vector2((float)Math.Sin(-RealRotation[1]), (float)Math.Cos(-RealRotation[1])), Real_Cyber_future_diff_pose);
                Real_Cyber_future_length_pose = Vector2.Dot(new Vector2(Real_forwardVector_normal[0], Real_forwardVector_normal[2]), Real_Cyber_future_diff_pose);//実機の進行方向の実機とモデルの差


                ///
                ///

                Real_Cyber_future_length_pose_compare.Add(Real_Cyber_future_length_pose);

                Debug.Log("diffpose" + Real_Cyber_future_length_pose + "  bector_length   " + Real_Cyber_future_diff_pose);
                if (Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 1] > Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 2] && Real_Cyber_future_length_pose > 0)
                {
                    //adapter1 = 0.5f;
                    if (Math.Abs(Real_Cyber_future_length_pose) >= Margin)
                    {
                        adapter2 -= 0.2f;
                    }
                    else if (Math.Abs(Real_Cyber_future_length_pose) > 0.01 && Math.Abs(Real_Cyber_future_length_pose) < Margin)
                    {
                        adapter2 -= 0.01f;
                    }


                    Debug.Log("deceler:" + adapter2);
                }
                else if (Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 1] < Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 2] && Real_Cyber_future_length_pose < 0)
                {
                    //adapter1 = 1.5f;
                    if (Math.Abs(Real_Cyber_future_length_pose) >= Margin)
                    {
                        adapter2 += 0.2f;
                    }
                    else if (Math.Abs(Real_Cyber_future_length_pose) > 0.01 && Math.Abs(Real_Cyber_future_length_pose) < Margin)
                    {
                        adapter2 += 0.01f;
                    }
                    // adapter2 += 0.1f;

                    Debug.Log("accel:" + adapter2);
                }
                //Vector3 forwardDirection = realRotation * Vector3.forward;
                Vector3 toTarget = targetObject.transform.position - RealPosition;
                float angle = Vector3.SignedAngle(Real_forwardVector, toTarget, Vector3.up);
                // Debug.Log("角の差 " + angle);
                side_diff = (Vector3.Distance(RealPosition, targetObject.transform.position)) * (float)Math.Sin(angle * Math.PI / 180);
                ///
                ///


                if (Math.Abs(side_diff) > Margin)
                {
                    Vector3 Real_forward = (real_posi_list[real_posi_list.Count - 1] - real_posi_list[real_posi_list.Count - 2]);
                    float direction = Vector2.Dot(new Vector2(Real_forwardVector[0], Real_forwardVector[2]), new Vector2(Real_forward[0], Real_forward[2]));
                    // Vector3 localOffset = new Vector3(-side_diff, 0, real_pose_length);  // 前方から見て進むオフセット
                    // Vector3 worldOffset = targetObject.transform.TransformDirection(localOffset);
                    //Vector3 RealPosition = targetObject.transform.position + worldOffset;
                    //Vector3 targetdirection = targetObject.transform.position - (RealPosition + (Real_forwardVector_normal * (Real_Cyber_future_length_pose + 2 * real_pose_length)));
                    Vector3 targetdirection = new Vector3((targetObject.transform.position[0] - (RealPosition[0] + (Real_forwardVector_normal * (Real_Cyber_future_length_pose + 2 * real_pose_length))[0])),0.0f, (targetObject.transform.position[2] - (RealPosition[2] + (Real_forwardVector_normal * (Real_Cyber_future_length_pose + 2 * real_pose_length))[2])));
                    // float Cyber_angle = Vector3.SignedAngle(targetObject.transform.rotation * Vector3.forward, worldOffset, Vector3.up);
                    //float Cyber_angle = Vector3.SignedAngle(targetObject.transform.rotation * Vector3.forward, -targetdirection, Vector3.up);
                    //float Cyber_angle = Vector3.SignedAngle(realRotation * Vector3.forward, -targetdirection, Vector3.up);
                    Vector3 Cyber_front_vec = new Vector3((float)(targetObject.transform.rotation * Vector3.forward)[0], 0.0f, (float)(targetObject.transform.rotation * Vector3.forward)[2]);
                    
                   // float Cyber_angle = Vector3.SignedAngle(targetObject.transform.rotation * Vector3.forward, -targetdirection, Vector3.up);
                    float Cyber_angle = Vector3.SignedAngle(Cyber_front_vec, -targetdirection, Vector3.up);

                    //Debug.Log("------角度の差は---^---^--- " + (targetObject.transform.position - (RealPosition + (Real_forwardVector_normal * (Real_Cyber_future_length_pose + 2 * real_pose_length)))));
                    Debug.Log("------実機前方---^---^--- " + (realRotation * Vector3.forward) + " 目標ベクトル " + -targetdirection+"横ずれ"+ side_diff);
                    Debug.Log("モデル前方 : " + Cyber_front_vec + " 目標ベクトル " + -targetdirection);
                    if (Math.Abs(side_diff) > Margin && Math.Abs(Cyber_angle) > Angular_Margin)
                    {
                      //  Debug.Log("change rotadoptor");
                        if (direction >= 0)//yellow
                        {

                            if (-Cyber_angle < 0)
                            {
                                //rotadapter = -0.1f;
                                rotadapter = -Cyber_angle * (float)((Math.PI) / 180.0f);
                                Debug.Log("1:rotadapter -= 0.1f " + rotadapter + "角度の差は " + Cyber_angle);
                            }
                            else if (-Cyber_angle >= 0)
                            {
                                //rotadapter = +0.1f;
                                rotadapter = -Cyber_angle * (float)((Math.PI) / 180.0f);
                                Debug.Log("2:rotadapter += 0.1f " + rotadapter + "角度の差は " + Cyber_angle);
                            }
                        }
                        else if (direction < 0)//blue
                        {
                            if (-Cyber_angle < 0)
                            {
                                //rotadapter = +0.1f;
                                rotadapter = -Cyber_angle * (float)((Math.PI) / 180.0f);
                                Debug.Log("3:rotadapter += 0.1f " + rotadapter + "角度の差は " + Cyber_angle);
                            }
                            else if (-Cyber_angle >= 0)
                            {
                                // rotadapter = -0.1f;
                                rotadapter = -Cyber_angle * (float)((Math.PI) / 180.0f);
                                Debug.Log("4:rotadapter -= 0.1f " + rotadapter + "角度の差は " + Cyber_angle);
                            }
                        }
                    }
                }



            }
            last_rotation = rotation_list[rotation_list.Count - last_time];
            diff_rot = last_rotation - RealRotation;
            if ((linear_or_rot == 2))
            {
                //Real_Cyber_future_length_anglar_compare.Add(diff_rot[1]);
                real_anglar_length = 0.0f;
                counter = 0;
                for (int i = real_diff_anglar_list.Count - 1; i > (real_diff_anglar_list.Count - last_time); i--)
                {
                    real_anglar_length = real_anglar_length + real_diff_anglar_list[i];
                    counter += 1;
                }
                //
                Debug.Log("前方方向の角度 " + realRotation * Vector3.forward);
                Vector3 Real_forwardVector = realRotation * Vector3.forward;
                Vector3 toTarget = rotation_for_list * Vector3.forward;
                float angle = Vector3.SignedAngle(toTarget, Real_forwardVector, Vector3.up);
                //
                //Real_Cyber_future_anglar_diff = rotation_for_list.eulerAngles[1] - (real_anglar_length + RealRotation[1]);
                if (Math.Abs(Real_Cyber_future_anglar_diff - Real_Cyber_future_length_anglar_compare[Real_Cyber_future_length_anglar_compare.Count - 1]) > Math.Abs(Real_Cyber_future_anglar_diff - Real_Cyber_future_length_anglar_compare[Real_Cyber_future_length_anglar_compare.Count - 1] - 360))
                {
                    Real_Cyber_future_anglar_diff = Real_Cyber_future_anglar_diff - 360;
                }
                Real_Cyber_future_length_anglar_compare.Add(Real_Cyber_future_anglar_diff);
                //if (Mathf.Abs(diff_rot[1]) >= Angular_Margin)
                //{

                //if (diff_rot[1] < -180)
                //{
                //    diff_rot[1] = diff_rot[1] + 360.0f;
                //}
                //if (diff_rot[1] > 0 && Real_Cyber_future_length_anglar_compare[Real_Cyber_future_length_anglar_compare.Count - 1] > Real_Cyber_future_length_anglar_compare[Real_Cyber_future_length_anglar_compare.Count - 2])
                if (real_anglar_length + angle < 0)
                {
                    rotadapter += 0.01f;
                }
                //else if (diff_rot[1] < 0 && Real_Cyber_future_length_anglar_compare[Real_Cyber_future_length_anglar_compare.Count - 1] < Real_Cyber_future_length_anglar_compare[Real_Cyber_future_length_anglar_compare.Count - 2])
                else if (real_anglar_length + angle >= 0)
                {
                    rotadapter -= 0.01f;
                }
                Debug.Log("実機の回転予想 " + real_anglar_length);
                Debug.Log("シミュレーターと実機の角度差 " + angle);
                Debug.Log("角度の差は " + (real_anglar_length + angle));
                Debug.Log("diffrot" + Real_Cyber_future_anglar_diff);// diff_rot[1]);
                Debug.Log("rotadapter" + rotadapter);
                //if (real_rotation_list[real_rotation_list.Count - 1][1] - real_rotation_list[real_rotation_list.Count - 2][1] > 0)//right rotation
                //{
                //    if (diff_rot[1] > 0)
                //    {
                //        rotadapter -= 0.01f;
                //    }
                //    else if (diff_rot[1] < 0)
                //    {
                //        rotadapter += 0.01f;
                //    }
                //}
                //else if (real_rotation_list[real_rotation_list.Count - 1][1] - real_rotation_list[real_rotation_list.Count - 2][1] < 0)//left rotation
                //{
                //    if (diff_rot[1] > 0)
                //    {
                //        rotadapter -= 0.01f;
                //    }
                //    else if (diff_rot[1] < 0)
                //    {
                //        rotadapter += 0.01f;
                //    }
                //}

                // targetObject.transform.position = targetObject.transform.position + new Vector3(0.0f, 0.030f, 0.0f);
                // Quaternion goal_angule = Quaternion.Euler(-new Vector3(0.0f, diff_rot[1], 0.0f) + rotation_for_list.eulerAngles);
                // targetObject.transform.rotation = goal_angule;
                //}
            }
            ////
            if (moover_sw == 2)
            {
                Debug.Log("vrcont_zerostop");
                if ((Vector3.Distance(real_posi_list[real_posi_list.Count - 1], targetObject.transform.position)) >= Margin)
                {
                    Debug.Log("vrcont_pose?set");
                    targetObject.GetComponent<Rigidbody>().isKinematic = true;
                    targetObject.transform.position = targetObject.transform.position + new Vector3(0.0f, 0.030f, 0.0f);
                    targetObject.transform.position = real_posi_list[real_posi_list.Count - 1];
                }


                if (Mathf.Abs(diff_rot[1]) >= Angular_Margin)
                {
                    targetObject.GetComponent<Rigidbody>().isKinematic = true;
                    targetObject.transform.position = targetObject.transform.position + new Vector3(0.0f, 0.030f, 0.0f);
                    Quaternion goal_angule = Quaternion.Euler(-new Vector3(0.0f, diff_rot[1], 0.0f) + rotation_for_list.eulerAngles);
                    targetObject.transform.rotation = goal_angule;
                }
                moover_sw = 3;
                zerotime = 0.0f;
                adapter1 = 1.0f;
                adapter2 = 0.0f;
                Stop_time = 0.0f;
                Debug.Log("adapter reset at 1015");
            }
            if (moover_sw == 3 && Stop_time > 2.0f)
            {
                moover_sw = 1;
                targetObject.GetComponent<Rigidbody>().isKinematic = false;
            }
            ////

            // Debug.Log("mooooverst");
            //
            //Debug.Log(string.Join(",", posi_list.Select(n => n.ToString())));
            //
            nextActionTime = NowTime.AddMilliseconds(intervalInMilliseconds);
            // Debug.Log(posi_list);
            // Debug.Log(RealPosition);

        }


    }
    void CMD_Calculator2(Vector3 RealPosition, Quaternion realRotation, DateTime NowTime, float TimeElapsed_adopt_starter_param)
    {
        real_posi_list.Add(RealPosition);
        Vector3 RealRotation = realRotation.eulerAngles;
        //real_rotation_list.Add(RealRotation);
        real_rotation_list.Add(realRotation * Vector3.forward);
        //real_diff_anglar_list.Add(real_rotation_list[real_rotation_list.Count - 1][1] - real_rotation_list[real_rotation_list.Count - 2][1]);
        real_diff_anglar_list.Add(Vector3.SignedAngle(real_rotation_list[real_rotation_list.Count - 2], real_rotation_list[real_rotation_list.Count - 1], Vector3.up));
        //
        //
        real_posi_length_list.Add(Vector3.Distance(real_posi_list[real_posi_list.Count - 1], real_posi_list[real_posi_list.Count - 2]));//実機の前フレームからの進んだ距離
        cyber_posi_length_list.Add(Vector3.Distance(posi_list[posi_list.Count - 1], posi_list[posi_list.Count - 2]));//サイバー空間のモデルの進んだ距離
        //
        //

        if (TimeElapsed_adopt_starter_param >= Time_Delay)
        {
            last_time = Mathf.RoundToInt(Time_Delay / (intervalInMilliseconds / 1000));//ラグ時間前のリストの数
            if ((linear_or_rot == 1) && (real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2]) < 1.1 && (real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2]) > 0.9)
            {
                
                real_pose_length = 0.0f;
                cyber_pose_length = 0.0f;
                counter = 0;
                for (int i = real_posi_length_list.Count - 1; i > (real_posi_length_list.Count - last_time - 1); i--)
                {
                    real_pose_length = real_pose_length + real_posi_length_list[i];//実空間の重機のラグ時間に進んだ距離

                    cyber_pose_length = cyber_pose_length + cyber_posi_length_list[i];
                    counter += 1;
                }
                
                /*
                //for test
                //
                real_pose_length = 5.0f;
                real_posi_list.Add(GameObject.Find("Real_Cube").transform.position);
                real_rotation_list.Add(GameObject.Find("Real_Cube").transform.rotation * Vector3.forward);
                //
                //
                */
                Vector3 Syberposition = targetObject.transform.position;
                Vector3 forwardposition = real_posi_list[real_posi_list.Count - 1] + real_rotation_list[real_rotation_list.Count - 1] * real_pose_length;
                Vector3 SybReaDistance = new Vector3((forwardposition[0] - Syberposition[0]), (0.0f), (forwardposition[2] - Syberposition[2]));
                float SybReaLength = SybReaDistance.magnitude;
                float angle = Vector3.SignedAngle(real_rotation_list[real_rotation_list.Count - 1], SybReaDistance, Vector3.up);
                float forward_diff = SybReaLength * (float)Math.Cos(angle * Math.PI / 180);
                side_diff = SybReaLength * (float)Math.Sin(angle * Math.PI / 180);

                //
                Debug.Log("SybReaLength: " + SybReaLength);
                Debug.Log("angle: " + angle);
                Debug.Log("forward : " + forward_diff);
                Debug.Log("side : " + side_diff);

                ///
                ///

                Real_Cyber_future_length_pose_compare.Add(Real_Cyber_future_length_pose);

               

            }
        }
    }

    void SW_Callback(BoolMsg msg)
    {
        if (msg.data == true && sw == 1)
        {
            unconfined = false;
        }
        else if (msg.data == false && sw == 0)
        {
            unconfined = true;
            prev_sw = 0;
        }

    }
}