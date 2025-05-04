using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using Unity.Robotics.Core;
using System;

public class mode_selector : MonoBehaviour
{
    // Start is called before the first frame update
    public int mode = 0;
    private int prev_mode;
    private int mode_return;
    GameObject Dump1;
    GameObject Excavator1;

    //void Start()
    void Update()
    {
        if (mode == 0) //simlator
        {
            if (mode != prev_mode)
            {
                prev_mode = mode;
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
            Dump1.GetComponent<cont_crowlar>().enabled = false;
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
            //zx200 controllor
            Excavator1.GetComponent<cont_joint>().enabled = true;
            Excavator1.GetComponent<Rigidbody>().isKinematic = false;
            //zx200 visualize
            Excavator1.GetComponent<JointSubscriber>().enabled = false;
            Excavator1.GetComponent<PoseSubscriber>().enabled = false;
            
            //clock
            GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>().enabled = true;


        }
        if (mode == 1)//visualization
        {
            if (mode != prev_mode)
            {
                prev_mode = mode;
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
            Dump1.GetComponent<cont_crowlar>().enabled = false;
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

            //zx200 controllor
            Excavator1.GetComponent<cont_joint>().enabled = false;
            Excavator1.GetComponent<Rigidbody>().isKinematic = true;

            //zx200 visualize
            Excavator1.GetComponent<JointSubscriber>().enabled = true;
            //GameObject.Find("zx200").GetComponent<PoseSubscriber>().enabled = true;
            //clock
            GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>().enabled = false;
            //vr controller


        }

        if (mode == 2) //simlator+controller
        {
            if (mode != prev_mode)
            {
                if (prev_mode != 0)
                {
                 //   mode = 0;
                  //  mode_return = 2;
                }
                prev_mode = mode;
            }
            Dump1 = GameObject.Find("ic120");
            Excavator1 = GameObject.Find("zx200");
            ///for simulator
            Dump1.GetComponent<DiffDriveController>().enabled = false;
            Dump1.GetComponent<JointStatePublisher>().enabled = false;
            
            Dump1.transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = false;
            //ic120 controllor
            Dump1.GetComponent<cont_crowlar>().enabled = true;
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
            //zx200 controllor
            Excavator1.GetComponent<cont_joint>().enabled = true;
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
