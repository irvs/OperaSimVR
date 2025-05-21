using UnityEngine;

public class FieldMainManager : MonoBehaviour
{
    //public 
    public enum SimOrRealOption { ForSimPhysX, ForSimAGX, ForReal }

    public SimOrRealOption ForSimOrReal;


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
