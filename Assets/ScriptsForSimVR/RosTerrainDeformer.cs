using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

using RosPointCloud2 = RosMessageTypes.Sensor.PointCloud2Msg;
using RosPointField = RosMessageTypes.Sensor.PointFieldMsg;

/// <summary>
/// PointCloud2に格納された三角形メッシュを、TerrainTilerが生成した
/// 複数のUnity Terrainへ反映する。
///
/// PointCloud2の頂点を3点ずつ取り出し、各Terrainタイルの範囲内で
/// 三角形内部の高さを補間してHeightmapを更新する。
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
    [Header("Terrain Tiler")]
    [Tooltip("TerrainTilerが付いている元のTerrainオブジェクト")]
    [SerializeField]
    private TerrainTiler terrainTiler;

    [Header("ROS 2")]
    [SerializeField]
    private string pointCloudTopic = "/merged_pointcloud_filtered/points";

    [Header("Processing")]
    [Tooltip("処理結果をログに表示する")]
    [SerializeField]
    private bool logStatistics = true;

    [Tooltip("各Terrainタイルの範囲外を無視する")]
    [SerializeField]
    private bool ignoreOutsideTerrain = true;

    [Tooltip("三角形内部の判定に使用する許容誤差")]
    [SerializeField]
    private float barycentricTolerance = 0.00001f;

    [Header("Height Offset")]
    [Tooltip("ROSのZ座標に加算する高さオフセット")]
    [SerializeField]
    private float heightOffset = 0f;

    [Header("Map Reference")]
    [SerializeField]
    private string referenceObjectName = "MapReferencePoint";

    [Header("Terrain Restore")]
    [Tooltip("RosTerrainDeformerが無効化されたとき、各タイルを初期高さに戻す")]
    [SerializeField]
    private bool restoreTerrainOnStop = true;

    private ROSConnection ros;
    private Transform mapReference;
    private bool initialized;

    private int receivedPointCount;
    private int validTriangleCount;
    private int appliedTriangleCount;
    private int updatedCellCount;

    private sealed class TileData
    {
        public Terrain terrain;
        public TerrainData terrainData;
        public int resolution;

        public float[,] originalHeights;
        public float[,] heights;
        public float[,] heightSums;
        public int[,] heightCounts;
    }

    private readonly List<TileData> tileDataList = new List<TileData>();

    private IEnumerator Start()
    {
        // TerrainTiler.Start()でタイル生成が完了するまで待機する。
        if (terrainTiler == null)
            terrainTiler = FindObjectOfType<TerrainTiler>();

        while (terrainTiler != null && !terrainTiler.IsInitialized)
            yield return null;

        if (terrainTiler == null)
        {
            Debug.LogError(
                "[RosTerrainDeformer] TerrainTilerが見つかりません。");
            enabled = false;
            yield break;
        }

        if (!InitializeTiles())
        {
            enabled = false;
            yield break;
        }

        GameObject referenceObject = GameObject.Find(referenceObjectName);
        if (referenceObject != null)
        {
            mapReference = referenceObject.transform;
            Debug.Log(
                $"[RosTerrainDeformer] MapReferencePoint found: " +
                $"Position={mapReference.position}");
        }
        else
        {
            Debug.LogWarning(
                $"[RosTerrainDeformer] Reference object '{referenceObjectName}' " +
                "が見つかりません。MapReferencePointなしで座標変換します。");
        }

        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<RosPointCloud2>(pointCloudTopic, OnPointCloudReceived);

        initialized = true;

        Debug.Log(
            "[RosTerrainDeformer] Initialized. " +
            $"Topic={pointCloudTopic}, Tiles={tileDataList.Count}");
    }

    /// <summary>
    /// TerrainTilerが生成した各タイルを初期化し、初期Heightmapを保存する。
    /// </summary>
    private bool InitializeTiles()
    {
        tileDataList.Clear();

        if (terrainTiler == null || terrainTiler.terrains == null)
        {
            Debug.LogError(
                "[RosTerrainDeformer] TerrainTilerまたはterrainsがnullです。");
            return false;
        }

        foreach (GameObject tileObject in terrainTiler.terrains)
        {
            if (tileObject == null)
                continue;

            Terrain terrain = tileObject.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
                continue;

            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;

            if (res < 2)
                continue;

            TileData tile = new TileData
            {
                terrain = terrain,
                terrainData = data,
                resolution = res,
                originalHeights = data.GetHeights(0, 0, res, res)
            };

            tile.heights = (float[,])tile.originalHeights.Clone();
            tile.heightSums = new float[res, res];
            tile.heightCounts = new int[res, res];

            tileDataList.Add(tile);
        }

        if (tileDataList.Count == 0)
        {
            Debug.LogError(
                "[RosTerrainDeformer] 有効なTerrainタイルがありません。");
            return false;
        }

        Debug.Log(
            $"[RosTerrainDeformer] Initialized {tileDataList.Count} terrain tiles.");

        return true;
    }

    /// <summary>
    /// PointCloud2受信
    /// </summary>
    private void OnPointCloudReceived(RosPointCloud2 msg)
    {
        if (!initialized || msg == null)
            return;

        if (msg.data == null || msg.data.Length == 0)
        {
            Debug.LogWarning(
                "[RosTerrainDeformer] PointCloud2のdataが空です。");
            return;
        }

        int pointStep = (int)msg.point_step;
        int width = (int)msg.width;
        int height = (int)msg.height;
        int rowStep = (int)msg.row_step;

        if (pointStep <= 0 || width <= 0 || height <= 0)
        {
            Debug.LogWarning(
                "[RosTerrainDeformer] PointCloud2の寸法が不正です。");
            return;
        }

        if (rowStep <= 0)
            rowStep = width * pointStep;

        if (rowStep < width * pointStep)
        {
            Debug.LogError(
                "[RosTerrainDeformer] row_stepが不正です。");
            return;
        }

        int xOffset;
        int yOffset;
        int zOffset;

        if (!TryGetFieldOffsets(
            msg.fields, pointStep,
            out xOffset, out yOffset, out zOffset))
        {
            Debug.LogError(
                "[RosTerrainDeformer] XYZフィールドが取得できません。");
            return;
        }

        long requiredBytes =
            (long)(height - 1) * rowStep +
            (long)width * pointStep;

        if (requiredBytes > msg.data.Length)
        {
            Debug.LogError(
                "[RosTerrainDeformer] PointCloud2のdata長が不足しています。");
            return;
        }

        ResetAllTileHeightmaps();

        receivedPointCount = 0;
        validTriangleCount = 0;
        appliedTriangleCount = 0;
        updatedCellCount = 0;

        // 3頂点ずつ三角形を構成する。座標変換はワールド座標まで行い、
        // タイルごとのローカル座標変換はRasterizeTriangleForTile内で行う。
        Vector3[] triangleWorld = new Vector3[3];
        bool[] valid = new bool[3];
        int vertexIndex = 0;

        for (int row = 0; row < height; row++)
        {
            int rowOffset = row * rowStep;

            for (int col = 0; col < width; col++)
            {
                int pointOffset = rowOffset + col * pointStep;

                float rosX = ReadFloat32(
                    msg.data, pointOffset + xOffset, msg.is_bigendian);

                float rosY = ReadFloat32(
                    msg.data, pointOffset + yOffset, msg.is_bigendian);

                float rosZ = ReadFloat32(
                    msg.data, pointOffset + zOffset, msg.is_bigendian);

                receivedPointCount++;

                bool pointValid =
                    IsFinite(rosX) &&
                    IsFinite(rosY) &&
                    IsFinite(rosZ);

                Vector3 worldPoint = Vector3.zero;

                if (pointValid)
                    pointValid = TryConvertToUnityWorld(rosX, rosY, rosZ, out worldPoint);

                triangleWorld[vertexIndex] = worldPoint;
                valid[vertexIndex] = pointValid;
                vertexIndex++;

                if (vertexIndex == 3)
                {
                    if (valid[0] && valid[1] && valid[2])
                    {
                        validTriangleCount++;

                        if (RasterizeTriangleToAllTiles(
                            triangleWorld[0],
                            triangleWorld[1],
                            triangleWorld[2]))
                        {
                            appliedTriangleCount++;
                        }
                    }

                    vertexIndex = 0;
                }
            }
        }

        if (vertexIndex != 0 && logStatistics)
        {
            Debug.LogWarning(
                "[RosTerrainDeformer] " +
                $"三角形にならない頂点が{vertexIndex}点あります。");
        }

        ApplyAllTileHeightmaps();

        if (logStatistics)
        {
            Debug.Log(
                "[RosTerrainDeformer] Processing completed. " +
                $"ReceivedPoints={receivedPointCount}, " +
                $"ValidTriangles={validTriangleCount}, " +
                $"AppliedTriangles={appliedTriangleCount}, " +
                $"UpdatedCells={updatedCellCount}, " +
                $"Tiles={tileDataList.Count}");
        }
    }

    /// <summary>
    /// ROS座標をUnityワールド座標に変換する。
    /// 各Terrainのローカル座標変換は後段で行う。
    /// </summary>
    private bool TryConvertToUnityWorld(
        float rosX,
        float rosY,
        float rosZ,
        out Vector3 world)
    {
        world = new Vector3(
            rosX,
            rosZ + heightOffset,
            rosY);

        if (mapReference != null)
            world += mapReference.position;

        return IsFinite(world.x) && IsFinite(world.y) && IsFinite(world.z);
    }

    /// <summary>
    /// 三角形を全タイルに対して処理する。
    /// タイル境界をまたぐ三角形も、各タイル側で個別にラスタライズする。
    /// </summary>
    private bool RasterizeTriangleToAllTiles(
        Vector3 worldA,
        Vector3 worldB,
        Vector3 worldC)
    {
        bool applied = false;

        foreach (TileData tile in tileDataList)
        {
            Vector3 a = tile.terrain.transform.InverseTransformPoint(worldA);
            Vector3 b = tile.terrain.transform.InverseTransformPoint(worldB);
            Vector3 c = tile.terrain.transform.InverseTransformPoint(worldC);

            if (RasterizeTriangleForTile(tile, a, b, c))
                applied = true;
        }

        return applied;
    }

    /// <summary>
    /// 指定タイルのローカル座標で三角形をラスタライズする。
    /// </summary>
    private bool RasterizeTriangleForTile(
        TileData tile,
        Vector3 a,
        Vector3 b,
        Vector3 c)
    {
        TerrainData data = tile.terrainData;
        Vector3 size = data.size;
        int resolution = tile.resolution;

        if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
            return false;

        float denominator =
            (b.z - c.z) * (a.x - c.x) +
            (c.x - b.x) * (a.z - c.z);

        if (Mathf.Abs(denominator) < 0.0000001f)
            return false;

        float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
        float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        float minZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
        float maxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));

        if (ignoreOutsideTerrain)
        {
            if (maxX < 0f || minX > size.x ||
                maxZ < 0f || minZ > size.z)
                return false;
        }

        // 三角形のXZ範囲をHeightmapのインデックス範囲に変換する。
        // タイル外の頂点があっても、タイル内に入る部分を処理する。
        int minIX = Mathf.Clamp(
            Mathf.CeilToInt(minX / size.x * (resolution - 1)),
            0, resolution - 1);

        int maxIX = Mathf.Clamp(
            Mathf.FloorToInt(maxX / size.x * (resolution - 1)),
            0, resolution - 1);

        int minIZ = Mathf.Clamp(
            Mathf.CeilToInt(minZ / size.z * (resolution - 1)),
            0, resolution - 1);

        int maxIZ = Mathf.Clamp(
            Mathf.FloorToInt(maxZ / size.z * (resolution - 1)),
            0, resolution - 1);

        if (minIX > maxIX || minIZ > maxIZ)
            return false;

        bool applied = false;

        for (int iz = minIZ; iz <= maxIZ; iz++)
        {
            for (int ix = minIX; ix <= maxIX; ix++)
            {
                float px = (float)ix / (resolution - 1) * size.x;
                float pz = (float)iz / (resolution - 1) * size.z;

                float w1, w2, w3;

                if (!TryGetBarycentricCoordinates(
                    px, pz, a, b, c, denominator,
                    out w1, out w2, out w3))
                    continue;

                if (w1 < -barycentricTolerance ||
                    w2 < -barycentricTolerance ||
                    w3 < -barycentricTolerance)
                    continue;

                float localHeight =
                    w1 * a.y +
                    w2 * b.y +
                    w3 * c.y;

                float normalizedHeight =
                    Mathf.Clamp01(localHeight / size.y);

                tile.heightSums[iz, ix] += normalizedHeight;
                tile.heightCounts[iz, ix]++;
                applied = true;
            }
        }

        return applied;
    }

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

        return IsFinite(w1) && IsFinite(w2) && IsFinite(w3);
    }

    /// <summary>
    /// 全タイルの集計配列を初期化する。
    /// 毎回、Play開始時に保存した初期Heightmapを基準に更新する。
    /// </summary>
    private void ResetAllTileHeightmaps()
    {
        foreach (TileData tile in tileDataList)
        {
            tile.heights = (float[,])tile.originalHeights.Clone();

            Array.Clear(tile.heightSums, 0, tile.heightSums.Length);
            Array.Clear(tile.heightCounts, 0, tile.heightCounts.Length);
        }
    }

    /// <summary>
    /// 全タイルの高さを計算してTerrainへ反映する。
    /// </summary>
    private void ApplyAllTileHeightmaps()
    {
        foreach (TileData tile in tileDataList)
            ApplyTileHeightmap(tile);
    }

    private void ApplyTileHeightmap(TileData tile)
    {
        int resolution = tile.resolution;

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        float originalMin = float.MaxValue;
        float originalMax = float.MinValue;
        float actualMin = float.MaxValue;
        float actualMax = float.MinValue;

        int changedCells = 0;
        int tileUpdatedCells = 0;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float original = tile.originalHeights[z, x];

                originalMin = Mathf.Min(originalMin, original);
                originalMax = Mathf.Max(originalMax, original);

                int count = tile.heightCounts[z, x];

                if (count == 0)
                {
                    tile.heights[z, x] = original;
                    actualMin = Mathf.Min(actualMin, original);
                    actualMax = Mathf.Max(actualMax, original);
                    continue;
                }

                float h = Mathf.Clamp01(tile.heightSums[z, x] / count);
                tile.heights[z, x] = h;

                minHeight = Mathf.Min(minHeight, h);
                maxHeight = Mathf.Max(maxHeight, h);
                actualMin = Mathf.Min(actualMin, h);
                actualMax = Mathf.Max(actualMax, h);

                tileUpdatedCells++;
                if (Mathf.Abs(h - original) > 0.0001f)
                    changedCells++;
            }
        }

        updatedCellCount += tileUpdatedCells;

        if (logStatistics)
        {
            string minText = tileUpdatedCells > 0 ? minHeight.ToString("F6") : "N/A";
            string maxText = tileUpdatedCells > 0 ? maxHeight.ToString("F6") : "N/A";

            Debug.Log(
                $"[RosTerrainDeformer] Tile={tile.terrain.name} statistics\n" +
                $"UpdatedCells={tileUpdatedCells}\n" +
                $"ChangedCells={changedCells}\n" +
                $"TriangleHeightMin={minText}\n" +
                $"TriangleHeightMax={maxText}\n" +
                $"OriginalTerrainMin={originalMin:F6}\n" +
                $"OriginalTerrainMax={originalMax:F6}\n" +
                $"FinalTerrainMin={actualMin:F6}\n" +
                $"FinalTerrainMax={actualMax:F6}\n" +
                $"TerrainHeight={tile.terrainData.size.y:F3}m");
        }

        if (tileUpdatedCells == 0)
            return;

        tile.terrainData.SetHeights(0, 0, tile.heights);
    }

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
            return false;

        foreach (RosPointField field in fields)
        {
            if (field == null)
                continue;

            int offset = (int)field.offset;

            if (offset < 0 ||
                offset + 4 > pointStep ||
                (int)field.datatype != 7 ||
                (int)field.count < 1)
                continue;

            if (field.name == "x")
                xOffset = offset;
            else if (field.name == "y")
                yOffset = offset;
            else if (field.name == "z")
                zOffset = offset;
        }

        return xOffset >= 0 && yOffset >= 0 && zOffset >= 0;
    }

    private float ReadFloat32(byte[] data, int offset, bool isBigEndian)
    {
        if (offset < 0 || offset + 4 > data.Length)
            return float.NaN;

        bool dataIsBigEndian = !BitConverter.IsLittleEndian;

        if (dataIsBigEndian == isBigEndian)
            return BitConverter.ToSingle(data, offset);

        byte[] temp = new byte[4];
        temp[0] = data[offset + 3];
        temp[1] = data[offset + 2];
        temp[2] = data[offset + 1];
        temp[3] = data[offset];

        return BitConverter.ToSingle(temp, 0);
    }

    private bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void RestoreOriginalTerrainTiles()
    {
        if (!restoreTerrainOnStop)
            return;

        foreach (TileData tile in tileDataList)
        {
            if (tile == null ||
                tile.terrainData == null ||
                tile.originalHeights == null)
                continue;

            tile.terrainData.SetHeights(0, 0, tile.originalHeights);
        }

        if (tileDataList.Count > 0)
            Debug.Log("[RosTerrainDeformer] All terrain tiles restored.");
    }

    private void OnDisable()
    {
        RestoreOriginalTerrainTiles();
    }
}
