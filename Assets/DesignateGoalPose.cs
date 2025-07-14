using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using System.Collections;
using System.Collections.Generic;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class DesignateGoalPose : MonoBehaviour
{
    ROSConnection ros;
    public bool SendSw;
    public string SendTopicName;
    private PoseStampedMsg message;
    TerrainCollisionPoint HitPoint;
    Vector3 GoalPosition;
    List<Vector3> ConbinePoints = new List<Vector3>();
    FieldMainManager FieldManager;
    Vector3 xAxis = Vector3.right;   // (1, 0, 0)
    Vector3 yAxis = Vector3.up;      // (0, 1, 0)
    Vector3 zAxis = Vector3.forward; // (0, 0, 1)
    Quaternion GoalAngle;
    public GameObject ArrayPrefab;  // 衝突位置に生成するArrayのPrefab

    // Start is called before the first frame update
    void Start()
    {
        HitPoint = FindObjectOfType<TerrainCollisionPoint>();
        FieldManager = FindObjectOfType<FieldMainManager>();
        ConbinePoints = HitPoint.PathForDB;
        message = new PoseStampedMsg();
        message.header = new HeaderMsg();
        message.header.stamp = new TimeMsg();
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(SendTopicName);
    }

    // Update is called once per frame
    void Update()
    {
        //if (ConbinePoints.Count>0) { CreateSphereAtCollisionPoint(ConbinePoints[0]); }
        if (SendSw && ConbinePoints[1] != null)
        {
            HitPoint.ConbineTwePoints = true;

            // Unity -> ROS transformation
            // Position: Unity(x,y,z) -> ROS(z,-x,y)
            // Quaternion: Unity(x,y,z,w) -> ROS(-z,x,-y,w)
            GoalPosition = new Vector3(ConbinePoints[0].z, -ConbinePoints[0].x, ConbinePoints[0].y);

            Vector3 direction = (ConbinePoints[1] - ConbinePoints[0]).normalized;
            float angleWithX = Mathf.Acos(Vector3.Dot(direction, xAxis)) * Mathf.Rad2Deg;
            float angleWithY = Mathf.Acos(Vector3.Dot(direction, yAxis)) * Mathf.Rad2Deg;
            float angleWithZ = Mathf.Acos(Vector3.Dot(direction, zAxis)) * Mathf.Rad2Deg;
            //GoalAngle = Quaternion.Euler(angleWithX, angleWithY, angleWithZ);
            Debug.Log("Angle : "+angleWithX+","+ angleWithY+","+ angleWithZ);
            GoalAngle = Quaternion.Euler(0.0f, angleWithX, 0.0f);
            GoalAngle = new Quaternion(-GoalAngle.z, GoalAngle.x, -GoalAngle.y, GoalAngle.w);
            Quaternion ArrayGoalAngle = Quaternion.Euler(-90.0f, angleWithX,  -90.0f);
            ArrayGoalAngle = new Quaternion(-GoalAngle.z, GoalAngle.x, -GoalAngle.y, GoalAngle.w);

            if (FieldManager.ForSimOrReal.ToString() == "ForSimPhysX")
            {

            }
            else if (FieldManager.ForSimOrReal.ToString() == "ForReal")
            {

            }
            CreateSphereAtCollisionPoint(ConbinePoints[0], ArrayGoalAngle);

            message.header.frame_id = "map";
            message.header.stamp = new TimeStamp(Clock.time);

            message.pose.position.x = GoalPosition.x;
            message.pose.position.y = GoalPosition.y;
            message.pose.position.y = GoalPosition.y;

            message.pose.orientation.x = this.transform.rotation.x;
            message.pose.orientation.y = this.transform.rotation.y;
            message.pose.orientation.z = this.transform.rotation.z;
            message.pose.orientation.w = this.transform.rotation.w;
            Debug.Log(GoalPosition + " : " + GoalAngle);

            ros.Publish(SendTopicName, message);
            SendSw = false;
        }
    }

    void CreateSphereAtCollisionPoint(Vector3 position ,Quaternion rotation)
    {
        // 球のPrefabが設定されている場合にインスタンス化
        if (ArrayPrefab != null)
        {
            // 球を生成
            GameObject sphere = Instantiate(ArrayPrefab, position, rotation);
            /*
            // 生成した球をリストに追加
            ArrayPrefab.Add(sphere);
            /*
            // もしリストに3つ以上のオブジェクトがある場合、古いものを削除
            if (ArrayPrefab.Count > 1)
            {
                // 最も古いオブジェクトを削除
                Destroy(ArrayPrefab[0]);
                // リストから削除
                ArrayPrefab.RemoveAt(0);
            }
            */
        }
    }
}
