using UnityEngine;

public class ModeChangerForIC120 : MonoBehaviour
{
    public enum ModeOption { NormalModeSimulator, PlayMode, PreviewMode, Else }
    public ModeOption WhichMode;
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

    void Start()
    {
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
        if (WhichMode.ToString() != WhichModePrev)
        {
            WhichModePrev = WhichMode.ToString();
        }

        if (WhichMode == ModeOption.NormalModeSimulator) //simlator
        {
            if (WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = WhichMode.ToString();
                DiffDriveController.ControlMode = 0;
            }
            //
            DiffDriveController.enabled = true;

            JointStatePublisher.enabled = true;

            PoseStampedPublisher.enabled = true;
            VesselController.enabled = true;
            VesselSubscriber.enabled = false;
            //
            Rigidbody.isKinematic = false;
            ArticulationBody_base.enabled = true;
            ArticulationBody_vessel.enabled = true;
            //
            PoseSubscriber.enabled = false;
        }
        if (WhichMode == ModeOption.PlayMode)//visualization
        {
            if (WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = WhichMode.ToString();
            }
            //
            DiffDriveController.enabled = false;

            JointStatePublisher.enabled = false;

            PoseStampedPublisher.enabled = false;
            VesselController.enabled = false;
            VesselSubscriber.enabled = true;
            //
            Rigidbody.isKinematic = true;
            ArticulationBody_base.enabled = false;
            ArticulationBody_vessel.enabled = false;

            PoseSubscriber.enabled = true;

        }

        if (WhichMode == ModeOption.PreviewMode) //simlator+controller
        {
            if (WhichMode.ToString() != WhichModePrev)
            {
                if (WhichModePrev != "NormalModeSimulator")
                {
                    //   mode = 0;
                    //  mode_return = 2;
                }
                WhichModePrev = WhichMode.ToString();
            }
            //
            DiffDriveController.enabled = true;
            DiffDriveController.ControlMode = 1;
            JointStatePublisher.enabled = false;

            PoseStampedPublisher.enabled = false;
            VesselController.enabled = false;
            VesselSubscriber.enabled = true;
            //
            Rigidbody.isKinematic = false;
            ArticulationBody_base.enabled = true;
            ArticulationBody_vessel.enabled = true;
            //
            PoseSubscriber.enabled = true;


        }

    }


}
