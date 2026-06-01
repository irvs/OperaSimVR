using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Trajectory;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using System.IO;
using System.Text;

public class EvaluatePlanTopic : MonoBehaviour
{
    public string SubscribePlanTopicName;
    public string SubscribeJointStateTopicName;
    PrevForPlayBackhoe PrevForPlayBackhoe;
    public List<(DateTime time, float[] angles)> PlanJointAngle;
    DateTime PlanCreatedTime;
    List<(double time, float[] planangles, float[] currentangles)> JointAngles;
    List<float[]> CurrentJointAngle;
    public bool ObtainPlan;
    public bool output;

    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<JointTrajectoryMsg>(SubscribePlanTopicName, OnPathReceived);
        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(SubscribeJointStateTopicName, OnJointAngleReceived);
        PrevForPlayBackhoe = GetComponent<PrevForPlayBackhoe>();
        JointAngles = new List<(double, float[], float[])>();
    }

    // Update is called once per frame
    void Update()
    {
        PlanJointAngle = PrevForPlayBackhoe.PlanJointAngle;
        if (ObtainPlan == true)
        {
            //obtain plan

            //plan
            
            //  PlanCreatedTime = PlanJointAngle[0].time;
            //current plan joint state
            float CurrentSwingPlan = PlanJointAngle[PlanJointAngle.Count - 1].angles[0];
            float CrrentBoomPlan = PlanJointAngle[PlanJointAngle.Count - 1].angles[1];
            float CrrentArmPlan = PlanJointAngle[PlanJointAngle.Count - 1].angles[2];
            float CrrentBucketPlan = PlanJointAngle[PlanJointAngle.Count - 1].angles[3];
            //current joint state
            float CurrentSwing = CurrentJointAngle[CurrentJointAngle.Count - 1][0];
            float CrrentBoom = CurrentJointAngle[CurrentJointAngle.Count - 1][1];
            float CrrentArm = CurrentJointAngle[CurrentJointAngle.Count - 1][2];
            float CrrentBucket = CurrentJointAngle[CurrentJointAngle.Count - 1][3];
            JointAngles.Add
                (
                 (Time.realtimeSinceStartupAsDouble, new float[] { CurrentSwingPlan, CrrentBoomPlan, CrrentArmPlan, CrrentBucketPlan }, new float[] { CurrentSwing, CrrentBoom, CrrentArm, CrrentBucket })
                );
            if (output == true)
            {
                ExportJointCSV();
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
        string path = Application.dataPath + "/plan_model.csv";
        StringBuilder sb = new StringBuilder();

        // ヘッダー
        sb.AppendLine("time,swingplan,boomplan,armplan,bucketplan,swingcurrent,boomcurrent,armcurrent,bucketcurrent");

        foreach (var data in JointAngles)
        {
            string line = data.time.ToString("F6");

            foreach (var angle in data.planangles)
            {
                line += "," + angle.ToString();
            }
            foreach (var angle in data.currentangles)
            {
                line += "," + angle.ToString();
            }

            sb.AppendLine(line);
        }

        File.WriteAllText(path, sb.ToString());

        Debug.Log("CSV保存: " + path);
    }
    
    void OnPathReceived(JointTrajectoryMsg msg)
    {
        DateTime utcNow = DateTime.UtcNow;
        Debug.Log(utcNow);
        ObtainPlan = true;
    }
    void OnJointAngleReceived(JointStateMsg msg)
    {
        CurrentJointAngle = new List<float[]>();
        CurrentJointAngle.Add
            (
             (new float[] { (float)msg.position[0], (float)msg.position[1], (float)msg.position[2], (float)msg.position[3] })
            );
    }
}
