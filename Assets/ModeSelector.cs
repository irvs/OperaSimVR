using UnityEngine;

public class ModeSelector : MonoBehaviour
{
    // Start is called before the first frame update
    // public int mode = 0;
    public enum ModeOption { NormalModeSimulator, PlayMode , PreviewMode , Else }
    public ModeOption WhichMode;
    string WhichModePrev;
    private int prev_mode;
    private int mode_return;
    GameObject Dump1;
    GameObject Excavator1;

    //void Start()
    void Update()
    {
        if (WhichMode.ToString() != WhichModePrev)
        {
            // if (WhichMode.ToString() == "NormalModeSimulator"){mode = 0;}
            // else if (WhichMode.ToString() == "PlayMode") { mode = 1; }
            // else if (WhichMode.ToString() == "PreviewMode") { mode = 2; }
            // else if (WhichMode.ToString() == "Else") { mode = 10; }
            WhichModePrev= WhichMode.ToString();
        }

        if (WhichMode == ModeOption.NormalModeSimulator) //simlator
        {
            if (WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = WhichMode.ToString();
                Dump1.GetComponent<DiffDriveController>().ControlMode = 0;
            }
            Dump1 = GameObject.Find("ic120");
            Excavator1 = GameObject.Find("zx200");

            //for simulator
            Dump1.GetComponent<DiffDriveController>().enabled = true;
            
            Dump1.GetComponent<JointStatePublisher>().enabled = true;

            Dump1.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = true;
            Dump1.transform.Find("base_link/vessel_link").gameObject.GetComponent<VesselController>().enabled = true;
            Dump1.transform.Find("base_link/vessel_link").gameObject.GetComponent<VesselSubscriber>().enabled = false;
            //ic120 controllor
            Dump1.GetComponent<Rigidbody>().isKinematic = false;
            //ic120 visualize
            Dump1.GetComponent<PoseSubscriber>().enabled = false;
            
            //for simulator
            Excavator1.GetComponent<DiffDriveController>().enabled = true;
            Excavator1.GetComponent<JointStatePublisher>().enabled = true;

            Excavator1.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = true;
            Excavator1.transform.Find("base_link/body_link").gameObject.GetComponent<JointPosController>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<JointPosController>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<JointPosController>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<JointPosController>().enabled = true;

            Excavator1.transform.Find("base_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_end_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_inner").gameObject.GetComponent<ArticulationBody>().enabled = true;
            //zx200 controllor
            Excavator1.GetComponent<JointControler>().enabled = true;
            Excavator1.GetComponent<Rigidbody>().isKinematic = false;
            //zx200 visualize
            Excavator1.GetComponent<JointSubscriber>().enabled = false;
            Excavator1.GetComponent<PoseSubscriber>().enabled = false;
            
            //clock
            GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>().enabled = true;


        }
        if (WhichMode == ModeOption.PlayMode)//visualization
        {
            if (WhichMode.ToString() != WhichModePrev)
            {
                WhichModePrev = WhichMode.ToString();
            }
            Dump1 = GameObject.Find("ic120");
            Excavator1 = GameObject.Find("zx200");
            ///for simulator
            Dump1.GetComponent<DiffDriveController>().enabled = false;
            
            Dump1.GetComponent<JointStatePublisher>().enabled = false;
            
            Dump1.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = false;
            Dump1.transform.Find("base_link/vessel_link").gameObject.GetComponent<VesselController>().enabled = false;
            Dump1.transform.Find("base_link/vessel_link").gameObject.GetComponent<VesselSubscriber>().enabled = true;
            //ic120 controllor
            Dump1.GetComponent<Rigidbody>().isKinematic = true;

            Dump1.GetComponent<PoseSubscriber>().enabled = true;

            ///for simulator
            Excavator1.GetComponent<DiffDriveController>().enabled = false;
            Excavator1.GetComponent<JointStatePublisher>().enabled = false;

            Excavator1.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = false;
            Excavator1.transform.Find("base_link/body_link").gameObject.GetComponent<JointPosController>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<JointPosController>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<JointPosController>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<JointPosController>().enabled = false;

            Excavator1.transform.Find("base_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
            Excavator1.transform.Find("base_link/body_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_end_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_inner").gameObject.GetComponent<ArticulationBody>().enabled = false;

            //zx200 controllor
            Excavator1.GetComponent<JointControler>().enabled = false;
            Excavator1.GetComponent<Rigidbody>().isKinematic = true;

            //zx200 visualize
            Excavator1.GetComponent<JointSubscriber>().enabled = true;
            //GameObject.Find("zx200").GetComponent<PoseSubscriber>().enabled = true;
            //clock
            GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>().enabled = false;
            //vr controller


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
            Dump1 = GameObject.Find("ic120");
            Excavator1 = GameObject.Find("zx200");
            ///for simulator
            Dump1.GetComponent<DiffDriveController>().enabled = true;
            Dump1.GetComponent<DiffDriveController>().ControlMode = 1;
            Dump1.GetComponent<JointStatePublisher>().enabled = false;
            
            Dump1.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = false;
            //ic120 controllor
            Dump1.GetComponent<Rigidbody>().isKinematic = false;
            //ic120 visualize
            //GameObject.Find("ic120").GetComponent<JointSubscriber_ic120>().enabled = false;
            Dump1.GetComponent<PoseSubscriber>().enabled = true;

            ///for simulator
            Excavator1.GetComponent<DiffDriveController>().enabled = false;
            Excavator1.GetComponent<JointStatePublisher>().enabled = false;

            Excavator1.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = false;
            Excavator1.transform.Find("base_link/body_link").gameObject.GetComponent<JointPosController>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<JointPosController>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<JointPosController>().enabled = false;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<JointPosController>().enabled = false;

            Excavator1.transform.Find("base_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_end_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
            Excavator1.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_inner").gameObject.GetComponent<ArticulationBody>().enabled = true;
            //zx200 controllor
            Excavator1.GetComponent<JointControler>().enabled = true;
            Excavator1.GetComponent<Rigidbody>().isKinematic = false;
            //zx200 visualize
            Excavator1.GetComponent<JointSubscriber>().enabled = false;
            Excavator1.GetComponent<PoseSubscriber>().enabled = false;
            //GameObject.Find("zx120").GetComponent<PoseSubscriber>().enabled = false;

            //clock
            GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>().enabled = false;

        }

    }


}
