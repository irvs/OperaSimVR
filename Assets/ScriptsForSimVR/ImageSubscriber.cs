using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class ImageSubscriber : MonoBehaviour
{
    [Header("ROS")]
    public string topicName = "/camera/image_raw/compressed";

    [Header("Display")]
    public Renderer targetRenderer;

    [Tooltip("Planeの高さ(m)")]
    public float fixedHeight = 1.0f;

    private ROSConnection ros;

    private Texture2D texture;

    private byte[] latestImage;
    private bool newImageReceived = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        texture = new Texture2D(2, 2);

        ros.Subscribe<CompressedImageMsg>(
            topicName,
            ImageCallback
        );
    }

    void ImageCallback(CompressedImageMsg msg)
    {
        //Debug.Log($"Received image: {msg.data.Length} bytes");

        latestImage = msg.data;
        newImageReceived = true;
    }

    void Update()
    {
        if (!newImageReceived)
            return;

        // JPEG/PNGデータをTextureへ展開
        texture.LoadImage(latestImage);

        // テクスチャ設定
        targetRenderer.material.mainTexture = texture;

        // アスペクト比計算
        float aspect = (float)texture.width / texture.height;

        //Debug.Log($"Image Size: {texture.width} x {texture.height}, Aspect: {aspect:F3}");

        // Planeサイズ調整
        // Unity Planeは10×10なので補正が必要
        Vector3 scale = targetRenderer.transform.localScale;

        scale.x = fixedHeight * aspect / 10.0f;
        scale.z = fixedHeight / 10.0f;

        targetRenderer.transform.localScale = scale;

        newImageReceived = false;
    }
}