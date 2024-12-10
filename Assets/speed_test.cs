using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class speed_test : MonoBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        // Rigidbodyコンポーネントを取得
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Rigidbodyの速度を取得
        Vector3 velocity = rb.velocity;

        // オブジェクトの速度を表示
        float speed = velocity.magnitude;
        Debug.Log("Speed: " + speed);
    }
}

