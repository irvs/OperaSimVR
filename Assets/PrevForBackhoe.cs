using System.Collections.Generic;
using UnityEngine;

public class PrevForBackhoe : MonoBehaviour
{
    Model_name ModelIdentifier;
    FieldMainManager FieldManager;
    PathJointSubscriber JointPathPlanSubscriber;
    private List<(List<double> joints, double time)> PlanPosition = new List<(List<double>, double)>();

    // 関節角の履歴
    public float PreviewTime = 2.0f;       // 何秒後を予測するか
    public bool JointChengeSw;
    int Counter;

    GameObject Excavator;
    // 各関節のオブジェクト
    Transform SwingObject;
    Transform BoomObject;
    Transform ArmObject;
    Transform BucketObject;

    // オフセット
    public float OffsetSwing;
    public float OffsetBoom;
    public float OffsetArm;
    public float OffsetBucket;

    private double playbackStartTime = -1;



    void Start()
    {
        var selector = GameObject.Find("FieldManager");
        JointPathPlanSubscriber = GetComponent<PathJointSubscriber>();
        ModelIdentifier = GetComponent<Model_name>();
        FieldManager = selector.GetComponent<FieldMainManager>();

        Excavator = this.gameObject;
        SwingObject = Excavator.transform.Find("base_link/body_link");
        BoomObject = Excavator.transform.Find("base_link/body_link/boom_link");
        ArmObject = Excavator.transform.Find("base_link/body_link/boom_link/arm_link");
        BucketObject = Excavator.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link");
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

        SwingObject.localRotation = Quaternion.Euler(0, swingDeg, 0);
        BoomObject.localRotation = Quaternion.Euler(boomDeg, 0, 0);
        ArmObject.localRotation = Quaternion.Euler(armDeg, 0, 0);
        BucketObject.localRotation = Quaternion.Euler(bucketDeg, 0, 0);
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
