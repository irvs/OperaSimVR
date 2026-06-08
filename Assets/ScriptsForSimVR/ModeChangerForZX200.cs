using UnityEngine;

public class ModeChangerForZX200 : MonoBehaviour
{
    public float HeightOffset;
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
    ModeSelector.ModeOption CurrentMode;
    public GameObject PreviewObject;
    GameObject PrevObject;
    PrevForBackhoe PrevForBackhoe;

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
            //arm
            JointStatePublisher.enabled = true;
            Com3FrontController.enabled = true;
            JointControler.enabled = false;
            JointSubscriber.enabled = false;

            JointPosController_body.enabled = true;
            JointPosController_boom.enabled = true;
            JointPosController_arm.enabled = true;
            JointPosController_bucket.enabled = true;
            
            JointPosController_body.JointChangeSW = true;
            JointPosController_boom.JointChangeSW = true;
            JointPosController_arm.JointChangeSW = true;
            JointPosController_bucket.JointChangeSW = true;

            ArticulationBody_base.enabled = true;
            ArticulationBody_body.enabled = true;
            ArticulationBody_boom.enabled = true;
            ArticulationBody_arm.enabled = true;
            ArticulationBody_bucket.enabled = true;
            ArticulationBody_bucket_end.enabled = true;
            ArticulationBody_bucket_inner.enabled = true;
        }
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode || mode.WhichMode == ModeSelector.ModeOption.PreviewAndPlay)//visualization
        {
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

            JointPosController_body.JointChangeSW = false;
            JointPosController_boom.JointChangeSW = false;
            JointPosController_arm.JointChangeSW = false;
            JointPosController_bucket.JointChangeSW = false;

            ArticulationBody_base.enabled = false;
            ArticulationBody_body.enabled = false;
            ArticulationBody_boom.enabled = false;
            ArticulationBody_arm.enabled = false;
            ArticulationBody_bucket.enabled = false;
            ArticulationBody_bucket_end.enabled = false;
            ArticulationBody_bucket_inner.enabled = false;

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

            JointPosController_body.JointChangeSW = false;
            JointPosController_boom.JointChangeSW = false;
            JointPosController_arm.JointChangeSW = false;
            JointPosController_bucket.JointChangeSW = false;

            ArticulationBody_base.enabled = true;
            ArticulationBody_body.enabled = true;
            ArticulationBody_boom.enabled = true;
            ArticulationBody_arm.enabled = true;
            ArticulationBody_bucket.enabled = true;
            ArticulationBody_bucket_end.enabled = true;
            ArticulationBody_bucket_inner.enabled = true;
        }

        if (mode.WhichMode == ModeSelector.ModeOption.PreviewAR)//visualization
        {
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

            JointPosController_body.JointChangeSW = false;
            JointPosController_boom.JointChangeSW = false;
            JointPosController_arm.JointChangeSW = false;
            JointPosController_bucket.JointChangeSW = false;

            ArticulationBody_base.enabled = false;
            ArticulationBody_body.enabled = false;
            ArticulationBody_boom.enabled = false;
            ArticulationBody_arm.enabled = false;
            ArticulationBody_bucket.enabled = false;
            ArticulationBody_bucket_end.enabled = false;
            ArticulationBody_bucket_inner.enabled = false;

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
        PrevForBackhoe = PrevObject.GetComponent<PrevForBackhoe>();
        PrevForBackhoe.SubscriberObject = this.gameObject;
        PrevForBackhoe.enabled = true;
    }
}
