using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldMainManager : MonoBehaviour
{
    //public 
    public enum SimOrRealOption { ForSimPhysX, ForSimAGX, ForReal }

    public SimOrRealOption ForSimOrReal;

    public bool ViaDB;
    public float offset_x = 0;
    public float offset_y = 0;
    public float offset_z = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       // Debug.Log(ForSimOrReal);
    }
}
