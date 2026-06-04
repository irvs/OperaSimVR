using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
public class TerrainCollisionPoint : MonoBehaviour
{
    public Transform handAnchor;       // 手の位置（HandAnchor）
    public Terrain terrain;            // 衝突したいTerrain
    public float rayLength = 10f;      // Rayの長さ（デフォルト10ユニット）
    private Vector3 collisionPoint;
    ControllerManager Geton_controller_manager;
    public LineRenderer lineRenderer;  // LineRendererの参照
    public GameObject spherePrefab;  // 衝突位置に生成する球のPrefab
    public bool ConbineTwePoints;
    public List<Vector3> PathForDB = new List<Vector3>();
    FieldMainManager FieldManager;
    Vector3 PrevPosition;
    public GameObject ArrayPrefab;  // 衝突位置に生成する球のPrefab

    // 生成された球を管理するリスト
    private List<GameObject> sphereObjects = new List<GameObject>();
    void Start()
    {
        FieldManager = FindObjectOfType<FieldMainManager>();
        terrain = FieldManager.terrain;
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        // LineRendererの初期設定
        lineRenderer.positionCount = 0;  // 2点間を結ぶライン
        lineRenderer.startWidth = 0.05f; // ラインの太さ（始点）
        lineRenderer.endWidth = 0.05f;   // ラインの太さ（終点）
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // ラインのマテリアル
        lineRenderer.startColor = Color.red; // ラインの始点の色
        lineRenderer.endColor = Color.red;   // ラインの終点の色
        Geton_controller_manager = FindObjectOfType<ControllerManager>();
    }
    void Update()
    {
        // 手の位置（HandAnchor）をRayの始点として使用
        Vector3 rayOrigin = handAnchor.position;  // HandAnchorの位置
        // Rayの方向は手の向き（handAnchor.forward）を使用
        Vector3 rayDirection = handAnchor.forward; // HandAnchorの前方向を使う
        // Rayを作成
        Ray ray = new Ray(rayOrigin, rayDirection);
        RaycastHit hit;
        // Raycastを飛ばしてTerrainと衝突した場合
        if (Physics.Raycast(ray, out hit, rayLength))
        {
            // 衝突対象がTerrainかどうかを確認
            if (hit.collider.CompareTag("Terrain"))
            {
                // 衝突位置の座標を表示
                collisionPoint = hit.point;
              //  Debug.Log("衝突位置: " + collisionPoint);
                // 衝突位置に球を生成
                CreateSphereAtCollisionPoint(collisionPoint);
            }
        }
        if (((Geton_controller_manager.GetOnMachine == ControllerManager.RideOption.GetOff && OVRInput.Get(OVRInput.RawButton.LIndexTrigger)) || (Geton_controller_manager.GetOnMachine == ControllerManager.RideOption.GetOff && Input.GetKey(KeyCode.B))) && ApproximatelyEqual(PrevPosition, collisionPoint))
        {
            PathForDB.Add(collisionPoint);
            PrevPosition = collisionPoint;
            lineRenderer.positionCount += 1;
            Debug.Log("add points: " + collisionPoint);
            if (PathForDB.Count >= 2)
            {
                Debug.Log("ready to commbine points");
                ConnectRender(PathForDB[PathForDB.Count - 2], collisionPoint);
                Debug.Log("commbine points");
            }
        }
        if (PathForDB.Count >= 2)
        {
            Vector3 direction = collisionPoint - PathForDB[PathForDB.Count - 2];
            // Rayを作成（始点と方向を指定）
            Ray rays = new Ray(PathForDB[PathForDB.Count - 2], direction);
            // Rayを可視化（デバッグ用）
            Debug.DrawRay(PathForDB[PathForDB.Count - 2], direction, Color.red);
            // Raycastを飛ばして衝突するかを確認
            RaycastHit hits;
            if (Physics.Raycast(rays, out hits))
            {
                // 衝突した場合、衝突点を表示
             //   Debug.Log("衝突した位置: " + hits.point);
            }
            if (ConbineTwePoints == true && PathForDB.Count >= 3)
            {
                PathForDB.Clear();
                lineRenderer.positionCount = 0;
                PathForDB.Add(PrevPosition);
                Debug.Log("Reset");
            }
        }
    }

    void ConnectRender(Vector3 startPosition, Vector3 endPosition)
    {
        // LineRendererを更新して、Rayを描画
        // 各点の位置をLineRendererに設定
        lineRenderer.positionCount = PathForDB.Count;
        for (int i = 0; i < PathForDB.Count; i++)
        {
            lineRenderer.SetPosition(i, PathForDB[i]);
        }
    }

    // 衝突位置に球を生成する関数
    void CreateSphereAtCollisionPoint(Vector3 position)
    {
        // 球のPrefabが設定されている場合にインスタンス化
        if (spherePrefab != null)
        {
            // 球を生成
            GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity);
            // 生成した球をリストに追加
            sphereObjects.Add(sphere);

            // もしリストに3つ以上のオブジェクトがある場合、古いものを削除
            if (sphereObjects.Count > 1)
            {
                // 最も古いオブジェクトを削除
                Destroy(sphereObjects[0]);
                // リストから削除
                sphereObjects.RemoveAt(0);
            }
        }
    }
    /*
    void CreateArrayAtCollisionPoint(Vector3 position)
    {
        // 球のPrefabが設定されている場合にインスタンス化
        if (ArrayPrefab != null)
        {
            // 球を生成
            GameObject Array = Instantiate(ArrayPrefab, position, Quaternion.identity);
        }
    }
    */
    bool ApproximatelyEqual(Vector3 a, Vector3 b, float tolerance = 0.1f)
    {
        return Vector3.Distance(a, b) > tolerance;
    }

}