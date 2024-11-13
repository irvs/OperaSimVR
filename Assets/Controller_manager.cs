using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Burst.Intrinsics.Arm;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Controller_manager : MonoBehaviour
{
    ControllerLay From_VRcont;
    public string Machine_name;
    public int GetOnMachine;
    List<string> Machine_Name_List = new List<string>();

    public int Player_posi_mover_SW = 0;
    //public int sw_zx200 = 0;

    Vector3 posiorigin;
    Quaternion rotrigin;
    public int num = 0;
   // Vector3 difang;
   // Vector3 dif_rot_zx200;
    //
    //float clock = 0;
    //static string targetmachine = (parentObjectName + "_cam");
    Quaternion machine_origin_rot;// = GameObject.Find("zx200_cam").transform.rotation;
    Vector3 dif_rot_from_machine;
    float timesec = Mathf.Floor((System.DateTime.Now.Millisecond) / 10);
    float deltatimesec = System.DateTime.Now.Millisecond;
    public GameObject targetObject;
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
        From_VRcont = FindObjectOfType<ControllerLay>();
        Machine_name = From_VRcont.GetOnVehicle;
        if (Machine_Name_List[Machine_Name_List.Count - 1] != Machine_name)
        {
            Machine_Name_List.Add(Machine_name);
        }
        Machine_name = Machine_Name_List[Machine_Name_List.Count - 1];
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
        }

     //   sw_zx200 = 0;
        if (Machine_name != null && Machine_name != "OVRPlayerController")
        {
            /*if (GetOnMachine == 1 && (Player_posi_mover_SW == 0))
            {
                //Aƒ{ƒ^ƒ“
                Debug.Log("INPUT_BBBBBBBBBBBBB");
                sw_zx200 = 1;
            }*/
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
            if ((Player_posi_mover_SW > 0))
            {
              //  Debug.Log("BBBBcccc");
                                
                GameObject.Find("OVRPlayerController").transform.position = GameObject.Find(Machine_name + "_cam").transform.position;

               // sw_zx200 = 0;
                num = 1;
            }
            //
            if ((Player_posi_mover_SW > 0) && OVRInput.GetDown(OVRInput.RawButton.B) && (num == 1))
            {
                //
                OVRPlayerController scriptA = targetObject.GetComponent<OVRPlayerController>();
                scriptA.RotationRatchet = 45;
                scriptA.RotationAmount = 0.5f;

                //
               // sw_zx200 = 0;
                Player_posi_mover_SW = 0;
                num = 0;
                //warp_triger = 0;
                //geton_ic120 = 0;
                //geton_zx200 = 0;
                //geton_c30r = 0;
                GetOnMachine = 0;
                GameObject.Find("OVRPlayerController").transform.position = posiorigin;
                GameObject.Find("OVRPlayerController").transform.rotation = rotrigin;
                GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = true;
                GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = true;
                Debug.Log("BAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");

            }
            //
            //Debug.Log(System.DateTime.Now.Millisecond);
            if (timesec != Mathf.Floor((System.DateTime.Now.Millisecond) / 10) && (Player_posi_mover_SW > 0))
            {

                Quaternion machine_rot_changed = GameObject.Find(Machine_name + "_cam").transform.rotation;
                dif_rot_from_machine = machine_rot_changed.eulerAngles - machine_origin_rot.eulerAngles;
                if (dif_rot_from_machine[0] < 90 && Mathf.Abs(dif_rot_from_machine[1]) < 330 && dif_rot_from_machine[2] < 90)
                {
                    GameObject.Find("OVRPlayerController").transform.Rotate(0, dif_rot_from_machine[1], 0);
                    machine_origin_rot = GameObject.Find(Machine_name + "_cam").transform.rotation;
                    timesec = Mathf.Floor((System.DateTime.Now.Millisecond) / 10);
                    //Debug.Log("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
                }
            }
        }//
    }
}
