using UnityEngine;

public class ModeChangerForMST110CR : MonoBehaviour
{
    string WhichModePrev;
    private int prev_mode;
    private int mode_return;
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
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode)//visualization
        {
            if (mode.WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = mode.WhichMode.ToString();
            }
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
    }
}
