using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosString = RosMessageTypes.Std.StringMsg;

/// <summary>
/// Subscribes to JSON String topics published by TmsUrParameterImportPublisher
/// and creates Unity GameObjects for block and machinery-position visualization.
/// Marker / MarkerArray are not used.
/// Requires ROS-TCP-Connector and Newtonsoft.Json (JObject/JArray).
/// </summary>
public class TmsParameterJsonVisualizer : MonoBehaviour
{
    [Header("ROS 2 topics")]
    [SerializeField] private string blockTopic = "/block_position_params_json";
    [SerializeField] private string machineryTopic = "/machinery_position_params_json";
    [SerializeField] private string jointTopic = "/joint_angle_params";

    [Header("Visualization")]
    [SerializeField] private bool showBlocks = true;
    [SerializeField] private bool showMachineryPositions = true;
    [SerializeField] private bool logJointAngleRecords = true;
    [SerializeField] private bool clearAndRebuildOnMessage = true;
    [SerializeField] private float defaultBlockHeight = 0.1f;
    [SerializeField] private float machineryMarkerDiameter = 0.8f;
    [SerializeField] private float blockLabelHeight = 0.25f;
    [SerializeField] private Transform blockParent;
    [SerializeField] private Transform machineryParent;

    [Header("ROS map -> Unity coordinate conversion")]
    [Tooltip("Typical ROS map (X forward, Y left, Z up) to Unity (X right, Y up, Z forward): Unity=(-ROS Y, ROS Z, ROS X).")]
    [SerializeField] private bool convertRosToUnity = true;

    [Header("Colors")]
    [SerializeField] private Color blockColor = new Color(0.2f, 0.7f, 0.3f, 0.8f);
    [SerializeField] private Color machineryColor = Color.red;

    private ROSConnection ros;
    private readonly List<GameObject> blockObjects = new List<GameObject>();
    private readonly List<GameObject> machineryObjects = new List<GameObject>();
    private bool gotBlockMessage;
    private bool gotMachineryMessage;
    private GameObject reference;

    [Serializable]
    private class Record
    {
        public string record_name;
        public JObject data;
    }

    /// <summary>
    /// 表示用オブジェクトの Collider を無効化する
    /// </summary>
    private static void DisableColliders(GameObject target)
    {
        if (target == null)
            return;

        foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    /// <summary>
    /// ROS 座標系から Unity 座標系に変換する。
    /// ROS:   X=前方、Y=左、Z=上
    /// Unity: X=右、Y=上、Z=前方
    /// </summary>
    private Vector3 RosToUnity(float x, float y, float z)
    {
        return new Vector3(-y, z, x);
    }

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        reference = GameObject.Find("MapReferencePoint");

        ros.Subscribe<RosString>(blockTopic, OnBlockJson);
        ros.Subscribe<RosString>(machineryTopic, OnMachineryJson);
        ros.Subscribe<RosString>(jointTopic, OnJointJson);

        Debug.Log($"[TmsParameterJsonVisualizer] Subscribed: {blockTopic}, {machineryTopic}, {jointTopic}");
    }

    private void OnBlockJson(RosString message)
    {
        if (!showBlocks || message == null || string.IsNullOrWhiteSpace(message.data))
            return;

        try
        {
            JArray records = JArray.Parse(message.data);

            if (clearAndRebuildOnMessage)
            {
                ClearObjects(blockObjects);
            }

            int created = 0;

            foreach (JToken token in records)
            {
                JObject record = token as JObject;
                JObject data = record?["data"] as JObject;

                string recordName =
                    (string)record?["record_name"] ?? "block";

                if (data == null ||
                    data["block_center"] == null ||
                    !(data["block_size"] is JObject size) ||
                    !(data["block_vector"] is JObject vector))
                {
                    Debug.LogWarning(
                        $"[TmsParameterJsonVisualizer] " +
                        $"Block '{recordName}' skipped: " +
                        "block_center/block_size/block_vector missing."
                    );
                    continue;
                }

                // block_center が配列でも単一オブジェクトでも処理する
                IEnumerable<JToken> centers;

                if (data["block_center"] is JArray centerArray)
                {
                    centers = centerArray.Children();
                }
                else if (data["block_center"] is JObject)
                {
                    centers = new[] { data["block_center"] };
                }
                else
                {
                    Debug.LogWarning(
                        $"[TmsParameterJsonVisualizer] " +
                        $"Block '{recordName}' skipped: " +
                        "invalid block_center format."
                    );
                    continue;
                }

                // 同じレコード内の各中心座標からブロックを生成
                int centerIndex = 0;

                foreach (JToken centerToken in centers)
                {
                    JObject center = centerToken as JObject;

                    if (center == null)
                    {
                        Debug.LogWarning(
                            $"[TmsParameterJsonVisualizer] " +
                            $"Block '{recordName}' center[{centerIndex}] " +
                            "is not a JSON object."
                        );

                        centerIndex++;
                        continue;
                    }

                    float x = GetFloat(center, "x", 0f);
                    float y = GetFloat(center, "y", 0f);
                    float z = GetFloat(center, "z", 0f);

                    float sx = GetFloat(size, "x", 1f);
                    float sy = GetFloat(size, "y", 1f);
                    float sz = GetFloat(
                        size, "z", defaultBlockHeight
                    );

                    if (sz <= 0f)
                    {
                        sz = defaultBlockHeight;
                    }

                    if (sx <= 0f || sy <= 0f || sz <= 0f)
                    {
                        Debug.LogWarning(
                            $"[TmsParameterJsonVisualizer] " +
                            $"Block '{recordName}' center[{centerIndex}] " +
                            "has non-positive dimensions."
                        );

                        centerIndex++;
                        continue;
                    }

                    float vx = GetFloat(vector, "x", 1f);
                    float vy = GetFloat(vector, "y", 0f);

                    float vectorLength = Mathf.Sqrt(vx * vx + vy * vy);

                    if (vectorLength < 1e-6f)
                    {
                        Debug.LogWarning(
                            $"[TmsParameterJsonVisualizer] " +
                            $"Block '{recordName}' has a zero block_vector."
                        );

                        centerIndex++;
                        continue;
                    }

                    vx /= vectorLength;
                    vy /= vectorLength;

                    // block_angle はラジアンとして扱う
                    float blockAngle = GetFloat(
                        data,
                        "block_angle",
                        Mathf.PI / 2f
                    );

                    if (blockAngle <= 0f || blockAngle > Mathf.PI / 2f)
                    {
                        Debug.LogWarning(
                            $"[TmsParameterJsonVisualizer] " +
                            $"Block '{recordName}' has an invalid block_angle."
                        );

                        centerIndex++;
                        continue;
                    }

                    string objectName = $"{recordName}_{centerIndex}";

                    CreateSlopedBlock(
                        objectName,
                        x, y, z,
                        sx, sy, sz,
                        vx, vy,
                        blockAngle
                    );

                    created++;
                    centerIndex++;

                }
            }

            gotBlockMessage = true;

            Debug.Log(
                $"[TmsParameterJsonVisualizer] " +
                $"Block objects created: {created}"
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[TmsParameterJsonVisualizer] " +
                $"Failed to parse block JSON: {e.Message}"
            );
        }
    }

    private void OnMachineryJson(RosString message)
    {
        if (!showMachineryPositions || message == null || string.IsNullOrWhiteSpace(message.data))
            return;

        try
        {
            JArray records = JArray.Parse(message.data);
            if (clearAndRebuildOnMessage)
                ClearObjects(machineryObjects);

            int created = 0;
            foreach (JToken token in records)
            {
                JObject record = token as JObject;
                JObject data = record?["data"] as JObject;
                string recordName = (string)record?["record_name"] ?? "machinery_position";
                if (data == null)
                    continue;

                List<float> xs = GetFloatList(data["x"]);
                List<float> ys = GetFloatList(data["y"]);
                List<float> zs = GetFloatList(data["z"]);
                if (xs.Count == 0 || ys.Count == 0)
                {
                    Debug.LogWarning($"[TmsParameterJsonVisualizer] Machinery '{recordName}' skipped: x/y missing.");
                    continue;
                }

                int count = Mathf.Min(xs.Count, ys.Count);
                for (int i = 0; i < count; i++)
                {
                    float z = i < zs.Count ? zs[i] : 0f;
                    Vector3 position = RosPositionToUnity(xs[i], ys[i], z);

                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    // 表示用オブジェクトなので衝突判定を無効化
                    DisableColliders(marker);
                    marker.name = $"MachineryPosition_{recordName}_{i}";
                    marker.transform.SetParent(machineryParent, false);
                    marker.transform.position = position;
                    marker.transform.localScale = Vector3.one * machineryMarkerDiameter;
                    Renderer renderer = marker.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = new Material(Shader.Find("Standard"));
                        renderer.material.color = machineryColor;
                    }
                    machineryObjects.Add(marker);

                    GameObject label = new GameObject($"Label_Machinery_{recordName}_{i}");
                    label.transform.SetParent(machineryParent, false);
                    label.transform.position = position + Vector3.up * (machineryMarkerDiameter * 0.75f);
                    TextMesh text = label.AddComponent<TextMesh>();
                    text.text = $"{recordName}[{i}]";
                    text.characterSize = 0.12f;
                    text.fontSize = 64;
                    text.anchor = TextAnchor.MiddleCenter;
                    text.alignment = TextAlignment.Center;
                    text.color = Color.yellow;
                    label.AddComponent<BillboardToCamera>();
                    machineryObjects.Add(label);
                    created++;
                }
            }

            gotMachineryMessage = true;
            Debug.Log($"[TmsParameterJsonVisualizer] Machinery position markers created: {created}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TmsParameterJsonVisualizer] Failed to parse machinery JSON: {e.Message}");
        }
    }

    private void OnJointJson(RosString message)
    {
        if (!logJointAngleRecords || message == null || string.IsNullOrWhiteSpace(message.data))
            return;

        try
        {
            JArray records = JArray.Parse(message.data);
            Debug.Log($"[TmsParameterJsonVisualizer] Joint-angle records received: {records.Count}");
            foreach (JToken token in records)
            {
                string name = (string)token?["record_name"] ?? "(unnamed)";
                // JSON structure varies by DB record; log raw data so the exact joint fields remain available.
                Debug.Log($"[JointAngle] {name}: {token?["data"]?.ToString(Newtonsoft.Json.Formatting.None)}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TmsParameterJsonVisualizer] Failed to parse joint-angle JSON: {e.Message}");
        }
    }

    // private Vector3 RosPositionToUnity(float x, float y, float z)
    // {
    //     return convertRosToUnity ? new Vector3(-y, z, x) : new Vector3(x, y, z);
    // }

    private Vector3 RosPositionToUnity(float x, float y, float z)
    {

        // ROS 座標系から Unity 座標系へ変換
        Vector3 modifyPosition = convertRosToUnity
            ? new Vector3(x, z, y)
            : new Vector3(x, y, z);

        // PoseSubscriber と同様に MapReferencePoint の位置を加算
        if (reference != null)
        {
            modifyPosition += new Vector3(
                reference.transform.position.x,
                0f,
                reference.transform.position.z
            );
        }

        return modifyPosition;

    }

    private static float GetFloat(JObject obj, string key, float fallback)
    {
        JToken value = obj?[key];
        if (value == null || value.Type == JTokenType.Null)
            return fallback;
        if (value.Type == JTokenType.Array)
            value = value.First;
        float result;
        return float.TryParse(value?.ToString(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out result) ? result : fallback;
    }

    private void CreateSlopedBlock(
        string objectName,
        float x, float y, float z,
        float sizeX, float sizeY, float sizeZ,
        float vectorX, float vectorY,
        float blockAngle)
    {
        // block_vector を正規化
        Vector2 direction = new Vector2(vectorX, vectorY).normalized;

        // block_vector と直交する幅方向
        Vector2 perpendicular = new Vector2(
            -direction.y,
            direction.x
        );

        // 斜面の水平オフセット
        float tanAngle = Mathf.Tan(blockAngle);

        float offset = Mathf.Abs(tanAngle) < 1e-6f
            ? 0f
            : sizeZ / tanAngle;

        // 斜面の向きを180度反転
        offset = -offset;

        // オフセットがブロック長を超えないよう制限
        offset = Mathf.Clamp(offset, -sizeX * 0.99f, sizeX * 0.99f);

        // block_center を底面の基準点とする
        Vector2 center = new Vector2(x, y);

        Vector2 start = center - direction * sizeX;

        // 底面の4頂点
        Vector3 p0 = RosPositionToUnity(
            start.x, start.y, z);

        Vector3 p1 = RosPositionToUnity(
            center.x, center.y, z);

        Vector3 p2 = RosPositionToUnity(
            start.x + perpendicular.x * sizeY,
            start.y + perpendicular.y * sizeY,
            z);

        Vector3 p3 = RosPositionToUnity(
            center.x + perpendicular.x * sizeY,
            center.y + perpendicular.y * sizeY,
            z);

        // 上面の4頂点
        // 上面全体を斜面方向と反対側へ水平移動させる
        Vector3 p4 = RosPositionToUnity(
            start.x + direction.x * offset,
            start.y + direction.y * offset,
            z + sizeZ);

        Vector3 p5 = RosPositionToUnity(
            center.x + direction.x * offset,
            center.y + direction.y * offset,
            z + sizeZ);

        Vector3 p6 = RosPositionToUnity(
            start.x + direction.x * offset
                + perpendicular.x * sizeY,
            start.y + direction.y * offset
                + perpendicular.y * sizeY,
            z + sizeZ);

        Vector3 p7 = RosPositionToUnity(
            center.x + direction.x * offset
                + perpendicular.x * sizeY,
            center.y + direction.y * offset
                + perpendicular.y * sizeY,
            z + sizeZ);

        Vector3[] vertices =
        {
            p0, p1, p2, p3,
            p4, p5, p6, p7
        };

        int[] triangles =
        {
            // 底面
            0, 2, 1,
            1, 2, 3,

            // 上面
            4, 5, 6,
            5, 7, 6,

            // 側面
            0, 1, 4,
            1, 5, 4,

            2, 6, 3,
            3, 6, 7,

            0, 4, 2,
            2, 4, 6,

            1, 3, 5,
            3, 7, 5
        };

        Vector3 origin = vertices[0];

        Vector3[] localVertices = new Vector3[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            localVertices[i] = vertices[i] - origin;
        }

        Mesh mesh = new Mesh();
        mesh.name = $"BlockMesh_{objectName}";
        mesh.vertices = localVertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject block = new GameObject($"Block_{objectName}");

        block.transform.SetParent(blockParent, false);
        block.transform.position = origin;
        block.transform.rotation = Quaternion.identity;
        block.transform.localScale = Vector3.one;

        MeshFilter meshFilter = block.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = block.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Standard"));
        meshRenderer.material.color = blockColor;

        blockObjects.Add(block);

        // ラベル
        GameObject label = new GameObject($"Label_{objectName}");

        label.transform.SetParent(blockParent, false);
        label.transform.position = RosPositionToUnity(
            x, y, z + sizeZ + blockLabelHeight);

        TextMesh text = label.AddComponent<TextMesh>();
        text.text = objectName;
        text.characterSize = 0.15f;
        text.fontSize = 64;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;

        label.AddComponent<BillboardToCamera>();

        blockObjects.Add(label);
    }

    private static List<float> GetFloatList(JToken token)
    {
        var values = new List<float>();
        if (token == null || token.Type == JTokenType.Null)
            return values;

        IEnumerable<JToken> items = token.Type == JTokenType.Array
            ? token.Children()
            : new[] { token };

        foreach (JToken item in items)
        {
            float value;
            if (float.TryParse(item.ToString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value))
                values.Add(value);
        }
        return values;
    }

    private static void ClearObjects(List<GameObject> objects)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                Destroy(obj);
        }
        objects.Clear();
    }
}

/// <summary>Rotates labels toward the active camera.</summary>
public class BillboardToCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
            transform.rotation = cam.transform.rotation;
    }
}