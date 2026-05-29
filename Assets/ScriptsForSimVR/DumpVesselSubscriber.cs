using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class DumpVesselSubscriber : MonoBehaviour
{
    public bool JointChengeSw;
    public bool ViaDB;
    private ROSConnection ros;
    [Header("Topic names")]
    public string VesselSubscriberTopicName = "dump/cmd";
    public string ViaDBVesselSubscriberTopicName = "dump/cmd/db";
    private string SubscriberTopicName;
  //  public ArticulationBody dump_joint;
    public enum JointOption { VesselOnly, SwingAndVessel}
    public JointOption JointSelection;
    public float AngleOfSwing;
    public float AngleOfVessel;
    public int SwingNumber;
    public int VesselNumber;
    public enum SwingPN {Positive, Negative}
    public SwingPN SwingDirection;
    public enum VesselPN {Positive, Negative}
    public VesselPN VesselDirection;
    GameObject RootObject;
    [Header("Objects")]
    public GameObject SwingObject;
    public GameObject VesselObject;
    int SwingInverter = 1;
    int VesselInverter = 1;

    public bool AddOrRemove;
    private ModeSelector mode;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        RootObject = this.gameObject;

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
        if (JointChengeSw && (mode.WhichMode == ModeSelector.ModeOption.PlayMode || mode.WhichMode == ModeSelector.ModeOption.PreviewAndPlay) && msg.position.Length > 1)
        {
            Quaternion RootTransform = RootObject.transform.rotation;
            if (SwingDirection == SwingPN.Negative){SwingInverter = -1;}
            else {SwingInverter = 1;}

            if (VesselDirection == VesselPN.Negative){VesselInverter = -1;}
            else{VesselInverter = 1;}
            if (JointSelection == JointOption.SwingAndVessel)
            {
                AngleOfSwing = (float)(msg.position[SwingNumber] * 180 / 3.14) * SwingInverter;
                AngleOfVessel = -(float)(msg.position[VesselNumber] * 180 / 3.14) * VesselInverter;

                // Debug.Log("Dump Target Position:" + AngleOfVessel);
                if (AddOrRemove == true)
                {
                    SwingObject.transform.rotation =  RootTransform * Quaternion.Euler(transform.rotation.eulerAngles.x, AngleOfSwing, transform.rotation.eulerAngles.z);
                    VesselObject.transform.rotation = RootTransform * Quaternion.Euler(AngleOfVessel, AngleOfSwing, transform.rotation.eulerAngles.z);
                }
                else if (AddOrRemove == false)
                {
                  //  Quaternion delta = Quaternion.Inverse(rotationA) * rotationB;
                    SwingObject.transform.rotation =  Quaternion.Inverse(RootTransform) * Quaternion.Euler(transform.rotation.eulerAngles.x, AngleOfSwing, transform.rotation.eulerAngles.z);
                    VesselObject.transform.rotation = Quaternion.Inverse(RootTransform) * Quaternion.Euler(AngleOfVessel, AngleOfSwing, transform.rotation.eulerAngles.z);
                }
            }
            else if(JointSelection == JointOption.VesselOnly)
            {
                AngleOfVessel = -(float)(msg.position[VesselNumber] * 180 / 3.14) * VesselInverter;
                VesselObject.transform.rotation = RootTransform * Quaternion.Euler(AngleOfVessel, AngleOfSwing, transform.rotation.eulerAngles.z);
            }
            
        }
        else
        {
            // Debug.LogWarning("Received position array does not have enough elements");
        }
    }
}
