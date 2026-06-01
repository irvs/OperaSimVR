using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public Terrain terrain;
    public Texture2D heightmap;  // 高さマップ画像
    public Texture2D texture;   // テクスチャ1（例: 草）
    public int TerrainWidth;//trrainの幅
    public int TerrainHeight;//terrainの奥行
    public int TerrainElevation = 600;//terrainの高さ
    public int WidthTexture = 300;//textureの幅
    public int HeightTexture = 300;//terrainの奥行
    public float OffsetWidthTexture = 0.5f;//textureのoffset
    public float OffsetHeightTexture = 0.5f;//textureのoffset
    public bool TerrainRecordSw;

    private float[,] originalHeights;  // 元の地形の高さデータを保持する配列
    int originalheight;
    int originalwidth;

    void Start()
    {
        GenerateTerrainFromHeightmap();
        ApplyTextures();
    }

    void GenerateTerrainFromHeightmap()
    {

        // 高さマップを使用して地形を生成

        int width = heightmap.width;
        int height = heightmap.height;


        float[,] heights = new float[width, height];

        for (int x = 0; x < width; x += 1)
        {
            for (int y = 0; y < height; y += 1)
            {
                // ピクセルの輝度を地形の高さに変換（0?1の範囲）
                heights[y, x] = heightmap.GetPixel(x, y).grayscale;
            }
        }

        // Terrainのサイズを高さマップのサイズに合わせる
        if (TerrainRecordSw == false)
        {
            terrain.terrainData = new TerrainData();
        }
          terrain.terrainData.heightmapResolution = width;

        terrain.terrainData.size = new Vector3(TerrainWidth, TerrainElevation, TerrainHeight); // 地形のサイズを設定（高さは適宜調整）

        // 高さマップに基づいて地形を設定
        terrain.terrainData.SetHeights(0, 0, heights);
        // 地形のterrainDataを取得
        TerrainData ThisterrainData = terrain.terrainData;
        // TerrainColliderを更新
        // terrainDataが更新されると、TerrainColliderも自動的に更新されますが、念のため再設定します
        terrain.GetComponent<TerrainCollider>().terrainData = ThisterrainData;

        // これで、TerrainColliderが新しい高さに基づいて衝突判定を行います

    }

    void ApplyTextures()
    {
        // 地形にテクスチャを適用
        TerrainLayer[] terrainLayers = new TerrainLayer[2];
        // テクスチャ1を設定
        TerrainLayer layer1 = new TerrainLayer();
        layer1.diffuseTexture = texture;
        // テクスチャのタイルサイズとオフセットを設定
        layer1.tileSize = new Vector2(WidthTexture, HeightTexture);  // テクスチャのサイズ (タイルあたりの広さ)
        layer1.tileOffset = new Vector2(OffsetWidthTexture, OffsetHeightTexture);  // テクスチャのオフセット (位置の調整)
        terrainLayers[0] = layer1;
        // 地形にTerrainLayerを設定
        terrain.terrainData.terrainLayers = terrainLayers;

    }

    
    // 元の地形状態を復元するメソッド
    void RestoreOriginalTerrainState()
    {
        if (originalHeights != null)
        {
            terrain.terrainData.size = new Vector3(originalwidth, TerrainElevation, originalheight);
            terrain.terrainData.SetHeights(0, 0, originalHeights);  // 高さマップを元に戻す                                                                    //
            // 地形のterrainDataを取得
            TerrainData ThisterrainData = terrain.terrainData;
            // TerrainColliderを更新
            // terrainDataが更新されると、TerrainColliderも自動的に更新されますが、念のため再設定します
            terrain.GetComponent<TerrainCollider>().terrainData = ThisterrainData;
            // これで、TerrainColliderが新しい高さに基づいて衝突判定を行います
            Debug.Log("Terrain return!!!.");
        }
    }
}
