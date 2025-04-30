using UnityEngine;
public class ControllerLay : MonoBehaviour
{
    public string GetOnVehicle;

   // public vrcmdvelcontroller VRcont;
    public string parentObjectName;
    public string hitObjectName;
    public int GetOn = 0;

    void OnCollisionEnter(Collision collision)
    {
        // 衝突したオブジェクトの親オブジェクトを取得
        GameObject hitObject = collision.gameObject;
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

    void OnTriggerStay(Collider other)
    {
        // トリガーに触れたオブジェクトの親オブジェクトを取得 
        GameObject hitObject = other.gameObject;
        GameObject parentObject = other.gameObject.transform.root.gameObject;

        if (parentObject != null)
        {
            parentObjectName = parentObject.name;
            //Debug.Log("Parent Object Name is: " + parentObjectName); 
            //
            //Debug.Log(GameObject.Find(parentObjectName).transform.position);
            //
            Transform current = other.gameObject.transform;
            // ルート（親がいないところ）までたどる
            while (current.parent.parent != null)
            {
                current = current.parent;
            }
          //  Transform root = current;
            //
            if (parentObjectName != "OVRPlayerController" && parentObjectName != null)
            {
                GetOnVehicle = parentObjectName;
                Debug.Log(current);
                Debug.Log("Hit Object Name: " + hitObjectName);
                Debug.Log("Parent Object Name: " + parentObjectName);
            }
        }
    }

    
}