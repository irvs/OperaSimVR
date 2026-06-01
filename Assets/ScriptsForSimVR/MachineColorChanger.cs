using UnityEngine;

public class ParentColorChanger : MonoBehaviour
{
    // 親オブジェクトから子オブジェクトを取得して色を変更する
    public Color newColor = Color.red;

    void Start()
    {
        foreach (Transform child in transform)
        {
            Renderer childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.material.color = newColor;
            }
            //Debug.Log("Color have changed. : " + gameObject.name);
        }
    }
}
