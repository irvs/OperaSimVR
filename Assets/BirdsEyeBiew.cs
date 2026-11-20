using UnityEngine;
using UnityEngine.XR;
public class BirdsEyeBiew : MonoBehaviour
{
    int birdseyeview = 0;
    [Header("Move Speed Parameters")]
    public float updownspeed = 5.0f;
    public float movespeed = 0.10f;
    public float rotspeed = 10.0f;
    Vector3 posiorigin;
    Quaternion rotrigin;
    GameObject PlayerObject;
    CharacterController CharacterController;

    void Start()
    {
        PlayerObject = GameObject.Find("OVRPlayerController");
        CharacterController = PlayerObject.GetComponent<CharacterController>();
    }
    
    void Update()
    {
        bool desktop = OperaSimVRDesktopInput.IsActive;
        bool enterDesktop = desktop && birdseyeview == 0 && Input.GetKeyDown(KeyCode.F6);
        bool exitDesktop = desktop && birdseyeview == 1 && Input.GetKeyDown(KeyCode.F6);
        Vector3 posiplayer = PlayerObject.transform.position;
        Quaternion rotplayer = PlayerObject.transform.rotation;
        if ((OVRInput.Get(OVRInput.RawButton.X) && OVRInput.Get(OVRInput.RawButton.Y) || enterDesktop) && birdseyeview == 0)
        {
            CharacterController.enabled = false;
            birdseyeview = 1;
            posiorigin = PlayerObject.transform.position;
            rotrigin = PlayerObject.transform.rotation;
        }
        //up
        if (OVRInput.Get(OVRInput.RawButton.X) && birdseyeview == 1)
        {
            PlayerObject.transform.position = PlayerObject.transform.position+new Vector3(0, updownspeed, 0);
        }
        //down
        if (OVRInput.Get(OVRInput.RawButton.Y) && birdseyeview == 1)
        {
            PlayerObject.transform.position = PlayerObject.transform.position + new Vector3(0, -updownspeed, 0);
        }
        //position
        if (birdseyeview == 1)
        {
            if (desktop)
            {
                float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
                float side = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
                float up = (Input.GetKey(KeyCode.PageUp) ? 1 : 0) - (Input.GetKey(KeyCode.PageDown) ? 1 : 0);
                PlayerObject.transform.position += PlayerObject.transform.rotation * new Vector3(side, 0, forward) * movespeed * 60f * Time.deltaTime;
                PlayerObject.transform.position += Vector3.up * up * updownspeed * Time.deltaTime;
                float turn = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
                PlayerObject.transform.Rotate(0, turn * rotspeed * Time.deltaTime, 0);
            }
            //get input from left joystick
            Vector2 stickL = movespeed * OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
            PlayerObject.transform.position += PlayerObject.transform.rotation * new Vector3(stickL.x, 0, stickL.y);
        }
        //rotation
        if (birdseyeview == 1)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickLeft))
            {
                PlayerObject.transform.Rotate(0, -rotspeed, 0);
            }
            else if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickRight))
            {
                PlayerObject.transform.Rotate(0, rotspeed, 0);
            }
        }
        //flont back rot
        if (birdseyeview == 1)
        {
            if (OVRInput.Get(OVRInput.RawButton.RThumbstickUp))
            {
                float rotationAmountX = 1f; // Rotation angle in the x direction

                PlayerObject.transform.RotateAround(PlayerObject.transform.position, PlayerObject.transform.TransformDirection(Vector3.right), rotationAmountX);
            }
            if (OVRInput.Get(OVRInput.RawButton.RThumbstickDown))
            {
                float rotationAmountX = 1f; // Rotation angle in the x direction

                PlayerObject.transform.RotateAround(PlayerObject.transform.position, PlayerObject.transform.TransformDirection(Vector3.right), -rotationAmountX);
            }
        }
        //end of bev
        if ((OVRInput.GetDown(OVRInput.RawButton.B) || exitDesktop) && birdseyeview == 1)
        {
            birdseyeview = 0;
            PlayerObject.transform.position = posiorigin + new Vector3(0,5,0);
            PlayerObject.transform.rotation = rotrigin;
            CharacterController.enabled = true;
        }
    }
}
