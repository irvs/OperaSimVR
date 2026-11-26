using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;

public class JointSubscriberBD : MonoBehaviour
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
    GameObject Bulldozer;
    Transform BladeLiftObject;
    Transform BladeAngleObject;
    Transform BladeTiltObject;
    [Header("Offsets")]
    public float OffsetBladeLift;
    public float OffsetBladeAngle;
    public float OffsetBladeTilt;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        SubscribeJointTopicName = JointSubscribeTopicName;
        if (ViaDB == true){SubscribeJointTopicName = ViaDBSubscribeTopicName;}
        ros.Subscribe<JointStateMsg>(SubscribeJointTopicName, Callback);
        JointPositions = new List<double> { 0.0f, 0.0f, 0.0f};
        mode = FindObjectOfType<ModeSelector>();

        Bulldozer = this.gameObject;
        BladeLiftObject = Bulldozer.transform.Find("base_link/body_link/blade_arm_link");
        BladeAngleObject = Bulldozer.transform.Find("base_link/body_link/boom_link/arm_link");
        BladeTiltObject = Bulldozer.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link");
    }
    void Update()
    {

    }

    void Callback(JointStateMsg msg)
    {
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode || mode.WhichMode == ModeSelector.ModeOption.PreviewAndPlay || mode.WhichMode == ModeSelector.ModeOption.PreviewAR)//Visual tool
        {
            for (int i = 0; i < 3; i++)
                JointPositions[i] = msg.position[i];
            
            if (JointChengeSw == true)
            {
                BladeLiftObject.localRotation = Quaternion.Euler((float)(JointPositions[0] * Mathf.Rad2Deg - OffsetBladeLift), 0, 0);
                BladeAngleObject.localRotation = Quaternion.Euler((float)(JointPositions[1] * Mathf.Rad2Deg - OffsetBladeAngle), 0, 0);
                BladeTiltObject.localRotation = Quaternion.Euler((float)(JointPositions[2] * Mathf.Rad2Deg) + OffsetBladeTilt, 0, 0);
            }
        }
    }
}
