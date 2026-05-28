using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerStay(Collider other)
    {
        Debug.Log("ÚG‚µ‚Ä‚¢‚é: " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player‚ÆÚG’†");
        }
    }

}
