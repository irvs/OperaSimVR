using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Trajectory;
using UnityEngine;
using System.Collections.Generic;

public class PathJointSubscriber : MonoBehaviour
{
    public string topicName = "subscribe_topic";
    ROSConnection ros;
    private JointTrajectoryMsg latestTraj;
    public double nowtime;
    public List<double> LatestJointPositions;
    public bool Obtained = false;

    public List<(List<double> joints, double time)> JointPositions = new List<(List<double>, double)>();

    void Start()
    {
        ros = ROSConnection.instance;
        ros.Subscribe<JointTrajectoryMsg>(topicName, CallbackTraj);
    }
    void CallbackTraj(JointTrajectoryMsg msg)
    {
       // Debug.Log($"Received JointTrajectory ({msg.points.Length} pts)");

        JointPositions.Clear();
        LatestJointPositions.Clear();
        latestTraj = msg;


        foreach (var pt in latestTraj.points)
        {
            JointPositions.Add(
                (new List<double>(pt.positions),
                pt.time_from_start.sec + pt.time_from_start.nanosec * 1e-9)
            );
            LatestJointPositions = new List<double>(pt.positions);
            nowtime = pt.time_from_start.sec + pt.time_from_start.nanosec * 1e-9;
        }
        Obtained = true;

    }
    public JointTrajectoryMsg GetTrajectory()
    {
        return latestTraj;
    }
}