using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
//using Oculus;
public class posichange : MonoBehaviour
{
    public int conum_zx200 = 0;
    Vector3 posiorigin;
    Quaternion rotrigin;
    // Start is called before the first frame update
    void Start()
    {
    }
    // Update is called once per frame
    void Update()
    {
        Vector3 posi = GameObject.Find("OVRPlayerController").transform.position;
        Quaternion rot = GameObject.Find("OVRPlayerController").transform.rotation;
        if ((Input.GetKey(KeyCode.P))/*(OVRInput.GetDown(OVRInput.RawButton.A))*/&&(conum_zx200 == 0))
        {
            posiorigin = GameObject.Find("OVRPlayerController").transform.position;
            rotrigin = GameObject.Find("OVRPlayerController").transform.rotation;
            conum_zx200 += 1;
            Debug.Log("a");
        }
        if ((conum_zx200>0))
        {
            Vector3 posis = GameObject.Find("zx200_cam").transform.position;
           // posis = posis + new Vector3(0, 10, 0);
            GameObject.Find("OVRPlayerController").transform.position = posis;
        }
        if ((Input.GetKey(KeyCode.P)))
        {
            Quaternion rots = GameObject.Find("zx200_cam").transform.rotation;
            GameObject.Find("OVRPlayerController").transform.rotation = rots;
        }
        if ((Input.GetKey(KeyCode.I)))
        {
            conum_zx200 = 0;
            GameObject.Find("OVRPlayerController").transform.position = posiorigin;
            GameObject.Find("OVRPlayerController").transform.rotation = rotrigin;
        }
        //
        //
    }
}