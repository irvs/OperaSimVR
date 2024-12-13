using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Key_mover : MonoBehaviour
{
    private float Playerlinear;
    private float PlayerUpper;
    public float LinearSpeed = 0.01f;
    public float AngularSpeed = 0.2f;
    public float UpperSpeed = 0.01f;
    Controller_manager VRManager;
    VR_cont_2 VRcontroller;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        VRManager = FindObjectOfType<Controller_manager>();
        VRcontroller = FindObjectOfType<VR_cont_2>();
        if (VRcontroller.sw != 1)
        {

            if (Input.GetKeyDown(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftShift) == false)
            {
                Playerlinear = LinearSpeed;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftShift) == false)
            {
                Playerlinear = -LinearSpeed;
            }
            else if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftShift))
            {
                PlayerUpper = UpperSpeed;
            }
            else if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftShift))
            {
                PlayerUpper = -UpperSpeed;
            }
            else if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                PlayerUpper = 0.0f;
                Playerlinear = 0.0f;
            }
            else if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                PlayerUpper = 0.0f;
                Playerlinear = 0.0f;
            }
            //transform.Rotate(Vector3.right * 0.1f * Time.deltaTime);
            //OVRCameraRigÇÃà íuïœçX
            GameObject.Find("OVRPlayerController").transform.position += GameObject.Find("OVRPlayerController").transform.rotation * (new Vector3(0, 0, (Playerlinear)));
            GameObject.Find("OVRPlayerController").transform.position = new(GameObject.Find("OVRPlayerController").transform.position[0], GameObject.Find("OVRPlayerController").transform.position[1]+ PlayerUpper, GameObject.Find("OVRPlayerController").transform.position[2]);
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
}
