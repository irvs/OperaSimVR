using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using System.Drawing.Printing;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

public class VR_cont_2 : MonoBehaviour
{
    public int sw = 0;
    private int prev_sw = 0;
    public int control_mode = 0;
    public bool emergency;
    public bool SimORReal;
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    public string SimPublishTopicName;
    public string RealPublishTopicName;
    private string SRPublishTopicName;
    public string SimSubscribeTopicName;
    public string RealSubscribeTopicName;
    private string SRSubscribeTopicName;
    public string controller_swTopicName = "controller_sw";
    private string controller_sw_return_TopicName;
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
    public int synchronization_sw = 0;
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
    mood_selector selected_mode;
    cont_crowlar crawler_controllor;
    int cmd_operation = 0;
    private float adapter1 = 1.0f;
    private float adapter2 = 0.0f;
    private float rotadapter = 0.0f;
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
    private float dissconnect_timer = 0.0f;
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


    ROSConnection ros;
    // private PoseStampedMsg twist;
    //Twist
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    private mood_selector mode;
    public float Margin = 0.2f;
    public float Angular_Margin = 0.2f;
    private float Stop_time = 0.0f;
    private float frontback = 0.0f;
    private float rotation = 0.0f;
    private long real_unix_time;
    private System.DateTime real_now_time;
    // Start is called before the first frame update
    void Start()
    {
        //
        VRManager = FindObjectOfType<Controller_manager>();
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimulater")
        {
            SimORReal = false;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
        {
            SimORReal = true;
        }

        if (VRManager != null)
        {
            Debug.Log("Player's health is: " + VRManager.num);
        }
        // start the ROS connection
        Debug.Log("check:baselink/pose");
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<BoolMsg>(controller_swTopicName);
        controller_sw_return_TopicName = controller_swTopicName + "_return";
        ros.Subscribe<BoolMsg>(controller_sw_return_TopicName, SW_Callback);
        if (SimORReal == true)
        {
            SRPublishTopicName = RealPublishTopicName;
            SRSubscribeTopicName = RealSubscribeTopicName;
        }
        else if (SimORReal == false)
        {
            SRPublishTopicName = SimPublishTopicName;
            SRSubscribeTopicName = SimSubscribeTopicName;
        }
        ros.RegisterPublisher<TwistMsg>(SRPublishTopicName);
        //
        //   twist = new PoseStampedMsg();
        ros.Subscribe<PoseStampedMsg>(SRSubscribeTopicName, Callback);

        Debug.Log("already:baselink/pose");
        //
        nextActionTime = DateTime.Now.AddMilliseconds(intervalInMilliseconds);
        //
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
        //
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
            if (timeElapsed >= publishMessageInterval / 2.0f)
            {
                // Debug.Log("Publish After Delay Time");
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
            CMD_anglar_list.Add(0.00f);
            linear_or_rot = 0;
        }
        else if (emergency == false)
        {
            if (dissconnect_timer >= 3.0f)
            {
                //emergency = true;
            }

            VRManager = FindObjectOfType<Controller_manager>();
            //Debug.Log("vrcmdvelcont");
            // if (VRManager != null && VRManager.geton_ic120 == 1 || sw == 1)
            if (sw == 1)
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

                    selected_mode = FindObjectOfType<mood_selector>();
                    //
                    timeElapsed += Time.deltaTime;
                    timeElapsed_Pose += Time.deltaTime;
                    sw_timeElapsed += Time.deltaTime;

                    if (selected_mode.mood == 2)
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
                    if (linear_or_rot == 1 || selected_mode.mood == 1 || control_mode == 0)
                    {
                        frontback = stickL.y;
                    }
                    else if (linear_or_rot == 2)
                    {
                        frontback = 0.0f;
                    }
                    if (linear_or_rot == 2 || selected_mode.mood == 1 || control_mode == 0)
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
                        }
                        //
                        if (Input.GetKey(KeyCode.LeftArrow) && linear_or_rot == 2 || Input.GetKey(KeyCode.LeftArrow) && selected_mode.mood == 1 || Input.GetKey(KeyCode.LeftArrow) && control_mode == 0)
                        {
                            rotation = rotspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.LeftArrow))
                        {
                            rotation = 0;
                        }
                        if (Input.GetKey(KeyCode.RightArrow) && linear_or_rot == 2 || Input.GetKey(KeyCode.RightArrow) && selected_mode.mood == 1 || Input.GetKey(KeyCode.RightArrow) && control_mode == 0)
                        {
                            rotation = -rotspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.RightArrow))
                        {
                            rotation = 0;
                        }
                        if (Input.GetKey(KeyCode.UpArrow) && linear_or_rot == 1 || Input.GetKey(KeyCode.UpArrow) && selected_mode.mood == 1 || Input.GetKey(KeyCode.UpArrow) && control_mode == 0)
                        {
                            frontback = linearspeed;
                        }
                        if (Input.GetKeyUp(KeyCode.UpArrow))
                        {
                            frontback = 0;
                        }
                        if (Input.GetKey(KeyCode.DownArrow) && linear_or_rot == 1 || Input.GetKey(KeyCode.DownArrow) && selected_mode.mood == 1 || Input.GetKey(KeyCode.DownArrow) && control_mode == 0)
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

                    if (control_mode == 1 && selected_mode.mood == 2)
                    {
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
                        }
                        /*
                        if (zerotime >= Time_Delay+3.0f)
                        {
                            moover_sw = 0;
                            frontback = 0.0f;
                            rotation = 0.0f;
                            adapter1 = 1.0f;
                            adapter2 = 0.0f;
                            rotadapter = 0.0f;
                            linear_or_rot = 0;
                            if (zerotime >= Time_Delay)
                            {
                                moover_sw = 2;
                            }

                        }
                        */
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
                        if (timeElapsed_start > (Time_Delay + 5.0f) && CMD_linear_list.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) >= 0)
                        {
                            //
                            int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                            linear.x = CMD_linear_list[CMD_linear_list.Count - (CMD_time)];
                            angular.z = CMD_anglar_list[CMD_anglar_list.Count - (CMD_time)];
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

                    }

                }//
            }//
        }
    }

    DateTime UnixTimeToDateTime(long unixTime)
    {
        // Unixエポックは1970年1月1日 00:00:00 UTCからの秒数なので、それを基にDateTimeを作成
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddSeconds(unixTime).ToLocalTime(); // ローカルタイムに変換
    }

    void Callback(PoseStampedMsg msg)
    {
        mode = FindObjectOfType<mood_selector>();
        dissconnect_timer = 0.0f;

        if (mode.mood == 2 && control_mode == 1 && sw == 1) //Controll mode (Pose modify)
        {
            model_name_space = FindObjectOfType<Model_name>();
            offset_x = model_name_space.OffsetList[0];
            offset_y = model_name_space.OffsetList[1];
            offset_z = model_name_space.OffsetList[2];
            //Debug.Log("moooovercallback");
            DateTime currentTime = DateTime.Now;
            timeElapsed_adopt += Time.deltaTime;
            //double time_adopt = Time.fixedTimeAsDouble;
            //Debug.Log(timeElapsed_adopt);
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

                Vector3 newPosition = new Vector3(((float)msg.pose.position.y * (-1) + offset_x), ((float)msg.pose.position.z) + offset_z, ((float)msg.pose.position.x) + offset_y);
                Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
                Vector3 NewRotation = newRotation.eulerAngles;
                real_posi_list.Add(newPosition);
                //real_rotation_list.Add(NewRotation);
                real_rotation_list.Add(newRotation * Vector3.forward);
                //real_diff_anglar_list.Add(real_rotation_list[real_rotation_list.Count - 1][1] - real_rotation_list[real_rotation_list.Count - 2][1]);
                real_diff_anglar_list.Add(Vector3.SignedAngle(real_rotation_list[real_rotation_list.Count - 2], real_rotation_list[real_rotation_list.Count - 1], Vector3.up));
                real_posi_length_list.Add(Vector3.Distance(real_posi_list[real_posi_list.Count - 1], real_posi_list[real_posi_list.Count - 2]));
                real_posi_length_list_x.Add((real_posi_list[real_posi_list.Count - 1][0]) - (real_posi_list[real_posi_list.Count - 2][0]));
                real_posi_length_list_z.Add((real_posi_list[real_posi_list.Count - 1][2]) - (real_posi_list[real_posi_list.Count - 2][2]));
                cyber_posi_length_list.Add(Vector3.Distance(posi_list[posi_list.Count - 1], posi_list[posi_list.Count - 2]));


                if (timeElapsed_adopt_starter >= Time_Delay)
                {
                    last_time = Mathf.RoundToInt(Time_Delay / (intervalInMilliseconds / 1000));
                    if ((linear_or_rot == 1) && (real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2]) < 1.1 && (real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2]) > 0.9)
                    {
                        real_pose_length = 0.0f;
                        real_pose_length_x = 0.0f;
                        real_pose_length_z = 0.0f;
                        cyber_pose_length = 0.0f;
                        counter = 0;
                        for (int i = real_posi_length_list.Count - 1; i > (real_posi_length_list.Count - last_time - 1); i--)
                        {
                            real_pose_length = real_pose_length + real_posi_length_list[i];
                            real_pose_length_x = real_pose_length_x + real_posi_length_list_x[i];
                            real_pose_length_z = real_pose_length_z + real_posi_length_list_z[i];
                            cyber_pose_length = cyber_pose_length + cyber_posi_length_list[i];
                            counter += 1;
                        }
                        //real_pose_length = real_pose_length / (counter);
                        //real_pose_length_x = real_pose_length_x / (counter);
                        //real_pose_length_z = real_pose_length_z / (counter);
                        cyber_pose_length = cyber_pose_length / (counter);

                        Vector3 Real_future_pose = new Vector3(newPosition[0] + real_pose_length_x, 0.0f, newPosition[2] + real_pose_length_z);
                        Vector2 Real_Cyber_future_diff_pose = new Vector2((targetObject.transform.position[0]) - Real_future_pose[0], (targetObject.transform.position[2]) - Real_future_pose[2]);
                        // Real_Cyber_future_length_pose = Vector2.Dot(new Vector2((float)Math.Sin(-NewRotation[1]), (float)Math.Cos(-NewRotation[1])), Real_Cyber_future_diff_pose);
                        Vector3 Real_forwardVector = newRotation * Vector3.forward; // Z軸方向
                        Vector3 Real_forwardVector_normal = Real_forwardVector.normalized;
                        //Real_Cyber_future_length_pose = Vector2.Dot(new Vector2((float)Math.Sin(-NewRotation[1]), (float)Math.Cos(-NewRotation[1])), Real_Cyber_future_diff_pose);
                        Real_Cyber_future_length_pose = Vector2.Dot(new Vector2(Real_forwardVector_normal[0], Real_forwardVector_normal[2]), Real_Cyber_future_diff_pose);

                        /*
                        if (Math.Abs(Real_Cyber_future_length_pose) < Margin)
                        {
                            Real_Cyber_future_length_pose = 0.0f;
                        }
                        Real_Cyber_future_length_pose_compare.Add(Real_Cyber_future_length_pose);

                        Debug.Log("diffpose" + Real_Cyber_future_length_pose + "  bector_length   " + Real_Cyber_future_diff_pose);
                        if (Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 1] > Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 2] && Real_Cyber_future_length_pose > 0)
                        {
                            //adapter1 = 0.5f;
                            adapter2 -= 0.1f;

                            Debug.Log("deceler:" + adapter2);
                        }
                        else if (Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 1] < Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 2] && Real_Cyber_future_length_pose < 0)
                        {
                            //adapter1 = 1.5f;
                            adapter2 += 0.1f;

                            Debug.Log("accel:" + adapter2);
                        }
                        //Vector3 forwardDirection = newRotation * Vector3.forward;
                        Vector3 toTarget = targetObject.transform.position - newPosition;
                        float angle = Vector3.SignedAngle(Real_forwardVector, toTarget, Vector3.up);
                        // Debug.Log("角の差 " + angle);
                        side_diff = (Vector3.Distance(newPosition, targetObject.transform.position)) * (float)Math.Sin(angle * Math.PI / 180);
                        */
                        ///
                        ///

                        Real_Cyber_future_length_pose_compare.Add(Real_Cyber_future_length_pose);

                        Debug.Log("diffpose" + Real_Cyber_future_length_pose + "  bector_length   " + Real_Cyber_future_diff_pose);
                        if (Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 1] > Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 2] && Real_Cyber_future_length_pose > 0)
                        {
                            //adapter1 = 0.5f;
                            if (Math.Abs(Real_Cyber_future_length_pose) >= Margin)
                            {
                                adapter2 -= 0.1f;
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
                                adapter2 += 0.1f;
                            }
                            else if (Math.Abs(Real_Cyber_future_length_pose) > 0.01 && Math.Abs(Real_Cyber_future_length_pose) < Margin)
                            {
                                adapter2 += 0.01f;
                            }
                            // adapter2 += 0.1f;

                            Debug.Log("accel:" + adapter2);
                        }
                        //Vector3 forwardDirection = newRotation * Vector3.forward;
                        Vector3 toTarget = targetObject.transform.position - newPosition;
                        float angle = Vector3.SignedAngle(Real_forwardVector, toTarget, Vector3.up);
                        // Debug.Log("角の差 " + angle);
                        side_diff = (Vector3.Distance(newPosition, targetObject.transform.position)) * (float)Math.Sin(angle * Math.PI / 180);
                        ///
                        ///


                        if (Math.Abs(side_diff) > Margin)
                        {
                            Vector3 Real_forward = (real_posi_list[real_posi_list.Count - 1] - real_posi_list[real_posi_list.Count - 2]);
                            float direction = Vector2.Dot(new Vector2(Real_forwardVector[0], Real_forwardVector[2]), new Vector2(Real_forward[0], Real_forward[2]));
                            // Vector3 localOffset = new Vector3(-side_diff, 0, real_pose_length);  // 前方から見て進むオフセット
                            // Vector3 worldOffset = targetObject.transform.TransformDirection(localOffset);
                            //Vector3 newPosition = targetObject.transform.position + worldOffset;
                            Vector3 targetdirection = targetObject.transform.position - (newPosition + (Real_forwardVector_normal * (Real_Cyber_future_length_pose + 2 * real_pose_length)));
                            // float Cyber_angle = Vector3.SignedAngle(targetObject.transform.rotation * Vector3.forward, worldOffset, Vector3.up);
                            //float Cyber_angle = Vector3.SignedAngle(targetObject.transform.rotation * Vector3.forward, -targetdirection, Vector3.up);
                            //float Cyber_angle = Vector3.SignedAngle(newRotation * Vector3.forward, -targetdirection, Vector3.up);
                            float Cyber_angle = Vector3.SignedAngle(targetObject.transform.rotation * Vector3.forward, -targetdirection, Vector3.up);

                            //Debug.Log("------角度の差は---^---^--- " + (targetObject.transform.position - (newPosition + (Real_forwardVector_normal * (Real_Cyber_future_length_pose + 2 * real_pose_length)))));
                            Debug.Log("------実機前方---^---^--- " + (newRotation * Vector3.forward) + " 目標ベクトル " + -targetdirection);
                            if (Math.Abs(side_diff) < Margin && Math.Abs(Cyber_angle) > Angular_Margin)
                            {
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
                    diff_rot = last_rotation - NewRotation;
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
                        Debug.Log("前方方向の角度 " + newRotation * Vector3.forward);
                        Vector3 Real_forwardVector = newRotation * Vector3.forward;
                        Vector3 toTarget = rotation_for_list * Vector3.forward;
                        float angle = Vector3.SignedAngle(toTarget, Real_forwardVector, Vector3.up);
                        //
                        //Real_Cyber_future_anglar_diff = rotation_for_list.eulerAngles[1] - (real_anglar_length + NewRotation[1]);
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
                    nextActionTime = currentTime.AddMilliseconds(intervalInMilliseconds);
                    // Debug.Log(posi_list);
                    // Debug.Log(newPosition);

                }

                //double deltaTime_adopt = time_adopt - previousTime_adopt;
                timeElapsed_adopt = 0.0f;
            }
            //previousTime_adopt = time_adopt;
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