using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Robotics.ROSTCPConnector;

public class ButtonSensorPodController : MonoBehaviour
{
    public string sceneName = "Scenes/SensorPodScene";
    public GameObject TrackingAnchor;

    public string rosHost = "127.0.0.1";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        ROSConnection.GetOrCreateInstance().Disconnect();
        ROSConnection ros = ROSConnection.GetOrCreateInstance();
        if (sceneName == "Scenes/SampleScene") {
            ros.Connect(rosHost, 10000);
        } else {
            ros.Connect(rosHost, 9090);

            Vector3 TrackingAnchorEulerAngles = TrackingAnchor.transform.rotation.eulerAngles;
            TrackingAnchor.transform.rotation = Quaternion.Euler(TrackingAnchorEulerAngles.x, TrackingAnchorEulerAngles.y - 180, TrackingAnchorEulerAngles.z);

            // Save now position and rotation for returning to MainScene
            SetPose.SetNowPose(TrackingAnchor.transform.position);
            SetPose.SetNowRot(TrackingAnchor.transform.rotation);

            // Set the rotation of OVRCameraRig for SensorPodScene
            SetCameraRotation.SetRotationY(TrackingAnchor.transform);
        }

      //  OVRPlayerController.MeshCreated = false;
        SceneManager.LoadScene(sceneName);
    }
}
