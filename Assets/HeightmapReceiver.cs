using System;
using UnityEngine;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
public class HeightmapReceiver : MonoBehaviour
{
    [SerializeField] string topicName = "/heightmap";
    [SerializeField] GameObject terrainPrefab;
    float heightScale;
    float offsetX, offsetY, offsetZ;
    int terrainHeight, terrainWidth;
    float[,] heightData;
    ApplyPartialHeightmap TerrainTransformer;

    void Start()
    {
        TerrainTransformer = GetComponent<ApplyPartialHeightmap>();
        ROSConnection.GetOrCreateInstance().Subscribe<ImageMsg>(topicName, ImageCallback);
    }


    void ImageCallback(ImageMsg msg)
    {
        int width = (int)msg.width;
        int height = (int)msg.height;
        byte[] imageData = msg.data;
        if (imageData.Length < 24)
        {
            Debug.LogError("[HeightmapReceiver] Image data too small to contain metadata.");
            return;
        }
        // --- Step 1: Extract metadata from first 24 bytes
        byte[] metaBytes = new byte[24];
        Array.Copy(imageData, 0, metaBytes, 0, 24);
        heightScale = BitConverter.ToSingle(metaBytes, 0);   // bytes 0-3
        offsetX = BitConverter.ToSingle(metaBytes, 4);   // bytes 4-7
        offsetZ = BitConverter.ToSingle(metaBytes, 8);   // bytes 8-11
        offsetY = BitConverter.ToSingle(metaBytes, 12);  // bytes 12-15
        terrainHeight = (int)BitConverter.ToSingle(metaBytes, 16); // bytes 16-19
        terrainWidth = (int)BitConverter.ToSingle(metaBytes, 20); // bytes 20-23
        Debug.Log($"[Heightmap] scale: {heightScale}, offset: ({offsetX}, {offsetY}, {offsetZ}), " +
                  $"terrain size: {terrainWidth} x {terrainHeight}");
        // --- Step 2: Extract heightmap pixels (start at row 1)
        int actualHeight = height - 1;  // because row 0 is metadata
        if (actualHeight <= 0 || terrainWidth <= 0)
        {
            Debug.LogError("[HeightmapReceiver] Invalid terrain size.");
            return;
        }
        heightData = new float[actualHeight, width];
        for (int y = 0; y < actualHeight; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y + 1) * width + x; // +1 to skip metadata row
                byte pixel = imageData[index];
                int flippedY = actualHeight - 1 - y;
                heightData[flippedY, x] = pixel * heightScale;
            }
        }


        // --- After extracting heightData ---

        int texWidth = width;
        int texHeight = height;

        // TextureFormat を RGB24 に変更（3チャンネルでグレースケール表現が可能）
        Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Point;

        Color32[] pixels = new Color32[texWidth * texHeight];

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int srcIndex = y * texWidth + x;
                int flippedY = texHeight - 1 - y;
                int dstIndex = flippedY * texWidth + x;

                byte value = imageData[srcIndex];
                // グレースケールとして R,G,B すべてに同じ値をセット
                pixels[dstIndex] = new Color32(value, value, value, 255);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();



        TerrainTransformer.image = tex;

        // --- Optional: Apply to Unity Terrain or Mesh
        //ApplyToTerrain(heightData);
        TerrainTransformer.Offsets = new Vector3(offsetX, offsetY, offsetZ);
        TerrainTransformer.applyWidthMeters = terrainHeight;
        TerrainTransformer.applyHeightMeters = terrainWidth;
        TerrainTransformer.maxImageHeightMeters = heightScale;
       // TerrainTransformer.image = imageData;
        TerrainTransformer.ApplyHeightmap = true;
    }

    void ApplyToTerrain(float[,] heightmap)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);
        Terrain terrain = Instantiate(terrainPrefab).GetComponent<Terrain>();
        terrain.terrainData.heightmapResolution = Mathf.Max(width, height) + 1;
        terrain.terrainData.size = new Vector3(width, 10.0f, height); // X,Z size and Y scale
        float[,] unityHeights = new float[height, width];
        float maxHeight = 0f;
        // Normalize heights [0,1] for Unity terrain
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (heightmap[y, x] > maxHeight)
                    maxHeight = heightmap[y, x];
            }
        }
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                unityHeights[y, x] = heightmap[y, x] / maxHeight;
            }
        }
        terrain.terrainData.SetHeights(0, 0, unityHeights);
    }
}






