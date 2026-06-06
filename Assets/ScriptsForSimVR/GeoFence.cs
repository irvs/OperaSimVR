using UnityEngine;

public class Geofence : MonoBehaviour
{
    Transform objectA; // 1つ目のオブジェクト
    public Transform EndpointObj; // 2つ目のオブジェクト
    private LineRenderer lineRenderer; // LineRendererコンポーネント
    public LayerMask collisionLayer; // 衝突を検出するレイヤー
    string parentObjectName;
    private GameObject targetObject;

    void Start()
    {
        // LineRendererコンポーネントを取得
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        objectA = this.gameObject.transform;
        UpdateLine();
    }

    void Update()
    {
        UpdateLine();
        CheckCollision();
    }

    void UpdateLine()
    {
        lineRenderer.SetPosition(0, objectA.position);
        lineRenderer.SetPosition(1, EndpointObj.position);
    }

    void CheckCollision()
    {
        Vector3 direction = EndpointObj.position - objectA.position;
        RaycastHit hit;

        // 直線（レイキャスト）を行い、衝突した場合の処理
        if (Physics.Raycast(objectA.position, direction.normalized, out hit, direction.magnitude, collisionLayer))
        {
            // 衝突した場合の処理
            Debug.DrawLine(objectA.position, hit.point, Color.red);
            // 親オブジェクトの名前を取得
            Transform parentTransform = hit.collider.transform.root;
            parentObjectName = parentTransform.gameObject.name;
            if (parentTransform != null)
            {
                Debug.Log("衝突したオブジェクト: " + parentObjectName);
                //
                targetObject = GameObject.Find(parentObjectName);
                DrivingCommandPublisher scriptA = targetObject.GetComponent<DrivingCommandPublisher>();
                if (scriptA != null)
                {
                    scriptA.emergency = true;
                    Debug.Log("emergency");
                }
                ModelIdentifier scriptB = targetObject.GetComponent<ModelIdentifier>();
                if (scriptB != null && scriptB.ObjectTypeIsPaperMachine == true) 
                {
                    DrivingCommandPublisher scriptC = GameObject.Find(scriptB.ParentMachine).GetComponent<DrivingCommandPublisher>();
                    if (scriptC != null)
                    {
                        scriptC.emergency = true;
                        Debug.Log("emergency");
                    }
                }
            }
        }
        else
        {
            // 衝突しなかった場合
            Debug.DrawLine(objectA.position, EndpointObj.position, Color.green);
        }

    }
}
