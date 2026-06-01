using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Nav;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using System.IO;
using System.Text;

public class EvaluatePlanTopic_Position : MonoBehaviour
{
    public string SubscribePlanTopicName;
    public string SubscribePositionTopicName;
    PrevForPlayPlanPath PrevForPlayPlanPath;
    public List<(DateTime time, Vector3 position)> PlanPositon;
    DateTime PlanCreatedTime;
    List<(double time, Vector3 planposition, Vector3 currentposition)> Positions;
    List<Vector3> CurrentPosition;
    List<Quaternion> CurrentOrientation;
    public bool ObtainPlan;
    public bool output;

    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<PathMsg>(SubscribePlanTopicName, OnPathReceived);
        //ROSConnection.GetOrCreateInstance().Subscribe<OdometryMsg>(SubscribePositionTopicName, OnCurrentPositionReceived);
        ROSConnection.GetOrCreateInstance().Subscribe<PoseStampedMsg>(SubscribePositionTopicName, OnCurrentPositionReceivedPoseSt);
        PrevForPlayPlanPath = GetComponent<PrevForPlayPlanPath>();
        Positions = new List<(double, Vector3, Vector3)>();
        CurrentPosition = new List<Vector3>();
        CurrentOrientation = new List<Quaternion>();
    }

    // Update is called once per frame
    void Update()
    {
        PlanPositon = PrevForPlayPlanPath.PlanPositon;
        if (ObtainPlan == true)
        {
            if (ObtainPlan && PlanPositon.Count > 0 && CurrentPosition.Count > 0)
            {
                var plan = PlanPositon[PlanPositon.Count - 1].position;
                var current = CurrentPosition[CurrentPosition.Count - 1];

                Positions.Add(
                    (
                        Time.realtimeSinceStartupAsDouble,
                        new Vector3(plan[0], plan[1], plan[2]),
                        current
                    )
                );

                if (output)
                {
                    ExportJointCSV();
                }
            }
        }
    }
    /*
    void ExportPlanCSV()
    {
        string path = Application.dataPath + "/plan.csv";
        StringBuilder sb = new StringBuilder();

        // ヘッダー
        sb.AppendLine("time,swing,boom,arm,bucket");

        foreach (var data in PrevForPlayBackhoe.PlanJointAngle)
        {
            string line = data.time.ToString();

            foreach (var angle in data.angles)
            {
                line += "," + angle.ToString();
            }

            sb.AppendLine(line);
        }

        File.WriteAllText(path, sb.ToString());

        Debug.Log("CSV保存: " + path);
    }
    */
    void ExportJointCSV()
    {
        string path = Application.dataPath + "/plan_model_position.csv";
        StringBuilder sb = new StringBuilder();

        // ヘッダー
        sb.AppendLine("time,swingplan,boomplan,armplan,bucketplan,swingcurrent,boomcurrent,armcurrent,bucketcurrent");

        foreach (var data in Positions)
        {
            string line = data.time.ToString("F6");

            line += "," + data.planposition.x;
            line += "," + data.planposition.y;
            line += "," + data.planposition.z;

            line += "," + data.currentposition.x;
            line += "," + data.currentposition.y;
            line += "," + data.currentposition.z;

            sb.AppendLine(line);
        }

        File.WriteAllText(path, sb.ToString());

        Debug.Log("CSV保存: " + path);
    }
    
    void OnPathReceived(PathMsg msg)
    {
        DateTime utcNow = DateTime.UtcNow;
        Debug.Log(utcNow);
        ObtainPlan = true;
    }
    void OnCurrentPositionReceived(OdometryMsg msg)
    {
        CurrentPosition.Add(
            new Vector3(
            (float)msg.pose.pose.position.x,
            (float)msg.pose.pose.position.y,
            (float)msg.pose.pose.position.z
            )
        );
        CurrentOrientation.Add(
            new Quaternion(
            (float)msg.pose.pose.orientation.x,
            (float)msg.pose.pose.orientation.y,
            (float)msg.pose.pose.orientation.z,
            (float)msg.pose.pose.orientation.w
            )
        );
    }
    void OnCurrentPositionReceivedPoseSt(PoseStampedMsg msg)
    {
        CurrentPosition.Add(
            new Vector3(
            (float)msg.pose.position.x,
            (float)msg.pose.position.y,
            (float)msg.pose.position.z
            )
        );
        CurrentOrientation.Add(
            new Quaternion(
            (float)msg.pose.orientation.x,
            (float)msg.pose.orientation.y,
            (float)msg.pose.orientation.z,
            (float)msg.pose.orientation.w
            )
        );
    }

    
}
