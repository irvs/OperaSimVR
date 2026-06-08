using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class Recorder : MonoBehaviour
{
    ROSConnection ros;
    public string RecordTopicName;

    private float cmdLinearVel;
    private float cmdAngularVel;
    private Vector3 Current_Record;
    
    public bool WriteSw;
    
    public List<Vector3> RecordList = new List<Vector3>();
    public List<long> TimeStampList = new List<long>();

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(RecordTopicName, Callback);
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

