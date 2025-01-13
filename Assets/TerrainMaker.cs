using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public Terrain terrain;
    public Texture2D heightmap;  // 高さマップ画像
    public Texture2D heightmapTexture;    // 使用するHeightmap画像（テクスチャ）
    public Texture2D texture1;   // テクスチャ1（例: 草）
    public Texture2D texture2;   // テクスチャ2（例: 岩）
    public float textureBlendHeight = 0.5f; // 高さに基づいてテクスチャをブレンドする境界
    public int TerrainWidth;//trrainの幅
    public int TerrainHeight;//terrainの奥行
    public int TerrainElevation = 600;//terrainの高さ
    public int WidthTexture = 300;//textureの幅
    public int HeightTexture = 300;//terrainの奥行
    public float OffsetWidthTexture = 0.5f;//textureのoffset
    public float OffsetHeightTexture = 0.5f;//textureのoffset

    private float[,] originalHeights;  // 元の地形の高さデータを保持する配列
    int originalheight;
    int originalwidth;

    void Start()
    {
      //  SaveOriginalTerrainState();  // 元の地形の状態を保存
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
          terrain.terrainData = new TerrainData();
          terrain.terrainData.heightmapResolution = width;
        //terrain.terrainData.heightmapResolution = TerrainWidth;
        //  terrain.terrainData.size = new Vector3(width, TerrainElevation, height); // 地形のサイズを設定（高さは適宜調整）
        terrain.terrainData.size = new Vector3(TerrainWidth, TerrainElevation, TerrainHeight); // 地形のサイズを設定（高さは適宜調整）

        // 高さマップに基づいて地形を設定
        terrain.terrainData.SetHeights(0, 0, heights);

        // 各領域に合わせてテクスチャを適用
        //ApplyTextureBasedOnHeight();

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
        layer1.diffuseTexture = texture1;

        // テクスチャのタイルサイズとオフセットを設定
        layer1.tileSize = new Vector2(WidthTexture, HeightTexture);  // テクスチャのサイズ (タイルあたりの広さ)
        layer1.tileOffset = new Vector2(OffsetWidthTexture, OffsetHeightTexture);  // テクスチャのオフセット (位置の調整)

        terrainLayers[0] = layer1;

        // テクスチャ2を設定（コメントアウトされている部分を再度有効にすることができます）
        /*
        TerrainLayer layer2 = new TerrainLayer();
        layer2.diffuseTexture = texture2;
        layer2.tileSize = new Vector2(5, 5);  // 例: テクスチャ2のサイズ
        terrainLayers[1] = layer2;
        */

        // 地形にTerrainLayerを設定
        terrain.terrainData.terrainLayers = terrainLayers;

        // 各領域に合わせてテクスチャを適用
        // ApplyTextureBasedOnHeight();
    }

    ///
    void ApplyTextureBasedOnHeight()
    {
        // 地形のデータを取得
        TerrainData terrainData = terrain.terrainData;
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, width, height);

        // 高さマップに基づいてテクスチャを適用
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 地形の高さを取得
                float worldX = x / (float)width * terrainData.size.x;
                float worldY = y / (float)height * terrainData.size.z;
                float heightAtPoint = terrain.SampleHeight(new Vector3(worldX, 0, worldY));

                // 高さによってテクスチャをブレンド
                if (heightAtPoint < textureBlendHeight * terrainData.size.y)
                {
                    // 高さが低い場合はtexture1
                    alphamaps[x, y, 0] = 1;
                    alphamaps[x, y, 1] = 0;
                }
                else
                {
                    // 高さが高い場合はtexture2
                    alphamaps[x, y, 0] = 0;
                    alphamaps[x, y, 1] = 1;
                }
            }
        }

        // アルファマップを適用してテクスチャを変更
        terrainData.SetAlphamaps(0, 0, alphamaps);
    }
    // 地形の状態を保存（高さマップ）
    void SaveOriginalTerrainState()
    {
        originalwidth = heightmap.width;
        originalheight = heightmap.height;
        originalHeights = new float[originalwidth, originalheight];
        TerrainData terrainData = terrain.terrainData;
        for (int x = 0; x < originalwidth; x++)
        {
            for (int y = 0; y < originalheight; y++)
            {
                //   originalHeights[x, y] = heightmap.GetPixel(x, y).grayscale;


                originalHeights[y, x] = terrainData.GetHeight(x, y);
            }
        }
    }
    
    // 元の地形状態を復元するメソッド
    void RestoreOriginalTerrainState()
    {
        if (originalHeights != null)
        {
            terrain.terrainData.size = new Vector3(originalwidth, TerrainElevation, originalheight);
            terrain.terrainData.SetHeights(0, 0, originalHeights);  // 高さマップを元に戻す
                                                                    //

            // 地形のterrainDataを取得
            TerrainData ThisterrainData = terrain.terrainData;
            // TerrainColliderを更新
            // terrainDataが更新されると、TerrainColliderも自動的に更新されますが、念のため再設定します
            terrain.GetComponent<TerrainCollider>().terrainData = ThisterrainData;

            // これで、TerrainColliderが新しい高さに基づいて衝突判定を行います
            //
            Debug.Log("Terrain return!!!.");
        }
    }
    /// 
    /// 
    /// </summary>
    /*
    private void OnApplicationQuit()
    {
        RestoreOriginalTerrainState();
        Debug.Log("Terrain reset.");
    }
    */
}
