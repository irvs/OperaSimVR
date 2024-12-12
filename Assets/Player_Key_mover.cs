using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Key_mover : MonoBehaviour
{
    private float Playerlinear;
    public float LinearSpeed = 0.01f;
    public float AngularSpeed = 0.2f;
    Controller_manager VRManager;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Playerlinear = LinearSpeed;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Playerlinear = -LinearSpeed;
        }
        else if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            Playerlinear = 0.0f;
        }
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            Playerlinear = 0.0f;
        }
        //OVRCameraRigÇÃà íuïœçX
        GameObject.Find("OVRPlayerController").transform.position += GameObject.Find("OVRPlayerController").transform.rotation * (new Vector3(0, 0, (Playerlinear)));
        VRManager = FindObjectOfType<Controller_manager>();
        if (VRManager.GetOnMachine != 1) 
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                GameObject.Find("OVRPlayerController").transform.Rotate(0, -AngularSpeed, 0);
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                GameObject.Find("OVRPlayerController").transform.Rotate(0, AngularSpeed, 0);
            }
        }
    }
}
