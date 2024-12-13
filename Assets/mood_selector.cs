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

public class mood_selector : MonoBehaviour
{
    // Start is called before the first frame update
    public int mode = 0;
    private int prev_mode;
    private int mode_return;

    //void Start()
    void Update()
    {
        if (mode == 0) //simlator
        {
            if (mode != prev_mode)
            {
                prev_mode = mode;
            }
            ///for simulator
            GameObject.Find("ic120").GetComponent<DiffDriveController>().enabled = true;
            GameObject.Find("zx200").GetComponent<DiffDriveController>().enabled = true;
            GameObject.Find("ic120").GetComponent<JointStatePublisher>().enabled = true;
            GameObject.Find("zx200").GetComponent<JointStatePublisher>().enabled = true;

            GameObject.Find("ic120").transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = true;
            GameObject.Find("zx200").transform.Find("base_link/body_link").gameObject.GetComponent<JointPosController>().enabled = true;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<JointPosController>().enabled = true;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<JointPosController>().enabled = true;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<JointPosController>().enabled = true;
            //Debug.Log("asdf");
            //ic120 controllor
            GameObject.Find("ic120").GetComponent<cont_crowlar>().enabled = false;
            GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = false;
            //zx200 controllor
            GameObject.Find("zx200").GetComponent<cont_joint>().enabled = true;
            GameObject.Find("zx200").GetComponent<Rigidbody>().isKinematic = false;
            //ic120 visualize
            //GameObject.Find("ic120").GetComponent<JointSubscriber_zx200>().enabled = false;
            GameObject.Find("ic120").GetComponent<PoseSubscriber>().enabled = false;
            //zx200 visualize
            GameObject.Find("zx200").GetComponent<JointSubscriber>().enabled = false;
            GameObject.Find("zx200").GetComponent<PoseSubscriber>().enabled = false;
            //clock
            GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>().enabled = true;
            //vr controller
            GameObject.Find("vr_cmd_vel_cont").GetComponent<vrcmdvelcontroller>().enabled = false;
            //GameObject.Find("vr_cmd_vel_cont").GetComponent<JointAnglePublisher>().enabled = false;
            GameObject.Find("vr_cmd_vel_cont").GetComponent<vrcmdc30rvelcontroller>().enabled = false;


        }
        if (mode == 1)//visualization
        {
            if (mode != prev_mode)
            {
                prev_mode = mode;
            }
            ///for simulator
            GameObject.Find("ic120").GetComponent<DiffDriveController>().enabled = false;
            GameObject.Find("zx200").GetComponent<DiffDriveController>().enabled = false;
            GameObject.Find("ic120").GetComponent<JointStatePublisher>().enabled = false;
            GameObject.Find("zx200").GetComponent<JointStatePublisher>().enabled = false;

            GameObject.Find("ic120").transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = false;
            GameObject.Find("zx200").transform.Find("base_link/body_link").gameObject.GetComponent<JointPosController>().enabled = false;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<JointPosController>().enabled = false;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<JointPosController>().enabled = false;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<JointPosController>().enabled = false;
            //ic120 controllor
            GameObject.Find("ic120").GetComponent<cont_crowlar>().enabled = false;
            GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = true;
            //zx200 controllor
            GameObject.Find("zx200").GetComponent<cont_joint>().enabled = false;
            GameObject.Find("zx200").GetComponent<Rigidbody>().isKinematic = true;
            //ic120 visualize
            //GameObject.Find("ic120").GetComponent<JointSubscriber_ic120>().enabled = true;
            GameObject.Find("ic120").GetComponent<PoseSubscriber>().enabled = true;
            //zx200 visualize
            GameObject.Find("zx200").GetComponent<JointSubscriber>().enabled = true;
            //GameObject.Find("zx200").GetComponent<PoseSubscriber>().enabled = true;
            //clock
            GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>().enabled = false;
            //vr controller
            GameObject.Find("vr_cmd_vel_cont").GetComponent<vrcmdvelcontroller>().enabled = false;
            GameObject.Find("vr_cmd_vel_cont").GetComponent<JointAnglePublisher>().enabled = false;
            GameObject.Find("vr_cmd_vel_cont").GetComponent<vrcmdc30rvelcontroller>().enabled = false;


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
            ///for simulator
            GameObject.Find("ic120").GetComponent<DiffDriveController>().enabled = false;
            GameObject.Find("zx200").GetComponent<DiffDriveController>().enabled = false;
            GameObject.Find("ic120").GetComponent<JointStatePublisher>().enabled = false;
            GameObject.Find("zx200").GetComponent<JointStatePublisher>().enabled = false;

            GameObject.Find("ic120").transform.Find("base_link").gameObject.GetComponent<PoseStampedPublisher>().enabled = false;
            GameObject.Find("zx200").transform.Find("base_link/body_link").gameObject.GetComponent<JointPosController>().enabled = false;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link").gameObject.GetComponent<JointPosController>().enabled = false;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link/arm_link").gameObject.GetComponent<JointPosController>().enabled = false;
            GameObject.Find("zx200").transform.Find("base_link/body_link/boom_link/arm_link/bucket_link").gameObject.GetComponent<JointPosController>().enabled = false;
            //ic120 controllor
            GameObject.Find("ic120").GetComponent<cont_crowlar>().enabled = true;
            GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = false;
            //zx200 controllor
            GameObject.Find("zx200").GetComponent<cont_joint>().enabled = true;
            GameObject.Find("zx200").GetComponent<Rigidbody>().isKinematic = false;
            //ic120 visualize
            //GameObject.Find("ic120").GetComponent<JointSubscriber_ic120>().enabled = false;
            GameObject.Find("ic120").GetComponent<PoseSubscriber>().enabled = true;
            //zx200 visualize
            GameObject.Find("zx200").GetComponent<JointSubscriber>().enabled = false;
            GameObject.Find("zx200").GetComponent<PoseSubscriber>().enabled = false;
            //GameObject.Find("zx120").GetComponent<PoseSubscriber>().enabled = false;
            //GameObject.Find("c30r").GetComponent<PoseSubscriber>().enabled = false;

            //vr controller
            //GameObject.Find("vr_cmd_vel_cont").GetComponent<vrcmdvelcontroller>().enabled = true;


            //clock
            GameObject.Find("ROS").transform.Find("WorldClock").gameObject.GetComponent<ROSClockPublisher>().enabled = false;

        }

    }


}
