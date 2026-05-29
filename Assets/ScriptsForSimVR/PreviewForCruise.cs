using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;///for evaluate///

public class PreviewForCruise : MonoBehaviour
{
    [Header("予測設定")]
    public float PredictTime = 2.0f; // t秒後の位置を表示
    public float historyDuration = 2.0f; // 過去2秒で速度計算
    public float historyInterval = 0.5f;  // 0.5秒ごとに位置を記録
    public bool Reset;
  //  public Vector3 PlanPositon;///for plan evaluate///
    public List<(DateTime time, Vector3 position)> PlanPositon;///for evaluate///

    // ---------------------------
    // 内部オブジェクト
    // ---------------------------
    GameObject targetObject;
    GameObject SelectorObject;
    GameObject Reference;
    public GameObject SubscriberObject;
    ModelIdentifier ModelIdentifier;
    FieldMainManager FieldManager;
    PoseSubscriber MachinePoseSubscriber;
    PathSubscriber PathSub;
    ModeSelector mode;

    // ---------------------------
    // 過去位置記録用
    // ---------------------------
    private class TimedPos
    {
        public Vector3 pos;
        public float time;
    }
    List<TimedPos> history = new List<TimedPos>();
    private float timer = 0f;

    void Start()
    {
        SelectorObject = GameObject.Find("FieldManager");
        Reference = GameObject.Find("MapReferencePoint");
        targetObject = this.gameObject;
        mode = SelectorObject.GetComponent<ModeSelector>();

        ModelIdentifier = GetComponent<ModelIdentifier>();
        MachinePoseSubscriber = SubscriberObject.GetComponent<PoseSubscriber>();
        PathSub = SubscriberObject.GetComponent<PathSubscriber>();
        PlanPositon = new List<(DateTime, Vector3)>();///for evaluate///

        FieldManager = SelectorObject.GetComponent<FieldMainManager>();
    }

    void Update()
    {
        if (PathSub == null || PathSub.PathPoints.Count < 2) return;

        Vector3 currentPos = MachinePoseSubscriber.MapMachinePosition;

        // ---------------------------
        // 0.5秒ごとに過去位置を記録
        // ---------------------------
        timer += Time.deltaTime;
        if (timer >= historyInterval)
        {
            timer -= historyInterval;
            history.Add(new TimedPos { pos = currentPos, time = Time.time });
            history.RemoveAll(h => Time.time - h.time > historyDuration);
        }

        // ---------------------------
        // 過去2秒間の平均速度を計算
        // ---------------------------
        float speed = CalcSmoothedSpeed();
        float predictedDistance = speed * PredictTime;

        // ---------------------------
        // 経路上で現在位置に最も近い点を探す
        // ---------------------------
        int closestIndex = GetClosestPointIndex(currentPos - Reference.transform.position, PathSub.PathPoints);

        // ---------------------------
        // t秒後の未来位置（経路上）インデックスを計算
        // ---------------------------
        int previewIndex = GetPreviewIndex(closestIndex, PathSub.PathPoints, predictedDistance);

        // ---------------------------
        // Unity座標系に変換
        // ---------------------------
        Vector3 previewPos = PathSub.PathPoints[previewIndex] + Reference.transform.position;

        // ---------------------------
        // Terrain 高さを反映
        // ---------------------------
        float yHeight = FieldManager.terrain.SampleHeight(previewPos)
                       + FieldManager.terrain.transform.position.y
                       + ModelIdentifier.Offset_y;
        previewPos.y = yHeight;

        if (mode.WhichMode == ModeSelector.ModeOption.PreviewAR){previewPos.y = yHeight + 10000f;}

        // ---------------------------
        // モデルを未来位置に移動
        // ---------------------------
        targetObject.transform.position = previewPos;
        PlanPositon.Add(
           (DateTime.UtcNow, new Vector3 (previewPos.x, previewPos.y, previewPos.z))
       );///for evaluate///

        // ---------------------------
        // Reset 処理
        // ---------------------------
        if (Reset)
        {
            Reset = false;
            PathSub.PathPoints.Clear();
        }
    }

    // ---------------------------
    // 過去2秒間の平均速度を計算
    // ---------------------------
    float CalcSmoothedSpeed()
    {
        if (history.Count < 2) return 0f;

        Vector3 oldest = history[0].pos;
        Vector3 newest = history[history.Count - 1].pos;
        float dt = history[history.Count - 1].time - history[0].time;
        if (dt <= 0f) return 0f;

        float dist = Vector3.Distance(newest, oldest);
        return dist / dt; // m/s
    }

    // ---------------------------
    // 経路で最も近い点を探す
    // ---------------------------
    int GetClosestPointIndex(Vector3 currentPos, List<Vector3> path)
    {
        float minDist = float.MaxValue;
        int index = 0;

        for (int i = 0; i < path.Count; i++)
        {
            float d = Vector3.Distance(currentPos, path[i]);
            if (d < minDist)
            {
                minDist = d;
                index = i;
            }
        }
        return index;
    }

    // ---------------------------
    // 指定距離だけ経路上を進んだ点のインデックスを返す
    // ---------------------------
    int GetPreviewIndex(int startIndex, List<Vector3> path, float distance)
    {
        float accumulated = 0f;

        for (int i = startIndex; i < path.Count - 1; i++)
        {
            accumulated += Vector3.Distance(path[i], path[i + 1]);
            if (accumulated >= distance)
                return i + 1;
        }

        return path.Count - 1; // 経路の最後
    }
}
