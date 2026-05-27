using UnityEngine;

public class ModeChangerForIC120 : MonoBehaviour
{
    string WhichModePrev;
    private int prev_mode;
    private int mode_return;
    GameObject Dump; 
    DiffDriveController DiffDriveController;
    JointStatePublisher JointStatePublisher;
    PoseStampedPublisher PoseStampedPublisher;
    VesselController VesselController;
    VesselSubscriber VesselSubscriber;
    Rigidbody Rigidbody;
    PoseSubscriber PoseSubscriber;
    ArticulationBody ArticulationBody_base;
    ArticulationBody ArticulationBody_vessel;
    ModeSelector mode;

    void Start()
    {
        mode = FindObjectOfType<ModeSelector>();

        Dump = this.gameObject;
        DiffDriveController = Dump.GetComponent<DiffDriveController>();
        JointStatePublisher = Dump.GetComponent<JointStatePublisher>();
        PoseStampedPublisher = Dump.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>();
        VesselController = Dump.transform.Find("base_link/vessel_link").gameObject.GetComponent<VesselController>();
        VesselSubscriber = Dump.transform.Find("base_link/vessel_link").gameObject.GetComponent<VesselSubscriber>();
        Rigidbody = Dump.GetComponent<Rigidbody>();

        ArticulationBody_base = Dump.transform.Find("base_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_vessel = Dump.transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>();

        PoseSubscriber = Dump.GetComponent<PoseSubscriber>();
    }
    void Update()
    {
        if (mode.WhichMode.ToString() != WhichModePrev)
        {
            WhichModePrev = mode.WhichMode.ToString();
        }

        if (mode.WhichMode == ModeSelector.ModeOption.NormalModeSimulator) //simlator
        {
            if (mode.WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = mode.WhichMode.ToString();
                DiffDriveController.ControlMode = 0;
            }
            //crawler&position
            DiffDriveController.enabled = true;
            PoseStampedPublisher.enabled = true;
            PoseSubscriber.enabled = false;

            Rigidbody.isKinematic = false;

            //joints
            JointStatePublisher.enabled = true;
            VesselController.enabled = true;
            VesselSubscriber.enabled = false;

            ArticulationBody_base.enabled = true;
            ArticulationBody_vessel.enabled = true;
        }
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode)//visualization
        {
            if (mode.WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = mode.WhichMode.ToString();
            }
            //crawler&position
            DiffDriveController.enabled = false;
            PoseStampedPublisher.enabled = false;
            PoseSubscriber.enabled = true;
            
            Rigidbody.isKinematic = true;

            //joints
            JointStatePublisher.enabled = false;
            VesselController.enabled = false;
            VesselSubscriber.enabled = true;

            ArticulationBody_base.enabled = false;
            ArticulationBody_vessel.enabled = false;
        }

        if (mode.WhichMode == ModeSelector.ModeOption.PreviewMode) //simlator+controller
        {
            if (mode.WhichMode.ToString() != WhichModePrev)
            {
                if (WhichModePrev != "NormalModeSimulator")
                {
                    //   mode = 0;
                    //  mode_return = 2;
                }
                WhichModePrev = mode.WhichMode.ToString();
            }
            //crawler&position
            DiffDriveController.enabled = true;
            DiffDriveController.ControlMode = 1;
            PoseStampedPublisher.enabled = false;
            PoseSubscriber.enabled = true;

            Rigidbody.isKinematic = false;
            //joints
            JointStatePublisher.enabled = false;
            VesselController.enabled = false;
            VesselSubscriber.enabled = true;
            
            ArticulationBody_base.enabled = true;
            ArticulationBody_vessel.enabled = true;
        }
    }
}
