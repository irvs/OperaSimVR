using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace OperaSimVR.ConePlacement
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class ConePlacementController : MonoBehaviour
    {
        [SerializeField] ConeLayout layout = new ConeLayout();
        [SerializeField] LayerMask surfaceLayers = ~0;
        [SerializeField] bool showGuides = true;
        [SerializeField] bool conesPlaced;
        [SerializeField] string filePath;
        [NonSerialized] public Action BeforeEdit;
        public ConeLayout Layout => layout;
        public bool IsEditing { get; private set; }
        public int SelectedId { get; private set; } = -1;
        public int PlacedConeCount { get; private set; }
        public string Status { get; private set; } = "F8 またはボタンで編集を開始";
        public bool HasPlacedCones => conesPlaced;
        public bool NeedsPlacement { get; private set; }
        public Camera EditingCamera => editingCamera;
        public static ConePlacementController Active { get; private set; }
        public static bool BlocksWorldInput => Active != null && Active.IsEditing;
        public static bool PointerOverPanel => Active != null && Active.IsPointerOverUI();

        Transform guidesRoot, conesRoot;
        Mesh coneMesh;
        Material coneMaterial, lineMaterial;
        Camera editingCamera, originalCamera;
        bool originalCameraEnabled;
        CursorLockMode savedCursorLock;
        bool savedCursorVisible;
        bool moveNextClick;
        string neighborA = "", neighborB = "";
        Vector2 panelScroll;
        List<ConeGroup> groups = new List<ConeGroup>();
        readonly Dictionary<string, string> spacingText = new Dictionary<string, string>();
        readonly List<Vector3> placedPositions = new List<Vector3>();
        GUISkin skin;
        Font japaneseFont;
        const float PanelWidth = 366;
        public IReadOnlyList<Vector3> PlacedPositions => placedPositions.AsReadOnly();
        public string FilePath
        {
            get => string.IsNullOrWhiteSpace(filePath) ? Path.Combine(Application.persistentDataPath, "cone-layout-" + gameObject.scene.name + ".json") : filePath;
            set => filePath = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Active = null;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != "Assets/Scenes/SampleScene.unity") return;
            if (FindObjectOfType<ConePlacementController>() == null)
                new GameObject("Cone Placement").AddComponent<ConePlacementController>();
        }

        void OnEnable()
        {
            if (Application.isPlaying) Active = this;
            RefreshGroups();
            if (coneMesh != null) { RebuildVisuals(); if (conesPlaced) PlaceCones(); }
        }

        void Start() { RebuildVisuals(); if (conesPlaced) PlaceCones(); }

        public void SetEditing(bool enabled)
        {
            if (IsEditing == enabled) return;
            IsEditing = enabled;
            if (enabled)
            {
                Active = this;
                savedCursorLock = Cursor.lockState;
                savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (Application.isPlaying) OpenEditingCamera();
                Status = "地面クリック: 頂点追加 ／ IDクリック: 選択";
            }
            else
            {
                CloseEditingCamera();
                Cursor.lockState = savedCursorLock;
                Cursor.visible = savedCursorVisible;
                moveNextClick = false;
            }
            SetGuideVisibility();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8)) SetEditing(!IsEditing);
            if (!IsEditing) return;
            if (editingCamera == null && Camera.main != null) OpenEditingCamera();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Input.GetKeyDown(KeyCode.Escape)) { SetEditing(false); return; }
            if (IsPointerOverUI() || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
            if (editingCamera != null)
            {
                float zoom = Input.mouseScrollDelta.y;
                if (zoom != 0) editingCamera.orthographicSize = Mathf.Clamp(editingCamera.orthographicSize * Mathf.Pow(.85f, zoom), 3f, 500f);
                if (Input.GetMouseButton(1))
                {
                    float scale = editingCamera.orthographicSize * 2f / Mathf.Max(1, Screen.height);
                    editingCamera.transform.position -= new Vector3(Input.GetAxis("Mouse X"), 0, Input.GetAxis("Mouse Y")) * scale * 20f;
                }
            }
            if (!Input.GetMouseButtonDown(0)) return;
            Camera camera = editingCamera != null ? editingCamera : Camera.main;
            if (camera == null) return;
            int pick = PickVertex(camera, Input.mousePosition);
            if (pick >= 0 && !moveNextClick) { SelectVertex(pick); return; }
            if (!TryPickSurface(camera.ScreenPointToRay(Input.mousePosition), out Vector3 point))
            { Status = "地面を選択してください（Terrain または ConePlacementSurface）。"; return; }
            if (moveNextClick) { MoveVertex(SelectedId, point); moveNextClick = false; }
            else AddVertex(point);
        }

        public int AddVertex(Vector3 position)
        {
            try
            {
                BeforeEdit?.Invoke();
                int id = layout.AddVertex(position);
                Changed(); SelectVertex(id); Status = "頂点 " + id + " を追加。隣接IDを入力して接続できます。";
                return id;
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
            { Status = e.Message; return -1; }
        }

        public void SelectVertex(int id)
        {
            ConeVertex vertex = layout.FindVertex(id);
            if (vertex == null) return;
            SelectedId = id;
            neighborA = vertex.Neighbors.Count > 0 ? vertex.Neighbors[0].ToString() : "";
            neighborB = vertex.Neighbors.Count > 1 ? vertex.Neighbors[1].ToString() : "";
            moveNextClick = false;
        }

        public bool SetNeighbors(int id, IEnumerable<int> neighbors)
        {
            BeforeEdit?.Invoke();
            if (!layout.SetNeighbors(id, neighbors, out string error)) { Status = error; return false; }
            Changed(); SelectVertex(id); Status = "隣接関係を更新しました。"; return true;
        }

        public bool MoveVertex(int id, Vector3 position)
        {
            BeforeEdit?.Invoke();
            if (!layout.MoveVertex(id, position, out string error)) { Status = error; return false; }
            Changed(); Status = "頂点 " + id + " を移動しました。"; return true;
        }

        public void DeleteSelected()
        {
            BeforeEdit?.Invoke();
            if (!layout.RemoveVertex(SelectedId, out string error)) { Status = error; return; }
            SelectedId = -1; moveNextClick = false; Changed(); Status = "頂点と接続を削除しました。";
        }

        public bool SetSpacing(string key, float value)
        {
            BeforeEdit?.Invoke();
            if (!layout.SetGroupSpacing(key, value, out string error)) { Status = error; return false; }
            spacingText[key] = value.ToString("0.###", CultureInfo.InvariantCulture);
            Changed(); Status = "間隔を更新しました。配置ボタンで反映してください。"; return true;
        }

        void Changed()
        {
            NeedsPlacement = conesPlaced;
            // Never leave old cones suggesting a boundary that no longer exists.
            if (conesRoot != null) conesRoot.gameObject.SetActive(false);
            PlacedConeCount = 0;
            placedPositions.Clear();
            RefreshGroups(); RebuildVisuals();
        }

        public void RefreshFromLayout()
        {
            spacingText.Clear();
            SelectedId = -1;
            Changed();
            if (conesPlaced) PlaceCones();
        }

        void RefreshGroups()
        {
            groups = layout.GetGroups();
            foreach (string stale in spacingText.Keys.Where(k => !groups.Any(g => g.Key == k)).ToArray()) spacingText.Remove(stale);
            foreach (ConeGroup group in groups)
                if (!spacingText.ContainsKey(group.Key)) spacingText[group.Key] = group.Spacing.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public bool PlaceCones()
        {
            if (!layout.TryGeneratePositions(ConeLayout.MaxCones, out List<Vector3> positions, out string error))
            { Status = error; return false; }
            var projected = new List<Vector3>();
            foreach (Vector3 position in positions)
            {
                if (!TryProjectToSurface(position, out Vector3 ground))
                { Status = "地面がない配置位置があります。頂点と接続線を確認してください。"; return false; }
                projected.Add(ground);
            }
            BeforeEdit?.Invoke();
            EnsureAssets();
            ClearRoot(ref conesRoot);
            conesRoot = CreateRoot("Placed Cones");
            for (int i = 0; i < projected.Count; i++)
            {
                var cone = new GameObject("Cone " + (i + 1));
                cone.transform.SetParent(conesRoot, false);
                cone.transform.position = projected[i];
                cone.AddComponent<MeshFilter>().sharedMesh = coneMesh;
                cone.AddComponent<MeshRenderer>().sharedMaterial = coneMaterial;
            }
            placedPositions.Clear(); placedPositions.AddRange(projected);
            conesPlaced = true; NeedsPlacement = false; PlacedConeCount = projected.Count;
            Status = projected.Count + "個のコーンを配置しました。";
            Debug.Log("[ConePlacement] " + Status);
            return true;
        }

        public void ClearCones()
        {
            BeforeEdit?.Invoke(); ClearRoot(ref conesRoot);
            conesPlaced = false; NeedsPlacement = false; PlacedConeCount = 0; placedPositions.Clear();
            Status = "配置したコーンを取り除きました。頂点は保持しています。";
        }

        public bool SaveLayout()
        {
            try
            {
                string path = Path.GetFullPath(FilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, layout.ToJson());
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                Status = "保存しました: " + path; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            { Status = "保存失敗: " + e.Message; return false; }
        }

        public bool LoadLayout()
        {
            try
            {
                if (new FileInfo(FilePath).Length > 2000000) { Status = "配置ファイルのサイズ上限を超えています。"; return false; }
                string json = File.ReadAllText(FilePath);
                if (!ConeLayout.TryFromJson(json, out ConeLayout loaded, out string error)) { Status = error; return false; }
                BeforeEdit?.Invoke();
                layout = loaded; ClearCones(); spacingText.Clear(); SelectedId = -1; moveNextClick = false;
                RefreshGroups(); RebuildVisuals(); Status = "配置データを読み込みました。配置ボタンでコーンを生成できます。"; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            { Status = "読込失敗: " + e.Message; return false; }
        }

        public bool TryPickSurface(Ray ray, out Vector3 position)
        {
            position = default;
            RaycastHit[] hits = Physics.RaycastAll(ray, 5000f, surfaceLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (!IsSurface(hit.collider)) return false;
                position = hit.point; return true;
            }
            return false;
        }

        static bool IsSurface(Collider collider) => collider is TerrainCollider || collider.GetComponentInParent<ConePlacementSurface>() != null;

        public bool TryProjectToSurface(Vector3 point, out Vector3 ground)
        {
            // Raycast to the highest allowed ground, so terrain tiles and marked mesh fields both work.
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(point.x, point.y + 2000f, point.z), Vector3.down,
                4000f, surfaceLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
                if (IsSurface(hit.collider)) { ground = hit.point; return true; }
            ground = point; return false;
        }

        public int PickVertex(Camera camera, Vector2 screenPosition)
        {
            int selected = -1; float nearest = 16 * 16;
            foreach (ConeVertex vertex in layout.Vertices)
            {
                Vector3 screen = camera.WorldToScreenPoint(vertex.Position + Vector3.up * .2f);
                if (screen.z <= 0) continue;
                float distance = ((Vector2)screen - screenPosition).sqrMagnitude;
                if (distance < nearest) { nearest = distance; selected = vertex.Id; }
            }
            return selected;
        }

        void OpenEditingCamera()
        {
            originalCamera = Camera.main;
            if (originalCamera == null) return;
            Vector3 target = originalCamera.transform.position;
            if (TryPickSurface(originalCamera.ViewportPointToRay(new Vector3(.5f, .5f)), out Vector3 hit)) target = hit;
            else if (TryProjectToSurface(target, out Vector3 beneath)) target = beneath;
            else if (Terrain.activeTerrains.Length > 0)
            {
                Vector3 nearest = target;
                float distance = float.MaxValue;
                foreach (Terrain terrain in Terrain.activeTerrains)
                {
                    Vector3 origin = terrain.transform.position, size = terrain.terrainData.size;
                    Vector3 candidate = new Vector3(Mathf.Clamp(target.x, origin.x + .1f, origin.x + size.x - .1f),
                        target.y, Mathf.Clamp(target.z, origin.z + .1f, origin.z + size.z - .1f));
                    float sqrDistance = (candidate - target).sqrMagnitude;
                    if (sqrDistance >= distance) continue;
                    candidate.y = terrain.SampleHeight(candidate) + origin.y;
                    nearest = candidate; distance = sqrDistance;
                }
                target = nearest;
            }
            var obj = new GameObject("Cone Placement Camera");
            obj.hideFlags = HideFlags.DontSave;
            editingCamera = obj.AddComponent<Camera>();
            // OVR cameras can have custom view/projection/culling matrices. Start with a fresh camera.
            editingCamera.backgroundColor = originalCamera.backgroundColor;
            editingCamera.clearFlags = CameraClearFlags.Skybox;
            editingCamera.cullingMask = ~0;
            editingCamera.depth = Camera.allCameras.Where(c => c != editingCamera && c.targetTexture == null)
                .Select(c => c.depth).DefaultIfEmpty(0).Max() + 1;
            editingCamera.stereoTargetEye = StereoTargetEyeMask.None;
            editingCamera.targetTexture = null;
            editingCamera.rect = new Rect(0, 0, 1, 1);
            editingCamera.orthographic = true; editingCamera.orthographicSize = 24;
            editingCamera.nearClipPlane = .1f; editingCamera.farClipPlane = 5000;
            editingCamera.transform.SetPositionAndRotation(target + Vector3.up * 100, Quaternion.Euler(90, 0, 0));
            editingCamera.tag = "MainCamera";
            originalCameraEnabled = originalCamera.enabled;
            originalCamera.enabled = false;
            editingCamera.enabled = true;
        }

        public void FrameLayout()
        {
            if (editingCamera == null || layout.Vertices.Count == 0) return;
            Bounds bounds = new Bounds(layout.Vertices[0].Position, Vector3.zero);
            foreach (ConeVertex vertex in layout.Vertices) bounds.Encapsulate(vertex.Position);
            // Reserve the right-hand panel while keeping all vertices in the visible field.
            float available = Mathf.Max(.2f, (Screen.width - PanelWidth - 36) / Screen.width);
            float size = Mathf.Max(8, bounds.size.z * .65f, bounds.size.x / (editingCamera.aspect * available) * .65f);
            editingCamera.orthographicSize = size;
            editingCamera.transform.position = bounds.center + new Vector3(size * editingCamera.aspect * (1 - available), 100, 0);
        }

        void CloseEditingCamera()
        {
            if (editingCamera != null) { editingCamera.enabled = false; DestroyObject(editingCamera.gameObject); editingCamera = null; }
            if (originalCamera != null) originalCamera.enabled = originalCameraEnabled;
            originalCamera = null;
        }

        void EnsureAssets()
        {
            if (coneMesh == null) coneMesh = ConeVisuals.CreateConeMesh();
            if (coneMaterial == null)
            {
                Shader shader = Resources.Load<Shader>("ConePlacement/ConeSurface");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                coneMaterial = new Material(shader) { name = "Cone placement colors", hideFlags = HideFlags.DontSave };
                lineMaterial = new Material(shader) { name = "Cone placement fluorescent line", color = new Color(.35f, 1f, .05f), hideFlags = HideFlags.DontSave };
            }
        }

        Transform CreateRoot(string name)
        {
            var root = new GameObject(name) { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        public void RebuildVisuals()
        {
            EnsureAssets(); ClearRoot(ref guidesRoot); guidesRoot = CreateRoot("Cone Layout Guides");
            foreach (ConeGroup group in groups)
            {
                int count = group.OrderedVertexIds.Count;
                int edges = group.IsClosed ? count : count - 1;
                for (int i = 0; i < edges; i++)
                {
                    Vector3 a = layout.FindVertex(group.OrderedVertexIds[i]).Position;
                    Vector3 b = layout.FindVertex(group.OrderedVertexIds[(i + 1) % count]).Position;
                    var obj = new GameObject("Link " + group.OrderedVertexIds[i] + "-" + group.OrderedVertexIds[(i + 1) % count]);
                    obj.transform.SetParent(guidesRoot, false);
                    var line = obj.AddComponent<LineRenderer>();
                    line.sharedMaterial = lineMaterial; line.startWidth = line.endWidth = .09f;
                    line.useWorldSpace = true;
                    int steps = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(a, b)), 1, 512);
                    line.positionCount = steps + 1;
                    for (int j = 0; j <= steps; j++)
                    {
                        Vector3 point = Vector3.Lerp(a, b, (float)j / steps);
                        if (TryProjectToSurface(point, out Vector3 ground)) point = ground;
                        line.SetPosition(j, point + Vector3.up * .1f);
                    }
                }
            }
            SetGuideVisibility();
        }

        void SetGuideVisibility() { if (guidesRoot != null) guidesRoot.gameObject.SetActive(showGuides || IsEditing); }
        static new void DestroyObject(UnityEngine.Object obj) { if (obj == null) return; if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        static void ClearRoot(ref Transform root)
        {
            if (root != null) { root.gameObject.SetActive(false); DestroyObject(root.gameObject); }
            root = null;
        }

        Rect PanelRect => new Rect(Mathf.Max(8, Screen.width - PanelWidth - 12), 48, Mathf.Min(PanelWidth, Screen.width - 16), Mathf.Max(100, Screen.height - 60));
        bool IsPointerOverUI()
        {
            Vector2 point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return new Rect(Mathf.Max(8, Screen.width - PanelWidth - 12), 8, PanelWidth, 34).Contains(point) || (IsEditing && PanelRect.Contains(point));
        }

        void EnsureSkin()
        {
            if (skin != null) return;
            skin = Instantiate(GUI.skin);
            skin.hideFlags = HideFlags.DontSave;
            japaneseFont = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans CJK JP", "Yu Gothic", "Meiryo", "Hiragino Sans", "Arial" }, 14);
            skin.font = japaneseFont;
            skin.label.fontSize = skin.button.fontSize = skin.textField.fontSize = skin.toggle.fontSize = 14;
            skin.label.wordWrap = true;
            skin.button.fixedHeight = 27;
            skin.textField.fixedHeight = 25;
        }

        void OnGUI()
        {
            GUISkin previous = GUI.skin;
            EnsureSkin(); GUI.skin = skin;
            if (GUI.Button(new Rect(Mathf.Max(8, Screen.width - PanelWidth - 12), 8, PanelWidth, 34),
                IsEditing ? "コーン配置を終了 [F8 / Esc]" : "コーン自動配置 [F8]"))
            { SetEditing(!IsEditing); GUI.skin = previous; GUIUtility.ExitGUI(); }
            if (IsEditing)
            {
                GUILayout.BeginArea(PanelRect, GUI.skin.box);
                panelScroll = GUILayout.BeginScrollView(panelScroll, false, true);
                GUILayout.BeginVertical(GUILayout.Width(PanelRect.width - 28));
                DrawControls();
                GUILayout.EndVertical(); GUILayout.EndScrollView(); GUILayout.EndArea();
            }
            Camera camera = editingCamera != null ? editingCamera : Camera.main;
            if ((IsEditing || showGuides) && camera != null)
                foreach (ConeVertex vertex in layout.Vertices)
                {
                    Vector3 p = camera.WorldToScreenPoint(vertex.Position + Vector3.up * .2f);
                    if (p.z <= 0 || p.x < 0 || p.x > Screen.width || p.y < 0 || p.y > Screen.height) continue;
                    Rect rect = new Rect(p.x - 22, Screen.height - p.y - 13, 44, 26);
                    if (IsEditing && PanelRect.Overlaps(rect)) continue;
                    Color old = GUI.backgroundColor; GUI.backgroundColor = vertex.Id == SelectedId ? Color.yellow : new Color(.5f, 1f, .2f);
                    if (IsEditing) { if (GUI.Button(rect, "P" + vertex.Id)) SelectVertex(vertex.Id); }
                    else GUI.Box(rect, "P" + vertex.Id);
                    GUI.backgroundColor = old;
                }
            GUI.skin = previous;
        }

        // The same controls are hosted by the Editor window for persistent Scene-view authoring.
        public void DrawControls()
        {
            GUILayout.Label("コーン配置線の編集");
            GUILayout.Label("地面クリックで頂点追加。IDを選び、隣接IDを0～2個指定します。空欄で接続を解除できます。");
            if (Application.isPlaying)
            {
                GUILayout.Label("右ドラッグ: 俯瞰移動 ／ ホイール: 拡大縮小");
                if (GUILayout.Button("配置線の全体を表示")) FrameLayout();
            }
            ConeVertex selected = layout.FindVertex(SelectedId);
            GUILayout.Space(6);
            if (selected != null)
            {
                GUILayout.Label("選択頂点: P" + selected.Id + "  (" + selected.Position.x.ToString("F2") + ", " + selected.Position.y.ToString("F2") + ", " + selected.Position.z.ToString("F2") + ") m");
                GUILayout.BeginHorizontal(); GUILayout.Label("隣接ID", GUILayout.Width(65));
                neighborA = GUILayout.TextField(neighborA, 10, GUILayout.MinWidth(0));
                neighborB = GUILayout.TextField(neighborB, 10, GUILayout.MinWidth(0));
                GUILayout.EndHorizontal();
                if (GUILayout.Button("隣接関係を適用"))
                {
                    var ids = new List<int>(); bool valid = true;
                    foreach (string input in new[] { neighborA, neighborB })
                    {
                        if (string.IsNullOrWhiteSpace(input)) continue;
                        if (int.TryParse(input.Trim(), out int id) && id > 0) ids.Add(id);
                        else valid = false;
                    }
                    if (valid) { SetNeighbors(SelectedId, ids); GUIUtility.ExitGUI(); }
                    else Status = "隣接IDは正の整数、または空欄にしてください。";
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(moveNextClick ? "移動をキャンセル" : "次のクリック位置へ移動")) moveNextClick = !moveNextClick;
                if (GUILayout.Button("頂点削除", GUILayout.Width(80))) { DeleteSelected(); GUIUtility.ExitGUI(); }
                GUILayout.EndHorizontal();
                if (moveNextClick) GUILayout.Label("移動先の地面をクリックしてください。");
            }
            else GUILayout.Label("選択頂点: なし");
            GUILayout.Space(7);
            GUILayout.Label("自動グループ: " + groups.Count + " ／ 頂点: " + layout.Vertices.Count);
            foreach (ConeGroup group in groups.ToArray())
            {
                string kind = group.IsClosed ? "エリア（閉路）" : group.OrderedVertexIds.Count == 1 ? "孤立点" : "線（開路）";
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("G" + group.OrderedVertexIds.Min() + " · " + kind + " · " + group.OrderedVertexIds.Count + "点");
                GUILayout.Label("ID: " + string.Join(", ", group.OrderedVertexIds));
                GUILayout.BeginHorizontal(); GUILayout.Label("間隔 (m)", GUILayout.Width(75));
                spacingText[group.Key] = GUILayout.TextField(spacingText[group.Key], 12, GUILayout.MinWidth(0));
                if (GUILayout.Button("適用", GUILayout.Width(52)))
                {
                    if (float.TryParse(spacingText[group.Key], NumberStyles.Float, CultureInfo.InvariantCulture, out float spacing)) SetSpacing(group.Key, spacing);
                    else Status = "間隔を数値で入力してください（例: 2.5）。";
                }
                GUILayout.EndHorizontal(); GUILayout.EndVertical();
            }
            GUILayout.Label("角・端点に必ず配置。端の余りは指定間隔以下。二重囲いは内周・外周を別々の閉路で定義。");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("コーンを配置 / 更新"))
            {
                // An un-applied text edit must not silently use the old value.
                bool valid = true;
                var values = new Dictionary<string, float>();
                foreach (ConeGroup group in groups.ToArray())
                {
                    if (!float.TryParse(spacingText[group.Key], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    { Status = "間隔を数値で入力してください。"; valid = false; break; }
                    if (!ConeLayout.IsFinite(value) || value < ConeLayout.MinimumSpacing || value > 10000)
                    { Status = "配置間隔は0.1～10000mで入力してください。"; valid = false; break; }
                    values[group.Key] = value;
                }
                if (valid)
                {
                    BeforeEdit?.Invoke();
                    foreach (var pair in values) layout.SetGroupSpacing(pair.Key, pair.Value, out _);
                    Changed(); PlaceCones();
                }
            }
            if (GUILayout.Button("配置を解除", GUILayout.Width(90))) ClearCones();
            GUILayout.EndHorizontal();
            bool visible = GUILayout.Toggle(showGuides, "編集終了後も配置線・IDを表示");
            if (visible != showGuides) { BeforeEdit?.Invoke(); showGuides = visible; SetGuideVisibility(); }
            GUILayout.Label("配置数: " + PlacedConeCount + (NeedsPlacement ? " （変更あり・再配置が必要）" : ""));
            GUILayout.Space(7); GUILayout.Label("配置データ (JSON)");
            filePath = GUILayout.TextField(FilePath, GUILayout.MinWidth(0), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存")) SaveLayout();
            if (GUILayout.Button("読込")) { LoadLayout(); GUIUtility.ExitGUI(); }
            GUILayout.EndHorizontal();
            GUILayout.Label(Status);
        }

        public void HandleSceneClick(Vector3 point)
        {
            if (moveNextClick && SelectedId >= 0) { MoveVertex(SelectedId, point); moveNextClick = false; }
            else AddVertex(point);
        }

        void OnDisable()
        {
            SetEditing(false);
            if (Active == this) Active = null;
            ClearRoot(ref guidesRoot); ClearRoot(ref conesRoot);
        }

        void OnDestroy()
        {
            DestroyObject(coneMesh); DestroyObject(coneMaterial); DestroyObject(lineMaterial);
            DestroyObject(skin); DestroyObject(japaneseFont);
        }
    }
}
