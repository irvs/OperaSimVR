using System.Collections.Generic;
using UnityEngine;
using System;///for evaluate///

public class PrevForPlayBackhoe : MonoBehaviour
{
    GameObject targetObject;
    Model_name ModelIdentifier;
    FieldMainManager FieldManager;
    PathJointSubscriber JointPathPlanSubscriber;
    private List<(List<double> joints, double time)> PlanPosition = new List<(List<double>, double)>();
    public List<(DateTime time, float[] angles)> PlanJointAngle;///for evaluate///


    // 関節角の履歴
    public float PreviewTime = 2.0f;       // 何秒後を予測するか
    public bool JointChengeSw;
    int Counter;

    // 各関節のオブジェクト
    public GameObject SwingObject;
    public GameObject BoomObject;
    public GameObject ArmObject;
    public GameObject BucketObject;

    // オフセット
    public float OffsetSwing;
    public float OffsetBoom;
    public float OffsetArm;
    public float OffsetBucket;

    private double playbackStartTime = -1;



    void Start()
    {
        var selector = GameObject.Find("FieldManager");
        targetObject = this.gameObject;
        JointPathPlanSubscriber = GetComponent<PathJointSubscriber>();
        ModelIdentifier = GetComponent<Model_name>();
        FieldManager = selector.GetComponent<FieldMainManager>();
        PlanJointAngle = new List<(DateTime, float[])>();
    }

    void Update()
    {
        PlanPosition = JointPathPlanSubscriber.JointPositions;

        if (PlanPosition == null || PlanPosition.Count == 0)
            return;

        // 受信開始: time=0 で軌道リセット
        if (JointPathPlanSubscriber.Obtained == true)
        {
            JointPathPlanSubscriber.Obtained = false;
            playbackStartTime = Time.timeAsDouble;
        }

        if (playbackStartTime < 0)
            playbackStartTime = Time.timeAsDouble;

        // 現在の経過時間
        double elapsed = Time.timeAsDouble - playbackStartTime;

        // ここが重要！
        double previewTime = elapsed + PreviewTime - 1;

        // previewTime に最も近いステップを探す
        int previewIndex = FindPreviewIndex(previewTime);

        if (previewIndex < 0 || previewIndex >= PlanPosition.Count)
            return;

        var pt = PlanPosition[previewIndex];

        ApplyJointAngles(pt.joints);
    }



    void ApplyJointAngles(List<double> joints)
    {
        // ラジアン→度に変換
        float swingDeg = -(float)(joints[0] * Mathf.Rad2Deg - OffsetSwing);
        float boomDeg = (float)(joints[1] * Mathf.Rad2Deg - OffsetBoom);
        float armDeg = (float)(joints[2] * Mathf.Rad2Deg - OffsetArm);
        float bucketDeg = (float)(joints[3] * Mathf.Rad2Deg - OffsetBucket);

        SwingObject.transform.localRotation = Quaternion.Euler(0, swingDeg, 0);
        BoomObject.transform.localRotation = Quaternion.Euler(boomDeg, 0, 0);
        ArmObject.transform.localRotation = Quaternion.Euler(armDeg, 0, 0);
        BucketObject.transform.localRotation = Quaternion.Euler(bucketDeg, 0, 0);

        // PlanJointAngle = new List<(DateTime, float[])>();
        PlanJointAngle.Add(
           (DateTime.UtcNow, new float[] { (float)joints[0], (float)joints[1] - OffsetSwing * (Mathf.PI / 180), (float)joints[2], (float)joints[3] })
       );
    }

    int FindPreviewIndex(double previewTime)
    {
        for (int i = 0; i < PlanPosition.Count; i++)
        {
            if (PlanPosition[i].time >= previewTime)
                return i;
        }

        return PlanPosition.Count - 1;  // 最後のフレーム
    }



}
