using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Robotics.ROSTCPConnector;
using static UnityEditor.PlayerSettings;

public class BackToMain : MonoBehaviour
{

    public string rosHost = "127.0.0.1";
    public GameObject TargetTerrain;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger) || Input.GetKey(KeyCode.B))
        {
            
            ROSConnection ros = ROSConnection.GetOrCreateInstance();
            ros.Disconnect();
            ros.Connect(rosHost,  10000);
            //   OVRPlayerController.MeshCreated = false;
         //   TargetTerrain.SetActive(true);
         //   GameObject.Find("TestTerrain").SetActive(true);
            SceneManager.LoadScene("Scenes/SampleScene");
        }
    }
}
