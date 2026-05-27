using UnityEngine;

public class ModeChangerForZX200 : MonoBehaviour
{
    string WhichModePrev;
    private int prev_mode;
    private int mode_return;
    GameObject Excavator;
    DiffDriveController DiffDriveController;
    PoseStampedPublisher PoseStampedPublisher;
    PoseSubscriber PoseSubscriber;
    Rigidbody Rigidbody;
    JointStatePublisher JointStatePublisher;
    Com3FrontController Com3FrontController;
    JointControler JointControler;
    JointSubscriber JointSubscriber;
    JointPosController JointPosController_body;
    JointPosController JointPosController_boom;
    JointPosController JointPosController_arm;
    JointPosController JointPosController_bucket;
    ArticulationBody ArticulationBody_base;
    ArticulationBody ArticulationBody_body;
    ArticulationBody ArticulationBody_boom;
    ArticulationBody ArticulationBody_arm;
    ArticulationBody ArticulationBody_bucket;
    ArticulationBody ArticulationBody_bucket_end;
    ArticulationBody ArticulationBody_bucket_inner;
    ModeSelector mode;

    void Start()
    {
        mode = FindObjectOfType<ModeSelector>();
        
        Excavator = this.gameObject;
        //crawler&position
        DiffDriveController = Excavator.GetComponent<DiffDriveController>();
        PoseStampedPublisher = Excavator.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>();
        PoseSubscriber = Excavator.GetComponent<PoseSubscriber>();
        
        Rigidbody = Excavator.GetComponent<Rigidbody>();

        //arm
        JointStatePublisher = Excavator.GetComponent<JointStatePublisher>();
        Com3FrontController = Excavator.GetComponent<Com3FrontController>();
        JointControler = Excavator.GetComponent<JointControler>();
        JointSubscriber = Excavator.GetComponent<JointSubscriber>();
        
        JointPosController_body = Excavator.transform.Find("base_link/body_link").gameObject.GetComponent<JointPosController>();
        JointPosController_boom = Excavator.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<JointPosController>();
        JointPosController_arm = Excavator.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<JointPosController>();
        JointPosController_bucket = Excavator.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<JointPosController>();
        
        ArticulationBody_base = Excavator.transform.Find("base_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_body = Excavator.transform.Find("base_link/body_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_boom = Excavator.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_arm = Excavator.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_bucket = Excavator.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_bucket_end = Excavator.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_end_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_bucket_inner = Excavator.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_inner").gameObject.GetComponent<ArticulationBody>();
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
            }
            //crawler&position
            DiffDriveController.enabled = true;
            PoseStampedPublisher.enabled = true;
            PoseSubscriber.enabled = false;

            Rigidbody.isKinematic = false;
            //arm
            JointStatePublisher.enabled = true;
            Com3FrontController.enabled = true;
            JointControler.enabled = false;
            JointSubscriber.enabled = false;

            JointPosController_body.enabled = true;
            JointPosController_boom.enabled = true;
            JointPosController_arm.enabled = true;
            JointPosController_bucket.enabled = true;

            ArticulationBody_base.enabled = true;
            ArticulationBody_body.enabled = true;
            ArticulationBody_boom.enabled = true;
            ArticulationBody_arm.enabled = true;
            ArticulationBody_bucket.enabled = true;
            ArticulationBody_bucket_end.enabled = true;
            ArticulationBody_bucket_inner.enabled = true;
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
            //arm
            JointStatePublisher.enabled = false;
            Com3FrontController.enabled = false;
            JointControler.enabled = false;
            JointSubscriber.enabled = true;

            JointPosController_body.enabled = false;
            JointPosController_boom.enabled = false;
            JointPosController_arm.enabled = false;
            JointPosController_bucket.enabled = false;

            ArticulationBody_base.enabled = false;
            ArticulationBody_body.enabled = false;
            ArticulationBody_boom.enabled = false;
            ArticulationBody_arm.enabled = false;
            ArticulationBody_bucket.enabled = false;
            ArticulationBody_bucket_end.enabled = false;
            ArticulationBody_bucket_inner.enabled = false;
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
            DiffDriveController.enabled = false;
            PoseStampedPublisher.enabled = false;
            PoseSubscriber.enabled = false;

            Rigidbody.isKinematic = false;
            //arm
            JointStatePublisher.enabled = false;
            Com3FrontController.enabled = false;
            JointControler.enabled = true;
            JointSubscriber.enabled = false;

            JointPosController_body.enabled = false;
            JointPosController_boom.enabled = false;
            JointPosController_arm.enabled = false;
            JointPosController_bucket.enabled = false;

            ArticulationBody_base.enabled = true;
            ArticulationBody_body.enabled = true;
            ArticulationBody_boom.enabled = true;
            ArticulationBody_arm.enabled = true;
            ArticulationBody_bucket.enabled = true;
            ArticulationBody_bucket_end.enabled = true;
            ArticulationBody_bucket_inner.enabled = true;
        }
    }
}
