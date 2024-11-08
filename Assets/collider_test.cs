using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collider_test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // 親オブジェクトの子全てのBoxColliderを無効にする
        BoxCollider[] colliders = GetComponentsInChildren<BoxCollider>();

        foreach (BoxCollider collider in colliders)
        {
            collider.enabled = false;
        }
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        foreach (MeshCollider meshCollider in meshColliders)
        {
            meshCollider.enabled = false;
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
