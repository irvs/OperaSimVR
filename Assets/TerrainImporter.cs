using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.Core;
using RosMessageTypes.TmsMsgDb;
using RosMessageTypes.Shape;
using Assimp;
public class MyServiceClient : MonoBehaviour
{
    ROSConnection ros;
 //   private string serviceName = "output/terrain/heightmap/image";
    private string serviceName = "output/terrain/mesh_srv";
    private float awaitingResponseUntilTimestamp;
    private float awaitingResponseUntilTimestampTx;
    public float requestInterval = 5.0f;

    public Terrain terrain;
    public Texture2D heightmap;  // 高さマップ画像
    public Texture2D TerrainTexture;
    public int TerrainWidth;//trrainの幅
    public int TerrainHeight;//terrainの奥行
    public int TerrainElevation;//terrainの高さ
    public int TextureWidth;
    public int TextureHeight;
    public bool TerrainRecordSw;
    public float OffsetWidthTexture;
    public float OffsetHeightTexture;
    private int ImgHeight;
    private int ImgWidth;
    private bool GetImage;
    private bool TextureRequestSW;
    SoilParticleSettings soilparticle;
    OVRPlayerController characont;

    // サービスリクエストを送信
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<TmsdbTerrainImageSrvRequest, TmsdbTerrainImageSrvResponse>(serviceName);
        ros.RegisterRosService<TmsdbTerrainImageSrvRequest, TmsdbTerrainImageSrvResponse>("output/terrain/texture");
        characont=GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>();
        characont.enabled = false;
     //   characont.GravityModifier = 0;
        GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
        GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = false;

        //  CallService("Test", 42);
    }
    void CallService(string name, int value)
    {
       /* var request = new TmsdbTerrainImageSrvRequest()
        {
            label = name,
            data = value
        };*/
        TmsdbTerrainImageSrvRequest Heightmaprequest = new TmsdbTerrainImageSrvRequest();
        ros.SendServiceMessage<TmsdbTerrainImageSrvResponse>(serviceName, Heightmaprequest, OnServiceResponse);

    }
    // サービスのレスポンスを受け取る
    void OnServiceResponse(TmsdbTerrainImageSrvResponse response)
    {
        Debug.Log("Received image!");
        // 受け取った画像データ（sensor_msgs/Image）を処理
        ImageMsg image = response.image;
        // 画像データをUnityで使える形式に変換する処理を追加
    //    Texture2D texture = new Texture2D((int)1024, (int)1024, TextureFormat.RGB24, false);
        Texture2D texture = new Texture2D((int)image.width, (int)image.height, TextureFormat.RGB24, false);
        texture.LoadRawTextureData(image.data);
        texture.Apply();
        // 画像を表示する（例えば、UIのImageコンポーネントに設定する）
       // GetComponent<Renderer>().material.mainTexture = texture;
        Debug.Log("Display image!");
        
        heightmap = texture;
        TerrainWidth = (int)response.terrainwidth;//trrainの幅
        TerrainHeight = (int)response.terrainheight;//terrainの奥行
        TerrainElevation = (int)response.terrainelevation;//terrainの高さ
    //    ImgHeight = (int)response.image.height;// - 1;
    //    ImgWidth = (int)response.image.width;// - 1;
        GetImage = true;
        Debug.Log(ImgHeight);

        ResizeTexture(heightmap,1024,1024);

        RenderTexture rt = new RenderTexture(1024, 1024, 24);
        RenderTexture.active = rt;
        Graphics.Blit(heightmap, rt);
        Texture2D resizedTexture = new Texture2D(1024, 1024);
        resizedTexture.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        resizedTexture.Apply();
        RenderTexture.active = null;
        heightmap = resizedTexture;
        Debug.Log("Resized!");

        Debug.Log(heightmap.height);
        GenerateTerrainFromHeightmap();

        TmsdbTerrainImageSrvRequest Heightmaprequest = new TmsdbTerrainImageSrvRequest();
        soilparticle = terrain.GetComponent<SoilParticleSettings>();
        soilparticle.enable = true;
        GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = true;
        GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = true;
        characont = GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>();
        characont.enabled = true;
       // characont.GravityModifier = 1;

        TextureRequestSW = true;
        ros.SendServiceMessage<TmsdbTerrainImageSrvResponse>("output/terrain/texture", Heightmaprequest, OnServiceResponseForTexture);
    }

    void OnServiceResponseForTexture(TmsdbTerrainImageSrvResponse response)
    {
        Debug.Log("Received texture!");
        // 受け取った画像データ（sensor_msgs/Image）を処理
        ImageMsg image = response.image;
        // 画像データをUnityで使える形式に変換する処理を追加
        //
        byte[] imageData = image.data;
        if (imageData.Length != (int)image.width * (int)image.height * 3)
        {
            Debug.LogError("Received image data size does not match expected size");
            return;
        }
        // 新しいTexture2Dを作成
        Texture2D texture = new Texture2D((int)image.width, (int)image.height, TextureFormat.RGB24, false);
        // BGR8形式のデータをRGB形式に変換してTexture2Dに設定
        Color[] colors = new Color[(int)image.width * (int)image.height];
        for (int i = 0; i < (int)image.width * (int)image.height; i++)
        {
            // BGR -> RGBに変換
            byte b = imageData[i * 3];
            byte g = imageData[i * 3 + 1];
            byte r = imageData[i * 3 + 2];
            colors[i] = new Color(r / 255f, g / 255f, b / 255f); // RGBを0-1の範囲に正規化
        }
        // 変換したカラーをテクスチャに設定
        texture.SetPixels(colors);
        texture.Apply(); // 変更を適用
        //
        //    Texture2D texture = new Texture2D((int)1024, (int)1024, TextureFormat.RGB24, false);
      //  Texture2D texture = new Texture2D((int)image.width, (int)image.height, TextureFormat.RGB24, false);
     //   texture.LoadRawTextureData(image.data);
     //   texture.Apply();
        // 画像を表示する（例えば、UIのImageコンポーネントに設定する）
        // GetComponent<Renderer>().material.mainTexture = texture;
        Debug.Log("Display texture!");

        TerrainTexture = texture;
        TextureWidth = (int)response.terrainwidth;//trrainの幅
        TextureHeight = (int)response.terrainheight;//terrainの奥行
                                                    //    ImgHeight = (int)response.image.height;// - 1;
                                                    //    ImgWidth = (int)response.image.width;// - 1;
        TextureRequestSW = false;

        ResizeTexture(TerrainTexture, 1024, 1024);

        RenderTexture rt = new RenderTexture(1024, 1024, 24);
        RenderTexture.active = rt;
        Graphics.Blit(TerrainTexture, rt);
        Texture2D resizedTexture = new Texture2D(1024, 1024);
        resizedTexture.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        resizedTexture.Apply();
        RenderTexture.active = null;
        TerrainTexture = resizedTexture;
        Debug.Log("Resized!");

        Color[] pixels = TerrainTexture.GetPixels();
        Texture2D flippedTexture = new Texture2D(TerrainTexture.width, TerrainTexture.height);
        for (int y = 0; y < TerrainTexture.height; y++)
        {
            for (int x = 0; x < TerrainTexture.width; x++)
            {
                flippedTexture.SetPixel( x, TerrainTexture.height - 1 - y, pixels[y * TerrainTexture.width + x]);
            }
        }
        flippedTexture.Apply();
        TerrainTexture = flippedTexture;
        ApplyTextures();
    }

    private void OnApplicationQuit()
    {
    //    rosSocket.Close();
    }

    void Update()
    {
        if (Time.time > awaitingResponseUntilTimestamp && GetImage == false)
        {
            TmsdbTerrainImageSrvRequest Heightmaprequest = new TmsdbTerrainImageSrvRequest();
            ros.SendServiceMessage<TmsdbTerrainImageSrvResponse>(serviceName, Heightmaprequest, OnServiceResponse);
            awaitingResponseUntilTimestamp = Time.time + requestInterval;
            Debug.Log("Service Requested(heightmap)");
        }
        if (TextureRequestSW == true && Time.time > awaitingResponseUntilTimestampTx)
        {
            TmsdbTerrainImageSrvRequest Heightmaprequest = new TmsdbTerrainImageSrvRequest();
            ros.SendServiceMessage<TmsdbTerrainImageSrvResponse>("output/terrain/texture", Heightmaprequest, OnServiceResponseForTexture);
            awaitingResponseUntilTimestampTx = Time.time + requestInterval;
            Debug.Log("Service Requested(Texture)");
        }

    }



    void GenerateTerrainFromHeightmap()
    {


        // 高さマップを使用して地形を生成

           int ImgWidth = heightmap.width;
           int ImgHeight = heightmap.height;

        
        
        float[,] heights = new float[ImgWidth, ImgHeight];
        
        for (int x = 0; x < ImgWidth-1; x += 1)
        {
            for (int y = 0; y < ImgHeight-1; y += 1)
            {
                // ピクセルの輝度を地形の高さに変換（0?1の範囲）
              //  Debug.Log(x + " , " + y);
                heights[y, x] = heightmap.GetPixel(x, ImgHeight - 1- y).grayscale;
            }
        }
        
        // Terrainのサイズを高さマップのサイズに合わせる
        if (TerrainRecordSw == false)
        {
            terrain.terrainData = new TerrainData();
        }
        terrain.terrainData.heightmapResolution = ImgWidth;
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
        Debug.Log("making a terrain");/*
        // これで、TerrainColliderが新しい高さに基づいて衝突判定を行います
        */
    }

    // Texture2Dをリサイズするメソッド
    private Texture2D ResizeTexture(Texture2D sourceTexture, int newWidth, int newHeight)
    {
        RenderTexture rt = new RenderTexture(newWidth, newHeight, 24);
        RenderTexture.active = rt;
        Graphics.Blit(sourceTexture, rt);
        Texture2D resizedTexture = new Texture2D(newWidth, newHeight);
        resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resizedTexture.Apply();
        RenderTexture.active = null;
        Debug.Log("Resized!");
        return resizedTexture;
    }

    void ApplyTextures()
    {
        // 地形にテクスチャを適用
        TerrainLayer[] terrainLayers = new TerrainLayer[2];

        // テクスチャ1を設定
        TerrainLayer layer1 = new TerrainLayer();
        layer1.diffuseTexture = TerrainTexture;

        // テクスチャのタイルサイズとオフセットを設定
        layer1.tileSize = new Vector2(TextureWidth, TextureHeight);  // テクスチャのサイズ (タイルあたりの広さ)
        layer1.tileOffset = new Vector2(OffsetWidthTexture, OffsetHeightTexture);  // テクスチャのオフセット (位置の調整)
        layer1.smoothness = 0f;
        layer1.metallic = 0f;

        terrainLayers[0] = layer1;

        // 地形にTerrainLayerを設定
        terrain.terrainData.terrainLayers = terrainLayers;

        // 各領域に合わせてテクスチャを適用
        // ApplyTextureBasedOnHeight();
    }

}


