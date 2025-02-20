using UnityEngine;
public class ControllerLay : MonoBehaviour
{
    public string GetOnVehicle;

   // public vrcmdvelcontroller VRcont;
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
            if (parentObjectName != "OVRPlayerController" && parentObjectName != null)
                {
                GetOnVehicle = parentObjectName;
                if (OVRInput.Get(OVRInput.RawButton.LIndexTrigger))
                {
                    
                    
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













