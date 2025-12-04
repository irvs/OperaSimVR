using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrevForPlayPlanPath : MonoBehaviour
{
    public float PreviewDistance = 2.0f;  // 何メートル先の経路を表示するか
    public bool Reset;

    GameObject targetObject;
    Model_name ModelIdentifier;
    FieldMainManager FieldManager;
    PoseSubscriber MachinePoseSubscriber;
    PathSubscriber PathSub; // ★ Nav2 Path を受け取るスクリプト

    GameObject SelectorObject;
    GameObject Reference;

    void Start()
    {
        SelectorObject = GameObject.Find("FieldManager");
        Reference = GameObject.Find("MapReferencePoint");
        targetObject = this.gameObject;

        MachinePoseSubscriber = GetComponent<PoseSubscriber>();
        ModelIdentifier = GetComponent<Model_name>();
        FieldManager = SelectorObject.GetComponent<FieldMainManager>();
        PathSub = GetComponent<PathSubscriber>(); // ★ 同じオブジェクトにアタッチしておく
    }

    void Update()
    {
        if (PathSub == null || PathSub.PathPoints.Count < 2)
            return;

        Vector3 robotPos = MachinePoseSubscriber.MapMachinePosition;

        // --- 経路上でロボットに最も近い点を探す ---
        int closestIndex = GetClosestPointIndex(robotPos, PathSub.PathPoints);

        // --- PreviewDistance だけ先の点を探す ---
        int previewIndex = GetPreviewIndex(closestIndex, PathSub.PathPoints, PreviewDistance);

        Vector3 previewPos = PathSub.PathPoints[previewIndex] + Reference.transform.position;

        // --- Terrain 高さ調整 ---
        float yHeight = FieldManager.terrain.SampleHeight(previewPos)
                        + FieldManager.terrain.transform.position.y
                        + ModelIdentifier.Offset_y;

        previewPos.y = yHeight;

        // --- モデルを未来位置へ移動 ---
        targetObject.transform.position = previewPos;

        if (Reset)
        {
            Reset = false;
            // Nav2 経路をクリアする必要がある場合のみ
            PathSub.PathPoints.Clear();
        }
    }

    // ==========================
    //    経路で最も近い点を探す
    // ==========================
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

    // ==========================
    //   指定距離だけ未来の点を探す
    // ==========================
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
