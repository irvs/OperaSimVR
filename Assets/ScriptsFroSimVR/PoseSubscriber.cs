using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
public class PoseSubscriber : MonoBehaviour
{
    public bool ViaDB;
    public bool WorldToMap;
    public enum PoseMessageType { OdometryMsg, PoseStampedMsg }
    public PoseMessageType PoseMsgType;
    GameObject targetObject;
    public string SimPhysXSubscribeTopicName;
    public string SimAGXSubscribeTopicName;
    public string RealSubscribeTopicName;
    public string ViaDBSubscribeTopicName;
    float offset_x = 0;
    float offset_y = 0;
    float offset_z = 0;
    float rot_offset_x = 0;
    float rot_offset_y = 0;
    float rot_offset_z = 0;
    private Vector3 rot_offset;
    private Vector3 chenged_orientation;
    public bool ChengePosition_sw;

    private mode_selector mode;
    FieldMainManager SimORRealSelecter;
    ROSConnection ros;
    Vector3 newPosition;
    Quaternion newRotation;
    public Vector3 MapMachinePosition;
    public Quaternion MapMachineRotation;
    GameObject SelectorObject;
    Model_name MachineManager;
    private bool IsPapermachine;
    GameObject Reference;
    public enum CoordinateSelect { JapaneseGeodeticSystem, ROS }
    public CoordinateSelect Coordinate;
    //
    Terrain terrain;
    FieldMainManager FieldManager;
    public bool UseHeightFromTopic;
    public bool DumpUpperCorrection;
    public float DumpUpperDiff;
    DumpVesselSub MachineUpperJoints;

    void Start()
    {
        targetObject = this.gameObject;
        SelectorObject = GameObject.Find("FieldManager");
        Reference = GameObject.Find("MapReferencePoint");
        ros = ROSConnection.GetOrCreateInstance();
        Debug.Log("check:baselink/pose");
        if (ViaDB == false)
        {
            // ROSコネクションへのサブスクライバーの登録
            SimORRealSelecter = FindObjectOfType<FieldMainManager>();
            if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimPhysX")
            {
                ros.Subscribe<PoseStampedMsg>(SimPhysXSubscribeTopicName, CallbackPS);
            }
            else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForSimAGX")
            {
                ros.Subscribe<OdometryMsg>(SimAGXSubscribeTopicName, Callback1);
            }
            else if (SimORRealSelecter.ForSimOrReal.ToString() == "ForReal")
            {
                if (PoseMsgType.ToString() == "PoseStampedMsg")
                {
                    ros.Subscribe<PoseStampedMsg>(RealSubscribeTopicName, CallbackPS);
                }
                else if (PoseMsgType.ToString() == "OdometryMsg")
                {
                    ros.Subscribe<OdometryMsg>(RealSubscribeTopicName, CallbackOd);
                }
            }
        }
        else if (ViaDB == true)// || SimORRealSelecter == true)
        {
            if (PoseMsgType.ToString() == "PoseStampedMsg")
            {
                ros.Subscribe<PoseStampedMsg>(ViaDBSubscribeTopicName, CallbackPS);
            }
            else if (PoseMsgType.ToString() == "OdometryMsg")
            {
                ros.Subscribe<OdometryMsg>(ViaDBSubscribeTopicName, CallbackOd);
            }
        }
        MachineManager = GetComponent<Model_name>();
        if (MachineManager.ObjectTypeIsPaperMachine == true)
        {
            IsPapermachine = true;
        }
        Debug.Log("already:baselink/pose");
    }



    void Callback1(OdometryMsg msg)
    {
        mode = FindObjectOfType<mode_selector>();
        if (mode.mode == 1) //Visual tool
        {
            MachineManager = GetComponent<Model_name>();
            offset_x = MachineManager.OffsetList[0];
            //  offset_y = MachineManager.OffsetList[1];
            offset_z = MachineManager.OffsetList[2];
            //Debug.Log(msg.pose.orientation);
            //
            newPosition = new Vector3(((float)msg.pose.pose.position.x + offset_x), ((float)msg.pose.pose.position.z) + offset_z, ((float)msg.pose.pose.position.y) + offset_y);
            newRotation = new((float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.y, (float)msg.pose.pose.orientation.w);
            if (mode.mode == 1) //Visual tool
            {
                //Debug.Log(newPosition);
                //Debug.Log(newRotation.eulerAngles);
                //
                // targetObject.GetComponent<Rigidbody>().isKinematic = false;
                targetObject.transform.position = newPosition - new Vector3(55.24f, 6.3f, 63.6f);
                //
                /*
                float Real_Cyber_angle_diff = Vector3.SignedAngle(targetObject.transform.rotation * Vector3.forward, newRotation * Vector3.forward, Vector3.up);
                if (Math.Abs(Real_Cyber_angle_diff) >= 0.5)
                {
                    targetObject.transform.position = new Vector3(0.0f, newPosition[1] + 0.5f, 0.0f);
                    for (int i = 0; 2.5f * i < (Math.Abs(Real_Cyber_angle_diff)); i++)
                    {
                        targetObject.transform.Rotate(0, 2.5f, 0);
                        Debug.Log("rot_change->"+i);
                    }

                }*/
                //
                rot_offset = new Vector3((float)rot_offset_x, (float)rot_offset_y, (float)rot_offset_z);
                chenged_orientation = newRotation.eulerAngles - rot_offset;
                targetObject.transform.rotation = Quaternion.Euler(chenged_orientation);
                targetObject.GetComponent<Rigidbody>().isKinematic = true;
                //
                //Debug.Log("rot_change_strange" + Real_Cyber_angle_diff);
            }
        }
    }

    void CallbackPS(PoseStampedMsg msg)
    {
        MachineManager = targetObject.GetComponent<Model_name>();
        newPosition = new Vector3(((float)msg.pose.position.x), ((float)msg.pose.position.z), ((float)msg.pose.position.y));
        newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
        PoseCheanger(newPosition, newRotation, MachineManager.Offset_z, MachineManager.Offset_y, MachineManager.Offset_x, MachineManager.OffsetRotation_x, MachineManager.OffsetRotation_y, MachineManager.OffsetRotation_z);
    }

    void CallbackOd(OdometryMsg msg)
    {
        MachineManager = targetObject.GetComponent<Model_name>();
        newPosition = new Vector3(((float)msg.pose.pose.position.x), ((float)msg.pose.pose.position.z), ((float)msg.pose.pose.position.y));
        newRotation = new((float)msg.pose.pose.orientation.y * (-1), (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.w * (-1));
        PoseCheanger(newPosition, newRotation, MachineManager.Offset_z, MachineManager.Offset_y, MachineManager.Offset_x, MachineManager.OffsetRotation_x, MachineManager.OffsetRotation_y, MachineManager.OffsetRotation_z);
    }

    void PoseCheanger(Vector3 NewPosition, Quaternion NewRotation, float OffsetX, float OffsetY, float OffsetZ, float RotOffsetX, float RotOffsetY, float RotOffsetZ)
    {
        Vector3 ModifyPosition = new Vector3((NewPosition.x), (NewPosition.y) - OffsetY, (NewPosition.z));
        if (Coordinate.ToString() == "JapaneseGeodeticSystem")
        {
            ModifyPosition = new Vector3((NewPosition.z), (NewPosition.y) - OffsetY, (NewPosition.x));
            Quaternion XYNewRotation = new Quaternion(NewRotation.x, -NewRotation.w, NewRotation.z, -NewRotation.y);
            NewRotation = XYNewRotation;
        }
        Reference = GameObject.Find("MapReferencePoint");
        if (WorldToMap == true)
        {
            ModifyPosition = new Vector3((NewPosition.x) - OffsetX, (NewPosition.y) - OffsetY, (NewPosition.z - OffsetZ));
            if (Coordinate.ToString() == "JapaneseGeodeticSystem")
            {
                ModifyPosition = new Vector3((NewPosition.z) - OffsetX, (NewPosition.y) - OffsetY, (NewPosition.x) - OffsetZ);
            }
        }
        if (DumpUpperCorrection == true)
        {
            MachineUpperJoints = GetComponent<DumpVesselSub>();
            DumpUpperDiff = MachineUpperJoints.AngleOfSwing;
            Quaternion CorrectRotation = Quaternion.Euler(NewRotation.eulerAngles - new Vector3(0.0f, DumpUpperDiff, 0.0f));
            NewRotation = CorrectRotation;
        }
        rot_offset = new Vector3(RotOffsetX, RotOffsetY, RotOffsetZ);
        chenged_orientation = NewRotation.eulerAngles - rot_offset;
        mode = SelectorObject.GetComponent<mode_selector>();
        if (mode.mode == 1 || (IsPapermachine == true && mode.mode == 2)) //Visual tool
        {
            if (ChengePosition_sw == true)
            {
                Vector3 MapPosition = ModifyPosition + new Vector3(Reference.transform.position.x, 0, Reference.transform.position.z);
                if (UseHeightFromTopic == false)
                {
                    FieldManager = SelectorObject.GetComponent<FieldMainManager>();

                    MapPosition.y = (FieldManager.terrain).SampleHeight(new Vector3(MapPosition.x, MapPosition.y, MapPosition.z)) + (FieldManager.terrain).transform.position.y + OffsetY;
                }
                targetObject.transform.position = MapPosition;
            }
            targetObject.transform.eulerAngles = chenged_orientation;
        }
        MapMachinePosition = new Vector3((NewPosition.x) - OffsetX, (NewPosition.y) - OffsetY, (NewPosition.z - OffsetZ)) + new Vector3(Reference.transform.position.x, 0, Reference.transform.position.z);
        MapMachineRotation = Quaternion.Euler(chenged_orientation);
    }
}