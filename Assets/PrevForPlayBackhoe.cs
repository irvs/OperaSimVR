using System.Collections.Generic;
using UnityEngine;

public class PrevForPlayBackhoe : MonoBehaviour
{
    GameObject targetObject;
    Model_name ModelIdentifier;
    FieldMainManager FieldManager;
    JointSubscriber MachineJointSubscriber;

    // 関節角の履歴
    private List<(List<double> joints, double time)> jointHistory = new List<(List<double>, double)>();
    private double timeWindow = 5.0;       // 直近5秒間を保持
    public float PreviewTime = 2.0f;       // 何秒後を予測するか
    private float processInterval = 0.5f;  // 処理間隔
    public bool Reset;
    public bool JointChengeSw;

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

    // 最新の関節角
    public List<double> JointPositions;

    void Start()
    {
        var selector = GameObject.Find("FieldManager");
        targetObject = this.gameObject;
        MachineJointSubscriber = GetComponent<JointSubscriber>();
        ModelIdentifier = GetComponent<Model_name>();
        FieldManager = selector.GetComponent<FieldMainManager>();

        JointPositions = new List<double> { 0, 0, 0, 0, 0, 0, 0, 0 };

        InvokeRepeating(nameof(ProcessJointPrediction), processInterval, processInterval);
    }

    void ProcessJointPrediction()
    {
        // 最新の関節角取得
        JointPositions = MachineJointSubscriber.JointPositions;
        double currentTime = Time.timeAsDouble;

        // 履歴に追加
        jointHistory.Add((new List<double>(JointPositions), currentTime));

        // 古いデータを削除
        jointHistory.RemoveAll(item => (currentTime - item.time) > timeWindow);

        if (jointHistory.Count < 2)
        {
            Debug.Log("Not enough joint data to predict.");
            return;
        }

        // 最新と最古を使って角速度を計算
        var oldest = jointHistory[0];
        var newest = jointHistory[jointHistory.Count - 1];
        double deltaTime = newest.time - oldest.time;
        if (deltaTime <= 0.0001) return;

        int jointCount = Mathf.Min(oldest.joints.Count, newest.joints.Count);
        List<double> predictedJoints = new List<double>(jointCount);

        for (int i = 0; i < jointCount; i++)
        {
            double jointVelocity = (newest.joints[i] - oldest.joints[i]) / deltaTime; // [rad/s]
            double predicted = newest.joints[i] + jointVelocity * PreviewTime;       // 外挿
            predictedJoints.Add(predicted);
        }

        // 角度更新（実際にモデルを動かす）
        if (JointChengeSw)
        {
            ApplyJointAngles(predictedJoints);
        }

     //   Debug.Log($"[{Time.time:F1}s] Predicted {PreviewTime}s later joint angles applied.");
    }

    void ApplyJointAngles(List<double> joints)
    {
        // ラジアン→度に変換
        float swingDeg = -(float)(joints[0] * Mathf.Rad2Deg - OffsetSwing);
        float boomDeg = (float)(joints[1] * Mathf.Rad2Deg - OffsetBoom);
        float armDeg = (float)(joints[2] * Mathf.Rad2Deg - OffsetArm);
        float bucketDeg = (float)(joints[3] * Mathf.Rad2Deg - OffsetBucket);

      //  SwingObject.transform.localRotation = Quaternion.Euler(0, swingDeg, 0);
     //   BoomObject.transform.localRotation = Quaternion.Euler(boomDeg, 0, 0);
     //   ArmObject.transform.localRotation = Quaternion.Euler(armDeg, 0, 0);
      //  BucketObject.transform.localRotation = Quaternion.Euler(bucketDeg, 0, 0);
        SwingObject.transform.rotation = Quaternion.Euler(targetObject.transform.rotation.eulerAngles.x, -((float)(JointPositions[0] * 180 / 3.14) - OffsetSwing), targetObject.transform.rotation.eulerAngles.z);
        BoomObject.transform.rotation = Quaternion.Euler((float)(JointPositions[1] * 180 / 3.14) - OffsetBoom, SwingObject.transform.rotation.eulerAngles.y, SwingObject.transform.rotation.eulerAngles.z);
        ArmObject.transform.rotation = Quaternion.Euler((float)(JointPositions[2] * 180 / 3.14) + (BoomObject.transform.rotation.eulerAngles.x) - OffsetArm, BoomObject.transform.rotation.eulerAngles.y, BoomObject.transform.rotation.eulerAngles.z);
        BucketObject.transform.rotation = Quaternion.Euler((float)(JointPositions[3] * 180 / 3.14) + (ArmObject.transform.rotation.eulerAngles.x) - OffsetBucket, ArmObject.transform.rotation.eulerAngles.y, ArmObject.transform.rotation.eulerAngles.z);
    }

    void Update()
    {
        if (Reset)
        {
            Reset = false;
            jointHistory.Clear();
        }
    }
}
