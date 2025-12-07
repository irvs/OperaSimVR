using System.Collections.Generic;
using UnityEngine;

public class PrevForPlay : MonoBehaviour
{
    Vector3 RealPosition;
    Quaternion RealRotation;
    GameObject targetObject;
    Model_name ModelIdentifier;
    FieldMainManager FieldManager;
    PoseSubscriber MachinePoseSubscriber;

    private List<(Vector3 position, double time)> positionHistory = new List<(Vector3, double)>();
    private double timeWindow = 5.0; // 直近5秒間の履歴を保持
    public float PreviewTime = 2.0f; // 何秒後を予測するか
    public float SamplingTime = 0.5f; // 処理間隔
    private float processInterval = 0.5f;
    GameObject SelectorObject;
    public bool Reset;

    void Start()
    {
        SelectorObject = GameObject.Find("FieldManager");
        targetObject = this.gameObject;
        MachinePoseSubscriber = GetComponent<PoseSubscriber>();
        ModelIdentifier = GetComponent<Model_name>();
        FieldManager = SelectorObject.GetComponent<FieldMainManager>();
        InvokeRepeating(nameof(ProcessPositions), processInterval, processInterval);
    }

    void ProcessPositions()
    {
        RealPosition = MachinePoseSubscriber.MapMachinePosition;
        RealRotation = MachinePoseSubscriber.MapMachineRotation;
        double currentTime = Time.timeAsDouble;

        positionHistory.Add((RealPosition, currentTime));
        positionHistory.RemoveAll(item => (currentTime - item.time) > timeWindow);

        if (positionHistory.Count < 2)
        {
            Debug.Log("Not enough data to predict.");
            return;
        }

        // --- 速度ベクトルを推定 ---
        var oldest = positionHistory[0];
        var newest = positionHistory[positionHistory.Count - 1];
        double deltaTime = newest.time - oldest.time;

        if (deltaTime <= 0.0001)
            return;

        Vector3 velocity = (newest.position - oldest.position) / (float)deltaTime;

        // --- 指定秒数後の位置を予測 ---
        Vector3 predictedPosition = newest.position + velocity * PreviewTime;

        // --- 高さを地形から取得 ---
        float Y_Height = FieldManager.terrain.SampleHeight(predictedPosition)
                        + FieldManager.terrain.transform.position.y
                        + ModelIdentifier.Offset_y;

        // --- オブジェクトを移動 ---
        targetObject.transform.position = new Vector3(predictedPosition.x, Y_Height, predictedPosition.z);

      //  Debug.Log($"[{Time.time:F1}s] Predicted {PreviewTime}s later: {predictedPosition}");
    }

    void Update()
    {
        if (Reset)
        {
            Reset = false;
            positionHistory.Clear();
        }
    }
}
