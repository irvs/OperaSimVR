using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Trajectory;
using Unity.Robotics.ROSTCPConnector;
using System.IO;
using System.Text;

public class EvaluatePlan : MonoBehaviour
{
    public string SubscribeTopicName;
    PrevForBackhoe PrevForBackhoe;
    public List<(DateTime time, float[] angles)> PlanJointAngle;
    DateTime PlanCreatedTime;
    public GameObject SwingPlanObject;
    public GameObject BoomPlanObject;
    public GameObject ArmPlanObject;
    public GameObject BucketPlanObject;
    public GameObject SwingObject;
    public GameObject BoomObject;
    public GameObject ArmObject;
    public GameObject BucketObject;
    List<(double time, float[] planangles, float[] currentangles)> JointAngles;
    public bool ObtainPlan;
    public bool output;

    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<JointTrajectoryMsg>(SubscribeTopicName, OnPathReceived);
        PrevForBackhoe = GetComponent<PrevForBackhoe>();
        JointAngles = new List<(double, float[], float[])>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ObtainPlan == true)
        {
            //obtain plan

            //plan
         //   PlanJointAngle = PrevForBackhoe.PlanJointAngle;
         //   PlanCreatedTime = PlanJointAngle[0].time;
            //current plan joint state
            float CurrentSwingPlan = SwingPlanObject.transform.localRotation.eulerAngles.y;
            float CrrentBoomPlan = BoomPlanObject.transform.localRotation.eulerAngles.x;
            float CrrentArmPlan = ArmPlanObject.transform.localRotation.eulerAngles.x;
            float CrrentBucketPlan = BucketPlanObject.transform.localRotation.eulerAngles.x;
            //current joint state
            float CurrentSwing = SwingObject.transform.localRotation.eulerAngles.y;
            float CrrentBoom = BoomObject.transform.localRotation.eulerAngles.x;
            float CrrentArm = ArmObject.transform.localRotation.eulerAngles.x;
            float CrrentBucket = BucketObject.transform.localRotation.eulerAngles.x;
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

        foreach (var data in PrevForBackhoe.PlanJointAngle)
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
}
