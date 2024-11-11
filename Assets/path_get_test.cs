using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using Unity.Robotics.Core;
using System;
using static UnityEditor.PlayerSettings;
using UnityEngine.WSA;

public class path_get_test : MonoBehaviour
{
    private PathMsg twist;
    public string robotName = "robot_name";
    public GameObject targetObject;
    public string Subscribe_topic_name = "subscribe_topic";
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    private mood_selector mode;
    private List<Vector3> path_list = new List<Vector3>();
    private List<Quaternion> path_angular_list = new List<Quaternion>();
    private Quaternion angular;
    ROSConnection ros;
    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        //Debug.Log("aaaaaaaa");
        twist = new PathMsg();
        Debug.Log("check:baselink/pose");
        // ROSコネクションへのサブスクライバーの登録
        ros.Subscribe<PathMsg>(Subscribe_topic_name, Callback);
        Debug.Log("already:baselink/pose");
        //
    }

    void Callback(PathMsg msg)
    {
       // mode = FindObjectOfType<mood_selector>();
        Debug.Log("path_get");
        Debug.Log(msg.ToString());
        Debug.Log(msg.poses.ToString());
        Debug.Log(msg.poses[10].ToString());
        path_list.Clear();
        path_angular_list.Clear();
        Debug.Log($"Received path with {msg.poses.Length} waypoints"); // 受信したパスのデータを表示
        foreach (var pose in msg.poses) 
        {
            //Debug.Log($"Pose: {pose.pose.position.x}, {pose.pose.position.y}");
            angular = new (-(float)pose.pose.orientation.y, (float)pose.pose.orientation.z, (float)pose.pose.orientation.x, -(float)pose.pose.orientation.w);
            path_list.Add(new Vector3(-(float)pose.pose.position.y, 0.0f, (float)pose.pose.position.x));
            path_angular_list.Add(angular);
            //targetObject.transform.position = new Vector3(-(float)pose.pose.position.y,0.0f,(float)pose.pose.position.x);
        }
        if(path_list.Count > 500) 
        {
            targetObject.transform.position = path_list[500];
            targetObject.transform.rotation = path_angular_list[500];
        }
        else if(path_list.Count <= 500)
        {
            targetObject.transform.position = path_list[path_list.Count-1];
            targetObject.transform.rotation = path_angular_list[path_angular_list.Count - 1];
        }
        
        /*
        if (mode.mood == 1) //Visual tool
        {
            //Debug.Log("zxcvbnm");
            //Debug.Log(msg.pose.orientation);
            //
            Vector3 newPosition = new Vector3(((float)msg.pose.position.y * (-1) + offset_x), ((float)msg.pose.position.z) + offset_z, ((float)msg.pose.position.x) + offset_y);
            Quaternion newRotation = new((float)msg.pose.orientation.y * (-1), (float)msg.pose.orientation.z, (float)msg.pose.orientation.x, (float)msg.pose.orientation.w * (-1));
            //Debug.Log(newPosition);
            //Debug.Log(newRotation.eulerAngles);
            //
            // targetObject.GetComponent<Rigidbody>().isKinematic = false;
            targetObject.transform.position = newPosition;
            
            //
            targetObject.transform.rotation = newRotation;
            targetObject.GetComponent<Rigidbody>().isKinematic = true;
            //
            //Debug.Log("rot_change_strange" + Real_Cyber_angle_diff);
        }*/

    }

}
