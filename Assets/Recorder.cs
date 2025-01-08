using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;
using System.Drawing.Printing;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

public class Recorder : MonoBehaviour
{
    ROSConnection ros;
    public string SimPhysXSubscribeTopicName;
    private string SRSubscribeTopicName;

    private float cmdLinearVel;
    private float cmdAngularVel;
    private Vector3 Current_Record;
    public List<Vector3> RecordList = new List<Vector3>();
    public bool RecordPlaySw;
    private long PlayDeltaTime;
    public List<long> TimeStampList = new List<long>();

    // Start is called before the first frame update
    void Start()
    {
        SRSubscribeTopicName = SimPhysXSubscribeTopicName;
        ros.Subscribe<TwistMsg>(SRSubscribeTopicName, Callback);
    }

    void Callback(TwistMsg msg)
    {
        cmdLinearVel = (float)msg.linear.x;
        cmdAngularVel = (float)msg.angular.z;
        // 現在の時刻
        DateTime currentTime = DateTime.Now;
        // Unixエポック時間（1970年1月1日からの経過秒数）
        long timestamp = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
        Current_Record = new Vector3(timestamp, cmdLinearVel, cmdAngularVel);
        RecordList.Add(Current_Record);
        TimeStampList.Add(timestamp);

    }

    DateTime UnixTimeToDateTime(long unixTime)
    {
        // Unixエポックは1970年1月1日 00:00:00 UTCからの秒数なので、それを基にDateTimeを作成
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddSeconds(unixTime).ToLocalTime(); // ローカルタイムに変換
    }

    // Update is called once per frame
    void Update()
    {
        
        if (RecordPlaySw == true)
        {
            for (int i = 0; i <= (RecordList.Count - 1); i++)
            {

                // 現在の時刻
                DateTime currentTime = DateTime.Now;
                // Unixエポック時間（1970年1月1日からの経過秒数）
                long timestamp = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();

                if (i == 0)
                {
                    
                    PlayDeltaTime = timestamp - TimeStampList[0];
                }

                if (timestamp - TimeStampList[i] >= PlayDeltaTime)
                {
                    cmdLinearVel = RecordList[i][1];
                    cmdAngularVel = RecordList[i][2];
                }
                

            }


        }
        

    }
}
