
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.Core;
using RosMessageTypes.TmsMsgDb;
using RosMessageTypes.TmsMsgUr;
using RosMessageTypes.Shape;
using Assimp;

public class WriteForDB : MonoBehaviour
{
    ROSConnection ros;
    public bool SendRequestSw;
    private string serviceName = "/input/DB_machine/data";
    private float awaitingResponseUntilTimestamp;
    private bool AcceptedRequest = false;
    public float requestInterval = 5.0f;

    private string MachineName;
    private string MachineKinds;
    private string RecordName;
    private string RecordType;
    private string WriteTarget;
    private List<double> targetpose;
    private List<double> targetrot;

    // サービスリクエストを送信
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<TmsdbTerrainDBPoseWriteSrvRequest, TmsdbTerrainDBPoseWriteSrvResponse>(serviceName);
    }

    // Update is called once per frame
    void Update()
    {
        if (SendRequestSw == true)
        {

            MachineName = "ic120";
            MachineKinds = "ic120";
            RecordType = "Position";
            WriteTarget = "test_PATH";
            //RecordName = "";
            targetpose = new List<double> { 0.0f, 0.0f, 0.0f };
            targetrot = new List<double> { 0.0f, 0.0f, 0.0f, 0.0f };

            if (Time.time > awaitingResponseUntilTimestamp && AcceptedRequest == false)
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

                ros.SendServiceMessage<TmsdbTerrainDBPoseWriteSrvResponse>(serviceName, DBWriterequest, OnServiceResponse);
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
