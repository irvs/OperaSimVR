using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using System.Drawing.Printing;
using System.Linq;
public class vrcmdvelcontroller : MonoBehaviour
{
    public int sw = 0;
    public int control_mode = 0;
    public float Time_Delay = 5.0f;
    public List<float> CMD_linear_list = new List<float>();
    List<float> CMD_anglar_list = new List<float>();
    List<string> CMD_time_list = new List<string>();
    List<Vector3> posi_list = new List<Vector3>();
    List<float> posi_list_z = new List<float>();/////////////////////////////////////
    List<Vector3> rotation_list = new List<Vector3>();
    private Vector3 last_pose;
    private Vector3 last_rotation;
    private int last_time;
    private Vector3 diff_pose;
    private Vector3 diff_rot;
    private Quaternion rotation_for_list;
    public GameObject targetObject;
    public int synchronization_sw = 0;
    //
    public float adopt_time = 1.0f;
    public float intervalInMilliseconds = 1000.0f; // 時間間隔（ミリ秒）
    private DateTime nextActionTime;
    //
    public int key = 1;
    //
    public ControllerLay laiser;
    vessel_kinametic vessel_joint_kinametic;
    mood_selector selected_mode;
    cont_crowlar crawler_controllor;
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
    private float timeElapsed_CMD = 0.0f;
    private float timeElapsed_Pose = 0.0f;
    private float timeElapsed_adopt = 0.0f;
    private float timeElapsed_adopt_starter = 0.0f;
    private float timeElapsed_start = 0.0f;
    private int starter_acsel = 0;
    private float acsel = 0.0f;
    private float vel_linear_acceleration;
    public float max_lnear_accelaration=2.5f;
    private float max_lnear_accel_per_pub;
    public float max_lnear_deceleration = -2.5f;
    private float max_lnear_deceleration_per_pub;
    private float diff_pose_distance;
    //
    private float point_theta;
    private float point_distance;
    //
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
    private mood_selector mode;
    public float Margin = 0.2f;
    public float Angular_Margin = 0.2f;
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
        ros.RegisterPublisher<TwistMsg>("ic120/tracks/cmd_vel");
        //cube = GetComponent<Rigidbody>();
        //
        //Debug.Log("aaaaaaaa");
        twist = new PoseStampedMsg();
        Debug.Log("check:baselink/pose");
        // ROSコネクションへのサブスクライバーの登録
        //ROSConnection.instance.Subscribe<TwistMsg>("/ic120/tracks/cmd_vel", Callback);
        ros.Subscribe<PoseStampedMsg>("/ic120/base_link/pose", Callback);
        Debug.Log("already:baselink/pose");
        //
        nextActionTime = DateTime.Now.AddMilliseconds(intervalInMilliseconds);
        //
        CMD_linear_list.Add(0.0f);
        max_lnear_accel_per_pub = publishMessageInterval * max_lnear_accelaration;
        max_lnear_deceleration_per_pub =  publishMessageInterval * max_lnear_deceleration;
    }
    // Update is called once per frame
    void Update()
    {
        //
        laiser = FindObjectOfType<ControllerLay>();
        //Debug.Log("vrcmdvelcont");
        if (laiser != null && laiser.geton_ic120 == 1 || sw == 1)
        {
            // Debug.Log("get: " + laiser.conum_zx200);
            if (laiser.conum_zx200 == 0 && sw != 1)
            {
                cmd_operation = 0;

            }
            else if (laiser.conum_zx200 > 0 || sw == 1)
            {
                cmd_operation = 1;
                //Debug.Log("geton");
                //}
                // }
                double time = Time.fixedTimeAsDouble;
                double deltaTime = time - previousTime;
                selected_mode = FindObjectOfType<mood_selector>();
                //
                timeElapsed += Time.deltaTime;
                timeElapsed_Pose += Time.deltaTime;
                
                if (selected_mode.mood == 2)
                {
                    timeElapsed_CMD += Time.deltaTime;
                    timeElapsed_adopt_starter += Time.deltaTime;
                    timeElapsed_start += Time.deltaTime;
                }
                

                //double time = Time.fixedTimeAsDouble;
                //double deltaTime = time - previousTime;
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
                float frontback = stickL.y;
                //linear.x = stickL.y;
                //Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                float rotation = -stickL.x;
                //angular.z = -stickL.x;
                //Debug.Log("x" + linear.x + "z" + angular.z);

                if (key == 1)
                {
                    if (Input.GetKey(KeyCode.Space))
                    {
                        // OVRManager.display.RecenterPose();
                        frontback = 0;
                        rotation = 0;
                    }
                    //
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        rotation = rotspeed;
                        //angular.z = angular.z + 0.1;
                        // Debug.Log("左アナログスティックを上に倒した");
                    }
                    if (Input.GetKeyUp(KeyCode.LeftArrow))
                    {
                        rotation = 0;
                        //angular.z = angular.z + 0.1;
                        // Debug.Log("左アナログスティックを上に倒した");
                    }
                    // 右に移動
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        rotation = -rotspeed;
                        //angular.z = angular.z + (-0.1);
                        // Debug.Log("右アナログスティックを上に倒した");
                    }
                    if (Input.GetKeyUp(KeyCode.RightArrow))
                    {
                        rotation = 0;
                        //angular.z = angular.z + (-0.1);
                        // Debug.Log("右アナログスティックを上に倒した");
                    }
                    // 前に移動
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        frontback = linearspeed;
                        //linear.x = linear.x + 0.1;
                    }

                    if (Input.GetKeyUp(KeyCode.UpArrow))
                    {
                        frontback = 0;
                        //linear.x = linear.x + 0.1;
                    }
                    // 後ろに移動
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        frontback = -linearspeed;
                        //linear.x = linear.x + (-0.1);
                    }
                    if (Input.GetKeyUp(KeyCode.DownArrow))
                    {
                        frontback = 0;
                        //linear.x = linear.x + (-0.1);
                    }
                }
                // Debug.Log("x" + frontback + "z" + rotation);

                //print(linear);
                //
                //
                //
                if (control_mode == 0)
                {
                    linear.x = frontback;
                    angular.z = rotation;
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
                        Debug.Log("Publish On Time");
                        ros.Publish("ic120/tracks/cmd_vel", Twist);
                        timeElapsed = 0.0f;
                    }
                    previousTime = time;

                }
                
                if (control_mode == 1 && selected_mode.mood == 2)
                {
                    //Debug.Log("cont_mode1");
                    if (timeElapsed_CMD >= publishMessageInterval)
                    {
                        vel_linear_acceleration = (frontback- CMD_linear_list[CMD_linear_list.Count - 1]) / (publishMessageInterval);

                        if (vel_linear_acceleration > max_lnear_accel_per_pub && frontback>= (CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub))
                        {
                            //Debug.Log(Mathf.Abs(frontback) + "  a  " + (CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub));
                            frontback = CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub;
                            //Debug.Log("accel");
                        }
                        else if (vel_linear_acceleration < max_lnear_deceleration_per_pub && frontback <= (CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_accel_per_pub))
                        {
                            frontback = CMD_linear_list[CMD_linear_list.Count - 1] + max_lnear_deceleration_per_pub;
                        }
                        //if (CMD_linear_list.Count >= 1 && CMD_linear_list[CMD_linear_list.Count - 1] == 0 && frontback >= 3 )
                        //{
                        //    starter_acsel = 1;
                        //    frontback = 0.5f;
                        //}
                        CMD_linear_list.Add(frontback);
                        CMD_anglar_list.Add(rotation);
                        timeElapsed_CMD = 0.0f;
                        
                        //TodayNow = DateTime.Now;

                        //テキストUIに年・月・日・秒を表示させる
                        CMD_time_list.Add(DateTime.Now.ToLongTimeString());
                        //Debug.Log("cont_mode1_add_list");

                    }
                    if (timeElapsed_Pose >= adopt_time)
                    {


                    }
                    if (timeElapsed_start > (Time_Delay + 5.0f) && CMD_linear_list.Count - (Mathf.RoundToInt(Time_Delay / publishMessageInterval)) >=0)
                    {
                        //
                        int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                        linear.x = CMD_linear_list[CMD_linear_list.Count - (CMD_time)];
                        angular.z = CMD_anglar_list[CMD_anglar_list.Count - (CMD_time)];
                        //Debug.Log(CMD_time_list[CMD_time_list.Count - (CMD_time)]);
                        //Send untiy_odom to turtlebot_control
                        TwistMsg Twist = new TwistMsg(
                          linear,
                          angular
                          );
                        //Debug.Log("cont_mode1_read_list");
                        //
                        //
                        // Finally send the message to server_endpoint.py running in ROS
                        if (timeElapsed >= publishMessageInterval)
                        {
                            // Debug.Log("Publish After Delay Time");
                            ros.Publish("ic120/tracks/cmd_vel", Twist);
                            timeElapsed = 0.0f;
                        }
                        previousTime = time;


                    }
                }

            }//
        }//
    }

    void Callback(PoseStampedMsg msg)
    {
        mode = FindObjectOfType<mood_selector>();

        if (mode.mood == -2) //Controll mode (Pose modify)
        {
            //Debug.Log("moooovercallback");
            DateTime currentTime = DateTime.Now;
            timeElapsed_adopt += Time.deltaTime;
            //double time_adopt = Time.fixedTimeAsDouble;
            //Debug.Log(timeElapsed_adopt);
            if (currentTime >= nextActionTime)//(timeElapsed_adopt >= adopt_time)
            {
                //Debug.Log("mooooverget");
                posi_list.Add(GameObject.Find("ic120").transform.position);
                posi_list_z.Add(GameObject.Find("ic120").transform.position.z);
                rotation_for_list = GameObject.Find("ic120").transform.rotation;
                rotation_list.Add(rotation_for_list.eulerAngles);


                Vector3 newPosition = new Vector3((float)msg.pose.position.y * (-1), (float)msg.pose.position.z, (float)msg.pose.position.x);
                Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
                Vector3 NewRotation = newRotation.eulerAngles;

                if (timeElapsed_adopt_starter >= Time_Delay)
                {
                    // Debug.Log("mooooverst");
                    //
                    Debug.Log(string.Join(",", posi_list.Select(n => n.ToString())));
                    //
                    last_time = Mathf.RoundToInt(Time_Delay / adopt_time);
                    last_pose = posi_list[posi_list.Count - last_time];
                    last_rotation = rotation_list[rotation_list.Count - last_time];
                    diff_pose = last_pose - newPosition;
                    diff_pose_distance = (float)Math.Sqrt(((Math.Pow((newPosition[0] - posi_list[posi_list.Count - last_time][0]), 2)) + (Math.Pow((newPosition[1] - posi_list[posi_list.Count - last_time][1]), 2))));
                    diff_rot = last_rotation - NewRotation;
                    Debug.Log("Diffpose" + diff_pose);
                    Debug.Log("Modelpose" + last_pose);
                    //Debug.Log(last_rotation);
                    vessel_joint_kinametic = FindObjectOfType<vessel_kinametic>();
                    if (synchronization_sw == 1)// && vessel_joint_kinametic.joint_sw == 1)
                    {
                    //    GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = true;
                        //GameObject.Find("ic120").GetComponent<ArticulationBody>().enabled = false;
                    //    GameObject.Find("ic120").transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
                    //    GameObject.Find("ic120").transform.Find("base_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
                    //    GameObject.Find("ic120").GetComponent<Rigidbody>().drag = 100000000000000;

                        //            if (Mathf.Abs(diff_pose[0]) >= Margin) 
                        //              {
                        //                    Debug.Log("x");
                        //                      GameObject.Find("ic120").transform.position -= new Vector3(diff_pose[0],0.0f,0.0f);
                        //                    }
                        //    if (Mathf.Abs(diff_pose[0]) >= Margin)
                        //    {
                        //        //Debug.Log("x");
                        //        targetObject.transform.position -= new Vector3(diff_pose[0], 0.0f, 0.0f);
                        //        for (int i = posi_list.Count - 1; i > (posi_list.Count - last_time); i--)
                        //        {
                        //            Vector3 point = posi_list[i];
                        //            point = point - new Vector3(diff_pose[0], 0.0f, 0.0f);
                        //            posi_list[i] = point;
                        //float point = posi_list[i][0];
                        //point = point + diff_pose[0];
                        //posi_list[i][0] = point;
                        // }
                        // }
                        // if (Mathf.Abs(diff_pose[1]) >= Margin)
                        // {
                        //       Debug.Log("y");
                        //         GameObject.Find("ic120").transform.position -= new Vector3(0.0f, diff_pose[1], 0.0f);
                        //       }
                        if (Mathf.Abs(diff_pose[0]) >= Margin)
                        {
                            GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = true;
                            GameObject.Find("ic120").transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
                            GameObject.Find("ic120").transform.Find("base_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
                            GameObject.Find("ic120").GetComponent<Rigidbody>().drag = 100000000000000;
                            // Debug.Log("z");
                            GameObject.Find("ic120").transform.position = GameObject.Find("ic120").transform.position + new Vector3(0.0f, 0.030f, 0.0f);

                            Vector3 goal_pose = targetObject.transform.position - new Vector3(diff_pose[0], 0.0f, 0.0f);
                            // GameObject.Find("ic120").GetComponent<Rigidbody>().position = goal_pose;//-= new Vector3(0.0f, 0.0f, diff_pose[2]);
                            // GameObject.Find("zx200").GetComponent<Rigidbody>().position = new Vector3(10.0f,0.0f, 10.0f);
                            targetObject.transform.position = goal_pose;
                            for (int i = posi_list.Count - 1; i > (posi_list.Count - last_time); i--)
                            {
                                Vector3 point = posi_list[i];
                                point = point - new Vector3(diff_pose[0], 0.0f, 0.0f);
                                posi_list[i] = point;
                                //float point = posi_list[i][0];
                                //point = point + diff_pose[0];
                                //posi_list[i][0] = point;
                            }
                        }
                        if (Mathf.Abs(diff_pose[2]) >= Margin)
                        {
                            GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = true;
                            GameObject.Find("ic120").transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
                            GameObject.Find("ic120").transform.Find("base_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
                            GameObject.Find("ic120").GetComponent<Rigidbody>().drag = 100000000000000;
                            // Debug.Log("z");
                            GameObject.Find("ic120").transform.position = GameObject.Find("ic120").transform.position + new Vector3(0.0f, 0.03f, 0.0f);
                            
                            Vector3 goal_pose = targetObject.transform.position - new Vector3(0.0f, 0.0f, diff_pose[2]);
                           // GameObject.Find("ic120").GetComponent<Rigidbody>().position = goal_pose;//-= new Vector3(0.0f, 0.0f, diff_pose[2]);
                           // GameObject.Find("zx200").GetComponent<Rigidbody>().position = new Vector3(10.0f,0.0f, 10.0f);
                            targetObject.transform.position = goal_pose;
                            for (int i = posi_list.Count - 1; i > (posi_list.Count - last_time); i--)
                            {
                                Vector3 point = posi_list[i];
                                point = point - new Vector3(0.0f, 0.0f, diff_pose[2]);
                                posi_list[i] = point;
                                //float point = posi_list[i][0];
                                //point = point + diff_pose[0];
                                //posi_list[i][0] = point;
                            }
                        }
                        //
                        if (Mathf.Abs(diff_rot[1]) >= Angular_Margin)
                        {
                            GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = true;
                            GameObject.Find("ic120").transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
                            GameObject.Find("ic120").transform.Find("base_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
                            GameObject.Find("ic120").GetComponent<Rigidbody>().drag = 100000000000000;
                            //now_pose = targetObject.transform.position;
                            targetObject.transform.position = GameObject.Find("ic120").transform.position + new Vector3(0.0f, 0.030f, 0.0f);
                            Quaternion goal_angule = Quaternion.Euler(- new Vector3(0.0f, diff_rot[1], 0.0f) + rotation_for_list.eulerAngles);
                            targetObject.transform.rotation = goal_angule;
                            for (int i = rotation_list.Count - 1; i > (rotation_list.Count - last_time); i--)
                            {
                                //for rotation
                                Vector3 point = rotation_list[i];
                                point = point - new Vector3(0.0f, diff_rot[1], 0.0f);
                                rotation_list[i] = point;
                                //for move
                                point_theta = (float)Math.Atan2((posi_list[i][1] - newPosition[1]), (posi_list[i][0] - newPosition[0]));
                                point_distance = (float)Math.Sqrt(((Math.Pow((newPosition[0] - posi_list[i][0]), 2)) + (Math.Pow((newPosition[1] - posi_list[i][1]), 2))));
                                Vector3 pose_point = posi_list[i];
                                point = new Vector3(point_distance * (float)Math.Sin(point_theta + diff_rot[1]), 0.0f, point_distance * (float)Math.Cos(point_theta + diff_rot[1]));//point - new Vector3(0.0f, 0.0f, diff_pose[2]);
                            }
                        }
                        //
                        Debug.Log("model" + GameObject.Find("ic120").transform.position + "real" + newPosition);
                        //GameObject.Find("ic120").transform.position -= diff_pose;
                        //GameObject.Find("ic120").transform.rotation = Quaternion.Euler(diff_rot + rotation_for_list.eulerAngles);
                        GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = false;
                        // Debug.Log("moooover");
                        GameObject.Find("ic120").GetComponent<Rigidbody>().drag = 0;
                    }
                    diff_pose = new Vector3(0, 0, 0);
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
}