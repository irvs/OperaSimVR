using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.TmsMsgDb;
using RosMessageTypes.TmsMsgUr;
using Unity.Robotics.UrdfImporter;

public class WriteForDB : MonoBehaviour
{
    ROSConnection ros;
    public bool SendRequestSw;
    public float requestInterval = 5.0f;
    public bool PubsubOrSrv;
    public string WriteTargetObject;
    public enum ModeOption { Position, JointState, PositionQuery }
    public ModeOption DataType;
    private string serviceName = "/input/DB_machine/data";
    private string DBWriteTopicName = "/input/writeDB/pose_joint";
    private float awaitingResponseUntilTimestamp;
    /*public*/ bool AcceptedRequest = false;

    private string MachineName;
    private string MachineKinds;
    public string RecordName;
    private string RecordType;
    private string WriteTarget;
    private Vector3 targetpose;
    private Quaternion targetrot;
    GameObject targetobject;
    ControllerManager From_VR_manager;
    ModelIdentifier MachineManager;
    public bool IsReal;
    private List<double> JointPositions;
    private List<ArticulationBody> joints;
    private List<string> jointNames;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        if (PubsubOrSrv == false)
        {
            ros.RegisterRosService<TmsdbTerrainDBPoseWriteSrvRequest, TmsdbTerrainDBPoseWriteSrvResponse>(serviceName);
        }
        else if (PubsubOrSrv == true)
        {
            ros.RegisterPublisher<TmsdbPoseWriteMsgMsg>(DBWriteTopicName);
        }
        From_VR_manager = FindObjectOfType<ControllerManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (SendRequestSw == true)
        {
            WriteTargetObject = From_VR_manager.Machine_name;
            targetobject = GameObject.Find(WriteTargetObject);
            if (targetobject != null)
            {
                MachineManager = targetobject.GetComponent<ModelIdentifier>();

                MachineName = WriteTargetObject;
                MachineKinds = MachineManager.KindsOfHeavyMachinery.ToString();
                RecordType = DataType.ToString();
                WriteTarget = "test_PATH";
                //RecordName = "";

                if (Time.time > awaitingResponseUntilTimestamp && AcceptedRequest == false)
                {
                    Pose();
                    Joint();
                    if (PubsubOrSrv == false)
                    {
                        TmsdbTerrainDBPoseWriteSrvRequest DBWriterequest = new TmsdbTerrainDBPoseWriteSrvRequest();
                        DBWriterequest.machine_name = MachineName;

                        DBWriterequest.machine_kinds = MachineKinds;
                        //   DBWriterequest.record_name = RecordName;
                        DBWriterequest.record_type = RecordType;
                        DBWriterequest.write_target = WriteTarget;


                        DBWriterequest.pose.pose.position.x = targetpose.x;
                        DBWriterequest.pose.pose.position.y = targetpose.y;
                        DBWriterequest.pose.pose.position.z = targetpose.z;
                        DBWriterequest.pose.pose.orientation.x = targetrot.x;
                        DBWriterequest.pose.pose.orientation.y = targetrot.y;
                        DBWriterequest.pose.pose.orientation.z = targetrot.z;
                        DBWriterequest.pose.pose.orientation.w = targetrot.w;

                        DBWriterequest.joints.position = new double[5];
                        DBWriterequest.joints.position[0] = JointPositions[0];
                        DBWriterequest.joints.position[1] = JointPositions[1];
                        DBWriterequest.joints.position[2] = JointPositions[2];
                        DBWriterequest.joints.position[3] = JointPositions[3];
                        DBWriterequest.joints.position[4] = JointPositions[4];
                        ros.SendServiceMessage<TmsdbTerrainDBPoseWriteSrvResponse>(serviceName, DBWriterequest, OnServiceResponse);
                    }
                    else if (PubsubOrSrv == true)
                    {
                        TmsdbPoseWriteMsgMsg DBWritedata = new TmsdbPoseWriteMsgMsg();
                        DBWritedata.machine_name = MachineName;

                        DBWritedata.machine_kinds = MachineKinds;
                        //   DBWriterequest.record_name = RecordName;
                        DBWritedata.record_type = RecordType;
                        DBWritedata.write_target = WriteTarget;

                        DBWritedata.pose.pose.position.x = targetpose.x;
                        DBWritedata.pose.pose.position.y = targetpose.y;
                        DBWritedata.pose.pose.position.z = targetpose.z;
                        DBWritedata.pose.pose.orientation.x = targetrot.x;
                        DBWritedata.pose.pose.orientation.y = targetrot.y;
                        DBWritedata.pose.pose.orientation.z = targetrot.z;
                        DBWritedata.pose.pose.orientation.w = targetrot.w;

                        DBWritedata.joints.position = new double[5];
                        DBWritedata.joints.position[0] = JointPositions[0];
                        DBWritedata.joints.position[1] = JointPositions[1];
                        DBWritedata.joints.position[2] = JointPositions[2];
                        DBWritedata.joints.position[3] = JointPositions[3];
                        DBWritedata.joints.position[4] = JointPositions[4];
                        ros.Publish(DBWriteTopicName, DBWritedata);
                        Debug.Log("Publish");
                    }
                    awaitingResponseUntilTimestamp = Time.time + requestInterval;
                    Debug.Log("Service Requested");
                }
            }
        }
    }

    void OnServiceResponse(TmsdbTerrainDBPoseWriteSrvResponse response)
    {
        Debug.Log("Written!");
        SendRequestSw = false;
      //  AcceptedRequest = true;
    }

    void Pose()
    {
        if (IsReal == false)
        {
            targetpose.x = targetobject.transform.position.z;
            targetpose.y = -targetobject.transform.position.x;

            targetrot.w = targetobject.transform.rotation.w;
            targetrot.x = targetobject.transform.rotation.x;
            targetrot.y = targetobject.transform.rotation.y;
            targetrot.z = targetobject.transform.rotation.z;
        }
        else if (IsReal == true)
        {
            GameObject Reference = GameObject.Find("MapReferencePoint");
            Vector3 ModifyPosition = targetobject.transform.position - new Vector3(Reference.transform.position.x, 0, Reference.transform.position.z);
            Vector2 WorldMachinePosition = new Vector2(ModifyPosition.x + MachineManager.Offset_z, ModifyPosition.z + MachineManager.Offset_x);
            Vector3 rot_offset = new Vector3(MachineManager.OffsetRotation_x, MachineManager.OffsetRotation_y, MachineManager.OffsetRotation_z);
            Quaternion chenged_orientation = Quaternion.Euler(targetobject.transform.eulerAngles + rot_offset);
            Quaternion NewRotation = new Quaternion(-chenged_orientation.z, -chenged_orientation.x, chenged_orientation.y, -chenged_orientation.w);
            targetpose.x = WorldMachinePosition.x;
            targetpose.y = WorldMachinePosition.y;

            targetrot.x = NewRotation.x;
            targetrot.y = NewRotation.y;
            targetrot.z = NewRotation.z;
            targetrot.w = NewRotation.w;
            Debug.Log(WorldMachinePosition + " , " + NewRotation);
        }
    }

    void Joint()
    {
        joints = new List<ArticulationBody>();
        jointNames = new List<string>();
        foreach (var joint in targetobject.GetComponentsInChildren<ArticulationBody>())
        {
            if (joint.isActiveAndEnabled)
            {
                var ujoint = joint.GetComponent<UrdfJoint>();
                if (ujoint && !(ujoint is UrdfJointFixed))
                {
                    joints.Add(joint);
                    jointNames.Add(ujoint.jointName);
                }
            }
        }
        JointPositions = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
        for (int i = 0; i < joints.Count; i++)
        {
            JointPositions[i] = joints[i].jointPosition[0];
        }
        Debug.Log(WriteTargetObject + " joint publish.");

    }
}
