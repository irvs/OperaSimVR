using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

public class PathWriter : MonoBehaviour
{
    public string Subscribe_topic_name = "subscribe_topic";
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;
    private ModeSelector mode;
    public List<Vector3> path_list = new List<Vector3>();
    private List<Quaternion> path_angular_list = new List<Quaternion>();
    private Quaternion angular;
    ROSConnection ros;
    private LineRenderer lineRenderer;
    GameObject Reference;

    // Start is called before the first frame update
    void Start()
    {
        Reference = GameObject.Find("MapReferencePoint");
        ros = ROSConnection.GetOrCreateInstance();
        // ROSコネクションへのサブスクライバーの登録
        ros.Subscribe<PathMsg>(Subscribe_topic_name, Callback);
        Debug.Log("already:PathWriter");
        //
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

    void Callback(PathMsg msg)
    {
        path_list.Clear();
        path_angular_list.Clear();
       // Debug.Log($"Received path with {msg.poses.Length} waypoints"); // 受信したパスのデータを表示

        foreach (var pose in msg.poses)
        {
            angular = new(-(float)pose.pose.orientation.y, (float)pose.pose.orientation.z, (float)pose.pose.orientation.x, -(float)pose.pose.orientation.w);
            path_list.Add(new Vector3(((float)pose.pose.position.x) + offset_x + (Reference.transform.position)[0], 0.0f + offset_y, ((float)pose.pose.position.y) + offset_z + (Reference.transform.position)[2]));
            path_angular_list.Add(angular);
        }

            lineRenderer.positionCount = path_list.Count;

            for (int i = 0; i < path_list.Count; i++)
            {
                lineRenderer.SetPosition(i, path_list[i]);
            }
    }
}
