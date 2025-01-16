using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Robotics.ROSTCPConnector;
using static UnityEditor.PlayerSettings;

public class BackToMain : MonoBehaviour
{

    public string rosHost = "127.0.0.1";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
        {
            ROSConnection.GetOrCreateInstance().Disconnect(); 
            ROSConnection ros = ROSConnection.GetOrCreateInstance();
            ros.Connect(rosHost,  10000);
         //   OVRPlayerController.MeshCreated = false;
            SceneManager.LoadScene("Scenes/SampleScene");
        }
    }
}
