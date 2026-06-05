using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;


public class ControllerManager : MonoBehaviour
{
    ControllerLay From_VRcont;
    public bool DesignateVehicleFromInspector;
    bool outside_sw = false;
    public string Machine_name;
    GameObject VehicletargetObject;
    public enum RideOption {GetOff, GetOn}
    public RideOption GetOnMachine;
    List<string> Machine_Name_List = new List<string>();
    Vector3 posiorigin;
    Quaternion rotrigin;
    public float movespeed = 1.0f;
    GameObject PlayertargetObject;
    GameObject MachineCameraPosition;
    private float Playerlinear;
    public bool DB_pose_sw;
    public bool DB_joint_sw;
    ModelIdentifier ModelInfo;
    SensorCameraNamespase SensorCameraInfo;
    SensorCameraImageSubscriber SensorCamerasImageSubscriber;
    OVRPlayerController PlayerControllScript;
    public TextMeshProUGUI myTMPText;
    private bool SensorCamera;
    ModeSelector mode;
    public GameObject PlaneObject;
    GameObject ScreenObject;
    PoseChanger PoseChanger;
    bool CreatedScreen;
    VRCrawlerOp VRCrawlerOp;
    JointAnglePublisher JointAnglePublisher;

    // Start is called before the first frame update
    void Start()
    {
        From_VRcont = FindObjectOfType<ControllerLay>();
        PlayertargetObject = GameObject.Find("OVRPlayerController");
        PlayerControllScript = PlayertargetObject.GetComponent<OVRPlayerController>();
        Machine_Name_List.Add("zero");
        mode = FindObjectOfType<ModeSelector>();
        SensorCamerasImageSubscriber = VehicletargetObject.GetComponent<SensorCameraImageSubscriber>();
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

        if (Machine_name != null && Machine_name != "" && Machine_name != "OVRPlayerController")
        {
            if (GetOnMachine == RideOption.GetOff && (OVRInput.Get(OVRInput.RawButton.LIndexTrigger) || Input.GetKeyDown(KeyCode.V)))
            {
                GetOnMachine = RideOption.GetOn;
                SwitchCameraPosition();
            }
            else if(GetOnMachine == RideOption.GetOn)
            {
                if (SensorCamera == false && outside_sw == false)
                {
                    if(mode.WhichMode == ModeSelector.ModeOption.PreviewAR)
                    {
                        if(!CreatedScreen)
                        {
                            CreatePlene();
                            CreatedScreen = true;
                        }
                        PlayertargetObject.transform.position = MachineCameraPosition.transform.position + new Vector3(0, 100, 0);    
                    }
                    else{PlayertargetObject.transform.position = MachineCameraPosition.transform.position;}
                }
                if (OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.B))
                {
                    GetOffCamera();
                }
                if (SensorCamera == false && ((OVRInput.GetDown(OVRInput.RawButton.X) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) == true) || Input.GetKeyDown(KeyCode.X)))
                {
                    if (VRCrawlerOp != null)
                    {
                        VRCrawlerOp.OnOffSw = VRCrawlerOp.ONOFF.On; ;
                        Debug.Log("controller on");
                    }
                    if (JointAnglePublisher != null)
                    {
                        JointAnglePublisher.OnOffSw = JointAnglePublisher.ONOFF.On;
                        Debug.Log("controller on");
                    }
                }
                if (outside_sw == true)
                {
                    ControllerInputOutside();
                    if ((OVRInput.GetDown(OVRInput.RawButton.X) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) == false) || Input.GetKeyDown(KeyCode.Z))
                    {
                        outside_sw = false;
                        Debug.Log("REgeton");
                    }
                }
                else if (outside_sw == false && ((OVRInput.GetDown(OVRInput.RawButton.X) && OVRInput.Get(OVRInput.RawButton.LIndexTrigger) == false)|| Input.GetKeyDown(KeyCode.Z)))
                {
                    Debug.Log("outside");
                    outside_sw = true;
                }
                if (OVRInput.GetDown(OVRInput.RawButton.Y) || Input.GetKeyDown(KeyCode.C))
                {
                    Emergency(true);
                    Debug.Log("emergency");
                }
                if ((OVRInput.GetDown(OVRInput.RawButton.Y) && OVRInput.Get(OVRInput.RawButton.RIndexTrigger) == true) || Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C))
                {
                    Emergency(false);
                    Debug.Log("unlock emergency");
                }
                if ((OVRInput.GetDown(OVRInput.RawButton.A) && OVRInput.Get(OVRInput.RawButton.RIndexTrigger) == false) || Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.A)){WriteForBD();}
            }
        }
    }

    public void SwitchCameraPosition()
    {
        VehicletargetObject = GameObject.Find(Machine_name);
        ModelInfo = VehicletargetObject.GetComponent<ModelIdentifier>();

        posiorigin = PlayertargetObject.transform.position;

        if (ModelInfo != null)
        {
            Transform camTransform = FindChildRecursive(VehicletargetObject.transform,"camera_point");
            if (camTransform != null)
            {
                MachineCameraPosition = camTransform.gameObject;
            }
            else
            {
                Debug.LogWarning($"{Machine_name}_cam が {VehicletargetObject.name} 配下に見つかりません");
            }
            SensorCamera = false;
            rotrigin = PlayertargetObject.transform.rotation;
            PlayertargetObject.GetComponent<CharacterController>().enabled = false;
            PlayertargetObject.GetComponent<Collider>().enabled = false;
            PlayertargetObject.transform.rotation = MachineCameraPosition.transform.rotation;
            PlayertargetObject.transform.SetParent(MachineCameraPosition.transform);///////////////
            VRCrawlerOp = VehicletargetObject.GetComponent<VRCrawlerOp>();
            JointAnglePublisher = VehicletargetObject.GetComponent<JointAnglePublisher>();
            UpdateTextWithMarkup(Machine_name, "#ff000055");
        } 
        else if (SensorCamerasImageSubscriber != null)
        {
            SensorCamera = true;
            SensorCameraInfo = GameObject.Find(From_VRcont.OneBeforeRootObjectName).GetComponent<SensorCameraNamespase>();
            SensorCamerasImageSubscriber.topicName = SensorCameraInfo.ImageTopicName;
            SensorCamerasImageSubscriber.isImageReceived = false;
            UpdateTextWithMarkup(From_VRcont.OneBeforeRootObjectName, "#ff000055");
        }
        else{Debug.Log("This object is not vhicle or sonsor camera");}
    }

    public void GetOffCamera()
    {
        PlayerControllScript.transform.SetParent(null); // 子オブジェクト解除/////

        if (SensorCamera == true) { SensorCamerasImageSubscriber.isImageReceived = true; }

        if (VRCrawlerOp != null)
        {
            VRCrawlerOp.OnOffSw = VRCrawlerOp.ONOFF.Off;
        }
        if (JointAnglePublisher != null)
        {
            JointAnglePublisher.OnOffSw = JointAnglePublisher.ONOFF.Off;
        }
        if (outside_sw == false)
        {
            PlayertargetObject.transform.position = posiorigin;
            PlayertargetObject.transform.rotation = rotrigin;
        }
        outside_sw = false;
        PlayertargetObject.GetComponent<Collider>().enabled = true;
        PlayertargetObject.GetComponent<CharacterController>().enabled = true;
        VehicletargetObject = null;
        UpdateTextWithMarkup("", "#ff000055");
        GetOnMachine = RideOption.GetOff;
        Debug.Log("Get off.");
    }
    public void ControllerInputOutside()
    {
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
    }
    public void Emergency(bool Bool)
    {
        if (VRCrawlerOp != null){VRCrawlerOp.emergency = Bool;}
    }

    public void WriteForBD()
    {
        Debug.Log("DB writer on");
        if (ModelInfo.KindsOfHeavyMachinery.ToString() == "IC120"){DB_pose_sw = true;}
        if (ModelInfo.KindsOfHeavyMachinery.ToString() == "ZX200"){DB_joint_sw = true;}
    }

    public void UpdateTextWithMarkup(string baseText, string colorCode)
    {
        if (myTMPText != null)
        {
            string coloredText = $"<mark={colorCode}>{baseText}</mark>";
            myTMPText.text = coloredText;
        }
    }

    public void CreatePlene()
    {
        ScreenObject = Instantiate(PlaneObject, MachineCameraPosition.transform.position + new Vector3(0, 100, 0), MachineCameraPosition.transform.rotation);
        PoseChanger = ScreenObject.GetComponent<PoseChanger>();
        PoseChanger.SubscriberObject = MachineCameraPosition;
        PoseChanger.enabled = true;
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
            {
                return child;
            }
            Transform result = FindChildRecursive(child, targetName);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}
