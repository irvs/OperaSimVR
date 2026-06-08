using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

public class PathWriter : MonoBehaviour
{
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    public List<Vector3> path_list = new List<Vector3>();
    private List<Quaternion> path_angular_list = new List<Quaternion>();
    private LineRenderer lineRenderer;
    GameObject Reference;
    PathSubscriber PathSub;
    public GameObject SubscriberObject;

    // Start is called before the first frame update
    void Start()
    {
        PathSub = SubscriberObject.GetComponent<PathSubscriber>();
        //
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        // LineRendererの設定
        lineRenderer.positionCount = path_list.Count;  // 頂点の数を設定
        lineRenderer.startWidth = 0.1f;  // 線の太さ（開始）
        lineRenderer.endWidth = 0.1f;    // 線の太さ（終了）
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));  // マテリアル設定（デフォルトのシェーダ）
        lineRenderer.startColor = Color.red; // 線の開始色
        lineRenderer.endColor = Color.green; // 線の終了色

        Reference = GameObject.Find("MapReferencePoint");

        // 各座標をLineRendererに設定
        for (int i = 0; i < path_list.Count; i++)
        {
            lineRenderer.SetPosition(i, path_list[i]);
        }

    }
    void Update()
    {
        if (PathSub == null || PathSub.PathPoints.Count < 2) return;
        WritePath(PathSub.PathPoints);
    }

    void WritePath(List<Vector3> PathPoints)
    {
        path_list.Clear();
        path_angular_list.Clear();
        path_list = new List<Vector3>(PathPoints);
       // Debug.Log($"Received path with {msg.poses.Length} waypoints"); // 受信したパスのデータを表示

        Vector3 refPos = Reference.transform.position;
        for (int i = 0; i < path_list.Count; i++)
        {
            path_list[i] += refPos + new Vector3(offset_x, offset_y, offset_z);
        }
        lineRenderer.positionCount = path_list.Count;
        for (int i = 0; i < path_list.Count; i++)
        {
            lineRenderer.SetPosition(i, path_list[i]);
        }
    }
}
