using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseChanger : MonoBehaviour
{
    PoseSubscriber MachinePoseSubscriber;
    public GameObject SubscriberObject;
    GameObject targetObject;
    public float HeightOffset = 100.0f;
    GameObject PlayerObject;

    // Start is called before the first frame update
    void Start()
    {
        MachinePoseSubscriber = SubscriberObject.GetComponent<PoseSubscriber>();
        targetObject = this.gameObject;
        PlayerObject = GameObject.Find("OVRPlayerController");
    }

    // Update is called once per frame
    void Update()
    {
        targetObject.transform.position = SubscriberObject.transform.position + new Vector3(0, HeightOffset, 0);
        targetObject.transform.rotation = SubscriberObject.transform.rotation;
        PlayerObject.transform.SetParent(targetObject.transform);
        PlayerObject.transform.position = targetObject.transform.position;
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
            {
                return child;
            }

            Transform result = FindChildRecursive(child, targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
