using System.Collections.Generic;
using UnityEngine;

public class PrevForPlayBackhoe : MonoBehaviour
{
    GameObject targetObject;
    Model_name ModelIdentifier;
    FieldMainManager FieldManager;
    PathJointSubscriber JointPathPlanSubscriber;
    private List<(List<double> joints, double time)> PlanPosition = new List<(List<double>, double)>();

    // 関節角の履歴
    public float PreviewTime = 2.0f;       // 何秒後を予測するか
    private float processInterval = 0.1f;  // 処理間隔
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

    // 最新の関節角
    public List<double> JointPositions;

    private double playbackStartTime = -1;
    public int currentPointIndex = 0;

    public float nowtime; 


    void Start()
    {
        var selector = GameObject.Find("FieldManager");
        targetObject = this.gameObject;
        JointPathPlanSubscriber = GetComponent<PathJointSubscriber>();
        ModelIdentifier = GetComponent<Model_name>();
        FieldManager = selector.GetComponent<FieldMainManager>();

        JointPositions = new List<double> { 0, 0, 0, 0, 0, 0, 0, 0 };

      //  InvokeRepeating(nameof(ProcessJointPrediction), processInterval, processInterval);
    }

    void Update()//ProcessJointPrediction()
    {
        PlanPosition = JointPathPlanSubscriber.JointPositions;

        if (JointPathPlanSubscriber.Obtained == true)
        {
            JointPathPlanSubscriber.Obtained = false;
            currentPointIndex = 0;
        }

        if (PlanPosition == null || PlanPosition.Count == 0)
        {
          //  Debug.Log("PlanPosition is null.");
            return;
        }

        // currentPointIndex が範囲外になっていないかチェック
        if (currentPointIndex >= PlanPosition.Count)
        {
            return; // もう再生終了
        }

        var pt = PlanPosition[currentPointIndex];

        nowtime = (float)pt.time;

        // pt.time = 0 の場合は再生開始
        if (pt.time <= 0.02)
        {
            playbackStartTime = Time.timeAsDouble;
            currentPointIndex = 0;
            pt = PlanPosition[currentPointIndex]; // 念のため再取得
            Debug.Log("Trajectory start detected → playbackStartTime reset");
        }

        // playbackStartTime が未設定なら設定
        if (playbackStartTime < 0)
            playbackStartTime = Time.timeAsDouble;

        double elapsed = Time.timeAsDouble - playbackStartTime;

        JointPositions = pt.joints;

        // 再生のタイミングチェック
        if (elapsed >= pt.time)
        {
            ApplyJointAngles(pt.joints);
            currentPointIndex++;

            // currentPointIndex が範囲外にならないようにチェック
            if (currentPointIndex >= PlanPosition.Count)
            {
                Debug.Log("Trajectory playback finished");
                //currentPointIndex = 0;
                return;
            }

            Debug.Log($"Applied step {currentPointIndex}/{PlanPosition.Count} at {elapsed:F2} sec");
        }
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
    }


}
