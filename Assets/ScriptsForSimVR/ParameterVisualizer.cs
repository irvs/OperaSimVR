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

    [Header("GeoFence Visualization")]
    [SerializeField] private string geoFenceTopic = "/geo_fence_params_json";
    [SerializeField] private bool showGeoFence = true;
    [SerializeField] private Color geoFenceColor = Color.yellow;
    [SerializeField] private float geoFenceLineWidth = 0.2f;

    // 頂点Cylinderの設定
    [SerializeField] private float geoFenceVertexDiameter = 0.15f;
    [SerializeField] private float geoFenceVertexHeight = 1.5f;
    [SerializeField] private Color geoFenceVertexColor = Color.red;

    // GeofenceのRaycastで検出する対象のLayer
    [SerializeField] private LayerMask geoFenceCollisionLayer;

    // Cylinderや境界線の親オブジェクト
    [SerializeField] private Transform geoFenceParent;

    private readonly List<GameObject> geoFenceObjects =
        new List<GameObject>();

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

    [SerializeField] private Terrain targetTerrain;

    // Terrain表面から少し浮かせる距離
    [SerializeField] private float terrainHeightOffset = 0.05f;


    [Header("Machinery Route Visualization")]
    [SerializeField] private string machineryConnectionTopic =
        "/machinery_connection_params_json";

    [SerializeField] private bool showMachineryConnections = true;

    // 建機位置の赤色と区別するため、オレンジ色にする
    [SerializeField] private Color machineryConnectionColor =
        new Color(1f, 0.25f, 0f, 1f);

    [SerializeField] private float machineryConnectionWidth = 0.15f;

    private readonly List<GameObject> machineryConnectionObjects =
        new List<GameObject>();

    /// <summary>
    /// Unityのワールド座標(X, Z)に対応するTerrain表面の高さを取得する。
    /// Terrainの範囲外、またはTerrain未設定の場合は元の高さを維持する。
    /// </summary>

    private float GetTerrainHeight(float worldX, float worldZ, float fallbackY)
    {
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return fallbackY;

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = targetTerrain.terrainData.size;

        float localX = worldX - terrainPos.x;
        float localZ = worldZ - terrainPos.z;

        // Terrainの範囲外なら従来の高さを使う
        if (localX < 0f || localX > terrainSize.x ||
            localZ < 0f || localZ > terrainSize.z)
        {
            return fallbackY;
        }

        float height = targetTerrain.SampleHeight(
            new Vector3(worldX, terrainPos.y, worldZ)
        );

        return height + terrainPos.y + terrainHeightOffset;
    }

    private Vector3 RosPositionToTerrain(Vector3 rosPosition)
    {
        // 既存のROS -> Unity変換とMapReferencePointの補正を維持
        Vector3 unityPosition = RosPositionToUnity(
            rosPosition.x,
            rosPosition.y,
            rosPosition.z
        );

        // Y座標だけTerrain表面の高さに置き換える
        unityPosition.y = GetTerrainHeight(
            unityPosition.x,
            unityPosition.z,
            unityPosition.y
        );

        return unityPosition;
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
        ros.Subscribe<RosString>(geoFenceTopic, OnGeoFenceJson);
        ros.Subscribe<RosString>(machineryConnectionTopic, OnMachineryConnectionJson);

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
                    // Vector3 position = RosPositionToUnity(xs[i], ys[i], z);
                    Vector3 position = RosPositionToTerrain(new Vector3(xs[i], ys[i], z));

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

    /// <summary>
    /// GeoFenceのJSONを受信して、Unity上に境界線を描画する。
    /// Marker / MarkerArrayは使用しない。
    /// </summary>
    private void OnGeoFenceJson(RosString message)
    {
        if (!showGeoFence ||
            message == null ||
            string.IsNullOrWhiteSpace(message.data))
        {
            return;
        }

        try
        {
            JArray records = JArray.Parse(message.data);

            if (clearAndRebuildOnMessage)
            {
                ClearObjects(geoFenceObjects);
            }

            int created = 0;

            foreach (JToken token in records)
            {
                JObject record = token as JObject;
                JObject data = record?["data"] as JObject;

                string recordName =
                    (string)record?["record_name"] ?? "geo_fence";

                if (data == null || data["coordinates"] == null)
                {
                    Debug.LogWarning(
                        $"[TmsParameterJsonVisualizer] " +
                        $"GeoFence '{recordName}' skipped: coordinates missing."
                    );
                    continue;
                }

                JToken coordinates = data["coordinates"];

                // 外側の配列をたどり、境界線の座標列を取得する。
                // 例：
                // coordinates = [[{x:..., y:...}, ...]]
                // coordinates = [[[x, y], [x, y], ...]]
                JArray points = GetGeoFenceRing(coordinates);

                if (points == null || points.Count < 2)
                {
                    Debug.LogWarning(
                        $"[TmsParameterJsonVisualizer] " +
                        $"GeoFence '{recordName}' skipped: " +
                        "at least two points are required."
                    );
                    continue;
                }

                List<Vector3> positions = new List<Vector3>();

                foreach (JToken point in points)
                {
                    float x;
                    float y;
                    float z;

                    if (point is JObject pointObject)
                    {
                        // { "x": ..., "y": ..., "z": ... } 形式
                        x = GetFloat(pointObject, "x", float.NaN);
                        y = GetFloat(pointObject, "y", float.NaN);
                        z = GetFloat(pointObject, "z", 0f);
                    }
                    else if (point is JArray coordinateArray &&
                            coordinateArray.Count >= 2)
                    {
                        // GeoJSON形式 [x, y] または [x, y, z]
                        if (!TryGetCoordinate(coordinateArray[0], out x) ||
                            !TryGetCoordinate(coordinateArray[1], out y))
                        {
                            Debug.LogWarning(
                                $"[TmsParameterJsonVisualizer] " +
                                $"GeoFence '{recordName}' contains invalid coordinates."
                            );
                            continue;
                        }

                        z = 0f;

                        if (coordinateArray.Count >= 3)
                        {
                            if (!TryGetCoordinate(coordinateArray[2], out z))
                            {
                                Debug.LogWarning(
                                    $"[TmsParameterJsonVisualizer] " +
                                    $"GeoFence '{recordName}' contains invalid Z coordinate."
                                );
                                continue;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[TmsParameterJsonVisualizer] " +
                            $"GeoFence '{recordName}' contains an unsupported point format."
                        );
                        continue;
                    }

                    if (float.IsNaN(x) || float.IsInfinity(x) ||
                        float.IsNaN(y) || float.IsInfinity(y) ||
                        float.IsNaN(z) || float.IsInfinity(z))
                    {
                        Debug.LogWarning(
                            $"[TmsParameterJsonVisualizer] " +
                            $"GeoFence '{recordName}' contains non-finite coordinates."
                        );
                        continue;
                    }

                    // 既存のブロック・建機表示と同じ座標変換を使用
                    positions.Add(RosPositionToUnity(x, y, z));
                }

                if (positions.Count < 2)
                {
                    Debug.LogWarning(
                        $"[TmsParameterJsonVisualizer] " +
                        $"GeoFence '{recordName}' skipped: " +
                        "not enough valid points."
                    );
                    continue;
                }

                // GeoFenceの親オブジェクト
                Transform parent = geoFenceParent != null
                    ? geoFenceParent
                    : machineryParent;

                // ============================================================
                // 1. 既存のGeoFence境界線を描画
                // ============================================================

                GameObject lineObject =
                    new GameObject($"GeoFence_{recordName}");

                if (parent != null)
                {
                    lineObject.transform.SetParent(parent, true);
                }

                LineRenderer line =
                    lineObject.AddComponent<LineRenderer>();

                line.useWorldSpace = true;
                line.positionCount = positions.Count;
                line.SetPositions(positions.ToArray());

                line.startWidth = geoFenceLineWidth;
                line.endWidth = geoFenceLineWidth;

                line.startColor = geoFenceColor;
                line.endColor = geoFenceColor;

                line.material = new Material(
                    Shader.Find("Sprites/Default")
                );

                line.numCapVertices = 2;
                line.numCornerVertices = 2;

                // 境界線を閉じる
                line.loop = true;

                geoFenceObjects.Add(lineObject);


                // ============================================================
                // 2. 各頂点にCylinderを生成
                // ============================================================

                List<GameObject> vertexObjects = new List<GameObject>();

                for (int i = 0; i < positions.Count; i++)
                {
                    GameObject vertex = GameObject.CreatePrimitive(
                        PrimitiveType.Cylinder
                    );

                    vertex.name = $"GeoFenceVertex_{recordName}_{i}";

                    if (parent != null)
                    {
                        vertex.transform.SetParent(parent, true);
                    }

                    // GeoFenceの頂点位置に配置
                    vertex.transform.position = positions[i];

                    // Cylinderの標準高さは2なので、Yスケールを高さの半分にする
                    vertex.transform.localScale = new Vector3(
                        geoFenceVertexDiameter,
                        geoFenceVertexHeight * 0.5f,
                        geoFenceVertexDiameter
                    );

                    // 表示用CylinderのColliderは無効化
                    // Raycastの検出対象にはしない
                    Collider vertexCollider = vertex.GetComponent<Collider>();

                    if (vertexCollider != null)
                    {
                        vertexCollider.enabled = false;
                    }

                    Renderer vertexRenderer = vertex.GetComponent<Renderer>();

                    if (vertexRenderer != null)
                    {
                        vertexRenderer.material = new Material(
                            Shader.Find("Standard")
                        );

                        vertexRenderer.material.color = geoFenceVertexColor;
                    }

                    vertexObjects.Add(vertex);
                    geoFenceObjects.Add(vertex);
                }


                // ============================================================
                // 3. 各頂点にGeofenceコンポーネントを追加
                //    EndpointObjを次の頂点に設定
                // ============================================================

                for (int i = 0; i < vertexObjects.Count; i++)
                {
                    GameObject vertex = vertexObjects[i];

                    // 次の頂点。最後の頂点は最初の頂点につなぐ
                    int nextIndex = (i + 1) % vertexObjects.Count;

                    GameObject nextVertex = vertexObjects[nextIndex];

                    Geofence geofence = vertex.AddComponent<Geofence>();

                    // 次の頂点を設定
                    geofence.EndpointObj = nextVertex.transform;

                    // 衝突判定対象のLayerを設定
                    geofence.collisionLayer = geoFenceCollisionLayer;
                }


                // ============================================================
                // 4. ログ
                // ============================================================

                created++;

                Debug.Log(
                    $"[TmsParameterJsonVisualizer] " +
                    $"GeoFence '{recordName}': " +
                    $"{positions.Count} vertices and " +
                    $"{positions.Count} collision segments created."
                );

                created++;

                Debug.Log(
                    $"[TmsParameterJsonVisualizer] " +
                    $"GeoFence '{recordName}': " +
                    $"{positions.Count} points created."
                );
            }

            Debug.Log(
                $"[TmsParameterJsonVisualizer] " +
                $"GeoFence objects created: {created}"
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[TmsParameterJsonVisualizer] " +
                $"Failed to parse GeoFence JSON: {e.Message}"
            );
        }
    }

    /// <summary>
    /// GeoFenceの座標列を取得する。
    /// Python側のcoordinates[0]という構造と、
    /// GeoJSONのPolygon/MultiLineStringの一般的な構造に対応する。
    /// </summary>
    private static JArray GetGeoFenceRing(JToken coordinates)
    {
        JToken current = coordinates;

        while (current is JArray array)
        {
            if (array.Count == 0)
            {
                return null;
            }

            // 先頭要素が座標オブジェクトなら、この配列が座標列
            if (array[0] is JObject)
            {
                return array;
            }

            // 先頭要素が数値配列 [x, y] なら、
            // 現在の配列が座標列
            if (array[0] is JArray firstArray &&
                firstArray.Count >= 2 &&
                firstArray[0].Type == JTokenType.Float ||
                array[0] is JArray firstNumericArray &&
                firstNumericArray.Count >= 2 &&
                firstNumericArray[0].Type == JTokenType.Integer)
            {
                return array;
            }

            // さらに内側の配列へ進む
            current = array[0];
        }

        return null;
    }

    /// <summary>
    /// JSONの数値をfloatに変換する。
    /// </summary>
    private static bool TryGetCoordinate(
        JToken token,
        out float value)
    {
        value = 0f;

        if (token == null)
        {
            return false;
        }

        return float.TryParse(
            token.ToString(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out value
        );
    }

    private void OnMachineryConnectionJson(RosString message)
    {
        if (!showMachineryConnections ||
            message == null ||
            string.IsNullOrWhiteSpace(message.data))
        {
            return;
        }

        try
        {
            JArray records = JArray.Parse(message.data);

            if (clearAndRebuildOnMessage)
            {
                ClearObjects(machineryConnectionObjects);
            }

            // node_id と Unity上の座標の対応表
            Dictionary<string, Vector3> nodePositions =
                new Dictionary<string, Vector3>();

            // 1. 全ノードの位置を取得
            foreach (JToken token in records)
            {
                JObject record = token as JObject;
                JObject data = record?["data"] as JObject;

                if (data == null ||
                    data["node_id"] == null ||
                    !(data["position"] is JObject position))
                {
                    continue;
                }

                string nodeId = data["node_id"].ToString();

                float x = GetFloat(position, "x", float.NaN);
                float y = GetFloat(position, "y", float.NaN);
                float z = GetFloat(position, "z", 0f);

                if (float.IsNaN(x) || float.IsNaN(y) ||
                    float.IsInfinity(x) || float.IsInfinity(y) ||
                    float.IsNaN(z) || float.IsInfinity(z))
                {
                    Debug.LogWarning(
                        $"[TmsParameterJsonVisualizer] " +
                        $"Invalid route node position: {nodeId}"
                    );
                    continue;
                }

                // 建機位置と同じ座標変換を行い、
                // Terrainが設定されていれば地表の高さに合わせる
                Vector3 unityPosition =
                    RosPositionToTerrain(new Vector3(x, y, z));

                nodePositions[nodeId] = unityPosition;
            }

            // 同じ接続を重複描画しないための集合
            HashSet<string> drawnEdges = new HashSet<string>();

            int created = 0;

            // 2. connect_node に従ってノード間に線を描画
            foreach (JToken token in records)
            {
                JObject record = token as JObject;
                JObject data = record?["data"] as JObject;

                if (data == null || data["node_id"] == null)
                {
                    continue;
                }

                string sourceId = data["node_id"].ToString();

                if (!nodePositions.ContainsKey(sourceId) ||
                    !(data["connect_node"] is JArray connectNodes))
                {
                    continue;
                }

                foreach (JToken targetToken in connectNodes)
                {
                    if (targetToken == null)
                    {
                        continue;
                    }

                    string targetId = targetToken.ToString();

                    if (sourceId == targetId ||
                        !nodePositions.ContainsKey(targetId))
                    {
                        continue;
                    }

                    // A -> B と B -> A を同じ接続として扱う
                    string edgeKey = string.CompareOrdinal(
                        sourceId, targetId) < 0
                        ? sourceId + "|" + targetId
                        : targetId + "|" + sourceId;

                    if (!drawnEdges.Add(edgeKey))
                    {
                        continue;
                    }

                    GameObject routeObject = new GameObject(
                        $"MachineryConnection_{sourceId}_{targetId}"
                    );

                    Transform parent = geoFenceParent != null
                        ? geoFenceParent
                        : machineryParent;

                    if (parent != null)
                    {
                        routeObject.transform.SetParent(parent, true);
                    }

                    LineRenderer line =
                        routeObject.AddComponent<LineRenderer>();

                    line.useWorldSpace = true;
                    line.positionCount = 2;

                    line.SetPosition(0, nodePositions[sourceId]);
                    line.SetPosition(1, nodePositions[targetId]);

                    line.startWidth = machineryConnectionWidth;
                    line.endWidth = machineryConnectionWidth;

                    line.startColor = machineryConnectionColor;
                    line.endColor = machineryConnectionColor;

                    line.material = new Material(
                        Shader.Find("Sprites/Default")
                    );

                    line.numCapVertices = 2;

                    machineryConnectionObjects.Add(routeObject);
                    created++;
                }
            }

            Debug.Log(
                $"[TmsParameterJsonVisualizer] " +
                $"Machinery connections created: {created}, " +
                $"nodes: {nodePositions.Count}"
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[TmsParameterJsonVisualizer] " +
                $"Failed to parse machinery connection JSON: {e.Message}"
            );
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
                reference.transform.position.y,
                // 0f,
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