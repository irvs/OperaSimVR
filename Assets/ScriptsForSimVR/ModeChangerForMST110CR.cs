using UnityEngine;

public class ModeChangerForMST110CR : MonoBehaviour
{
    public float HeightOffset;
    GameObject Dump; 
    DiffDriveController DiffDriveController;
    Com3FrontController Com3FrontController;
    JointStatePublisher JointStatePublisher;
    GroundTruthPublisher GroundTruthPublisher;
    Rigidbody Rigidbody;
    PoseSubscriber PoseSubscriber;
    ArticulationBody ArticulationBody_base;
    ArticulationBody ArticulationBody_body;
    DumpVesselSubscriber DumpVesselSubscriber;
    ArticulationBody ArticulationBody_vessel_cylinder_link;
    ArticulationBody ArticulationBody_vessel_rod_link;
    ArticulationBody ArticulationBody_vessel_link;
    ModeSelector mode;
    ModeSelector.ModeOption CurrentMode;
    public GameObject PreviewObject;
    GameObject PrevObject;
    PreviewForCruise PreviewForCruise;

    void Start()
    {
        mode = FindObjectOfType<ModeSelector>();

        Dump = this.gameObject;
        //crawler&position
        DiffDriveController = Dump.GetComponent<DiffDriveController>();
        GroundTruthPublisher = Dump.transform.Find("base_link").gameObject.GetComponent<GroundTruthPublisher>();
        PoseSubscriber = Dump.GetComponent<PoseSubscriber>();
        
        Rigidbody = Dump.GetComponent<Rigidbody>();

        //vessel
        JointStatePublisher = Dump.GetComponent<JointStatePublisher>();
        Com3FrontController = Dump.GetComponent<Com3FrontController>();
        DumpVesselSubscriber = Dump.GetComponent<DumpVesselSubscriber>();
        
        ArticulationBody_base = Dump.transform.Find("base_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_body = Dump.transform.Find("base_link/body_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_vessel_cylinder_link = Dump.transform.Find("base_link/body_link/vessel_cylinder_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_vessel_rod_link = Dump.transform.Find("base_link/body_link/vessel_cylinder_link/vessel_rod_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_vessel_link = Dump.transform.Find("base_link/body_link/vessel_link").gameObject.GetComponent<ArticulationBody>();
    }
    void Update()
    {
        if (CurrentMode == ModeSelector.ModeOption.PreviewAndPlay || mode.WhichMode == ModeSelector.ModeOption.PreviewAR)
        {
            if(CurrentMode != mode.WhichMode)
            {
                Destroy(PrevObject);
            }
        }
        if (mode.WhichMode == ModeSelector.ModeOption.NormalModeSimulator) //simlator
        {
            //crawler&position
            DiffDriveController.enabled = true;
            GroundTruthPublisher.enabled = true;
            PoseSubscriber.enabled = false;

            Rigidbody.isKinematic = false;
            
            //vessel
            JointStatePublisher.enabled = true;
            Com3FrontController.enabled = true;
            DumpVesselSubscriber.enabled = false;

            ArticulationBody_base.enabled = true;
            ArticulationBody_body.enabled = true;
            ArticulationBody_vessel_cylinder_link.enabled = true;
            ArticulationBody_vessel_rod_link.enabled = true;
            ArticulationBody_vessel_link.enabled = true;
        }
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode || mode.WhichMode == ModeSelector.ModeOption.PreviewAndPlay || mode.WhichMode == ModeSelector.ModeOption.PreviewAR)//visualization
        {
            //crawler&position
            DiffDriveController.enabled = false;
            GroundTruthPublisher.enabled = false;
            PoseSubscriber.enabled = true;

            Rigidbody.isKinematic = true;

            //vessel
            JointStatePublisher.enabled = false;
            Com3FrontController.enabled = false;
            DumpVesselSubscriber.enabled = true;
            
            ArticulationBody_base.enabled = false;
            ArticulationBody_body.enabled = false;
            ArticulationBody_vessel_cylinder_link.enabled = false;
            ArticulationBody_vessel_rod_link.enabled = false;
            ArticulationBody_vessel_link.enabled = false;

            if(mode.WhichMode == ModeSelector.ModeOption.PreviewAndPlay)
            {
                if(CurrentMode != ModeSelector.ModeOption.PreviewAndPlay)
                {
                    InitializePreviewmodel();
                }     
            }
            else if(mode.WhichMode == ModeSelector.ModeOption.PreviewAR)
            {
                if(CurrentMode != ModeSelector.ModeOption.PreviewAR)
                {
                    HeightOffset = 100.0f;
                    InitializePreviewmodel();
                    HeightOffset = 0.0f;
                }     
            }
        }

        if (mode.WhichMode == ModeSelector.ModeOption.PreviewModeForTeleop) //simlator+controller
        {
            //crawler&position
            DiffDriveController.enabled = true;
            DiffDriveController.ControlMode = 1;
            GroundTruthPublisher.enabled = false;
            PoseSubscriber.enabled = true;

            Rigidbody.isKinematic = false;

            //vessel
            JointStatePublisher.enabled = false;
            Com3FrontController.enabled = false;
            DumpVesselSubscriber.enabled = true;

            ArticulationBody_base.enabled = true;
            ArticulationBody_body.enabled = true;
            ArticulationBody_vessel_cylinder_link.enabled = true;
            ArticulationBody_vessel_rod_link.enabled = true;
            ArticulationBody_vessel_link.enabled = true;
        }
        CurrentMode = mode.WhichMode;
    }
    void InitializePreviewmodel()
        {
            PrevObject = Instantiate(PreviewObject, this.gameObject.transform.position + new Vector3(0, HeightOffset, 0), this.gameObject.transform.rotation);
            PreviewForCruise = PrevObject.GetComponent<PreviewForCruise>();
            PreviewForCruise.SubscriberObject = this.gameObject;
            PreviewForCruise.enabled = true;
        }
}
