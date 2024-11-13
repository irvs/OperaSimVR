using UnityEngine;
public class ControllerLay : MonoBehaviour
{
    int warp_triger = 0;
    public string GetOnVehicle;
    public int geton_ic120 = 0;
    public int geton_zx200 = 0;
    public int geton_c30r = 0;
    public vrcmdvelcontroller VRcont;
    public string parentObjectName;
    public int GetOn = 0;

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
            if (parentObjectName != "OVRPlayerController")
            {
                GetOnVehicle = parentObjectName;
                if (OVRInput.Get(OVRInput.RawButton.LIndexTrigger))
                {
                    /*
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
                    */
                    //GetOnVehicle = parentObjectName;
                    //GetOn = 1;
                    
                }


            }
        }
    }
    ///
    ///
    ///
    public int conum_zx200 = 0;

    

    public int num = 0;


    
}













