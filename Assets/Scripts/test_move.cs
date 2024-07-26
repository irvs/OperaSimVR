using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Oculus;

public class BallSpawner : MonoBehaviour
{
    //public GameObject prefab;
    //public float spawnSpeed = 5;
    
    // Update is called once per frame
    void Update()
    {
       /* #if ENABLE_LEGACY_INPUT_MANAGER*/
   /* if(OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            //GameObject spawnBall = Instantiate(prefab, transform.position, Quaternion.identity);
            //Rigidbody spawnBallRB = spawnBall.GetComponent<Rigidbody>();
            //spawnBallRB.velocity = transform.forward*spawnSpeed;
            Debug.Log("ok.");
        }
        /*#endif*/
    }
}