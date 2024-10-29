using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using System.Drawing.Printing;
using System.Linq;
public class VR_cont_2 : MonoBehaviour
{
    public int sw = 0;
    public int control_mode = 0;
    public float Time_Delay = 5.0f;
    public List<float> CMD_linear_list_for_cyber = new List<float>();
    public List<float> CMD_linear_list = new List<float>();
    List<float> CMD_anglar_list = new List<float>();
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
    List<float> CMD_anglar_list_for_cyber = new List<float>();
    List<float> Real_Cyber_future_length_pose_compare = new List<float>();
    private Vector3 last_pose;
    private Vector3 last_rotation;
    private int last_time;
    private Vector3 diff_pose;
    private Vector3 diff_rot;
    private Quaternion rotation_for_list;
    public GameObject targetObject;
    public int synchronization_sw = 0;
    private float zerotime;
    //
    public float adopt_time = 1.0f;
    public float intervalInMilliseconds = 1000.0f; //    ԊԊu i ~   b j
    private DateTime nextActionTime;
    //
    public int key = 1;
    //
    public ControllerLay laiser;
    vessel_kinametic vessel_joint_kinametic;
    mood_selector selected_mode;
    cont_crowlar crawler_controllor;
    int cmd_operation = 0;
    private float adapter1 = 1.0f;
    private float adapter2 = 0.0f;
    private float rotadapter = 0.0f;
    private int linear_or_rot = 1;
    private int moover_sw = 1;
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
    public float max_lnear_accelaration = 2.5f;
    private float max_lnear_accel_per_pub;
    public float max_lnear_deceleration = -2.5f;
    private float max_lnear_deceleration_per_pub;
    private float diff_pose_distance;
    //
    private float point_theta;
    private float point_distance;
    //
    private float real_pose_length = 0.0f;
    private float real_pose_length_x = 0.0f;
    private float real_pose_length_z = 0.0f;
    private float Real_Cyber_future_length_pose = 0.0f;

    private float cyber_pose_length = 0.0f;
    private int counter;
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
    private float Stop_time = 0.0f;
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

        //ROSConnection.instance.Subscribe<TwistMsg>("/ic120/tracks/cmd_vel", Callback);
        ros.Subscribe<PoseStampedMsg>("/ic120/base_link/pose", Callback);
        Debug.Log("already:baselink/pose");
        //
        nextActionTime = DateTime.Now.AddMilliseconds(intervalInMilliseconds);
        //
        CMD_linear_list.Add(0.0f);
        CMD_linear_list_for_cyber.Add(0.0f);
        max_lnear_accel_per_pub = publishMessageInterval * max_lnear_accelaration;
        max_lnear_deceleration_per_pub = publishMessageInterval * max_lnear_deceleration;
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
                    zerotime += Time.deltaTime;
                    Stop_time += Time.deltaTime;
                }


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
                    if (Input.GetKey(KeyCode.LeftArrow) && linear_or_rot == 2)
                    {
                        rotation = rotspeed;
                        //angular.z = angular.z + 0.1;
                    }
                    if (Input.GetKeyUp(KeyCode.LeftArrow))
                    {
                        rotation = 0;
                        //angular.z = angular.z + 0.1;
                    }
                    
                    if (Input.GetKey(KeyCode.RightArrow) && linear_or_rot == 2)
                    {
                        rotation = -rotspeed;
                        //angular.z = angular.z + (-0.1);
                    }
                    if (Input.GetKeyUp(KeyCode.RightArrow))
                    {
                        rotation = 0;
                        //angular.z = angular.z + (-0.1);
                    }

                    if (Input.GetKey(KeyCode.UpArrow) && linear_or_rot == 1)
                    {
                        frontback = linearspeed;
                        //linear.x = linear.x + 0.1;
                    }

                    if (Input.GetKeyUp(KeyCode.UpArrow))
                    {
                        frontback = 0;
                        //linear.x = linear.x + 0.1;
                    }
 
                    if (Input.GetKey(KeyCode.DownArrow) && linear_or_rot == 1)
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
                    if (linear_or_rot == 1 && frontback != 0 && moover_sw == 1)
                    {
                        zerotime = 0.0f;
                    }
                    if (linear_or_rot == 2 && rotation != 0 && moover_sw == 1)
                    {
                        zerotime = 0.0f;
                    }
                    if (zerotime >= Time_Delay)
                    {
                        moover_sw = 0;
                        frontback = 0.0f;
                        rotation = 0.0f;
                        adapter1 = 1;
                        adapter2 = 0;
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
      
                        CMD_time_list.Add(DateTime.Now.ToLongTimeString());
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

        if (mode.mood == 2) //Controll mode (Pose modify)
        {
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


                Vector3 newPosition = new Vector3((float)msg.pose.position.y * (-1), (float)msg.pose.position.z, (float)msg.pose.position.x);
                Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
                Vector3 NewRotation = newRotation.eulerAngles;
                real_posi_list.Add(newPosition);
                real_rotation_list.Add(NewRotation);
                real_posi_length_list.Add(Vector3.Distance(real_posi_list[real_posi_list.Count - 1], real_posi_list[real_posi_list.Count - 2]));
                real_posi_length_list_x.Add((real_posi_list[real_posi_list.Count - 1][0]) - (real_posi_list[real_posi_list.Count - 2][0]));
                real_posi_length_list_z.Add((real_posi_list[real_posi_list.Count - 1][2]) - (real_posi_list[real_posi_list.Count - 2][2]));
                cyber_posi_length_list.Add(Vector3.Distance(posi_list[posi_list.Count - 1], posi_list[posi_list.Count - 2]));


                if (timeElapsed_adopt_starter >= Time_Delay)
                {
                    last_time = Mathf.RoundToInt(Time_Delay / (intervalInMilliseconds / 1000));
                    if ((linear_or_rot == 1) && (real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2]) < 1.1 && (real_posi_length_list[real_posi_length_list.Count - 2]) / (real_posi_length_list[real_posi_length_list.Count - 1]) > 0.9)
                    {
                        real_pose_length = 0.0f;
                        real_pose_length_x = 0.0f;
                        real_pose_length_z = 0.0f;
                        cyber_pose_length = 0.0f;
                        counter = 0;
                        for (int i = real_posi_length_list.Count - 1; i > (real_posi_length_list.Count - last_time); i--)
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
                        Real_Cyber_future_length_pose = Vector2.Dot(Real_Cyber_future_diff_pose, new Vector2((float)Math.Sin(NewRotation[1]), (float)Math.Cos(NewRotation[1])));
                        if (Math.Abs(Real_Cyber_future_length_pose) < Margin)
                        {
                            Real_Cyber_future_length_pose = 0.0f;
                        }
                        Real_Cyber_future_length_pose_compare.Add(Real_Cyber_future_length_pose);

                        Debug.Log("diffpose" + Real_Cyber_future_length_pose);
                        if (Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 1] > Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 2] && Real_Cyber_future_length_pose > 0) 
                        {
                            //adapter1 = 0.5f;
                            adapter2 -= 0.1f;

                            Debug.Log("deceler");
                        }
                        else if (Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 1] < Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 2] && Real_Cyber_future_length_pose < 0)
                        {
                            //adapter1 = 1.5f;
                            adapter2 += 0.1f;

                            Debug.Log("accel");
                        }


                        //if (Real_Cyber_future_length_pose >= 0)
                        //{
                        //    if (cyber_pose_length > real_pose_length)
                        //    {
                        //        //adapter1 = 0.5f;
                        //        adapter2 -= 0.1f;

                        //        Debug.Log("deceler");
                        //    }

                        //}
                        //else if (Real_Cyber_future_length_pose < 0)
                        //{
                        //    if (cyber_pose_length < real_pose_length)
                        //    {
                        //        //adapter1 = 1.5f;
                        //        adapter2 += 0.1f;
                        //
                        //        Debug.Log("accel");
                        //    }
                        //
                        //}


                    }
                    last_rotation = rotation_list[rotation_list.Count - last_time];
                    diff_rot = last_rotation - NewRotation;
                    if ((linear_or_rot == 2))
                    {
                        if (Mathf.Abs(diff_rot[1]) >= Angular_Margin)
                        {
                            //now_pose = targetObject.transform.position;
                            if (diff_rot[1] > 0)
                            {

                            }
                            targetObject.transform.position = targetObject.transform.position + new Vector3(0.0f, 0.030f, 0.0f);
                            Quaternion goal_angule = Quaternion.Euler(-new Vector3(0.0f, diff_rot[1], 0.0f) + rotation_for_list.eulerAngles);
                            targetObject.transform.rotation = goal_angule;
                        }

                    }
                    if (moover_sw == 2)
                    {
                        Debug.Log("vrcont_zerostop");
                        if ((Vector3.Distance(real_posi_list[real_posi_list.Count - 1], targetObject.transform.position)) >= Margin)
                        {
                            Debug.Log("vrcont_pose?set");
                            targetObject.GetComponent<Rigidbody>().isKinematic = true;
                           // targetObject.GetComponent<Rigidbody>().drag = 100000000000000;
                            targetObject.transform.position = targetObject.transform.position + new Vector3(0.0f, 0.030f, 0.0f);
                            targetObject.transform.position = real_posi_list[real_posi_list.Count - 1];
                           // targetObject.GetComponent<Rigidbody>().isKinematic = false;
                           // targetObject.GetComponent<Rigidbody>().drag = 0;
                        }

                        
                        if (Mathf.Abs(diff_rot[1]) >= Angular_Margin)
                        {
                            targetObject.GetComponent<Rigidbody>().isKinematic = true;
                         //   targetObject.GetComponent<Rigidbody>().drag = 100000000000000;
                            targetObject.transform.position = targetObject.transform.position + new Vector3(0.0f, 0.030f, 0.0f);
                            Quaternion goal_angule = Quaternion.Euler(-new Vector3(0.0f, diff_rot[1], 0.0f) + rotation_for_list.eulerAngles);
                            targetObject.transform.rotation = goal_angule;
                         //   targetObject.GetComponent<Rigidbody>().isKinematic = false;
                         //   targetObject.GetComponent<Rigidbody>().drag = 0;
                        }
                        
                        moover_sw = 3;
                        zerotime = 0.0f;
                        adapter1 = 1;
                        adapter2 = 0;
                        Stop_time = 0.0f;


                    }
                    if (moover_sw == 3 && Stop_time > 2.0f)
                    {
                        moover_sw = 1;
                        targetObject.GetComponent<Rigidbody>().isKinematic = false;
                    }

                    // Debug.Log("mooooverst");
                    //
                    Debug.Log(string.Join(",", posi_list.Select(n => n.ToString())));
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
}