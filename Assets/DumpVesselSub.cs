using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class DumpVesselSub : MonoBehaviour
{
    public bool ViaDB;
    private ROSConnection ros;
    [Header("Topic names")]
    public string VesselSubscriberTopicName = "dump/cmd";
    public string ViaDBVesselSubscriberTopicName;
    private string SubscriberTopicName;
  //  public ArticulationBody dump_joint;
    public float AngleOfSwing;
    public float AngleOfVessel;
    public int SwingNumber;
    public int VesselNumber;
    [Header("Objects")]
    public GameObject RootObject;
    public GameObject SwingObject;
    public GameObject VesselObject;

    public bool AddOrRemove;
    private ModeSelector mode;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();


        if (ViaDB == true)
        {
            SubscriberTopicName = ViaDBVesselSubscriberTopicName;
        }
        else if (ViaDB == false)
        {
            SubscriberTopicName = VesselSubscriberTopicName;
        }
        ros.Subscribe<JointStateMsg>(SubscriberTopicName, ExecuteVesselControl);
        mode = FindObjectOfType<ModeSelector>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ExecuteVesselControl(JointStateMsg msg)
    {
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode && msg.position.Length > 1)
        {
            AngleOfSwing = (float)(msg.position[SwingNumber] * 180 / 3.14);
            AngleOfVessel = -(float)(msg.position[VesselNumber] * 180 / 3.14);

            // Debug.Log("Dump Target Position:" + AngleOfVessel);
            Quaternion RootTransform = RootObject.transform.rotation;
            if (AddOrRemove == true)
            {
                SwingObject.transform.rotation = RootTransform * Quaternion.Euler(transform.rotation.eulerAngles.x, AngleOfSwing, transform.rotation.eulerAngles.z);
                VesselObject.transform.rotation = RootTransform * Quaternion.Euler(AngleOfVessel, AngleOfSwing, transform.rotation.eulerAngles.z);
            }
            else if (AddOrRemove == false)
            {
              //  Quaternion delta = Quaternion.Inverse(rotationA) * rotationB;
                SwingObject.transform.rotation = Quaternion.Inverse(RootTransform) * Quaternion.Euler(transform.rotation.eulerAngles.x, AngleOfSwing, transform.rotation.eulerAngles.z);
                VesselObject.transform.rotation = Quaternion.Inverse(RootTransform) * Quaternion.Euler(AngleOfVessel, AngleOfSwing, transform.rotation.eulerAngles.z);
            }
        }
        else
        {
            // Debug.LogWarning("Received position array does not have enough elements");
        }
    }
}
