using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Trajectory;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using System.IO;
using System.Text;

public class EvaluateCurrentTopic : MonoBehaviour
{
    public string SubscribePlanTopicName;
    public string SubscribeJointStateTopicName;
    PrevForBackhoe PrevForBackhoe;
    public List<(DateTime time, float[] angles)> PlanJointAngle;
    DateTime PlanCreatedTime;
    List<(double time, float[] planangles, float[] currentangles)> JointAngles;
    List<float[]> CurrentJointAngle;

    public GameObject SwingObject;
    public GameObject BoomObject;
    public GameObject ArmObject;
    public GameObject BucketObject;

    public bool ObtainPlan;
    public bool output;

    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<JointTrajectoryMsg>(SubscribePlanTopicName, OnPathReceived);
        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(SubscribeJointStateTopicName, OnJointAngleReceived);
        PrevForBackhoe = GetComponent<PrevForBackhoe>();
        JointAngles = new List<(double, float[], float[])>();
    }

    // Update is called once per frame
    void Update()
    {
       // PlanJointAngle = PrevForBackhoe.PlanJointAngle;
        if (ObtainPlan == true)
        {
            //obtain plan

            //plan
            
            //  PlanCreatedTime = PlanJointAngle[0].time;
            //current plan joint state
    //        Debug.Log(PlanJointAngle);
      //      float CurrentSwingPlan = PlanJointAngle[PlanJointAngle.Count - 1].angles[0];
        //    float CrrentBoomPlan = PlanJointAngle[PlanJointAngle.Count - 1].angles[1];
          //  float CrrentArmPlan = PlanJointAngle[PlanJointAngle.Count - 1].angles[2];
            //float CrrentBucketPlan = PlanJointAngle[PlanJointAngle.Count - 1].angles[3];
            //current joint state
            float CurrentSwing = CurrentJointAngle[CurrentJointAngle.Count - 1][0];
            float CrrentBoom = CurrentJointAngle[CurrentJointAngle.Count - 1][1];
            float CrrentArm = CurrentJointAngle[CurrentJointAngle.Count - 1][2];
            float CrrentBucket = CurrentJointAngle[CurrentJointAngle.Count - 1][3];
            //
            //current joint state
            float CurrentSwingModel = SwingObject.transform.localRotation.eulerAngles.y;
            float CrrentBoomModel = BoomObject.transform.localRotation.eulerAngles.x;
            float CrrentArmModel = ArmObject.transform.localRotation.eulerAngles.x;
            float CrrentBucketModel = BucketObject.transform.localRotation.eulerAngles.x;

            JointAngles.Add
                (
                 (Time.realtimeSinceStartupAsDouble,  new float[] { CurrentSwing, CrrentBoom, CrrentArm, CrrentBucket }, new float[] { CurrentSwingModel, CrrentBoomModel, CrrentArmModel, CrrentBucketModel })
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
        string path = Application.dataPath + "/visualize_model.csv";
        StringBuilder sb = new StringBuilder();

        // ヘッダー
        sb.AppendLine("time,swing,boom,arm,bucket,swingmodel,boommodel,armmodel,bucketmodel");

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
