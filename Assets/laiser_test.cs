using UnityEngine;
public class ParentObjectName : MonoBehaviour
{
    int warp_triger = 0;
    public int geton_ic120 = 0;
    public int geton_zx200 = 0;
    public int geton_c30r = 0;
    public vrcmdvelcontroller VRcont;
    public string parentObjectName;

    void OnCollisionEnter(Collision collision)
    {
        // 衝突したオブジェクトの親オブジェクトを取得 
        GameObject parentObject = collision.gameObject.transform.root.gameObject;
        if (parentObject != null)
        {
            parentObjectName = parentObject.name;
            //Debug.Log("Parent Object Name: " + parentObjectName);
            //
            //
            Debug.Log(GameObject.Find(parentObjectName).transform.position);
            //

            //
        }
        else
        {
            Debug.Log("The object has no parent.");
        }
    }

    //string parentObjectName;

    void OnTriggerStay(Collider other)
    {
        // トリガーに触れたオブジェクトの親オブジェクトを取得 
        GameObject parentObject = other.gameObject.transform.root.gameObject;
        if (parentObject != null)
        {
            parentObjectName = parentObject.name;
            //Debug.Log("Parent Object Name is: " + parentObjectName); 
            //
            //Debug.Log(GameObject.Find(parentObjectName).transform.position);
            //
            if (parentObjectName != "OVRPlayerController" && (OVRInput.Get(OVRInput.RawButton.LIndexTrigger)))
            {
                warp_triger = 1;
                Debug.Log("fgfffffffffffffff");
                Debug.Log(warp_triger);
                //
                //Debug.Log(parentObjectName);
                if (parentObjectName == "ic120")
                {
                    Debug.Log("vvvSic120");
                    geton_ic120 = 1;
                    geton_zx200 = 0;
                    geton_c30r = 0;

                }
                if (parentObjectName == "zx200")
                {
                    Debug.Log("vvvSic200");
                    geton_ic120 = 0;
                    geton_zx200 = 1;
                    geton_c30r = 0;

                }
                if (parentObjectName == "c30r")
                {
                    Debug.Log("vvvSc30r");
                    geton_ic120 = 0;
                    geton_zx200 = 0;
                    geton_c30r = 1;

                }
            }


        }
        else
        {
            Debug.Log("The object has no parent.");
        }
    }
    ///
    ///
    ///
    public int conum_zx200 = 0;
    public int sw_zx200 = 0;
    public float swsize = 0.05f;
    Vector3 posiorigin;
    Quaternion rotrigin;
    public int num = 0;
    Vector3 difang;
    Vector3 dif_rot_zx200;
    //
    float clock = 0;
    //static string targetmachine = (parentObjectName + "_cam");
    Quaternion origin_rot_zx200;// = GameObject.Find("zx200_cam").transform.rotation;
    Vector3 dif_rot_zx200_forcube;
    float timesec = Mathf.Floor((System.DateTime.Now.Millisecond) / 10);
    float deltatimesec = System.DateTime.Now.Millisecond;
    public GameObject targetObject;
    //

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(parentObjectName);
        //if (parentObjectName == null || parentObjectName== "OVRPlayerController")
        //    {
        //        Debug.Log("qwert");
        //    }

        sw_zx200 = 0;
        if (parentObjectName != null && parentObjectName != "OVRPlayerController")
        {

            Vector3 posi = GameObject.Find("LeftHandAnchor").transform.position;
            Quaternion rot = GameObject.Find("OVRPlayerController").transform.rotation;
            //Vector3 resetsw=GameObject.Find("zx200_cam").transform.position;
            Vector3 resetsw = GameObject.Find(parentObjectName + "_cam").transform.position;
            Quaternion rot_zx200 = GameObject.Find(parentObjectName + "_cam").transform.rotation;

            if (warp_triger == 1)
            {
                //Aボタン
                Debug.Log("INPUT_BBBBBBBBBBBBB");
                sw_zx200 = 1;
            }
            //
            if ((sw_zx200 == 1) && (conum_zx200 == 0))
            {
                posiorigin = GameObject.Find("OVRPlayerController").transform.position;
                rotrigin = GameObject.Find("OVRPlayerController").transform.rotation;
                conum_zx200 += 1;
                Debug.Log("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaS");
            }
            //
            if ((conum_zx200 > 0) && (num == 0))
            {
                GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
                GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = false;
                GameObject.Find("OVRPlayerController").transform.rotation = GameObject.Find(parentObjectName + "_cam").transform.rotation;//Quaternion.Euler(30, 0, 0);
            }
            //
            if ((conum_zx200 > 0))
            {
                Debug.Log("BBBBcccc");
                GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = false;
                Vector3 poscam = GameObject.Find("OVRPlayerController").transform.position;
                GameObject.Find("OVRPlayerController").transform.position = poscam + ((GameObject.Find(parentObjectName + "_cam").transform.position) - poscam) + new Vector3(0, 0, 0);
                /*if (geton_zx200 == 1) 
                {
                    GameObject.Find("OVRPlayerController").transform.rotation = (GameObject.Find(parentObjectName + "_cam").transform.rotation);
                }*/
                //conum_zx200=0;
                sw_zx200 = 0;
                num = 1;


            }
            //
            if ((conum_zx200 > 0) && OVRInput.GetDown(OVRInput.RawButton.B) && (num == 1))
            {
                //
                OVRPlayerController scriptA = targetObject.GetComponent<OVRPlayerController>();
                scriptA.RotationRatchet = 45;
                scriptA.RotationAmount = 0.5f;
                //
                sw_zx200 = 0;
                conum_zx200 = 0;
                num = 0;
                warp_triger = 0;
                geton_ic120 = 0;
                geton_zx200 = 0;
                geton_c30r = 0;
                GameObject.Find("OVRPlayerController").transform.position = posiorigin;
                GameObject.Find("OVRPlayerController").transform.rotation = rotrigin;
                //GameObject.Find("OVRPlayerController").GetComponent<Collider>().enabled = true;
                GameObject.Find("OVRPlayerController").GetComponent<CharacterController>().enabled = true;
                Debug.Log("BAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");

            }
            //

            //
            //Debug.Log(System.DateTime.Now.Millisecond);
            if (timesec != Mathf.Floor((System.DateTime.Now.Millisecond) / 10) && (conum_zx200 > 0))
            {

                clock = 0;
                Quaternion rot_zx200_changed_forcube = GameObject.Find(parentObjectName + "_cam").transform.rotation;
                dif_rot_zx200_forcube = rot_zx200_changed_forcube.eulerAngles - origin_rot_zx200.eulerAngles;
                //Debug.Log("delta"+dif_rot_zx200_forcube);
                //Debug.Log("zx200"+rot_zx200_changed_forcube.eulerAngles);
                //Debug.Log("cube"+GameObject.Find("rot_test_cube").transform.rotation.eulerAngles);
                //Debug.Log("cam"+GameObject.Find("zx200_cam").transform.rotation.eulerAngles);
                if (dif_rot_zx200_forcube[0] < 90 && Mathf.Abs(dif_rot_zx200_forcube[1]) < 330 && dif_rot_zx200_forcube[2] < 90)
                {
                    //GameObject.Find("rot_test_cube").transform.Rotate(dif_rot_zx200_forcube[0], dif_rot_zx200_forcube[1], dif_rot_zx200_forcube[2]);
                    GameObject.Find("OVRPlayerController").transform.Rotate(0, dif_rot_zx200_forcube[1], 0);
                    origin_rot_zx200 = GameObject.Find(parentObjectName + "_cam").transform.rotation;
                    timesec = Mathf.Floor((System.DateTime.Now.Millisecond) / 10);
                    //Debug.Log("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
                }
            }
        }//

        //



    }
}













