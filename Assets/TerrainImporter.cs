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
public class MyServiceClient : MonoBehaviour
{
    ROSConnection ros;
 //   private string serviceName = "output/terrain/heightmap/image";
    private string serviceName = "output/terrain/mesh_srv";
    private float awaitingResponseUntilTimestamp;
    public float requestInterval = 5.0f;
    // サービスリクエストを送信
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<TmsdbTerrainImageSrvRequest, TmsdbTerrainImageSrvResponse>(serviceName);

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
        Texture2D texture = new Texture2D((int)image.width, (int)image.height, TextureFormat.RGB24, false);
        texture.LoadRawTextureData(image.data);
        texture.Apply();
        // 画像を表示する（例えば、UIのImageコンポーネントに設定する）
        GetComponent<Renderer>().material.mainTexture = texture;
        Debug.Log("Display image!");
    }
    private void OnApplicationQuit()
    {
    //    rosSocket.Close();
    }

    void Update()
    {
        if (Time.time > awaitingResponseUntilTimestamp)
        {
            TmsdbTerrainImageSrvRequest Heightmaprequest = new TmsdbTerrainImageSrvRequest();
            ros.SendServiceMessage<TmsdbTerrainImageSrvResponse>(serviceName, Heightmaprequest, OnServiceResponse);
            awaitingResponseUntilTimestamp = Time.time + requestInterval;
            Debug.Log("Service Requested");
        }
    }

}


