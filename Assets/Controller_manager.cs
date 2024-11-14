using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Burst.Intrinsics.Arm;
using static UnityEditor.Experimental.GraphView.GraphView;
using System;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using System.Drawing.Printing;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

public class Controller_manager : MonoBehaviour
{
    ControllerLay From_VRcont;
    public bool designate_vehicle;
    bool controller_sw;
    bool outside_sw;
    public string Machine_name;
    GameObject VehicletargetObject;
    public int GetOnMachine;
    List<string> Machine_Name_List = new List<string>();
    
    public int Player_posi_mover_SW = 0;

    Vector3 posiorigin;
    Quaternion rotrigin;
    public int num = 0;
    public float movespeed = 1.0f;
    Quaternion machine_origin_rot;
    Vector3 dif_rot_from_machine;
    float timesec = Mathf.Floor((System.DateTime.Now.Millisecond) / 10);
    float deltatimesec = System.DateTime.Now.Millisecond;
    public GameObject PlayertargetObject;
    private float Playerlinear;
    //

    // Start is called before the first frame update
    void Start()
    {
        From_VRcont = FindObjectOfType<ControllerLay>();
        Machine_Name_List.Add("zero");
    }

    // Update is called once per frame
    void Update()
    {
        if (designate_vehicle == false)
        {
            From_VRcont = FindObjectOfType<ControllerLay>();
            Machine_name = From_VRcont.GetOnVehicle;
            if (Machine_name != null && Machine_name != "OVRPlayerController")
            {
                if (Machine_Name_List[Machine_Name_List.Count - 1] != Machine_name)
                {
                    Machine_Name_List.Add(Machine_name);
                }
            }
                
            Machine_name = Machine_Name_List[Machine_Name_List.Count - 1];
        }

        //GetOnMachine = From_VRcont.GetOn;
        if (OVRInput.Get(OVRInput.RawButton.LIndexTrigger))
        {
            /*
            warp_triger = 1;
            Debug.Log("fgfffffffffffffff");
            Debug.Log(warp_triger);
            //
            //Debug.Log(parentObjectName);
            if (parentObjectName == "ic120")
            {
                Debug.Log("vvvSic120");
                geton_ic120 = 1;
                geton_zx200 = 0;
                geton_c30r = 0;

            }
            if (parentObjectName == "zx200")
            {
                Debug.Log("vvvSic200");
                geton_ic120 = 0;
                geton_zx200 = 1;
                geton_c30r = 0;

            }
            if (parentObjectName == "c30r")
            {
                Debug.Log("vvvSc30r");
                geton_ic120 = 0;
                geton_zx200 = 0;
                geton_c30r = 1;

            }
            */
            //GetOnVehicle = parentObjectName;
            //GetOn = 1;
            GetOnMachine = 1;
            VehicletargetObject = GameObject.Find(Machine_name);
        }

        //   sw_zx200 = 0;
        if (Machine_name != null && Machine_name != "OVRPlayerController")
        {
            //
            if ((GetOnMachine == 1) && (Player_posi_mover_SW == 0))
            {
                Debug.Log("INPUT_BBBBBBBBBBBBB");
                posiorigin = GameObject.Find("OVRPlayerController").transform.position;
                rotrigin = GameObject.Find("OVRPlayerController").transform.rotation;
                GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
                GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = false;
                GameObject.Find("OVRPlayerController").transform.rotation = GameObject.Find(Machine_name + "_cam").transform.rotation;
                Player_posi_mover_SW += 1;
                //    Debug.Log("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaS");
            }
            //
            if ((Player_posi_mover_SW > 0) && outside_sw == false) 
            {
                //  Debug.Log("BBBBcccc");

                PlayertargetObject.transform.position = GameObject.Find(Machine_name + "_cam").transform.position;

                // sw_zx200 = 0;
                num = 1;
            }
            //
            if (((Player_posi_mover_SW > 0) && OVRInput.GetDown(OVRInput.RawButton.B) && (num == 1)) || ((Player_posi_mover_SW > 0) && (num == 1)) && GetOnMachine == 0)
            {
                //
                OVRPlayerController scriptA = PlayertargetObject.GetComponent<OVRPlayerController>();
                scriptA.RotationRatchet = 45;
                scriptA.RotationAmount = 0.5f;

                //
                // sw_zx200 = 0;
                Player_posi_mover_SW = 0;
                num = 0;
                controller_sw = false;
                //warp_triger = 0;
                //geton_ic120 = 0;
                //geton_zx200 = 0;
                //geton_c30r = 0;
                GetOnMachine = 0;
                VR_cont_2 scriptB = GameObject.Find(Machine_name).GetComponent<VR_cont_2>();
                if (scriptB != null)
                {
                    scriptB.sw = 0;
                }
                PlayertargetObject.transform.position = posiorigin;
                PlayertargetObject.transform.rotation = rotrigin;
                PlayertargetObject.GetComponent<Collider>().enabled = true;
                PlayertargetObject.GetComponent<CharacterController>().enabled = true;
                Debug.Log("BAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");

            }
            //
            //Debug.Log(System.DateTime.Now.Millisecond);
            if (timesec != Mathf.Floor((System.DateTime.Now.Millisecond) / 10) && (Player_posi_mover_SW > 0) && outside_sw == false) 
            {

                Quaternion machine_rot_changed = GameObject.Find(Machine_name + "_cam").transform.rotation;
                dif_rot_from_machine = machine_rot_changed.eulerAngles - machine_origin_rot.eulerAngles;
                if (dif_rot_from_machine[0] < 90 && Mathf.Abs(dif_rot_from_machine[1]) < 330 && dif_rot_from_machine[2] < 90)
                {
                    PlayertargetObject.transform.Rotate(0, dif_rot_from_machine[1], 0);
                    machine_origin_rot = GameObject.Find(Machine_name + "_cam").transform.rotation;
                    timesec = Mathf.Floor((System.DateTime.Now.Millisecond) / 10);
                    //Debug.Log("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
                }
            }

            if (((controller_sw == true) && OVRInput.GetDown(OVRInput.RawButton.X)) || ((controller_sw == true) && Input.GetKeyDown(KeyCode.X)))
            {
                Debug.Log("outside");
                outside_sw = true;
                
            }

            if (((Player_posi_mover_SW > 0) && OVRInput.GetDown(OVRInput.RawButton.X) && (num == 1)) || ((Player_posi_mover_SW > 0) && (num == 1)) && Input.GetKeyDown(KeyCode.X))
            {
                VR_cont_2 scriptB = GameObject.Find(Machine_name).GetComponent<VR_cont_2>();
                if (scriptB != null)
                {
                    scriptB.sw = 1;
                    Debug.Log("controller on");
                    controller_sw = true;
                }
            }
            
            if (outside_sw == true)
            {
                //左ジョイスティックの情報取得
                Vector2 stickR = movespeed * OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
                Playerlinear = stickR.y;
                if (Input.GetKey(KeyCode.W))
                {
                    Playerlinear = 0.01f;
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    Playerlinear = -0.01f;
                }
                //OVRCameraRigの位置変更
                GameObject.Find("OVRPlayerController").transform.position += GameObject.Find("OVRPlayerController").transform.rotation * (new Vector3(0, 0, (Playerlinear)));
                if (Input.GetKey(KeyCode.Q))
                {
                    GameObject.Find("OVRPlayerController").transform.Rotate(0, -0.2f, 0);
                }
                else if (Input.GetKey(KeyCode.E))
                {
                    GameObject.Find("OVRPlayerController").transform.Rotate(0, 0.2f, 0);
                }
                if (Math.Abs(stickR.x) > 0.2)
                {
                    GameObject.Find("OVRPlayerController").transform.Rotate(0, stickR.x, 0);
                }

                if (OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.B))
                {
                    outside_sw = false;
                    num = 1;
                    Debug.Log("REgeton");
                }
            }

        }//
    } 
}
