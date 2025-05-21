using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class Recorder : MonoBehaviour
{
    ROSConnection ros;
    public string SimPhysXSubscribeTopicName;
    private string SRSubscribeTopicName;

    private float cmdLinearVel;
    private float cmdAngularVel;
    private Vector3 Current_Record;
    
    public bool WriteSw;
    public bool RecordPlaySw;
    
    public List<Vector3> RecordList = new List<Vector3>();
    public List<long> TimeStampList = new List<long>();
    private long PlayDeltaTime;

    // Start is called before the first frame update
    void Start()
    {
        SRSubscribeTopicName = SimPhysXSubscribeTopicName;
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(SRSubscribeTopicName, Callback);
    }

    void Callback(TwistMsg msg)
    {
        if (WriteSw == true) 
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

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int score;
}
public class DataManager : MonoBehaviour
{
    public List<PlayerData> playerDataList = new List<PlayerData>();

    // データをJSON形式で保存
    public void SaveData()
    {
        string json = JsonUtility.ToJson(new Serialization<PlayerData>(playerDataList));
        System.IO.File.WriteAllText(Application.persistentDataPath + "/playerData.json", json);
    }

    // データを読み込む
    public void LoadData()
    {
        string filePath = Application.persistentDataPath + "/playerData.json";
        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);
            playerDataList = JsonUtility.FromJson<Serialization<PlayerData>>(json).ToList();
        }
    }
}
[System.Serializable]
public class Serialization<T>
{
    public List<T> items;

    public Serialization(List<T> items)
    {
        this.items = items;
    }

    public List<T> ToList()
    {
        return items;
    }
}
public class GameManager : MonoBehaviour
{
    private DataManager dataManager;

    private void Start()
    {
        dataManager = GetComponent<DataManager>();
        dataManager.LoadData();  // プレイ開始時にデータを読み込む
    }

    private void OnApplicationQuit()
    {
        dataManager.SaveData();  // アプリケーション終了時にデータを保存
    }
}

