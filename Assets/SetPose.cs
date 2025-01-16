using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Robotics.ROSTCPConnector;

public class SetPose : MonoBehaviour
{
    public GameObject Player;
    public static Vector3 Pose = new Vector3(0, 0, 0);
    public static Quaternion Rot = new Quaternion(0, 0, 0, 0);
    
    private Vector3 PrevPose = new Vector3(0, 0, 0);
    private DateTime PrevTime;

    // Start is called before the first frame update
    void Start()
    {
        Pose += new Vector3(0, 1f, 0);
        SetPrevPose();
    }

    // Update is called once per frame
    void Update()
    {
        if (Player.transform.position.y < -10f)
        {
            // Reload Scene
            ROSConnection.GetOrCreateInstance().Disconnect();
        //    OVRPlayerController.MeshCreated = false;
            SceneManager.LoadScene("Scenes/MainScene");
        }
    }

    public static void SetNowPose(Vector3 pose)
    {
        Pose = pose;
    }

    public static void SetNowRot(Quaternion rot)
    {
        Rot = rot;
    }

    private void SetPrevPose()
    {
        Player.transform.position = Pose;
        Player.transform.rotation = Rot;
    }
}
