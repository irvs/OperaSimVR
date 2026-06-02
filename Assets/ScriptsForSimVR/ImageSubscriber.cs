using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class ImageSubscriber : MonoBehaviour
{
    public string topicName = "/camera/image_raw/compressed";
    public Renderer targetRenderer;

    ROSConnection ros;

    Texture2D texture;

    byte[] latestImage;
    bool newImageReceived = false;

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
        latestImage = msg.data;
        newImageReceived = true;
    }

    void Update()
    {
        if (newImageReceived)
        {
            texture.LoadImage(latestImage);
            targetRenderer.material.mainTexture = texture;

            newImageReceived = false;
        }
    }
}