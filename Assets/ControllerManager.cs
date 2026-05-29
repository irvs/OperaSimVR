using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;


public class ControllerManager : MonoBehaviour
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
    ModelIdentifier ModelInfo;
    SensorCameraNamespase SensorCameraInfo;
    SensorCameraImageSubscriber SensorCamerasImageSubscriber;
    OVRPlayerController PlayerControllScript;
    //
    public TextMeshProUGUI myTMPText;
    private bool Sensorpod;

    // Start is called before the first frame update
    void Start()
    {
        From_VRcont = FindObjectOfType<ControllerLay>();
        PlayertargetObject = GameObject.Find("OVRPlayerController");
        OVRPlayerController PlayerControllScript = PlayertargetObject.GetComponent<OVRPlayerController>();
        From_VRcont = FindObjectOfType<ControllerLay>();
        Machine_Name_List.Add("zero");
    }

    // Update is called once per frame
    void Update()
    {
        if (DesignateVehicleFromInspector == false)
        {
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
                ModelInfo = VehicletargetObject.GetComponent<ModelIdentifier>();
                if (ModelInfo != null)
                {
                    MachineCameraPosition = GameObject.Find(Machine_name + "_cam");
                    Sensorpod = false;
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
                    UpdateTextWithMarkup(Machine_name, "#ff000055");
                }
                SensorCamerasImageSubscriber = VehicletargetObject.GetComponent<SensorCameraImageSubscriber>();
                
                if (SensorCamerasImageSubscriber != null)
                {
                    button = false;
                    Sensorpod = true;
                    posiorigin = PlayertargetObject.transform.position;
                    SensorCameraInfo = GameObject.Find(From_VRcont.OneBeforeRootObjectName).GetComponent<SensorCameraNamespase>();
                    SensorCamerasImageSubscriber.topicName = SensorCameraInfo.ImageTopicName;
                    num = 1;
                    SensorCamerasImageSubscriber.isImageReceived = false;
                    UpdateTextWithMarkup(From_VRcont.OneBeforeRootObjectName, "#ff000055");
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
                if (Sensorpod == true) { SensorCamerasImageSubscriber.isImageReceived = true; }

                GetOnMachine = 0;
                VRCrawlerOp scriptB_c = VehicletargetObject.GetComponent<VRCrawlerOp>();
                if (scriptB_c != null)
                {
                    scriptB_c.OnOffSw = VRCrawlerOp.ONOFF.Off;
                }
                JointAnglePublisher scriptB_b = VehicletargetObject.GetComponent<JointAnglePublisher>();
                if (scriptB_b != null)
                {
                    scriptB_b.OnOffSw = JointAnglePublisher.ONOFF.On;
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
                UpdateTextWithMarkup("", "#ff000055");
                Debug.Log("Get off.");

            }

            if (((PlayerPoseMove_SW > 0) && OVRInput.GetDown(OVRInput.RawButton.X) && (num == 1) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) == true) || ((PlayerPoseMove_SW > 0) && (num == 1)) && Input.GetKeyDown(KeyCode.X))
            {
                VRCrawlerOp scriptB_c = VehicletargetObject.GetComponent<VRCrawlerOp>();
                if (scriptB_c != null)
                {
                    scriptB_c.OnOffSw = VRCrawlerOp.ONOFF.On; ;
                    Debug.Log("controller on");
                }
                JointAnglePublisher scriptB_b = VehicletargetObject.GetComponent<JointAnglePublisher>();
                if (scriptB_b != null)
                {
                    scriptB_b.OnOffSw = JointAnglePublisher.ONOFF.On;
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
                VRCrawlerOp EMG_sw = VehicletargetObject.GetComponent<VRCrawlerOp>();
                if (EMG_sw != null)
                {
                    EMG_sw.emergency = true;
                    Debug.Log("emergency");
                }
            }
            if ((OVRInput.GetDown(OVRInput.RawButton.Y) && OVRInput.Get(OVRInput.RawButton.RIndexTrigger) == true) || Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C))
            {
                emergency_sw = false;
                VRCrawlerOp EMG_sw = VehicletargetObject.GetComponent<VRCrawlerOp>();
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

    public void UpdateTextWithMarkup(string baseText, string colorCode)
    {
        if (myTMPText != null)
        {
            string coloredText = $"<mark={colorCode}>{baseText}</mark>";
            myTMPText.text = coloredText;
        }
    }

}
