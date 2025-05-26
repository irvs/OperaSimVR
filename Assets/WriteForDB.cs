using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.TmsMsgDb;
using RosMessageTypes.TmsMsgUr;


public class WriteForDB : MonoBehaviour
{
    ROSConnection ros;
    public bool SendRequestSw;
    private string serviceName = "/input/DB_machine/data";
    private string DBWriteTopicName = "/input/writeDB/pose_joint";
    private float awaitingResponseUntilTimestamp;
    public bool AcceptedRequest = false;
    public float requestInterval = 5.0f;

    private string MachineName;
    private string MachineKinds;
    private string RecordName;
    private string RecordType;
    private string WriteTarget;
    private List<double> targetpose;
    private List<double> targetrot;
    public bool PubsubOrSrv;

    // �T�[�r�X���N�G�X�g�𑗐M
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
    }

    // Update is called once per frame
    void Update()
    {
        if (SendRequestSw == true)
        {

            MachineName = "ic120";
            MachineKinds = "ic120";
            RecordType = "joint";
            WriteTarget = "test_PATH";
            //RecordName = "";
            targetpose = new List<double> { 0.0f, 0.0f, 0.0f};
            targetrot = new List<double> { 0.0f, 0.0f, 0.0f, 0.0f};
            float SwingPos = 0.0f;
            float BoomPos = 0.0f;
            float ArmPos = 0.0f;
            float BucketPos = 0.0f;
            float BucketEnd = 0.0f;

            if (Time.time > awaitingResponseUntilTimestamp && AcceptedRequest == false)
            {
                if (PubsubOrSrv == false)
                {
                    TmsdbTerrainDBPoseWriteSrvRequest DBWriterequest = new TmsdbTerrainDBPoseWriteSrvRequest();
                    DBWriterequest.machine_name = MachineName;

                    DBWriterequest.machine_kinds = MachineKinds;
                    //   DBWriterequest.record_name = RecordName;
                    DBWriterequest.record_type = RecordType;
                    DBWriterequest.write_target = WriteTarget;


                    DBWriterequest.pose.pose.position.x = targetpose[0];
                    DBWriterequest.pose.pose.position.y = targetpose[1];
                    DBWriterequest.pose.pose.position.z = targetpose[2];
                    DBWriterequest.pose.pose.orientation.x = targetrot[0];
                    DBWriterequest.pose.pose.orientation.y = targetrot[1];
                    DBWriterequest.pose.pose.orientation.z = targetrot[2];
                    DBWriterequest.pose.pose.orientation.w = targetrot[3];

                    DBWriterequest.joints.position = new double[5];
                    DBWriterequest.joints.position[0] = SwingPos;
                    DBWriterequest.joints.position[1] = BoomPos;
                    DBWriterequest.joints.position[2] = ArmPos;
                    DBWriterequest.joints.position[3] = BucketPos;
                    DBWriterequest.joints.position[4] = BucketEnd;
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

                    DBWritedata.pose.pose.position.x = targetpose[0];
                    DBWritedata.pose.pose.position.y = targetpose[1];
                    DBWritedata.pose.pose.position.z = targetpose[2];
                    DBWritedata.pose.pose.orientation.x = targetrot[0];
                    DBWritedata.pose.pose.orientation.y = targetrot[1];
                    DBWritedata.pose.pose.orientation.z = targetrot[2];
                    DBWritedata.pose.pose.orientation.w = targetrot[3];

                    DBWritedata.joints.position = new double[5];
                    DBWritedata.joints.position[0] = SwingPos;
                    DBWritedata.joints.position[1] = BoomPos;
                    DBWritedata.joints.position[2] = ArmPos;
                    DBWritedata.joints.position[3] = BucketPos;
                    DBWritedata.joints.position[4] = BucketEnd;
                    ros.Publish(DBWriteTopicName, DBWritedata);
                    Debug.Log("Publish");
                }
                               
                awaitingResponseUntilTimestamp = Time.time + requestInterval;
                Debug.Log("Service Requested");
            }
        }


    }

    void OnServiceResponse(TmsdbTerrainDBPoseWriteSrvResponse response)
    {
        Debug.Log("Written!");

        AcceptedRequest = true;

    }
}
