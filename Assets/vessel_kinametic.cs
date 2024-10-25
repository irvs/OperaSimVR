using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class vessel_kinametic : MonoBehaviour
{
    public int joint_sw = 0;
    Vector3 vesselposition;
    Vector3 vessel_pose_init;
    Vector3 vessel_rot_init;
    public ArticulationBody articulationBody; // ArticulationBodyをアサイン
    public Transform childObject; // 子オブジェクトをアサイン
    vrcmdvelcontroller controller_synchro;
    public Transform targetParent;
    // Start is called before the first frame update
    void Start()
    {
        Vector3 vessel_pose_init = new Vector3(0.0f, 0.94605f, -3.0153f);
        Vector3 vessel_rot_init = new Vector3(0.0f, 0.0f, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(GameObject.Find("ic120").transform.Find("base_link/vessel_link").position);
        // if (joint_sw == 1)
        //{
        //      GameObject.Find("ic120").transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>().useGravity = false;
        //        GameObject.Find("ic120").transform.Find("base_link").gameObject.GetComponent<ArticulationBody>().enabled = false;
        //          GameObject.Find("ic120").transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>().useGravity = false;
        //            vesselposition = GameObject.Find("ic120").transform.position + vessel_pose_init;
        //GameObject.Find("ic120").transform.Find("base_link/vessel_link").position = vesselposition;
        // vesselrotation = GameObject.Find("ic120").transform.rotation;
        //GameObject.Find("ic120").transform.Find("base_link/vessel_link").rotation = vesselrotation;
        //    childObject.SetParent(transform);
        //      joint_sw = 3;
        //        childObject.position = transform.TransformPoint(articulationBody.anchorPosition);
        //          childObject.rotation = transform.rotation * articulationBody.transform.rotation;
        //        }
        // if (joint_sw == 2)
        //   {
        //          GameObject.Find("ic120").transform.Find("base_link/vessel_link").gameObject.GetComponent<ArticulationBody>().enabled = true;
        //        }
        //if (joint_sw == 3)
        //  {
        //        childObject.position = transform.TransformPoint(articulationBody.anchorPosition);
        //          childObject.rotation = transform.rotation * articulationBody.transform.rotation;
        //        }
        controller_synchro = FindObjectOfType<vrcmdvelcontroller>();
        if (joint_sw == 4 )//|| controller_synchro.synchronization_sw == 1)
        {
            if (articulationBody != null)
            {
                GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = false;
                bool isActive = articulationBody.enabled;
                articulationBody.enabled = !isActive;
                // ArticulationBodyが無効な場合、子オブジェクトを親にくっつける
                if (!articulationBody.enabled)
                {
                    childObject.SetParent(targetParent);
                    joint_sw = 1;
                }
                else
                {
                    //GameObject.Find("ic120").transform.position = GameObject.Find("ic120").transform.position + new Vector3(0.0f, 1.50f, 0.0f);
                    GameObject.Find("ic120").transform.Find("base_link").transform.position = new Vector3(0.0f, 10.0f, 0.0f);
                    // 有効に戻した場合、子オブジェクトを再度ArticulationBodyの子にする
                    childObject.SetParent(articulationBody.transform,true);
                    GameObject.Find("ic120").transform.Find("base_link").transform.position = new Vector3(0.0f, 10.0f, 0.0f);
                    joint_sw = 2;
                }
                GameObject.Find("ic120").GetComponent<Rigidbody>().isKinematic = true;

            }
        }

    }
}
