using UnityEngine;

public class Player_Key_mover : MonoBehaviour
{
    private float Playerlinear;
    private float PlayerUpper;
    private float PlayerSide;
    private float PlayerFrontRot;
    public float LinearSpeed = 0.01f;
    public float AngularSpeed = 0.2f;
    public float UpperSpeed = 0.01f;
    Controller_manager VRManager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        VRManager = FindObjectOfType<Controller_manager>();
        if (VRManager.PlayerPoseMove_SW == 0)//VRcontroller.sw != 1)
        {
            GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = false;
            if (Input.GetKeyDown(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftShift) == false && Input.GetKey(KeyCode.RightShift) == false)
            {
                Playerlinear = LinearSpeed;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftShift) == false && Input.GetKey(KeyCode.RightShift) == false)
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
            else if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.LeftShift) == true)
            {
                PlayerSide = -LinearSpeed;
            }
            else if (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftShift) == true)
            {
                PlayerSide = LinearSpeed;
            }
            else if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                PlayerSide = 0.0f;
            }
            else if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                PlayerSide = 0.0f;
            }
            
            //transform.Rotate(Vector3.right * 0.1f * Time.deltaTime);
            //OVRCameraRigÇÃà íuïœçX
            GameObject.Find("OVRPlayerController").transform.position += GameObject.Find("OVRPlayerController").transform.rotation * (new Vector3(PlayerSide, 0, (Playerlinear)));
            GameObject.Find("OVRPlayerController").transform.position = new(GameObject.Find("OVRPlayerController").transform.position[0], GameObject.Find("OVRPlayerController").transform.position[1]+ PlayerUpper, GameObject.Find("OVRPlayerController").transform.position[2]);
            if (VRManager.PlayerPoseMove_SW == 0)
            {
                if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.LeftShift) == false)
                {
                    GameObject.Find("OVRPlayerController").transform.Rotate(0, -AngularSpeed, 0);
                }
                else if (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftShift) == false)
                {
                    GameObject.Find("OVRPlayerController").transform.Rotate(0, AngularSpeed, 0);
                }
                else if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftShift) == false && Input.GetKey(KeyCode.RightShift) == true)
                {
                    GameObject.Find("OVRCameraRig").transform.Rotate(-AngularSpeed, 0, 0);
                }
                else if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftShift) == false && Input.GetKey(KeyCode.RightShift) == true)
                {
                    GameObject.Find("OVRCameraRig").transform.Rotate(AngularSpeed, 0, 0);
                }
            }
        }
    }
}
