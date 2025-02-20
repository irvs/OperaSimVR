using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.IO;

public class ROSImageReceiver : MonoBehaviour
{
    public string topicName;
    private Texture2D texture;
    public Renderer targetRenderer;
    private ROSConnection ros;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<ImageMsg>(topicName, OnImageReceived);
    }

    // ROS2�̉摜���b�Z�[�W���󂯎�����Ƃ��ɌĂ΂��R�[���o�b�N
    private void OnImageReceived(ImageMsg rosImage)
    {
        byte[] imageData = rosImage.data;

        // PNG�摜��ǂݍ��݁ATexture2D�ɕϊ�����
        texture = new Texture2D(2, 2);  // �T�C�Y�͌�ŕύX����
        texture.LoadImage(imageData);  // �o�C�g�f�[�^����摜��ǂݍ���

        // MaxSize��ύX
        texture.Reinitialize(texture.width, texture.height);  // �T�C�Y�ɍ��킹�ă��T�C�Y

        // �}�e���A���ɓK�p����Ȃ�
        GetComponent<Renderer>().material.mainTexture = texture;
    }


    //
    private void SetMaxSize(Texture2D texture, int maxSize)
    {
        int newWidth = Mathf.Min(texture.width, maxSize);
        int newHeight = Mathf.Min(texture.height, maxSize);

        // �摜���ő�T�C�Y���傫����΃��T�C�Y
        if (texture.width > maxSize || texture.height > maxSize)
        {
            texture.Reinitialize(newWidth, newHeight);
        }
    }


}
