using UnityEngine;
public class ControllerLaser : MonoBehaviour
{
    public string GetOnVehicle;
    public string parentObjectName;
    public string OneBeforeRootObjectName;

    void OnCollisionEnter(Collision collision)
    {
        // 衝突したオブジェクトの親オブジェクトを取得
        GameObject hitObject = collision.gameObject;
        GameObject parentObject = collision.gameObject.transform.root.gameObject;
        if (parentObject != null)
        {
            parentObjectName = parentObject.name;
            Debug.Log(GameObject.Find(parentObjectName).transform.position);
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
            Transform current = other.gameObject.transform;
            if (current != null && current.parent != null)
            {
                // ルートまでたどる
                while (current.parent.parent != null)
                {
                    current = current.parent;
                }
            }

            if (parentObjectName != "OVRPlayerController" && parentObjectName != null)
            {
                GetOnVehicle = parentObjectName;
                OneBeforeRootObjectName = current.gameObject.name;
                // Debug.Log("Hit Object Name: " + hitObjectName);
                // Debug.Log("Parent Object Name: " + parentObjectName);
            }
        }
    }

    
}