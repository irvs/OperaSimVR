using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Nav;
using Unity.Robotics.ROSTCPConnector;

public class PathSubscriber : MonoBehaviour
{
    public List<Vector3> PathPoints = new List<Vector3>();
    public string SubscribeTopicName = "subscribe_topic";

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
                (float)pose.pose.position.x,
                (float)pose.pose.position.z,   // ROSÇÃZÅ®UnityÇÃY
                (float)pose.pose.position.y    // ROSÇÃYÅ®UnityÇÃZ
            );

            PathPoints.Add(unityPos);
        }
    }
}
