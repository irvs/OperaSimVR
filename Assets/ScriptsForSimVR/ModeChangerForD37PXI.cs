using UnityEngine;

public class ModeChangerForD37PXI0 : MonoBehaviour
{
    public float HeightOffset;
    GameObject Bulldozer;
    DiffDriveController DiffDriveController;
    PoseStampedPublisher PoseStampedPublisher;
    PoseSubscriber PoseSubscriber;
    Rigidbody Rigidbody;
    JointStatePublisher JointStatePublisher;
    Com3FrontController Com3FrontController;
    JointControler JointControler;
    JointSubscriberBD JointSubscriberBD;
    JointPosController JointPosController_body;
    JointPosController JointPosController_boom;
    JointPosController JointPosController_arm;
    JointPosController JointPosController_bucket;
    ArticulationBody ArticulationBody_base;
    ArticulationBody ArticulationBody_body;
    ArticulationBody ArticulationBody_blade_arm_cylinder;
    ArticulationBody ArticulationBody_blade_arm_piston;
    ArticulationBody ArticulationBody_blade_arm;
    ArticulationBody ArticulationBody_blade_angle_piston_left;
    ArticulationBody ArticulationBody_blade_angle_piston_right;
    ArticulationBody ArticulationBody_blade_blade_angle;
    ArticulationBody ArticulationBody_blade;
    ModeSelector mode;
    ModeSelector.ModeOption CurrentMode;
    public GameObject PreviewObject;
    GameObject PrevObject;
    PrevForBackhoe PrevForBackhoe;

    void Start()
    {
        mode = FindObjectOfType<ModeSelector>();
        
        Bulldozer = this.gameObject;
        //crawler&position
        DiffDriveController = Bulldozer.GetComponent<DiffDriveController>();
        PoseStampedPublisher = Bulldozer.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>();
        PoseSubscriber = Bulldozer.GetComponent<PoseSubscriber>();
        
        Rigidbody = Bulldozer.GetComponent<Rigidbody>();

        //arm
        // JointStatePublisher = Bulldozer.GetComponent<JointStatePublisher>();
        // Com3FrontController = Bulldozer.GetComponent<Com3FrontController>();
        // JointControler = Bulldozer.GetComponent<JointControler>();
        JointSubscriberBD = Bulldozer.GetComponent<JointSubscriberBD>();
        
        // JointPosController_body = Bulldozer.transform.Find("base_link/body_link").gameObject.GetComponent<JointPosController>();
        // JointPosController_boom = Bulldozer.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<JointPosController>();
        // JointPosController_arm = Bulldozer.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<JointPosController>();
        // JointPosController_bucket = Bulldozer.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<JointPosController>();
        
        // ArticulationBody_base = Bulldozer.transform.Find("base_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_body = Bulldozer.transform.Find("base_link/body_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_blade_arm_cylinder = Bulldozer.transform.Find("base_link/body_link/blade_arm_cylinder_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_blade_arm_piston = Bulldozer.transform.Find("base_link/body_link/blade_arm_cylinder_link/blade_arm_piston_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_blade_arm = Bulldozer.transform.Find("base_link/body_link/blade_arm_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_blade_angle_piston_left = Bulldozer.transform.Find("base_link/body_link/blade_arm_link/blade_angle_piston_left_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_blade_angle_piston_right = Bulldozer.transform.Find("base_link/body_link/blade_arm_link/blade_angle_piston_right_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_blade_blade_angle = Bulldozer.transform.Find("base_link/body_link/blade_arm_link/blade_angle_link").gameObject.GetComponent<ArticulationBody>();
        ArticulationBody_blade = Bulldozer.transform.Find("base_link/body_link/blade_arm_link/blade_angle_link/blade_link").gameObject.GetComponent<ArticulationBody>();
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
            // JointStatePublisher.enabled = true;
            // Com3FrontController.enabled = true;
            // JointControler.enabled = false;
            JointSubscriberBD.enabled = false;

            // JointPosController_body.enabled = true;
            // JointPosController_boom.enabled = true;
            // JointPosController_arm.enabled = true;
            // JointPosController_bucket.enabled = true;
            
            // JointPosController_body.JointChangeSW = true;
            // JointPosController_boom.JointChangeSW = true;
            // JointPosController_arm.JointChangeSW = true;
            // JointPosController_bucket.JointChangeSW = true;

            ArticulationBody_base.enabled = true;
            ArticulationBody_body.enabled = true;
            ArticulationBody_blade_arm_cylinder.enabled = true;
            ArticulationBody_blade_arm_piston.enabled = true;
            ArticulationBody_blade_arm.enabled = true;
            ArticulationBody_blade_angle_piston_left.enabled = true;
            ArticulationBody_blade_angle_piston_right.enabled = true;
            ArticulationBody_blade_blade_angle.enabled = true;
            ArticulationBody_blade.enabled = true;
        }
        if (mode.WhichMode == ModeSelector.ModeOption.PlayMode || mode.WhichMode == ModeSelector.ModeOption.PreviewAndPlay)//visualization
        {
            //crawler&position
            DiffDriveController.enabled = false;
            PoseStampedPublisher.enabled = false;
            PoseSubscriber.enabled = true;

            Rigidbody.isKinematic = true;
            //arm
            // JointStatePublisher.enabled = false;
            // Com3FrontController.enabled = false;
            // JointControler.enabled = false;
            JointSubscriberBD.enabled = true;

            // JointPosController_body.enabled = false;
            // JointPosController_boom.enabled = false;
            // JointPosController_arm.enabled = false;
            // JointPosController_bucket.enabled = false;

            // JointPosController_body.JointChangeSW = false;
            // JointPosController_boom.JointChangeSW = false;
            // JointPosController_arm.JointChangeSW = false;
            // JointPosController_bucket.JointChangeSW = false;

            ArticulationBody_base.enabled = false;
            ArticulationBody_body.enabled = false;
            ArticulationBody_blade_arm_cylinder.enabled = false;
            ArticulationBody_blade_arm_piston.enabled = false;
            ArticulationBody_blade_arm.enabled = false;
            ArticulationBody_blade_angle_piston_left.enabled = false;
            ArticulationBody_blade_angle_piston_right.enabled = false;
            ArticulationBody_blade_blade_angle.enabled = false;
            ArticulationBody_blade.enabled = false;

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
            // JointStatePublisher.enabled = false;
            // Com3FrontController.enabled = false;
            // JointControler.enabled = true;
            JointSubscriberBD.enabled = false;

            // JointPosController_body.enabled = false;
            // JointPosController_boom.enabled = false;
            // JointPosController_arm.enabled = false;
            // JointPosController_bucket.enabled = false;

            // JointPosController_body.JointChangeSW = false;
            // JointPosController_boom.JointChangeSW = false;
            // JointPosController_arm.JointChangeSW = false;
            // JointPosController_bucket.JointChangeSW = false;

            ArticulationBody_base.enabled = true;
            ArticulationBody_body.enabled = true;
            ArticulationBody_blade_arm_cylinder.enabled = true;
            ArticulationBody_blade_arm_piston.enabled = true;
            ArticulationBody_blade_arm.enabled = true;
            ArticulationBody_blade_angle_piston_left.enabled = true;
            ArticulationBody_blade_angle_piston_right.enabled = true;
            ArticulationBody_blade_blade_angle.enabled = true;
            ArticulationBody_blade.enabled = true;
        }

        if (mode.WhichMode == ModeSelector.ModeOption.PreviewAR)//visualization
        {
            //crawler&position
            DiffDriveController.enabled = false;
            PoseStampedPublisher.enabled = false;
            PoseSubscriber.enabled = true;

            Rigidbody.isKinematic = true;
            //arm
            // JointStatePublisher.enabled = false;
            // Com3FrontController.enabled = false;
            // JointControler.enabled = false;
            JointSubscriberBD.enabled = true;

            // JointPosController_body.enabled = false;
            // JointPosController_boom.enabled = false;
            // JointPosController_arm.enabled = false;
            // JointPosController_bucket.enabled = false;

            // JointPosController_body.JointChangeSW = false;
            // JointPosController_boom.JointChangeSW = false;
            // JointPosController_arm.JointChangeSW = false;
            // JointPosController_bucket.JointChangeSW = false;

            ArticulationBody_base.enabled = false;
            ArticulationBody_body.enabled = false;
            ArticulationBody_blade_arm_cylinder.enabled = false;
            ArticulationBody_blade_arm_piston.enabled = false;
            ArticulationBody_blade_arm.enabled = false;
            ArticulationBody_blade_angle_piston_left.enabled = false;
            ArticulationBody_blade_angle_piston_right.enabled = false;
            ArticulationBody_blade_blade_angle.enabled = false;
            ArticulationBody_blade.enabled = false;
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
