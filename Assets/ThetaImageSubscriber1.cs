using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

public class ThetaImageSubscriber1 : MonoBehaviour
{
    public string topicName = "/client1/theta_image";
    public List<string> topicNameList = new List<string>();
    public Skybox skybox;
    public float displayFrequency = 72.0f; // Up to 90Hz?
    private Texture2D texture2D;
    private byte[] imageData;
    public bool isImageReceived = true; // 画像が受信されたかどうかのフラグ
    private bool SkyChanged = true;

    private bool isSubscribed = false;
    private ROSConnection rosConnection;
    private Vector3 PosOrigin;
    public int SensorPodsNumber = 1;
    private bool IsVRorKey;

    // 何も設定していないSkyboxを設定するための変数
    public Material defaultSkyboxMaterial;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // 例えば、何かの条件で切り替え（キー入力など）
        if ((Input.GetKey(KeyCode.B) || OVRInput.GetDown(OVRInput.RawButton.B)) && isImageReceived == false) // スペースキーで切り替える例
        {
            isImageReceived = true;
        }
        if (isImageReceived == true && SkyChanged != isImageReceived)
        {
            ResetSkybox(); // サブスクライブした画像が表示されていればデフォルトのSkyboxに戻す
            Unsubscribe();
            ResetCameraPosition();
        }
        else if (isImageReceived == false && SkyChanged != isImageReceived)
        {
            texture2D = new Texture2D(1, 1);
            texture2D.Apply();
            OVRPlugin.systemDisplayFrequency = displayFrequency;
            DisplayImage(); // サブスクライブした画像が無ければ、何も設定していないSkyboxに切り替える
            Subscribe();
            ChangeCameraPosition();
        }
        SkyChanged = isImageReceived;
    }

    // 画像がサブスクライブされた場合に呼び出されるメソッド
    private void RenderThetaImage(CompressedImageMsg msg)
    {
        Debug.Log("Received Theta Image Message");

        if (isImageReceived == false)
        {
            texture2D.LoadImage(msg.data);

            skybox.material = new Material(Shader.Find("Skybox/Panoramic"));
            // skyboxにテクスチャを設定
            skybox.material.SetTexture("_MainTex", texture2D);

        }
    }

    // 受信した画像を表示するメソッド
    private void DisplayImage()
    {
        skybox.material = new Material(Shader.Find("Skybox/Panoramic"));
        skybox.material.SetTexture("_MainTex", texture2D); // skyboxにサブスクライブした画像をセット
    }

    // デフォルトのSkyboxに戻すメソッド
    private void ResetSkybox()
    {
        skybox.material = defaultSkyboxMaterial; // 何も設定していないSkyboxに戻す
    }

    // サブスクライブを開始するメソッド
    private void Subscribe()
    {
        if (!isSubscribed)
        {
            if (SensorPodsNumber >= 1 && SensorPodsNumber <= topicNameList.Count)
            {
                topicName = topicNameList[SensorPodsNumber - 1];
            }
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

    // サブスクライブを停止するメソッド
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
