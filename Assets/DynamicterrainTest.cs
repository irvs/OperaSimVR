using UnityEngine;

public class ApplyPartialHeightmap : MonoBehaviour
{
    public bool ApplyHeightmap;
    [Header("Terrain to modify")]
    public Terrain terrain;

    [Header("Heightmap image")]
    public Texture2D image;

    [Header("Apply area (in world meters)")]
    public Vector3 worldStartPosition = new Vector3(50, 0, 50); // ← Unity座標系(x, y, z)
    public float applyWidthMeters = 20f;   // x方向[m]
    public float applyHeightMeters = 20f;  // z方向[m]

    private float[,] originalHeights;

    public float maxImageHeightMeters = 5f;

    // 画像側の0が意味する基準高さ（例：0m）
    public float sourceBaseHeightMeters = 0f;

    // Terrain 側の最低高さ（例：50m）
    public float targetBaseHeightMeters = 10f;

    void Start()
    {
        if (terrain == null || image == null)
        {
            Debug.LogError("Terrain or image is missing.");
            return;
        }
    }

    void Update()
    {
        if (ApplyHeightmap) { ApplyNewTerrain(); }
        ApplyHeightmap = false;
    }

    void ApplyNewTerrain()
    {
        TerrainData terrainData = terrain.terrainData;

        // Terrainのワールド原点
        Vector3 terrainOrigin = terrain.GetPosition();
        Vector3 terrainSize = terrainData.size;
        int heightmapResolution = terrainData.heightmapResolution;

        // ワールド座標 → 高さマップインデックスに変換
        int startX = Mathf.RoundToInt((worldStartPosition.x - terrainOrigin.x) / terrainSize.x * (heightmapResolution - 1));
        int startY = Mathf.RoundToInt((worldStartPosition.z - terrainOrigin.z) / terrainSize.z * (heightmapResolution - 1));

        int areaWidth = Mathf.RoundToInt(applyWidthMeters / terrainSize.x * (heightmapResolution - 1));
        int areaHeight = Mathf.RoundToInt(applyHeightMeters / terrainSize.z * (heightmapResolution - 1));

        // 範囲チェック
        if (startX < 0 || startY < 0 || startX + areaWidth > heightmapResolution || startY + areaHeight > heightmapResolution)
        {
            Debug.LogError("範囲がTerrain外です。");
            return;
        }

        // 保存
        originalHeights = terrainData.GetHeights(startX, startY, areaWidth, areaHeight);

        // Resize画像
        Texture2D resizedImage = ResizeTexture(image, areaWidth, areaHeight);

        // 高さの配列に変換して反映
        float[,] newHeights = new float[areaHeight, areaWidth];

        // 地形全体の最大高さ
        float terrainHeightMax = terrainData.size.y;

        // 高さのオフセット（メートル）
        float heightOffset = targetBaseHeightMeters - sourceBaseHeightMeters;

        // 正規化された高さに変換
        float normalizedOffset = heightOffset / terrainHeightMax;

        for (int y = 0; y < areaHeight; y++)
        {
            for (int x = 0; x < areaWidth; x++)
            {
                float gray = resizedImage.GetPixel(x, y).grayscale;

                if (gray == 0.0f)
                {
                    newHeights[y, x] = originalHeights[y, x];
                }
                else
                {
                    float adjusted = gray * (maxImageHeightMeters / terrainData.size.y) + normalizedOffset;
                    newHeights[y, x] = Mathf.Clamp01(adjusted);
                }
            }
        }


        terrainData.SetHeights(startX, startY, newHeights);
        Debug.Log($"Applied heightmap to world position {worldStartPosition} with area {applyWidthMeters}x{applyHeightMeters} m");
    }

    void OnDisable()
    {
        if (terrain && originalHeights != null)
        {
            TerrainData terrainData = terrain.terrainData;

            Vector3 terrainOrigin = terrain.GetPosition();
            Vector3 terrainSize = terrainData.size;
            int heightmapResolution = terrainData.heightmapResolution;

            int startX = Mathf.RoundToInt((worldStartPosition.x - terrainOrigin.x) / terrainSize.x * (heightmapResolution - 1));
            int startY = Mathf.RoundToInt((worldStartPosition.z - terrainOrigin.z) / terrainSize.z * (heightmapResolution - 1));

            int areaWidth = originalHeights.GetLength(1);
            int areaHeight = originalHeights.GetLength(0);

            terrainData.SetHeights(startX, startY, originalHeights);
            Debug.Log("Terrain restored.");
        }
    }

    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}
