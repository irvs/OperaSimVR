using UnityEngine;

public class ModeChangerForIC120 : MonoBehaviour
{
    public float HeightOffset;
    GameObject Dump; 
    DiffDriveController DiffDriveController;
    JointStatePublisher JointStatePublisher;
    PoseStampedPublisher PoseStampedPublisher;
    VesselController VesselController;
    DumpVesselSubscriber DumpVesselSubscriber;
    Rigidbody Rigidbody;
    PoseSubscriber PoseSubscriber;
    ArticulationBody ArticulationBody_base;
    ArticulationBody ArticulationBody_vessel;
    ModeSelector mode;
    ModeSelector.ModeOption CurrentMode;
    public GameObject PreviewObject;
    GameObject PrevObject;
    PreviewForCruise PreviewForCruise;

    void Start()
    {
        mode = FindObjectOfType<ModeSelector>();

        Dump = this.gameObject;
        DiffDriveController = Dump.GetComponent<DiffDriveController>();
        JointStatePublisher = Dump.GetComponent<JointStatePublisher>();
        PoseStampedPublisher = Dump.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>();
        VesselController = Dump.transform.Find("base_link/vessel_link").gameObject.GetComponent<VesselController>();
        DumpVesselSubscriber = Dump.GetComponent<DumpVesselSubscriber>();
        Rigidbody = Dump.GetComponent<Rigidbody>();

        ArticulationBody_base = Dump.transform.Find("base_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_vessel = Dump.transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>();

        PoseSubscriber = Dump.GetComponent<PoseSubscriber>();
    }
    void Update()
    {
        if (CurrentMode == ModeSelector.ModeOption.PreviewAndPlay || CurrentMode == ModeSelector.ModeOption.PreviewAR)
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
            PoseStampedPublisher.enabled = true;
            PoseSubscriber.enabled = false;

            Rigidbody.isKinematic = false;

            //joints
            JointStatePublisher.enabled = true;
            VesselController.enabled = true;
            DumpVesselSubscriber.enabled = false;

            ArticulationBody_base.enabled = true;
            ArticulationBody_vessel.enabled = true;
        }
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode || mode.WhichMode == ModeSelector.ModeOption.PreviewAndPlay)//visualization
        {
            //crawler&position
            DiffDriveController.enabled = false;
            PoseStampedPublisher.enabled = false;
            PoseSubscriber.enabled = true;
            
            Rigidbody.isKinematic = true;

            //joints
            JointStatePublisher.enabled = false;
            VesselController.enabled = false;
            DumpVesselSubscriber.enabled = true;

            ArticulationBody_base.enabled = false;
            ArticulationBody_vessel.enabled = false;

            if(mode.WhichMode == ModeSelector.ModeOption.PreviewAndPlay)
            {
                if(CurrentMode != ModeSelector.ModeOption.PreviewAndPlay)
                {
                    InitializePreviewmodel();
                }     
            }
        }

        if (mode.WhichMode == ModeSelector.ModeOption.PreviewModeForTeleop) //simlator+controller
        {
            //crawler&position
            DiffDriveController.enabled = true;
            DiffDriveController.ControlMode = 1;
            PoseStampedPublisher.enabled = false;
            PoseSubscriber.enabled = true;

            Rigidbody.isKinematic = false;
            //joints
            JointStatePublisher.enabled = false;
            VesselController.enabled = false;
            DumpVesselSubscriber.enabled = true;
            
            ArticulationBody_base.enabled = true;
            ArticulationBody_vessel.enabled = true;
        }
        if (mode.WhichMode == ModeSelector.ModeOption.PreviewAR)//visualization
        {
            //crawler&position
            DiffDriveController.enabled = false;
            PoseStampedPublisher.enabled = false;
            PoseSubscriber.enabled = true;
            PoseSubscriber.ChengePosition_sw = false;
            
            Rigidbody.isKinematic = true;

            //joints
            JointStatePublisher.enabled = false;

            VesselController.enabled = false;
            DumpVesselSubscriber.enabled = true;
            DumpVesselSubscriber.JointChengeSw = false;

            ArticulationBody_base.enabled = false;
            ArticulationBody_vessel.enabled = false;

            if(mode.WhichMode == ModeSelector.ModeOption.PreviewAR)
            {
                if(CurrentMode != ModeSelector.ModeOption.PreviewAR)
                {
                    HeightOffset = 100.0f;
                    InitializePreviewmodel();
                    HeightOffset = 0.0f;
                }     
            }
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
