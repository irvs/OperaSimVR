using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;

public class JointSubscriber : MonoBehaviour
{
    public bool JointChengeSw;
    public string JointSubscribeTopicName;
    public string ViaDBSubscribeTopicName;
    public bool ViaDB;
    private string SubscribeJointTopicName;
    private ModeSelector mode;
    public List<double> JointPositions;
    ROSConnection ros;
    [Header("Objects")]
    public GameObject SwingObject;
    public GameObject BoomObject;
    public GameObject ArmObject;
    public GameObject BucketObject;
    [Header("Offsets")]
    public float OffsetSwing;
    public float OffsetBoom;
    public float OffsetArm;
    public float OffsetBucket;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        SubscribeJointTopicName = JointSubscribeTopicName;
        if (ViaDB == true){SubscribeJointTopicName = ViaDBSubscribeTopicName;}
        ros.Subscribe<JointStateMsg>(SubscribeJointTopicName, Callback);
        JointPositions = new List<double> { 0.0f, 0.0f, 0.0f, 0.0f};
        mode = FindObjectOfType<ModeSelector>();
    }
    void Update()
    {

    }

    void Callback(JointStateMsg msg)
    {
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode)//Visual tool
        {
            for (int i = 0; i < 4; i++)
                JointPositions[i] = msg.position[i];
            
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
