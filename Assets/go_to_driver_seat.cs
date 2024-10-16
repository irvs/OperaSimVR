using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System;


public class go_to_driver_seat : MonoBehaviour
{
    public int conum_zx200 = 0;
    public int sw_zx200 = 0;
    public float swsize=0.05f;
    Vector3 posiorigin;
    Quaternion rotrigin;
    public int num = 0;
     Vector3 difang;
    Vector3 dif_rot_zx200;
    //
    float clock=0;
    Quaternion origin_rot_zx200;// = GameObject.Find("zx200_cam").transform.rotation;
    Vector3 dif_rot_zx200_forcube;
    float timesec= Mathf.Floor((System.DateTime.Now.Millisecond)/10);
    float deltatimesec= System.DateTime.Now.Millisecond;
    //

    [SerializeField]
    GameObject ovr_Rig;
   // OVRPlayerController contoroller; 

    // Start is called before the first frame update
    void Start()
    {
    }
    // Update is called once per frame
    void Update()
    {
        sw_zx200 = 0;
    
        Vector3 posi = GameObject.Find("LeftHandAnchor").transform.position;
        Quaternion rot = GameObject.Find("OVRPlayerController").transform.rotation;
        Vector3 resetsw=GameObject.Find("zx200_cam").transform.position;
        Quaternion rot_zx200 = GameObject.Find("zx200_cam").transform.rotation;

        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            //Aボタン
            Debug.Log("INPUT_BBBBBBBBBBBBB");
            sw_zx200 = 1;
        }
        //
        if ((sw_zx200 == 1)&&(conum_zx200 == 0))
        {
            posiorigin = GameObject.Find("OVRPlayerController").transform.position;
            rotrigin = GameObject.Find("OVRPlayerController").transform.rotation;
            conum_zx200 += 1;
            Debug.Log("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaS");
        }
        //
        if((conum_zx200>0)&&(num==0))
        {
            GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
            GameObject.Find("OVRPlayerController").transform.rotation = GameObject.Find("zx200_cam").transform.rotation;//Quaternion.Euler(30, 0, 0);
        }
        //
        if ((conum_zx200>0))
        {
            Debug.Log("BBBBcccc");
            GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
            Vector3 poscam = GameObject.Find("OVRPlayerController").transform.position;
            GameObject.Find("OVRPlayerController").transform.position = poscam + ((GameObject.Find("zx200_cam").transform.position)-poscam)+new Vector3 (0, 0, 0);
            //conum_zx200=0;
            sw_zx200 = 0;
            num=1;
            
            
        }
        //
        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            sw_zx200 = 0;
            conum_zx200=0;
            num = 0;
            GameObject.Find("OVRPlayerController").transform.position = posiorigin;
            GameObject.Find("OVRPlayerController").transform.rotation = rotrigin;
            GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = true;
            Debug.Log("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        }
        //
        //rot_test
        /*
        Vector3 dif_rot_zx200_forcube;
        Quaternion rot_zx200_changed_forcube = GameObject.Find("zx200-body").transform.rotation;
            dif_rot_zx200_forcube = rot_zx200_changed_forcube.eulerAngles - rot_zx200.eulerAngles;
            Debug.Log(dif_rot_zx200_forcube);
            //GameObject.Find("OVRPlayerController").transform.rotation += Quaternion.(rot_zx200_changed.eulerAngles - rot_zx200.eulerAngles);
            GameObject.Find("rot_test_cube").transform.Rotate(dif_rot_zx200_forcube[0], dif_rot_zx200_forcube[1], dif_rot_zx200_forcube[2]);
            Debug.Log(rot.eulerAngles - rot_zx200.eulerAngles);
            */
        //
        //Debug.Log(System.DateTime.Now.Millisecond);
        if (timesec !=  Mathf.Floor((System.DateTime.Now.Millisecond)/10)&&(conum_zx200>0))
        {
            clock=0;
            Quaternion rot_zx200_changed_forcube = GameObject.Find("zx200_cam").transform.rotation;
            dif_rot_zx200_forcube = rot_zx200_changed_forcube.eulerAngles - origin_rot_zx200.eulerAngles;
            //Debug.Log("delta"+dif_rot_zx200_forcube);
            //Debug.Log("zx200"+rot_zx200_changed_forcube.eulerAngles);
            //Debug.Log("cube"+GameObject.Find("rot_test_cube").transform.rotation.eulerAngles);
            //Debug.Log("cam"+GameObject.Find("zx200_cam").transform.rotation.eulerAngles);
            if (dif_rot_zx200_forcube[0]<90 && dif_rot_zx200_forcube[1]<90 && dif_rot_zx200_forcube[2]<90)
            {
            //GameObject.Find("rot_test_cube").transform.Rotate(dif_rot_zx200_forcube[0], dif_rot_zx200_forcube[1], dif_rot_zx200_forcube[2]);
            GameObject.Find("OVRPlayerController").transform.Rotate(0, dif_rot_zx200_forcube[1], 0);
            origin_rot_zx200=GameObject.Find("zx200_cam").transform.rotation;
            timesec= Mathf.Floor((System.DateTime.Now.Millisecond)/10);
            //Debug.Log("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            }
        }
        //
       

//       if ((conum_zx200>0))
//        {
//            Vector3 posis = GameObject.Find("Sphere_pos").transform.position;
//            posis = posis + new Vector3(0, 10, 0);
//            GameObject.Find("OVRPlayerController").transform.position = posis;
//        }
//        if (/*(Input.GetKey(KeyCode.P))*/(sw_zx200 == 1))
//        {
//            Quaternion rots = GameObject.Find("Sphere_pos").transform.rotation;
//            GameObject.Find("OVRPlayerController").transform.rotation = rots;
//        }
        //
        /*if ((Input.GetKey(KeyCode.I)))
        {
            conum_zx200 = 0;
            GameObject.Find("OVRPlayerController").transform.position = posiorigin;
            GameObject.Find("OVRPlayerController").transform.rotation = rotrigin;
        }*/
        //
        //
    }
}