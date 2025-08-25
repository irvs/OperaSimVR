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
    float[,] heightData;
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<ImageMsg>(topicName, ImageCallback);
    }
    void ImageCallback(ImageMsg msg)
    {
        int width = (int)msg.width;
        int height = (int)msg.height;
        byte[] imageData = msg.data;
        // --- Step 1: Extract metadata from first 16 pixels (mono8 = 1 byte per pixel)
        byte[] metaBytes = new byte[16];
        Array.Copy(imageData, 0, metaBytes, 0, 16);
        heightScale = BitConverter.ToSingle(metaBytes, 0);     // bytes 0-3
        offsetX = BitConverter.ToSingle(metaBytes, 4);     // bytes 4-7
        offsetY = BitConverter.ToSingle(metaBytes, 8);     // bytes 8-11
        offsetZ = BitConverter.ToSingle(metaBytes, 12);    // bytes 12-15
        Debug.Log($"[Heightmap] scale: {heightScale}, offset: ({offsetX}, {offsetY}, {offsetZ})");
        // --- Step 2: Extract heightmap pixels (2nd row and beyond)
        int actualHeight = height - 1;
        heightData = new float[actualHeight, width];
        for (int y = 0; y < actualHeight; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y + 1) * width + x;  // +1 because row 0 is metadata
                byte pixel = imageData[index];
                heightData[y, x] = pixel * heightScale;
            }
        }
        // --- Optional: Apply to Unity Terrain or Mesh
        ApplyToTerrain(heightData);
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






