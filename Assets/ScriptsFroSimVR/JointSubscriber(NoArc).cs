using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;

public class JointSubscriberNoArc : MonoBehaviour
{
    public bool ViaDB;
    public bool JointChengeSw;
    private double pos_of_swing_joint;
    private double pos_of_boom_joint;
    private double pos_of_arm_joint;
    private double pos_of_bucket_joint;
    private List<string> jointNames;
    private List<ArticulationBody> targetjoints;
    private List<string> targetjointNames;
    private double targetPos;
    private float dissconnect_timer;
    GameObject targetObject;
    public string PhysXSubscribeTopicName;
    public string AGXSubscribeTopicName;
    public string RealSubscribeTopicName;
    public string ViaDBSubscribeTopicName;
    private string SubscribeJointTopicName;
    private mode_selector mode;
    FieldMainManager SimORRealSelecter;
    public List<double> JointPositions;
    ROSConnection ros;
    public GameObject SwingObject;
    public GameObject BoomObject;
    public GameObject ArmObject;
    public GameObject BucketObject;
    public float OffsetSwing;
    public float OffsetBoom;
    public float OffsetArm;
    public float OffsetBucket;

    public float TestBucketAngle;

    void Start()
    {
        targetObject = this.gameObject;
        ros = ROSConnection.GetOrCreateInstance();
        SimORRealSelecter = FindObjectOfType<FieldMainManager>();
        if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
        {
            SubscribeJointTopicName = PhysXSubscribeTopicName;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")
        {
            SubscribeJointTopicName = AGXSubscribeTopicName;
        }
        else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
        {
            SubscribeJointTopicName = RealSubscribeTopicName;
        }
        if (ViaDB == true)// || SimORRealSelecter.ViaDB == true)
        {
            SubscribeJointTopicName = ViaDBSubscribeTopicName;
        }
        Debug.Log("check:joint_states_pub");
        // ROSコネクションへのサブスクライバーの登録
        ros.Subscribe<JointStateMsg>(SubscribeJointTopicName, Callback);
        Debug.Log("already:joint_states_pub");
        ///
        JointPositions = new List<double> { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    }
    void Update()
    {
        dissconnect_timer += Time.deltaTime;
    }

    void Callback(JointStateMsg msg)
    {
        //Debug.Log("joint_subscribe");
        mode = FindObjectOfType<mode_selector>();
        dissconnect_timer = 0.0f;

        if (mode.mode == 1)//Visual tool
        {
            //
            targetjoints = new List<ArticulationBody>();
            targetjointNames = new List<string>();
            //
            pos_of_swing_joint = msg.position[0];
            pos_of_boom_joint = msg.position[1];
            pos_of_arm_joint = msg.position[2];
            pos_of_bucket_joint = msg.position[3];
            JointPositions[0] = msg.position[0];
            JointPositions[1] = msg.position[1];
            JointPositions[2] = msg.position[2];
            JointPositions[3] = msg.position[3];
            JointPositions[4] = msg.velocity[0];
            JointPositions[5] = msg.velocity[1];
            JointPositions[6] = msg.velocity[2];
            JointPositions[7] = msg.velocity[3];
            //
            if (JointChengeSw == true)
            {
                int j = 0;
         //       SwingObject.transform.rotation = Quaternion.Euler(targetObject.transform.rotation.eulerAngles.x, -((float)(JointPositions[0] * 180 / 3.14) - OffsetSwing), targetObject.transform.rotation.eulerAngles.z);
         //       BoomObject.transform.rotation = Quaternion.Euler((float)(JointPositions[1] * 180 / 3.14) - OffsetBoom, SwingObject.transform.rotation.eulerAngles.y, SwingObject.transform.rotation.eulerAngles.z);
         //       ArmObject.transform.rotation = Quaternion.Euler((float)(JointPositions[2] * 180 / 3.14) + (BoomObject.transform.rotation.eulerAngles.x) - OffsetArm, BoomObject.transform.rotation.eulerAngles.y, BoomObject.transform.rotation.eulerAngles.z);
                // BucketObject.transform.rotation = Quaternion.Euler((float)(JointPositions[3] * 180 / 3.14) + (ArmObject.transform.rotation.eulerAngles.x) - OffsetBucket, ArmObject.transform.rotation.eulerAngles.y, ArmObject.transform.rotation.eulerAngles.z);


                SwingObject.transform.localRotation = Quaternion.Euler(0, -(float)(JointPositions[0] * Mathf.Rad2Deg - OffsetSwing), 0);
                BoomObject.transform.localRotation = Quaternion.Euler((float)(JointPositions[1] * Mathf.Rad2Deg - OffsetBoom), 0, 0);
                ArmObject.transform.localRotation = Quaternion.Euler((float)(JointPositions[2] * Mathf.Rad2Deg - OffsetArm), 0, 0);
                BucketObject.transform.localRotation = Quaternion.Euler((float)(JointPositions[3] * Mathf.Rad2Deg) + OffsetBucket, 0, 0);

            }
        }
    }
}

/*
float swingDeg = -(float)(joints[0] * Mathf.Rad2Deg - OffsetSwing);
float boomDeg = (float)(joints[1] * Mathf.Rad2Deg - OffsetBoom);
float armDeg = (float)(joints[2] * Mathf.Rad2Deg - OffsetArm);
float bucketDeg = (float)(joints[3] * Mathf.Rad2Deg - OffsetBucket);

SwingObject.transform.localRotation = Quaternion.Euler(0, -(float)(joints[0] * Mathf.Rad2Deg - OffsetSwing), 0);
BoomObject.transform.localRotation = Quaternion.Euler((float)(joints[1] * Mathf.Rad2Deg - OffsetBoom), 0, 0);
ArmObject.transform.localRotation = Quaternion.Euler((float)(joints[2] * Mathf.Rad2Deg - OffsetArm), 0, 0);
BucketObject.transform.localRotation = Quaternion.Euler((float)(joints[3] * Mathf.Rad2Deg - OffsetBucket), 0, 0);
*/