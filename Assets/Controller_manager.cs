using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class Controller_manager : MonoBehaviour
{
    ControllerLay From_VRcont;
    public bool DesignateVehicleFromInspector;
    public bool emergency_sw = false;
    bool outside_sw = false;
    public string Machine_name;
    GameObject VehicletargetObject;
    public int GetOnMachine = 0;
    List<string> Machine_Name_List = new List<string>();
    public int PlayerPoseMove_SW = 0;
    Vector3 posiorigin;
    Quaternion rotrigin;
    int num = 0;
    public float movespeed = 1.0f;
    GameObject PlayertargetObject;
    GameObject MachineCameraPosition;
    private float Playerlinear;
    private bool button;
    public bool DB_pose_sw;
    public bool DB_joint_sw;
    private float DiffRotPlayerMachine;
    Model_name ModelInfo;
    SensorCameraNamespase SensorCameraInfo;
    SensorCameraImageSubscriber SensorCamerasImageSubscriber;
    OVRPlayerController PlayerControllScript;
    //

    // Start is called before the first frame update
    void Start()
    {
        From_VRcont = FindObjectOfType<ControllerLay>();
        PlayertargetObject = GameObject.Find("OVRPlayerController");
        Machine_Name_List.Add("zero");
    }

    // Update is called once per frame
    void Update()
    {
        OVRPlayerController PlayerControllScript = PlayertargetObject.GetComponent<OVRPlayerController>();
        if (DesignateVehicleFromInspector == false)
        {
            From_VRcont = FindObjectOfType<ControllerLay>();
            Machine_name = From_VRcont.GetOnVehicle;
            if (Machine_name != null && Machine_name != "OVRPlayerController")
            {
                if (Machine_Name_List[Machine_Name_List.Count - 1] != Machine_name)
                {
                    Machine_Name_List.Add(Machine_name);
                }
                // Debug.Log(Machine_name);
            }
            Machine_name = Machine_Name_List[Machine_Name_List.Count - 1];
        }

        if (GetOnMachine == 0 && (Machine_name != null && Machine_name != "") && (OVRInput.Get(OVRInput.RawButton.LIndexTrigger) || Input.GetKeyDown(KeyCode.V)))
        {
            GetOnMachine = 1;
            Debug.Log(Machine_name);
        }

        if (Machine_name != null && Machine_name != "" && Machine_name != "OVRPlayerController")
        {
            button = true;
            if ((GetOnMachine == 1) && (PlayerPoseMove_SW == 0))
            {
                VehicletargetObject = GameObject.Find(Machine_name);
                ModelInfo = VehicletargetObject.GetComponent<Model_name>();
                if (ModelInfo != null)
                {
                    MachineCameraPosition = GameObject.Find(Machine_name + "_cam");
                    //Debug.Log(Machine_name : " + machine");
                    posiorigin = PlayertargetObject.transform.position;
                    rotrigin = PlayertargetObject.transform.rotation;
                    PlayertargetObject.GetComponent<CharacterController>().enabled = false;
                    PlayertargetObject.GetComponent<Collider>().enabled = false;
                    PlayerControllScript.RotationRatchet = 45;
                    PlayerControllScript.RotationAmount = 0.5f;
                    PlayertargetObject.transform.rotation = MachineCameraPosition.transform.rotation;
                    PlayertargetObject.transform.SetParent(MachineCameraPosition.transform);///////////////
                    PlayerPoseMove_SW += 1;
                }
                SensorCamerasImageSubscriber = VehicletargetObject.GetComponent<SensorCameraImageSubscriber>();
                
                if (SensorCamerasImageSubscriber != null)
                {
                    button = false;
                    posiorigin = PlayertargetObject.transform.position;
                    SensorCameraInfo = GameObject.Find(From_VRcont.OneBeforeRootObjectName).GetComponent<SensorCameraNamespase>();
                    SensorCamerasImageSubscriber.topicName = SensorCameraInfo.ImageTopicName;
                    num = 1;
                    SensorCamerasImageSubscriber.isImageReceived = false;
                }
            }
            if ((PlayerPoseMove_SW > 0) && outside_sw == false)
            {
                PlayertargetObject.transform.position = MachineCameraPosition.transform.position;
                num = 1;
            }
            if (((PlayerPoseMove_SW > 0 || GetOnMachine == 1) && OVRInput.GetDown(OVRInput.RawButton.B) && (num == 1)) || ((PlayerPoseMove_SW > 0 || GetOnMachine == 1) && (num == 1)) && Input.GetKeyDown(KeyCode.B))
            {
                PlayerControllScript.RotationRatchet = 45;
                PlayerControllScript.RotationAmount = 0.5f;
                PlayerControllScript.transform.SetParent(null); // 子オブジェクト解除/////

                PlayerPoseMove_SW = 0;
                num = 0;

                GetOnMachine = 0;
                VR_cont_2 scriptB_c = VehicletargetObject.GetComponent<VR_cont_2>();
                if (scriptB_c != null)
                {
                    scriptB_c.sw = 0;
                }
                JointAnglePublisher scriptB_b = VehicletargetObject.GetComponent<JointAnglePublisher>();
                if (scriptB_b != null)
                {
                    scriptB_b.sw = 0;
                }

                if (outside_sw == false)
                {
                    PlayertargetObject.transform.position = posiorigin;
                    PlayertargetObject.transform.rotation = rotrigin;
                }
                else
                {
                    outside_sw = false;
                }
                PlayertargetObject.GetComponent<Collider>().enabled = true;
                PlayertargetObject.GetComponent<CharacterController>().enabled = true;
                Debug.Log("Get off.");

            }

            if (((PlayerPoseMove_SW > 0) && OVRInput.GetDown(OVRInput.RawButton.X) && (num == 1) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) == true) || ((PlayerPoseMove_SW > 0) && (num == 1)) && Input.GetKeyDown(KeyCode.X))
            {
                VR_cont_2 scriptB_c = VehicletargetObject.GetComponent<VR_cont_2>();
                if (scriptB_c != null)
                {
                    scriptB_c.sw = 1;
                    Debug.Log("controller on");
                }
                JointAnglePublisher scriptB_b = VehicletargetObject.GetComponent<JointAnglePublisher>();
                if (scriptB_b != null)
                {
                    scriptB_b.sw = 1;
                    Debug.Log("controller on");
                }
            }

            if (outside_sw == true)
            {
                PlayerControllScript.RotationRatchet = 45;
                PlayerControllScript.RotationAmount = 0.5f;
                //
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
                PlayertargetObject.transform.position += PlayertargetObject.transform.rotation * (new Vector3(0, 0, (Playerlinear)));
                if (Input.GetKey(KeyCode.Q))
                {
                    PlayertargetObject.transform.Rotate(0, -0.2f, 0);
                }
                else if (Input.GetKey(KeyCode.E))
                {
                    PlayertargetObject.transform.Rotate(0, 0.2f, 0);
                }
                if (Math.Abs(stickR.x) > 0.2)
                {
                    PlayertargetObject.transform.Rotate(0, stickR.x, 0);
                }

                if ((OVRInput.GetDown(OVRInput.RawButton.X) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) == false && outside_sw == true) || (Input.GetKeyDown(KeyCode.Z) && outside_sw == true))
                {
                    outside_sw = false;
                    num = 1;
                    Debug.Log("REgeton");
                    PlayerControllScript.RotationRatchet = 0;
                    PlayerControllScript.RotationAmount = 0.0f;
                    button = false;
                }
            }
            if ((OVRInput.GetDown(OVRInput.RawButton.X) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) == false && outside_sw == false && button == true) || (Input.GetKeyDown(KeyCode.Z) && outside_sw == false && button == true))
            {
                Debug.Log("outside");
                outside_sw = true;
            }
            if ((OVRInput.GetDown(OVRInput.RawButton.Y)) || (Input.GetKeyDown(KeyCode.C)))
            {
                emergency_sw = true;
                VR_cont_2 EMG_sw = VehicletargetObject.GetComponent<VR_cont_2>();
                if (EMG_sw != null)
                {
                    EMG_sw.emergency = true;
                    Debug.Log("emergency");
                }
            }
            if ((OVRInput.GetDown(OVRInput.RawButton.Y) && OVRInput.Get(OVRInput.RawButton.RIndexTrigger) == true) || Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C))
            {
                emergency_sw = false;
                VR_cont_2 EMG_sw = VehicletargetObject.GetComponent<VR_cont_2>();
                if (EMG_sw != null)
                {
                    EMG_sw.emergency = false;
                    Debug.Log("unlock emergency");
                }
            }
            if ((OVRInput.GetDown(OVRInput.RawButton.A) && OVRInput.Get(OVRInput.RawButton.RIndexTrigger) == false) || Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log("DB writer on");
                if (ModelInfo.KindsOfHeavyMachinery.ToString() == "IC120")
                {
                    DB_pose_sw = true;
                }
                if (ModelInfo.KindsOfHeavyMachinery.ToString() == "ZX200")
                {
                    DB_joint_sw = true;
                }
            }
        }
    }
}
