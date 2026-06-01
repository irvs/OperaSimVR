using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Nav;
using Unity.Robotics.ROSTCPConnector;

public class PathSubscriber : MonoBehaviour
{
    public List<Vector3> PathPoints = new List<Vector3>();
    public string SubscribeTopicName = "subscribe_topic";
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;

    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<PathMsg>(SubscribeTopicName, OnPathReceived);
    }

    void OnPathReceived(PathMsg path)
    {
        PathPoints.Clear();

        foreach (var pose in path.poses)
        {
            Vector3 unityPos = new Vector3(
                (float)pose.pose.position.x + offset_x,
                (float)pose.pose.position.z + offset_y,   // ROSÇÃZÅ®UnityÇÃY
                (float)pose.pose.position.y + offset_z    // ROSÇÃYÅ®UnityÇÃZ
            );

            PathPoints.Add(unityPos);
        }
    }
}
