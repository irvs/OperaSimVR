using UnityEngine;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Visualization;
using RosMessageTypes.Geometry;

public class WaypointViz : MonoBehaviour
{
    ROSConnection ros;
    public string TopicName = "waypoint_markers";
    // Prefab をインスペクタからアタッチ
    public GameObject waypointPrefab;
    public Transform waypointParent;
    // オブジェクトのリスト（後で削除・更新用）
    private List<GameObject> waypointObjects = new List<GameObject>();
    private List<Vector3> pointlist = new List<Vector3>();
    FieldMainManager FieldManager;
    GameObject SelectorObject;
    GameObject Reference;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<MarkerMsg>(TopicName, MarkerCallback);
        Reference = GameObject.Find("MapReferencePoint");
        SelectorObject = GameObject.Find("FieldManager");
        FieldManager = SelectorObject.GetComponent<FieldMainManager>();
    }

    void MarkerCallback(MarkerMsg msg)
    {
        for (int i = 0; i < msg.points.Length; i++)
        {
            Vector3 position = new Vector3(
                (float)msg.points[i].x,
                (float)msg.points[i].z, // ROSのz軸がUnityのy軸に対応
                (float)msg.points[i].y  // ROSのy軸がUnityのz軸に対応
            );

            Vector3 worldPosition = position + new Vector3(Reference.transform.position.x, 0, Reference.transform.position.z);
            worldPosition.y = FieldManager.terrain.SampleHeight(worldPosition) + FieldManager.terrain.transform.position.y;

            if (pointlist.Count <= i)
            {
                pointlist.Add(position);
                GameObject newObj = Instantiate(waypointPrefab, worldPosition, Quaternion.identity, waypointParent);
                waypointObjects.Add(newObj);
            }

            else if (pointlist[i] != position)
            {
                pointlist[i] = position;
                Destroy(waypointObjects[i]);

                GameObject newObj = Instantiate(waypointPrefab, worldPosition, Quaternion.identity, waypointParent);
                waypointObjects[i] = newObj;  // 差し替える
            }
        }
    }
}
