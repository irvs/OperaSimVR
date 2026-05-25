using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

public class VRCrawlerOp : MonoBehaviour
{
    public enum ONOFF { Off, On }
    public ONOFF OnOffSw;
    public bool emergency;
    private int prev_sw = 0;
    public bool key;
    public bool UseRos2Topic;
    public string TwistPublishTopicName;
    /*public*/
    public string EmergencyTopicName;
    List<float> CMD_linear_list_for_cyber = new List<float>();
    List<float> CMD_linear_list = new List<float>();
    List<float> CMD_anglar_list_for_cyber = new List<float>();
    List<float> CMD_anglar_list = new List<float>();
    List<Vector3> posi_list = new List<Vector3>();
    List<Vector3> rotation_list = new List<Vector3>();
    List<Vector3> real_posi_list = new List<Vector3>();
    List<Vector3> real_rotation_list = new List<Vector3>();
    List<float> real_posi_length_list = new List<float>();
    List<float> real_posi_length_list_x = new List<float>();
    List<float> real_posi_length_list_z = new List<float>();
    List<float> Real_Cyber_future_length_pose_compare = new List<float>();
    List<float> Real_Cyber_future_length_anglar_compare = new List<float>();
    List<float> real_diff_anglar_list = new List<float>();
    private Vector3 last_rotation;
    private int last_time;
    private Vector3 diff_rot;
    private Quaternion rotation_for_list;
    private float real_anglar_length = 0.0f;
    GameObject targetObject;
    private float zerotime;
    //
    //
    //
    Controller_manager VRManager;
    mode_selector mode;
    PoseSubscriber RealPosition;
    float adapter1 = 1.0f;
    float adapter2 = 0.0f;
    float rotadapter = 0.0f;
    private int moover_sw = 1;
    float movespeed = 5.0f;
    public float LinearSpeed = 1.00f;
    public float RotSpeed = 0.50f;
    //
    private float timeElapsed;
    private float timeElapsed_adopt_starter = 0.0f;
    private float timeElapsed_start = 0.0f;
    private float sw_timeElapsed = 0.0f;
    private float dissconnect_timer;
    private float vel_linear_acceleration;
    public float MaxLinearAcceleration = 2.5f;
    private float max_lnear_accel_per_pub;
    public float MaxLinearDeceleration = -2.5f;
    private float max_lnear_deceleration_per_pub;
    private float vel_angular_acceleration;
    public float MaxAngularAcceleration = 3.2f;
    private float max_angular_accel_per_pub;
    public float MaxAngularDeceleration = -3.2f;
    private float max_angular_deceleration_per_pub;
    private float side_diff = 0.0f;
    //
    private float real_pose_length = 0.0f;
    private float real_pose_length_x = 0.0f;
    private float real_pose_length_z = 0.0f;
    private float Real_Cyber_future_length_pose = 0.0f;
    private float Real_Cyber_future_anglar_diff = 0.0f;

    private int counter;
    public float publishMessageInterval = 0.02f;//50Hz
    private Vector3 newPosition;
    private Quaternion newRotation;
    private int prev_control_mode;
    ROSConnection ros;
    Vector3Msg linear = new Vector3Msg(0f, 0f, 0f);
    Vector3Msg angular = new Vector3Msg(0f, 0f, 0f);
    public bool RecordPlaySw;//cmd record play
    public float Time_Delay = 5.0f;
    public float intervalInMilliseconds = 1000.0f;
    private DateTime nextActionTime;
    public bool synchronization_sw;
    public int LinearOrRot = 0;
    public float Margin = 0.2f;
    public float Angular_Margin = 0.2f;
    private float Stop_time = 0.0f;
    private float frontback = 0.0f;
    private float rotation = 0.0f;
    DiffDriveController diffDriveController;
    int RecordCounter = 0;
    private long PlayDeltaTime;
    private double cmdLinearVel;//cmd record play
    private double cmdAngularVel;//cmd record play
    float PrevLinearCMD;
    float PrevAngularCMD;
    Recorder CMD_Recorder;

    // Start is called before the first frame update
    void Start()
    {
        targetObject = this.gameObject;
        Debug.Log("check:VRCrawlerOp");
        ros = ROSConnection.GetOrCreateInstance();
        diffDriveController = GetComponent<DiffDriveController>();
        RealPosition = GetComponent<PoseSubscriber>();
        CMD_Recorder = GetComponent<Recorder>();
        VRManager = FindObjectOfType<Controller_manager>();
        mode = FindObjectOfType<mode_selector>();

        ros.RegisterPublisher<BoolMsg>(EmergencyTopicName);
        ros.RegisterPublisher<TwistMsg>(TwistPublishTopicName);
        //
        Debug.Log("already:VRCrawlerOp");
        //
        nextActionTime = DateTime.Now.AddMilliseconds(intervalInMilliseconds);
        //
        CMD_linear_list = new List<float> { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        CMD_linear_list_for_cyber.Add(0.0f);
        CMD_anglar_list.Add(0.0f);
        CMD_anglar_list_for_cyber.Add(0.0f);
        real_rotation_list.Add(new Vector3(0.0f, 0.0f, 0.0f));
        real_posi_list.Add(new Vector3(0.0f, 0.0f, 0.0f));
        posi_list.Add(new Vector3(0.0f, 0.0f, 0.0f));
        Real_Cyber_future_length_anglar_compare.Add(0.0f);
        max_lnear_accel_per_pub = publishMessageInterval * MaxLinearAcceleration;
        max_lnear_deceleration_per_pub = publishMessageInterval * MaxLinearDeceleration;
        max_angular_accel_per_pub = publishMessageInterval * MaxAngularAcceleration;
        max_angular_deceleration_per_pub = publishMessageInterval * MaxAngularDeceleration;
    }
    // Update is called once per frame
    void Update()
    {
        if (emergency || VRManager.emergency_sw)
        {
            timeElapsed += Time.deltaTime;
            if (timeElapsed >= publishMessageInterval / 2.0f)
            {
            EmergencyStop();
            timeElapsed = 0.0f;
            }
            CMD_linear_list_for_cyber.Clear();
            CMD_anglar_list_for_cyber.Clear();
            CMD_linear_list.Clear();
            CMD_anglar_list.Clear();
            CMD_linear_list_for_cyber.Add(0.00f);
            CMD_anglar_list_for_cyber.Add(0.00f);
            CMD_anglar_list.Add(0.00f);
            CMD_linear_list = new List<float> { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            CMD_anglar_list = new List<float> { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            return;   
        }
        else if (emergency == false)
        {
            if (OnOffSw == ONOFF.On && RecordPlaySw == false)
            {
                prev_sw = 1;

                if (VRManager.PlayerPoseMove_SW > 0 || OnOffSw == ONOFF.On)
                {

                    timeElapsed += Time.deltaTime;
                    sw_timeElapsed += Time.deltaTime;
                    if (mode.mode == 2)
                    {
                        timeElapsed_adopt_starter += Time.deltaTime;
                        timeElapsed_start += Time.deltaTime;
                        zerotime += Time.deltaTime;
                        Stop_time += Time.deltaTime;
                        dissconnect_timer += Time.deltaTime;
                    }

                    Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                    if (LinearOrRot == 0)
                    {
                        if (Math.Abs(stickL.y) > 0.2)
                        {
                            LinearOrRot = 1;
                            Debug.Log("linear");
                        }
                        else if (Math.Abs(stickL.x) > 0.2)
                        {
                            LinearOrRot = 2;
                            Debug.Log("angular");
                        }
                    }
                    //
                    if (LinearOrRot == 1 || mode.mode == 1 || mode.mode == 0)
                    {
                        frontback = LinearSpeed * stickL.y;
                    }
                    else if (LinearOrRot == 2)
                    {
                        frontback = 0.0f;
                    }
                    if (LinearOrRot == 2 || mode.mode == 1 || mode.mode == 0)
                    {
                        rotation = -RotSpeed * stickL.x;
                    }
                    else if (LinearOrRot == 1 || mode.mode == 0)
                    {
                        rotation = 0.0f;
                    }

                    if (key == true)
                    {
                        if (LinearOrRot == 0)
                        {
                            if ((Input.GetKey(KeyCode.UpArrow)) || (Input.GetKey(KeyCode.DownArrow)))
                            {
                                LinearOrRot = 1;
                            }
                            if ((Input.GetKey(KeyCode.LeftArrow)) || (Input.GetKeyUp(KeyCode.RightArrow)))
                            {
                                LinearOrRot = 2;
                            }
                        }
                        if ((Input.GetKey(KeyCode.LeftArrow) && LinearOrRot == 2) || (Input.GetKey(KeyCode.LeftArrow) && (mode.mode == 1 || mode.mode == 0)))
                        {
                            rotation = RotSpeed;
                        }
                        if (Input.GetKeyUp(KeyCode.LeftArrow))
                        {
                            rotation = 0;
                        }
                        if ((Input.GetKey(KeyCode.RightArrow) && LinearOrRot == 2) || (Input.GetKey(KeyCode.RightArrow) && (mode.mode == 1 || mode.mode == 0)))
                        {
                            rotation = -RotSpeed;
                        }
                        if (Input.GetKeyUp(KeyCode.RightArrow))
                        {
                            rotation = 0;
                        }
                        if ((Input.GetKey(KeyCode.UpArrow) && LinearOrRot == 1) || (Input.GetKey(KeyCode.UpArrow) && (mode.mode == 1 || mode.mode == 0)))
                        {
                            frontback = LinearSpeed;
                        }
                        if (Input.GetKeyUp(KeyCode.UpArrow))
                        {
                            frontback = 0;
                        }
                        if ((Input.GetKey(KeyCode.DownArrow) && LinearOrRot == 1) || (Input.GetKey(KeyCode.DownArrow) && (mode.mode == 1 || mode.mode == 0)))
                        {
                            frontback = -LinearSpeed;
                        }
                        if (Input.GetKeyUp(KeyCode.DownArrow))
                        {
                            frontback = 0;
                        }
                    }
                    //

                    if (mode.mode == 2)
                    {
                        if (((LinearOrRot == 2 && rotation != 0) || (LinearOrRot == 1 && frontback != 0)) && moover_sw == 1)
                        {
                            zerotime = 0.0f;
                        }
                        if (frontback == 0 && rotation == 0)
                        {
                            adapter1 = 1.0f;
                            adapter2 = 0.0f;
                            rotadapter = 0.0f;
                        }
                        if (zerotime >= Time_Delay + 3.0f && synchronization_sw == true)
                        {
                            moover_sw = 0;
                            frontback = 0.0f;
                            rotation = 0.0f;
                            adapter1 = 1.0f;
                            adapter2 = 0.0f;
                            rotadapter = 0.0f;
                            LinearOrRot = 0;
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
                    }
                    if (timeElapsed >= publishMessageInterval)
                    {
                        if (mode.mode == 0 || mode.mode == 1)
                        {
                            PrevLinearCMD = frontback;
                            PrevAngularCMD = rotation;
                        }
                        else if (mode.mode == 2)
                        {
                            PrevLinearCMD = CMD_linear_list[CMD_linear_list.Count - 1];
                            PrevAngularCMD = CMD_anglar_list[CMD_anglar_list.Count - 1];
                        }
                        vel_linear_acceleration = (frontback - PrevLinearCMD) / (publishMessageInterval);
                        if (vel_linear_acceleration > max_lnear_accel_per_pub && frontback >= (PrevLinearCMD + max_lnear_accel_per_pub))
                        {
                            frontback = PrevLinearCMD + max_lnear_accel_per_pub;
                        }
                        else if (vel_linear_acceleration < max_lnear_deceleration_per_pub && frontback <= (PrevLinearCMD + max_lnear_accel_per_pub))
                        {
                            frontback = PrevLinearCMD + max_lnear_deceleration_per_pub;
                        }
                        vel_angular_acceleration = (rotation - PrevAngularCMD) / (publishMessageInterval);
                        if (vel_angular_acceleration > max_angular_accel_per_pub && rotation >= (PrevAngularCMD + max_angular_accel_per_pub))
                        {
                            rotation = PrevAngularCMD + max_angular_accel_per_pub;
                        }
                        else if (vel_angular_acceleration < max_angular_deceleration_per_pub && rotation <= (PrevAngularCMD + max_angular_accel_per_pub))
                        {
                            rotation = PrevAngularCMD + max_angular_deceleration_per_pub;
                        }
                        if (mode.mode == 0)
                        {
                            if (UseRos2Topic == true)
                            {
                                diffDriveController.ControlMode = 0;
                            }
                            else if (UseRos2Topic == false)
                            {
                                diffDriveController.ControlMode = 1;
                                diffDriveController.LinearCMD = frontback;
                                diffDriveController.AngularCMD = rotation;
                            }
                            timeElapsed = 0.0f;
                        }

                        if (mode.mode == 1)
                        {
                            linear.x = frontback;
                            angular.z = rotation;
                            TwistMsg Twist = new TwistMsg(linear, angular);
                            //  Debug.Log("Publish On Time");
                            ros.Publish(TwistPublishTopicName, Twist);
                            timeElapsed = 0.0f;
                        }
                        if (mode.mode == 2)
                        {
                            CMD_linear_list.Add(frontback);
                            CMD_linear_list_for_cyber.Add(frontback * adapter1 + adapter2);
                            CMD_anglar_list.Add(rotation);
                            CMD_anglar_list_for_cyber.Add(rotation + rotadapter);
                            diffDriveController.LinearCMD = frontback * adapter1 + adapter2;
                            diffDriveController.AngularCMD = rotation + rotadapter;
                            int CMD_time = Mathf.RoundToInt(Time_Delay / publishMessageInterval);
                            if (timeElapsed_start > (Time_Delay + 5.0f) && CMD_linear_list.Count - (CMD_time) - 1 >= 0 && CMD_anglar_list.Count - (CMD_time) - 1 >= 0)
                            {
                                linear.x = CMD_linear_list[CMD_linear_list.Count - CMD_time - 1];
                                angular.z = CMD_anglar_list[CMD_anglar_list.Count - CMD_time - 1];
                                TwistMsg Twist = new TwistMsg(linear, angular);
                                // Debug.Log("Publish After Delay Time");
                                ros.Publish(TwistPublishTopicName, Twist);

                            }
                            timeElapsed = 0.0f;
                        }
                    }

                    if (mode.mode == 1)
                    {
                        moover_sw = 1;
                        if (prev_control_mode != 1)
                        {
                            emergency = true;
                            prev_control_mode = 1;
                        }
                    }

                    if (mode.mode == 2)
                    {
                        if (prev_control_mode != 2)
                        {
                            emergency = true;
                            prev_control_mode = 2;
                        }



                        // Debug.Log(RealPosition);
                        //  Debug.Log(RealPosition.newPosition);
                        newPosition = RealPosition.MapMachinePosition;
                        newRotation = RealPosition.MapMachineRotation;
                        dissconnect_timer = 0.0f;

                        if (mode.mode == 2 && OnOffSw == ONOFF.On) //Controll mode (Pose modify)
                        {
                            DateTime currentTime = DateTime.Now;
                            if (currentTime >= nextActionTime)
                            {
                                posi_list.Add(targetObject.transform.position);
                                rotation_for_list = targetObject.transform.rotation;
                                rotation_list.Add(rotation_for_list.eulerAngles);
                                CMD_Calculator(newPosition, newRotation, DateTime.Now, timeElapsed_adopt_starter);
                            }
                        }
                    }
                }
            }


            if (mode.mode == 2 && RecordPlaySw == true)
            {

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
                    cmdLinearVel = (double)(CMD_Recorder.RecordList[RecordCounter][1] * adapter1 + adapter2);
                    cmdAngularVel = (double)(CMD_Recorder.RecordList[RecordCounter][2] + rotadapter);
                    if (Math.Abs(CMD_Recorder.RecordList[RecordCounter][1]) <= 0.001)
                    {
                        cmdLinearVel = 0.0f;
                        cmdAngularVel = 0.0f;
                    }
                    if (Math.Abs(CMD_Recorder.RecordList[RecordCounter][2]) <= 0.0000000000000000001)
                    {
                        //  cmdAngularVelForPlay = 0.0f;
                    }
                    diffDriveController.LinearCMD = (float)cmdLinearVel;
                    diffDriveController.AngularCMD = (float)cmdAngularVel;
                }
                if (RecordCounter == (CMD_Recorder.RecordList).Count - 1)
                {
                    //  RecordCounter = 0;
                }
                //    Debug.Log("PublishingLinear" + cmdLinearVel + " : " + adapter1 + " : " + adapter2 + " : " + rotadapter);
                //    Debug.Log("PublishingAngular" + cmdAngularVel);


                // Debug.Log(RealPosition);
                //  Debug.Log(RealPosition.newPosition);
                newPosition = RealPosition.MapMachinePosition;
                newRotation = RealPosition.MapMachineRotation;
                dissconnect_timer = 0.0f;

                if (currentTime >= nextActionTime)
                {
                    posi_list.Add(targetObject.transform.position);
                    rotation_for_list = targetObject.transform.rotation;
                    rotation_list.Add(rotation_for_list.eulerAngles);
                    CMD_Calculator(newPosition, newRotation, DateTime.Now, timeElapsed_adopt_starter);
                }


                timeElapsed_adopt_starter += Time.deltaTime;
                timeElapsed_start += Time.deltaTime;
                zerotime += Time.deltaTime;
                Stop_time += Time.deltaTime;
                dissconnect_timer += Time.deltaTime;
            }
        }
    }


    void CMD_Calculator(Vector3 RealPosition, Quaternion realRotation, DateTime NowTime, float TimeElapsed_adopt_starter_param)
    {
        // Debug.Log("CMD_Calculator");
        real_posi_list.Add(RealPosition);
        Vector3 RealRotation = realRotation.eulerAngles;
        real_rotation_list.Add(realRotation * Vector3.forward);
        real_diff_anglar_list.Add(Vector3.SignedAngle(real_rotation_list[real_rotation_list.Count - 2], real_rotation_list[real_rotation_list.Count - 1], Vector3.up));
        real_posi_length_list.Add(Vector3.Distance(real_posi_list[real_posi_list.Count - 1], real_posi_list[real_posi_list.Count - 2]));//実機の前フレームからの進んだ距離
        real_posi_length_list_x.Add((real_posi_list[real_posi_list.Count - 1][0]) - (real_posi_list[real_posi_list.Count - 2][0]));//world座標のx方向に進んだ距離
        real_posi_length_list_z.Add((real_posi_list[real_posi_list.Count - 1][2]) - (real_posi_list[real_posi_list.Count - 2][2]));//world座標のz方向に進んだ距離

        // Debug.Log("Real : "+ RealPosition+" : " + targetObject.transform.position);

        if (TimeElapsed_adopt_starter_param >= Time_Delay)
        {
            last_time = Mathf.RoundToInt(Time_Delay / (intervalInMilliseconds / 1000));//ラグ時間前のリストの数
            if (((LinearOrRot == 1) && ((real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2])) < 1.3 && ((real_posi_length_list[real_posi_length_list.Count - 1]) / (real_posi_length_list[real_posi_length_list.Count - 2])) > 0.7))//|| (RecordPlaySw == true))
            {
                real_pose_length = 0.0f;
                real_pose_length_x = 0.0f;
                real_pose_length_z = 0.0f;
                counter = 0;
                for (int i = real_posi_length_list.Count - 1; i >= (real_posi_length_list.Count - last_time - 1); i--)
                {
                    real_pose_length = real_pose_length + real_posi_length_list[i];//実空間の重機のラグ時間に進んだ距離
                    real_pose_length_x = real_pose_length_x + real_posi_length_list_x[i];
                    real_pose_length_z = real_pose_length_z + real_posi_length_list_z[i];
                    counter += 1;
                }

                Vector3 Real_future_pose = new Vector3(RealPosition[0] + real_pose_length_x, 0.0f, RealPosition[2] + real_pose_length_z);
                Vector2 Real_Cyber_future_diff_pose = new Vector2((posi_list[posi_list.Count - 1][0]) - Real_future_pose[0], (posi_list[posi_list.Count - 1][2]) - Real_future_pose[2]);//サイバー空間の重機のモデルと実空間の重機の未来の位置の差
                Vector3 Real_forwardVector = realRotation * Vector3.forward; // Z軸方向
                Vector3 Real_forwardVector_normal = Real_forwardVector.normalized;
                Real_Cyber_future_length_pose = Vector2.Dot(new Vector2(Real_forwardVector_normal[0], Real_forwardVector_normal[2]), Real_Cyber_future_diff_pose);//実機の進行方向の実機とモデルの差
                ///
                Real_Cyber_future_length_pose_compare.Add(Real_Cyber_future_length_pose);

                Debug.Log("diffpose" + Real_Cyber_future_length_pose + "  bector_length   " + Real_Cyber_future_diff_pose);
                if (Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 1] > Real_Cyber_future_length_pose_compare[Real_Cyber_future_length_pose_compare.Count - 2] && Real_Cyber_future_length_pose > 0)
                {
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
                    if (Math.Abs(Real_Cyber_future_length_pose) >= Margin)
                    {
                        adapter2 += 0.2f;
                    }
                    else if (Math.Abs(Real_Cyber_future_length_pose) > 0.01 && Math.Abs(Real_Cyber_future_length_pose) < Margin)
                    {
                        adapter2 += 0.01f;
                    }

                    Debug.Log("accel:" + adapter2);
                }
                Vector3 toTarget = posi_list[posi_list.Count - 1] - RealPosition;
                float angle = Vector3.SignedAngle(Real_forwardVector, toTarget, Vector3.up);
                // Debug.Log("角の差 " + angle);
                side_diff = (Vector3.Distance(RealPosition, posi_list[posi_list.Count - 1])) * (float)Math.Sin(angle * Math.PI / 180);

                if (Math.Abs(side_diff) > Margin)
                {
                    Vector3 Real_forward = (real_posi_list[real_posi_list.Count - 1] - real_posi_list[real_posi_list.Count - 2]);//実機の進んだx,y,z距離
                    float direction = Vector2.Dot(new Vector2(Real_forwardVector[0], Real_forwardVector[2]), new Vector2(Real_forward[0], Real_forward[2]));//実機とサイバー空間の重機の前方の角度差
                    Vector3 targetdirection = new Vector3((targetObject.transform.position[0] - (RealPosition[0] + (Real_forwardVector_normal * (Real_Cyber_future_length_pose + 2 * real_pose_length))[0])), 0.0f, (targetObject.transform.position[2] - (RealPosition[2] + (Real_forwardVector_normal * (Real_Cyber_future_length_pose + 2 * real_pose_length))[2])));//実機とモデルの未来の位置とモデルの位置との差
                    Vector3 Cyber_front_vec = new Vector3((float)(targetObject.transform.rotation * Vector3.forward)[0], 0.0f, (float)(targetObject.transform.rotation * Vector3.forward)[2]);//モデルの前方                 
                    float Cyber_angle = Vector3.SignedAngle(Cyber_front_vec, -targetdirection, Vector3.up);

                    //   Debug.Log("実機前方" + (realRotation * Vector3.forward) + " 目標ベクトル " + -targetdirection+"横ずれ"+ side_diff);
                    //   Debug.Log("モデル前方 : " + Cyber_front_vec + " 目標ベクトル " + -targetdirection);
                    if (Math.Abs(side_diff) > Margin && Math.Abs(Cyber_angle) > Angular_Margin)
                    {
                        if (direction >= 0)//yellow
                        {
                            if (-Cyber_angle < 0)
                            {
                                rotadapter = -Cyber_angle * (float)((Math.PI) / 180.0f);
                                //    Debug.Log("1:rotadapter -= 0.1f " + rotadapter + "角度の差は " + Cyber_angle);
                            }
                            else if (-Cyber_angle >= 0)
                            {
                                rotadapter = -Cyber_angle * (float)((Math.PI) / 180.0f);
                                //    Debug.Log("2:rotadapter += 0.1f " + rotadapter + "角度の差は " + Cyber_angle);
                            }
                        }
                        else if (direction < 0)//blue
                        {
                            if (-Cyber_angle < 0)
                            {
                                rotadapter = -Cyber_angle * (float)((Math.PI) / 180.0f);
                                //    Debug.Log("3:rotadapter += 0.1f " + rotadapter + "角度の差は " + Cyber_angle);
                            }
                            else if (-Cyber_angle >= 0)
                            {
                                rotadapter = -Cyber_angle * (float)((Math.PI) / 180.0f);
                                //    Debug.Log("4:rotadapter -= 0.1f " + rotadapter + "角度の差は " + Cyber_angle);
                            }
                        }
                    }
                }
            }
            last_rotation = rotation_list[rotation_list.Count - last_time];
            diff_rot = last_rotation - RealRotation;
            if ((LinearOrRot == 2))
            {
                real_anglar_length = 0.0f;
                counter = 0;
                for (int i = real_diff_anglar_list.Count - 1; i > (real_diff_anglar_list.Count - last_time); i--)
                {
                    real_anglar_length = real_anglar_length + real_diff_anglar_list[i];
                    counter += 1;
                }
                Vector3 Real_forwardVector = realRotation * Vector3.forward;//実機の前方
                Vector3 toTarget = rotation_for_list * Vector3.forward;//サイバー空間のモデルの前方
                float angle = Vector3.SignedAngle(toTarget, Real_forwardVector, Vector3.up);//重機とサイバー空間のモデルの角度差
                //
                if (Math.Abs(Real_Cyber_future_anglar_diff - Real_Cyber_future_length_anglar_compare[Real_Cyber_future_length_anglar_compare.Count - 1]) > Math.Abs(Real_Cyber_future_anglar_diff - Real_Cyber_future_length_anglar_compare[Real_Cyber_future_length_anglar_compare.Count - 1] - 360))
                {
                    Real_Cyber_future_anglar_diff = Real_Cyber_future_anglar_diff - 360;
                }
                Real_Cyber_future_length_anglar_compare.Add(Real_Cyber_future_anglar_diff);
                if (real_anglar_length + angle < 0)
                {
                    rotadapter += 0.01f;
                }
                else if (real_anglar_length + angle >= 0)
                {
                    rotadapter -= 0.01f;
                }
                // Debug.Log("角度の差は " + (real_anglar_length + angle));
                // Debug.Log("diffrot" + Real_Cyber_future_anglar_diff);// diff_rot[1]);
                // Debug.Log("rotadapter" + rotadapter);
            }
            if (moover_sw == 2)
            {
                //  Debug.Log("vrcont_zerostop");
                if ((Vector3.Distance(real_posi_list[real_posi_list.Count - 1], targetObject.transform.position)) >= Margin)
                {
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
            nextActionTime = NowTime.AddMilliseconds(intervalInMilliseconds);
        }
    }



    private void EmergencyStop()
    {
        linear.x = 0;
        angular.z = 0;

        ros.Publish(EmergencyTopicName, new BoolMsg(true));
        ros.Publish(TwistPublishTopicName, new TwistMsg(linear, angular));

        frontback = 0;
        rotation = 0;

        diffDriveController.LinearCMD = 0;
        diffDriveController.AngularCMD = 0;
    }
    private void ReadInput()
    {
        Vector2 stick = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);

        frontback = LinearSpeed * stick.y;
        rotation = -RotSpeed * stick.x;

        if (!key) return;

        if (Input.GetKey(KeyCode.UpArrow))
            frontback = LinearSpeed;
        else if (Input.GetKey(KeyCode.DownArrow))
            frontback = -LinearSpeed;

        if (Input.GetKey(KeyCode.LeftArrow))
            rotation = RotSpeed;
        else if (Input.GetKey(KeyCode.RightArrow))
            rotation = -RotSpeed;
    }
}