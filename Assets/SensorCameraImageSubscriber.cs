using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class SensorCameraImageSubscriber : MonoBehaviour
{
    public string topicName = "/client1/theta_image";
    public Skybox skybox;
    public float displayFrequency = 72.0f; // Up to 90Hz?
    private Texture2D texture2D;
    public bool isImageReceived = true; // �摜����M���ꂽ���ǂ����̃t���O
    private bool SkyChanged = true;
    private bool isSubscribed = false;
    private ROSConnection rosConnection;
    private Vector3 PosOrigin;
   // public int SensorPodsNumber = 1;
    private bool IsVRorKey;
    // �����ݒ肵�Ă��Ȃ�Skybox��ݒ肷�邽�߂̕ϐ�
    public Material defaultSkyboxMaterial;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // �Ⴆ�΁A�����̏����Ő؂�ւ��i�L�[���͂Ȃǁj
        if ((Input.GetKey(KeyCode.B) || OVRInput.GetDown(OVRInput.RawButton.B)) && isImageReceived == false)
        {
            isImageReceived = true;
        }
        if (isImageReceived == true && SkyChanged != isImageReceived)
        {
            ResetSkybox(); // �T�u�X�N���C�u�����摜���\������Ă���΃f�t�H���g��Skybox�ɖ߂�
            Unsubscribe();
            ResetCameraPosition();
        }
        else if (isImageReceived == false && SkyChanged != isImageReceived)
        {
            texture2D = new Texture2D(1, 1);
            texture2D.Apply();
            OVRPlugin.systemDisplayFrequency = displayFrequency;
            DisplayImage(); // �T�u�X�N���C�u�����摜��������΁A�����ݒ肵�Ă��Ȃ�Skybox�ɐ؂�ւ���
            Subscribe();
            ChangeCameraPosition();
        }
        SkyChanged = isImageReceived;
    }

    // �摜���T�u�X�N���C�u���ꂽ�ꍇ�ɌĂяo����郁�\�b�h
    private void RenderThetaImage(CompressedImageMsg msg)
    {
        Debug.Log("Received Theta Image Message");

        if (isImageReceived == false)
        {
            texture2D.LoadImage(msg.data);

            skybox.material = new Material(Shader.Find("Skybox/Panoramic"));
            // skybox�Ƀe�N�X�`����ݒ�
            skybox.material.SetTexture("_MainTex", texture2D);

        }
    }

    // ��M�����摜��\�����郁�\�b�h
    private void DisplayImage()
    {
        skybox.material = new Material(Shader.Find("Skybox/Panoramic"));
        skybox.material.SetTexture("_MainTex", texture2D); // skybox�ɃT�u�X�N���C�u�����摜���Z�b�g
    }

    // �f�t�H���g��Skybox�ɖ߂����\�b�h
    private void ResetSkybox()
    {
        skybox.material = defaultSkyboxMaterial; // �����ݒ肵�Ă��Ȃ�Skybox�ɖ߂�
    }

    // �T�u�X�N���C�u���J�n���郁�\�b�h
    private void Subscribe()
    {
        if (!isSubscribed)
        {
            if (topicName != null)
            {
                rosConnection = ROSConnection.GetOrCreateInstance();
                rosConnection.Subscribe<CompressedImageMsg>(topicName, RenderThetaImage);
                isSubscribed = true;
                Debug.Log("Subscribed to the topic: " + topicName);
            }
            else
            {
                Debug.Log("Subscribed topic name is null");
            }
        }
    }

    // �T�u�X�N���C�u���~���郁�\�b�h
    private void Unsubscribe()
    {
        if (isSubscribed)
        {
            if (topicName != null)
            {
                rosConnection.Unsubscribe(topicName);
                Debug.Log("Unsubscribed from the topic: 1 : " + topicName);
                isSubscribed = false;
                Debug.Log("Unsubscribed from the topic: " + topicName);
                //  rosConnection = null;
            }
        }
    }

    private void ChangeCameraPosition()
    {
        PosOrigin = GameObject.Find("OVRPlayerController").transform.position;
        if (GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled == true) 
        {
            IsVRorKey = true;
        }
        GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
        GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = false;
        GameObject.Find("OVRPlayerController").transform.position = new Vector3(0.0f,10000.0f,0.0f);
    }

    private void ResetCameraPosition()
    {
        GameObject.Find("OVRPlayerController").transform.position = PosOrigin;
        if (IsVRorKey == true)
        {
            GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = true;
            GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = true;
        }
    }
}
