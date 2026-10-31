using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

using RosPointCloud2 = RosMessageTypes.Sensor.PointCloud2Msg;
using RosPointField = RosMessageTypes.Sensor.PointFieldMsg;

/// <summary>
/// PointCloud2に格納された三角形メッシュをUnity Terrainに反映する。
///
/// PointCloud2の頂点を3点ずつ取り出し、
/// 三角形内部の高さを補間してTerrain Heightmapを更新する。
///
/// ROS座標 -> Unity座標:
/// Unity X = ROS X
/// Unity Y = ROS Z
/// Unity Z = ROS Y
///
/// 使用トピック:
/// /merged_pointcloud_filtered/points
/// </summary>
public class RosTerrainDeformer : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField]
    private Terrain targetTerrain;

    [Header("ROS 2")]
    [SerializeField]
    private string pointCloudTopic =
        "/merged_pointcloud_filtered/points";

    [Header("Processing")]
    [Tooltip("処理結果をログに表示する")]
    [SerializeField]
    private bool logStatistics = true;

    [Tooltip("Terrainの範囲外を無視する")]
    [SerializeField]
    private bool ignoreOutsideTerrain = true;

    [Tooltip("三角形内部の判定に使用する許容誤差")]
    [SerializeField]
    private float barycentricTolerance = 0.00001f;

    [Header("Height Offset")]
    [Tooltip("ROSのZ座標に加算する高さオフセット")]
    [SerializeField]
    private float heightOffset = 0f;

    private ROSConnection ros;
    private TerrainData terrainData;

    private int resolution;

    // 元のTerrainの高さ
    private float[,] originalHeights;

    // Terrainに適用する高さ
    private float[,] heights;

    // 三角形が重なった場合の高さ集計
    private float[,] heightSums;
    private int[,] heightCounts;

    private int receivedPointCount;
    private int validTriangleCount;
    private int appliedTriangleCount;
    private int updatedCellCount;

    private bool initialized;

    [Header("Map Reference")]
    [SerializeField] private string referenceObjectName = "MapReferencePoint";

    private Transform mapReference;

    private void Start()
    {
        InitializeTerrain();

        if (!initialized)
        {
            enabled = false;
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();

        ros.Subscribe<RosPointCloud2>(
            pointCloudTopic,
            OnPointCloudReceived
        );

        Debug.Log(
            "[RosTerrainDeformer] Initialized. " +
            $"Topic={pointCloudTopic}, " +
            $"Resolution={resolution}, " +
            $"TerrainPosition={targetTerrain.transform.position}, " +
            $"TerrainSize={terrainData.size}"
        );

        GameObject referenceObject = GameObject.Find(referenceObjectName);

        if (referenceObject != null)
        {
            mapReference = referenceObject.transform;

            Debug.Log(
                $"[RosTerrainDeformer] MapReferencePoint found: " +
                $"Position={mapReference.position}"
            );
        }
        else
        {
            Debug.LogError(
                $"[RosTerrainDeformer] Reference object '{referenceObjectName}' not found."
            );
        }
    }

    /// <summary>
    /// Terrainの初期化
    /// </summary>
    private void InitializeTerrain()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }

        if (targetTerrain == null)
        {
            Debug.LogError(
                "[RosTerrainDeformer] Terrainが設定されていません。"
            );
            return;
        }

        terrainData = targetTerrain.terrainData;

        if (terrainData == null)
        {
            Debug.LogError(
                "[RosTerrainDeformer] TerrainDataがありません。"
            );
            return;
        }

        resolution = terrainData.heightmapResolution;

        if (resolution < 2)
        {
            Debug.LogError(
                "[RosTerrainDeformer] Heightmap resolutionが不正です。"
            );
            return;
        }

        originalHeights = terrainData.GetHeights(
            0, 0, resolution, resolution
        );

        heights = (float[,])originalHeights.Clone();

        heightSums = new float[resolution, resolution];
        heightCounts = new int[resolution, resolution];

        initialized = true;
    }

    /// <summary>
    /// PointCloud2受信
    /// </summary>
    private void OnPointCloudReceived(RosPointCloud2 msg)
    {
        if (!initialized || msg == null)
        {
            return;
        }

        if (msg.data == null || msg.data.Length == 0)
        {
            Debug.LogWarning(
                "[RosTerrainDeformer] PointCloud2のdataが空です。"
            );
            return;
        }

        int pointStep = (int)msg.point_step;
        int width = (int)msg.width;
        int height = (int)msg.height;
        int rowStep = (int)msg.row_step;

        if (pointStep <= 0 || width <= 0 || height <= 0)
        {
            Debug.LogWarning(
                "[RosTerrainDeformer] PointCloud2の寸法が不正です。"
            );
            return;
        }

        if (rowStep <= 0)
        {
            rowStep = width * pointStep;
        }

        if (rowStep < width * pointStep)
        {
            Debug.LogError(
                "[RosTerrainDeformer] row_stepが不正です。"
            );
            return;
        }

        int xOffset;
        int yOffset;
        int zOffset;

        if (!TryGetFieldOffsets(
            msg.fields,
            pointStep,
            out xOffset,
            out yOffset,
            out zOffset))
        {
            Debug.LogError(
                "[RosTerrainDeformer] XYZフィールドが取得できません。"
            );
            return;
        }

        long requiredBytes =
            (long)(height - 1) * rowStep +
            (long)width * pointStep;

        if (requiredBytes > msg.data.Length)
        {
            Debug.LogError(
                "[RosTerrainDeformer] PointCloud2のdata長が不足しています。"
            );
            return;
        }

        ResetHeightmap();

        receivedPointCount = 0;
        validTriangleCount = 0;
        appliedTriangleCount = 0;
        updatedCellCount = 0;

        // 3頂点ずつ三角形を構成する
        Vector3[] triangle = new Vector3[3];
        bool[] valid = new bool[3];

        int vertexIndex = 0;

        for (int row = 0; row < height; row++)
        {
            int rowOffset = row * rowStep;

            for (int col = 0; col < width; col++)
            {
                int pointOffset =
                    rowOffset + col * pointStep;

                float rosX = ReadFloat32(
                    msg.data,
                    pointOffset + xOffset,
                    msg.is_bigendian
                );

                float rosY = ReadFloat32(
                    msg.data,
                    pointOffset + yOffset,
                    msg.is_bigendian
                );

                float rosZ = ReadFloat32(
                    msg.data,
                    pointOffset + zOffset,
                    msg.is_bigendian
                );

                receivedPointCount++;

                bool pointValid =
                    IsFinite(rosX) &&
                    IsFinite(rosY) &&
                    IsFinite(rosZ);

                Vector3 localPoint = Vector3.zero;

                if (pointValid)
                {
                    pointValid = TryConvertToTerrainLocal(
                        rosX,
                        rosY,
                        rosZ,
                        out localPoint
                    );
                }

                triangle[vertexIndex] = localPoint;
                valid[vertexIndex] = pointValid;

                vertexIndex++;

                if (vertexIndex == 3)
                {
                    if (valid[0] && valid[1] && valid[2])
                    {
                        validTriangleCount++;

                        if (RasterizeTriangle(
                            triangle[0],
                            triangle[1],
                            triangle[2]))
                        {
                            appliedTriangleCount++;
                        }
                    }

                    vertexIndex = 0;
                }
            }
        }

        // 3点に満たない端数は三角形として扱わない
        if (vertexIndex != 0 && logStatistics)
        {
            Debug.LogWarning(
                "[RosTerrainDeformer] " +
                $"三角形にならない頂点が{vertexIndex}点あります。"
            );
        }

        ApplyHeightmap();

        if (logStatistics)
        {
            Debug.Log(
                "[RosTerrainDeformer] Processing completed. " +
                $"ReceivedPoints={receivedPointCount}, " +
                $"ValidTriangles={validTriangleCount}, " +
                $"AppliedTriangles={appliedTriangleCount}, " +
                $"UpdatedCells={updatedCellCount}"
            );
        }
    }

    /// <summary>
    /// ROS座標をTerrainローカル座標に変換する
    /// </summary>
    private bool TryConvertToTerrainLocal(
        float rosX,
        float rosY,
        float rosZ,
        out Vector3 local)
    {
        // ROS座標 -> Unityワールド座標
        Vector3 unityWorldPosition = new Vector3(
            rosX,
            rosZ + heightOffset,
            rosY
        );

        // MapReferencePointの位置を加算
        if (mapReference != null)
        {
            unityWorldPosition += mapReference.position;
        }

        // Unityワールド座標 -> Terrainローカル座標
        local = targetTerrain.transform.InverseTransformPoint(
            unityWorldPosition
        );

        return
            IsFinite(local.x) &&
            IsFinite(local.y) &&
            IsFinite(local.z);
    }

    /// <summary>
    /// 三角形内部を走査し、Terrain Heightmapに高さを設定する。
    /// </summary>
    private bool RasterizeTriangle(
        Vector3 a,
        Vector3 b,
        Vector3 c)
    {
        Vector3 size = terrainData.size;

        if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
        {
            return false;
        }

        // XZ平面上の三角形の面積を判定
        float denominator =
            (b.z - c.z) * (a.x - c.x) +
            (c.x - b.x) * (a.z - c.z);

        if (Mathf.Abs(denominator) < 0.0000001f)
        {
            // XZ平面に投影したときに面積がない三角形
            return false;
        }

        // 三角形のXZ方向の範囲
        float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
        float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));

        float minZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
        float maxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));

        // Terrain範囲との交差判定
        if (ignoreOutsideTerrain)
        {
            if (maxX < 0f || minX > size.x ||
                maxZ < 0f || minZ > size.z)
            {
                return false;
            }
        }

        // Heightmap上のインデックスに変換
        int minIX = Mathf.Clamp(
            Mathf.CeilToInt(minX / size.x * (resolution - 1)),
            0,
            resolution - 1
        );

        int maxIX = Mathf.Clamp(
            Mathf.FloorToInt(maxX / size.x * (resolution - 1)),
            0,
            resolution - 1
        );

        int minIZ = Mathf.Clamp(
            Mathf.CeilToInt(minZ / size.z * (resolution - 1)),
            0,
            resolution - 1
        );

        int maxIZ = Mathf.Clamp(
            Mathf.FloorToInt(maxZ / size.z * (resolution - 1)),
            0,
            resolution - 1
        );

        bool applied = false;

        // 三角形のBounding Box内を走査
        for (int iz = minIZ; iz <= maxIZ; iz++)
        {
            for (int ix = minIX; ix <= maxIX; ix++)
            {
                // Heightmap上の位置をTerrainローカル座標に変換
                float px =
                    (float)ix / (resolution - 1) * size.x;

                float pz =
                    (float)iz / (resolution - 1) * size.z;

                float w1;
                float w2;
                float w3;

                if (!TryGetBarycentricCoordinates(
                    px,
                    pz,
                    a,
                    b,
                    c,
                    denominator,
                    out w1,
                    out w2,
                    out w3))
                {
                    continue;
                }

                // 三角形の外側なら無視
                if (w1 < -barycentricTolerance ||
                    w2 < -barycentricTolerance ||
                    w3 < -barycentricTolerance)
                {
                    continue;
                }

                // 三角形内部の高さを補間
                float localHeight =
                    w1 * a.y +
                    w2 * b.y +
                    w3 * c.y;

                // Terrain Heightmapは0～1の正規化値
                float normalizedHeight =
                    Mathf.Clamp01(localHeight / size.y);

                heightSums[iz, ix] += normalizedHeight;
                heightCounts[iz, ix]++;

                applied = true;
            }
        }

        return applied;
    }

    /// <summary>
    /// XZ平面上の重心座標を計算する。
    /// </summary>
    private bool TryGetBarycentricCoordinates(
        float px,
        float pz,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        float denominator,
        out float w1,
        out float w2,
        out float w3)
    {
        w1 =
            ((b.z - c.z) * (px - c.x) +
             (c.x - b.x) * (pz - c.z)) /
            denominator;

        w2 =
            ((c.z - a.z) * (px - c.x) +
             (a.x - c.x) * (pz - c.z)) /
            denominator;

        w3 = 1f - w1 - w2;

        return
            IsFinite(w1) &&
            IsFinite(w2) &&
            IsFinite(w3);
    }

    /// <summary>
    /// 集計した高さをTerrainに反映する。
    /// </summary>
    private void ApplyHeightmap()
    {
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        float originalMin = float.MaxValue;
        float originalMax = float.MinValue;

        float actualMin = float.MaxValue;
        float actualMax = float.MinValue;

        int changedCells = 0;
        updatedCellCount = 0;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float original = originalHeights[z, x];

                originalMin = Mathf.Min(originalMin, original);
                originalMax = Mathf.Max(originalMax, original);

                int count = heightCounts[z, x];

                if (count == 0)
                {
                    heights[z, x] = original;
                    actualMin = Mathf.Min(actualMin, original);
                    actualMax = Mathf.Max(actualMax, original);
                    continue;
                }

                float h = heightSums[z, x] / count;

                h = Mathf.Clamp01(h);

                heights[z, x] = h;

                minHeight = Mathf.Min(minHeight, h);
                maxHeight = Mathf.Max(maxHeight, h);

                actualMin = Mathf.Min(actualMin, h);
                actualMax = Mathf.Max(actualMax, h);

                updatedCellCount++;

                if (Mathf.Abs(h - original) > 0.0001f)
                {
                    changedCells++;
                }
            }
        }

        Debug.Log(
            "[RosTerrainDeformer] Heightmap statistics\n" +
            $"UpdatedCells={updatedCellCount}\n" +
            $"ChangedCells={changedCells}\n" +
            $"TriangleHeightMin={minHeight:F6}\n" +
            $"TriangleHeightMax={maxHeight:F6}\n" +
            $"OriginalTerrainMin={originalMin:F6}\n" +
            $"OriginalTerrainMax={originalMax:F6}\n" +
            $"FinalTerrainMin={actualMin:F6}\n" +
            $"FinalTerrainMax={actualMax:F6}\n" +
            $"TerrainHeight={terrainData.size.y:F3}m"
        );

        if (updatedCellCount == 0)
        {
            Debug.LogWarning(
                "[RosTerrainDeformer] 更新対象のセルがありません。"
            );
            return;
        }

        terrainData.SetHeights(0, 0, heights);

        Debug.Log(
            "[RosTerrainDeformer] SetHeights completed."
        );
    }

    /// <summary>
    /// Heightmapの集計値を初期化する。
    /// </summary>
    private void ResetHeightmap()
    {
        heights = (float[,])originalHeights.Clone();

        Array.Clear(
            heightSums,
            0,
            heightSums.Length
        );

        Array.Clear(
            heightCounts,
            0,
            heightCounts.Length
        );
    }

    /// <summary>
    /// PointCloud2のXYZフィールドのオフセットを取得する。
    /// FLOAT32 datatype = 7
    /// </summary>
    private bool TryGetFieldOffsets(
        RosPointField[] fields,
        int pointStep,
        out int xOffset,
        out int yOffset,
        out int zOffset)
    {
        xOffset = -1;
        yOffset = -1;
        zOffset = -1;

        if (fields == null)
        {
            return false;
        }

        foreach (RosPointField field in fields)
        {
            if (field == null)
            {
                continue;
            }

            int offset = (int)field.offset;

            if (offset < 0 ||
                offset + 4 > pointStep ||
                (int)field.datatype != 7 ||
                (int)field.count < 1)
            {
                continue;
            }

            if (field.name == "x")
            {
                xOffset = offset;
            }
            else if (field.name == "y")
            {
                yOffset = offset;
            }
            else if (field.name == "z")
            {
                zOffset = offset;
            }
        }

        return
            xOffset >= 0 &&
            yOffset >= 0 &&
            zOffset >= 0;
    }

    /// <summary>
    /// PointCloud2からFLOAT32を読み取る。
    /// </summary>
    private float ReadFloat32(
        byte[] data,
        int offset,
        bool isBigEndian)
    {
        if (offset < 0 || offset + 4 > data.Length)
        {
            return float.NaN;
        }

        bool dataIsBigEndian = !BitConverter.IsLittleEndian;

        if (dataIsBigEndian == isBigEndian)
        {
            return BitConverter.ToSingle(data, offset);
        }

        byte[] temp = new byte[4];

        temp[0] = data[offset + 3];
        temp[1] = data[offset + 2];
        temp[2] = data[offset + 1];
        temp[3] = data[offset];

        return BitConverter.ToSingle(temp, 0);
    }

    private bool IsFinite(float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }
}