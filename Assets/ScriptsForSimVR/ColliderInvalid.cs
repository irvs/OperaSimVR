using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderInvalid : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // �e�I�u�W�F�N�g�̎q�S�Ă�BoxCollider�𖳌��ɂ���
        BoxCollider[] colliders = GetComponentsInChildren<BoxCollider>();

        foreach (BoxCollider collider in colliders)
        {
            //collider.enabled = false;
            collider.isTrigger = true;
        }
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        foreach (MeshCollider meshCollider in meshColliders)
        {
            //meshCollider.enabled = false;
            meshCollider.isTrigger = true;
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
