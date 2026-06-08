using UnityEngine;

public class PlayerKeyMover : MonoBehaviour
{
    private float Playerlinear;
    private float PlayerUpper;
    private float PlayerSide;
    public float LinearSpeed = 0.01f;
    public float AngularSpeed = 0.2f;
    public float UpperSpeed = 0.01f;
    ControllerManager VRManager;
    GameObject PlayerObject;
    Collider Collider;
    // Start is called before the first frame update
    void Start()
    {
        PlayerObject = GameObject.Find("OVRPlayerController");
        VRManager = FindObjectOfType<ControllerManager>();
        Collider = PlayerObject.GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (VRManager.GetOnMachine == ControllerManager.RideOption.GetOff)
        {
            Collider.enabled = false;
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
            
            //OVRCameraRigの位置変更
            PlayerObject.transform.position += PlayerObject.transform.rotation * (new Vector3(PlayerSide, 0, Playerlinear));
            PlayerObject.transform.position = new (PlayerObject.transform.position[0], PlayerObject.transform.position[1]+ PlayerUpper, PlayerObject.transform.position[2]);
            if (VRManager.GetOnMachine == ControllerManager.RideOption.GetOff)
            {
                if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.LeftShift) == false)
                {
                    PlayerObject.transform.Rotate(0, -AngularSpeed, 0);
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
