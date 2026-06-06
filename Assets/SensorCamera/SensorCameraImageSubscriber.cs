using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class SensorCameraImageSubscriber : MonoBehaviour
{
    public string topicName = "/client1/theta_image";
    public Skybox skybox;
    public float displayFrequency = 72.0f; // Up to 90Hz?
    private Texture2D texture2D;
    public bool isImageReceived = true; 
    private bool SkyChanged = true;
    private bool isSubscribed = false;
    private ROSConnection rosConnection;
    private Vector3 PosOrigin;
   // public int SensorPodsNumber = 1;
    private bool IsVRorKey;
    public Material defaultSkyboxMaterial;
    private GameObject PlayerObject;
    CharacterController CharacterController;
    Collider PlayerCollider;

    // Start is called before the first frame update
    void Start()
    {
        PlayerObject = GameObject.Find("OVRPlayerController");
        CharacterController = PlayerObject.GetComponent<CharacterController>();
        PlayerCollider = PlayerObject.GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        if ((Input.GetKey(KeyCode.B) || OVRInput.GetDown(OVRInput.RawButton.B)) && isImageReceived == false)
        {
            isImageReceived = true;
        }
        if (isImageReceived == true && SkyChanged != isImageReceived)
        {
            ResetSkybox(); 
            Unsubscribe();
            ResetCameraPosition();
        }
        else if (isImageReceived == false && SkyChanged != isImageReceived)
        {
            texture2D = new Texture2D(1, 1);
            texture2D.Apply();
            OVRPlugin.systemDisplayFrequency = displayFrequency;
            DisplayImage();
            Subscribe();
            ChangeCameraPosition();
        }
        SkyChanged = isImageReceived;
    }

    private void RenderThetaImage(CompressedImageMsg msg)
    {
        Debug.Log("Received Theta Image Message");

        if (isImageReceived == false)
        {
            texture2D.LoadImage(msg.data);

            skybox.material = new Material(Shader.Find("Skybox/Panoramic"));
            skybox.material.SetTexture("_MainTex", texture2D);

        }
    }

    private void DisplayImage()
    {
        skybox.material = new Material(Shader.Find("Skybox/Panoramic"));
        skybox.material.SetTexture("_MainTex", texture2D); 
    }

    private void ResetSkybox()
    {
        skybox.material = defaultSkyboxMaterial;
    }

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
        PosOrigin = PlayerObject.transform.position;
        if (CharacterController.enabled == true) 
        {
            IsVRorKey = true;
        }
        CharacterController.enabled = false;
        PlayerCollider.enabled = false;
        PlayerObject.transform.position = new Vector3(0.0f,10000.0f,0.0f);
    }

    private void ResetCameraPosition()
    {
        PlayerObject.transform.position = PosOrigin;
        if (IsVRorKey == true)
        {
            CharacterController.enabled = true;
            PlayerCollider.enabled = true;
        }
    }
}
