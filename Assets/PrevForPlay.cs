using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class PrevForPlay : MonoBehaviour
{
    Vector3 RealPosition;
    Quaternion RealRotation;
    GameObject targetObject;
    PoseSubscriber MachinePoseSubscriber;
    private List<(Vector3 position, double time)> positionHistory = new List<(Vector3, double)>();
    private double timeWindow = 5.0; // 直近5秒間
    public int PreviewTime;
    public int SamplingTime;
    private float processInterval = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        targetObject = this.gameObject;
        MachinePoseSubscriber = GetComponent<PoseSubscriber>();
        InvokeRepeating(nameof(ProcessPositions), processInterval, processInterval);
    }

    void ProcessPositions()
    {
        RealPosition = MachinePoseSubscriber.MapMachinePosition;
        RealRotation = MachinePoseSubscriber.MapMachineRotation;
        // 現在時刻を取得（秒単位）
        double currentTime = Time.timeAsDouble;

        positionHistory.Add((RealPosition, currentTime));

        // 古いデータを削除
        positionHistory.RemoveAll(item => (currentTime - item.time) > timeWindow);

        Debug.Log(RealPosition);
        // 一定間隔で呼ばれる処理
        Vector3[] recentPositions = GetRecentPositions();


        // ここで可視化・解析・平均計算などを行う
        if (recentPositions.Length == 0)
        {
            Debug.Log("No positions to process.");
            return;
        }

        Vector3 sum = Vector3.zero;
        foreach (var pos in recentPositions)
        {
            sum += pos;
        }

        Vector3 averagePosition = sum / recentPositions.Length;

        Debug.Log($"[{Time.time:F1}s] Average position: {averagePosition}");

        targetObject.transform.position = RealPosition + averagePosition;

    }


    // Update is called once per frame
    void Update()
    {
       
    }

    public Vector3[] GetRecentPositions()
    {
        Vector3[] array = new Vector3[positionHistory.Count];
        for (int i = 0; i < positionHistory.Count; i++)
        {
            array[i] = positionHistory[i].position;
        }
        return array;
    }
}
