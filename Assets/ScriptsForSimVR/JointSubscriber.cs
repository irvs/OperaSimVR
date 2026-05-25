using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;

public class JointSubscriber : MonoBehaviour
{
    public bool ViaDB;
    public bool JointChengeSw;
    private float dissconnect_timer;
    GameObject targetObject;
    public string JointSubscribeTopicName;
    public string ViaDBSubscribeTopicName;
    private string SubscribeJointTopicName;
    private mode_selector mode;
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
        SubscribeJointTopicName = JointSubscribeTopicName;
        if (ViaDB == true)// || SimORRealSelecter.ViaDB == true)
        {
            SubscribeJointTopicName = ViaDBSubscribeTopicName;
        }
        Debug.Log("check:joint_states_pub");
        // ROSコネクションへのサブスクライバーの登録
        ros.Subscribe<JointStateMsg>(SubscribeJointTopicName, Callback);
        Debug.Log("already:joint_states_pub");
        ///
        JointPositions = new List<double> { 0.0f, 0.0f, 0.0f, 0.0f};
    }
    void Update()
    {
        dissconnect_timer += Time.deltaTime;
    }

    void Callback(JointStateMsg msg)
    {
        mode = FindObjectOfType<mode_selector>();
        dissconnect_timer = 0.0f;

        if (mode.mode == 1)//Visual tool
        {
            JointPositions[0] = msg.position[0];
            JointPositions[1] = msg.position[1];
            JointPositions[2] = msg.position[2];
            JointPositions[3] = msg.position[3];
            //
            if (JointChengeSw == true)
            {
                SwingObject.transform.localRotation = Quaternion.Euler(0, -(float)(JointPositions[0] * Mathf.Rad2Deg - OffsetSwing), 0);
                BoomObject.transform.localRotation = Quaternion.Euler((float)(JointPositions[1] * Mathf.Rad2Deg - OffsetBoom), 0, 0);
                ArmObject.transform.localRotation = Quaternion.Euler((float)(JointPositions[2] * Mathf.Rad2Deg - OffsetArm), 0, 0);
                BucketObject.transform.localRotation = Quaternion.Euler((float)(JointPositions[3] * Mathf.Rad2Deg) + OffsetBucket, 0, 0);

            }
        }
    }
}
