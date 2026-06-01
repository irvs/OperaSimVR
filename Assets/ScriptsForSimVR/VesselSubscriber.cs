using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class VesselSubscriber : MonoBehaviour
{
    public bool ViaDB;
    private ROSConnection ros;
    public string VesselSubscriberTopicName = "dump/cmd";
    public string ViaDBVesselSubscriberTopicName;
    private string SubscriberTopicName;
    public ArticulationBody dump_joint;
    private float angle_of_vessel;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        dump_joint = this.GetComponent<ArticulationBody>();

        if (dump_joint)
        {
            var drive = dump_joint.xDrive;
            drive.stiffness = 100000;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            dump_joint.xDrive = drive;
        }
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        if (ViaDB == true) 
        { 
            SubscriberTopicName = ViaDBVesselSubscriberTopicName; 
        }
        else if (ViaDB == false) 
        {
            SubscriberTopicName = VesselSubscriberTopicName; 
        }
        ros.Subscribe<JointStateMsg>(SubscriberTopicName, ExecuteVesselControl);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ExecuteVesselControl(JointStateMsg msg)
    {
        if (msg.position.Length > 1)
        {
            angle_of_vessel = (float)msg.position[1];
            // Debug.Log("Dump Target Position:" + angle_of_vessel);
            var drive = dump_joint.xDrive;
            drive.target = (float)(angle_of_vessel * Mathf.Rad2Deg);
            dump_joint.xDrive = drive;
        }
        else
        {
           // Debug.LogWarning("Received position array does not have enough elements");
        }
    }
}
