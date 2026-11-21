using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OperaSimVR.ConePlacement.Ros;
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
        [SerializeField] ConeRosSettings rosService = new ConeRosSettings();
        [NonSerialized] public Action BeforeEdit;
        public ConeLayout Layout => layout;
        public bool IsEditing { get; private set; }
        public int SelectedId { get; private set; } = -1;
        public int PlacedConeCount { get; private set; }
        public string Status { get; private set; } = "Press F8 or click Cone Placement to start.";
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
        readonly List<ConePositionGroup> placedGroups = new List<ConePositionGroup>();
        string placedLayoutJson;
        bool lastPlacementFailed;
        CancellationTokenSource rosCancellation;
        Task<SetConeLayoutResponse> rosTask;
        bool showRosSettings;
        string rosPortText;
        public bool IsSendingToRos => rosTask != null;
        public string RosStatus { get; private set; } = "Ready. Place cones, then send once.";
        public ConeRosSettings RosSettings => rosService;
        GUISkin skin;
        Font uiFont;
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
                Status = "Click ground to add a point. Click a point ID to select it.";
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
            PollRosService();
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
            { Status = "Click ground with Terrain or ConePlacementSurface."; return; }
            if (moveNextClick) { MoveVertex(SelectedId, point); moveNextClick = false; }
            else AddVertex(point);
        }

        public int AddVertex(Vector3 position)
        {
            try
            {
                BeforeEdit?.Invoke();
                int id = layout.AddVertex(position);
                Changed(); SelectVertex(id); Status = "Point " + id + " added. Enter a neighbor ID to connect it.";
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
            Changed(); SelectVertex(id); Status = "Connections updated."; return true;
        }

        public bool MoveVertex(int id, Vector3 position)
        {
            BeforeEdit?.Invoke();
            if (!layout.MoveVertex(id, position, out string error)) { Status = error; return false; }
            Changed(); Status = "Point " + id + " moved."; return true;
        }

        public void DeleteSelected()
        {
            BeforeEdit?.Invoke();
            if (!layout.RemoveVertex(SelectedId, out string error)) { Status = error; return; }
            SelectedId = -1; moveNextClick = false; Changed(); Status = "Point and connections deleted.";
        }

        public bool SetSpacing(string key, float value)
        {
            BeforeEdit?.Invoke();
            if (!layout.SetGroupSpacing(key, value, out string error)) { Status = error; return false; }
            spacingText[key] = value.ToString("0.###", CultureInfo.InvariantCulture);
            Changed(); Status = "Spacing updated. Click Place / Update Cones to apply."; return true;
        }

        void Changed()
        {
            NeedsPlacement = conesPlaced;
            // Never leave old cones suggesting a boundary that no longer exists.
            if (conesRoot != null) conesRoot.gameObject.SetActive(false);
            PlacedConeCount = 0;
            placedPositions.Clear();
            placedGroups.Clear();
            placedLayoutJson = null;
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
            lastPlacementFailed = true;
            if (!layout.TryGeneratePositionGroups(ConeLayout.MaxCones, out var generatedGroups, out string error))
            { Status = error; return false; }
            var projected = new List<Vector3>();
            var groundByPosition = new Dictionary<Vector3, Vector3>();
            var projectedGroups = new List<ConePositionGroup>();
            foreach (ConePositionGroup group in generatedGroups)
            {
                var groupPositions = new List<Vector3>();
                foreach (Vector3 position in group.Positions)
                {
                    if (!groundByPosition.TryGetValue(position, out Vector3 ground))
                    {
                        if (!TryProjectToSurface(position, out ground))
                        { Status = "Some cone positions have no ground. Check the points and lines."; return false; }
                        groundByPosition.Add(position, ground);
                        projected.Add(ground);
                    }
                    groupPositions.Add(ground);
                }
                projectedGroups.Add(new ConePositionGroup(group, groupPositions));
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
            placedGroups.Clear(); placedGroups.AddRange(projectedGroups);
            placedLayoutJson = layout.ToJson(false);
            lastPlacementFailed = false;
            conesPlaced = true; NeedsPlacement = false; PlacedConeCount = projected.Count;
            Status = projected.Count + " cones placed.";
            Debug.Log("[ConePlacement] " + Status);
            return true;
        }

        /// <summary>Export the actual placed cones, without resampling or changing the visible layout.</summary>
        public bool TryGetPlacedGroups(out IReadOnlyList<ConePositionGroup> result, out string error)
        {
            result = Array.Empty<ConePositionGroup>();
            error = null;
            if (!conesPlaced || NeedsPlacement || lastPlacementFailed || placedPositions.Count == 0
                || placedGroups.Count == 0 || placedLayoutJson != layout.ToJson(false)
                || conesRoot == null || !conesRoot.gameObject.activeInHierarchy)
            { error = "Place / Update Cones before exporting the layout."; return false; }
            if (conesRoot.childCount != placedPositions.Count)
            { error = "Placed cones have changed. Click Place / Update Cones before exporting."; return false; }
            for (int i = 0; i < placedPositions.Count; i++)
            {
                Transform cone = conesRoot.GetChild(i);
                if (!cone.gameObject.activeInHierarchy || !((cone.position - placedPositions[i]).sqrMagnitude <= .00000001f))
                { error = "Placed cones have moved or been hidden. Click Place / Update Cones before exporting."; return false; }
            }
            result = Array.AsReadOnly(placedGroups.ToArray());
            return true;
        }

        public void ClearCones()
        {
            BeforeEdit?.Invoke(); ClearRoot(ref conesRoot);
            conesPlaced = false; NeedsPlacement = false; PlacedConeCount = 0; placedPositions.Clear();
            placedGroups.Clear(); placedLayoutJson = null;
            Status = "Cones cleared. Points and connections kept.";
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
                Status = "Saved: " + path; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            { Status = "Save failed: " + e.Message; return false; }
        }

        public bool LoadLayout()
        {
            try
            {
                if (new FileInfo(FilePath).Length > 2000000) { Status = "Layout file exceeds the size limit."; return false; }
                string json = File.ReadAllText(FilePath);
                if (!ConeLayout.TryFromJson(json, out ConeLayout loaded, out string error)) { Status = error; return false; }
                BeforeEdit?.Invoke();
                layout = loaded; ClearCones(); spacingText.Clear(); SelectedId = -1; moveNextClick = false;
                RefreshGroups(); RebuildVisuals(); Status = "Layout loaded. Click Place / Update Cones to generate cones."; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            { Status = "Load failed: " + e.Message; return false; }
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
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "DejaVu Sans" }, 14);
            skin.font = uiFont;
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
                IsEditing ? "Exit Cone Placement [F8 / Esc]" : "Cone Placement [F8]"))
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
            GUILayout.Label("Edit Cone Layout");
            GUILayout.Label("Click ground to add points. Select a point and enter up to two neighbor IDs. Leave both fields blank to disconnect.");
            if (Application.isPlaying)
            {
                GUILayout.Label("Right-drag: pan | Scroll: zoom");
                if (GUILayout.Button("Frame Layout")) FrameLayout();
            }
            ConeVertex selected = layout.FindVertex(SelectedId);
            GUILayout.Space(6);
            if (selected != null)
            {
                GUILayout.Label("Selected Point: P" + selected.Id + "  (" + selected.Position.x.ToString("F2") + ", " + selected.Position.y.ToString("F2") + ", " + selected.Position.z.ToString("F2") + ") m");
                GUILayout.BeginHorizontal(); GUILayout.Label("Neighbor IDs", GUILayout.Width(92));
                neighborA = GUILayout.TextField(neighborA, 10, GUILayout.MinWidth(0));
                neighborB = GUILayout.TextField(neighborB, 10, GUILayout.MinWidth(0));
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Apply Connections"))
                {
                    var ids = new List<int>(); bool valid = true;
                    foreach (string input in new[] { neighborA, neighborB })
                    {
                        if (string.IsNullOrWhiteSpace(input)) continue;
                        if (int.TryParse(input.Trim(), out int id) && id > 0) ids.Add(id);
                        else valid = false;
                    }
                    if (valid) { SetNeighbors(SelectedId, ids); GUIUtility.ExitGUI(); }
                    else Status = "Enter positive whole numbers for neighbor IDs, or leave blank.";
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(moveNextClick ? "Cancel Move" : "Move Point")) moveNextClick = !moveNextClick;
                if (GUILayout.Button("Delete Point", GUILayout.Width(96))) { DeleteSelected(); GUIUtility.ExitGUI(); }
                GUILayout.EndHorizontal();
                if (moveNextClick) GUILayout.Label("Click ground at the new position.");
            }
            else GUILayout.Label("Selected Point: None");
            GUILayout.Space(7);
            GUILayout.Label("Groups: " + groups.Count + " | Points: " + layout.Vertices.Count);
            foreach (ConeGroup group in groups.ToArray())
            {
                string kind = group.IsClosed ? "Area (closed)" : group.OrderedVertexIds.Count == 1 ? "Single point" : "Line (open)";
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("G" + group.OrderedVertexIds.Min() + " · " + kind + " · " + group.OrderedVertexIds.Count + " points");
                GUILayout.Label("ID: " + string.Join(", ", group.OrderedVertexIds));
                GUILayout.BeginHorizontal(); GUILayout.Label("Spacing (m)", GUILayout.Width(82));
                spacingText[group.Key] = GUILayout.TextField(spacingText[group.Key], 12, GUILayout.MinWidth(0));
                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                {
                    if (float.TryParse(spacingText[group.Key], NumberStyles.Float, CultureInfo.InvariantCulture, out float spacing)) SetSpacing(group.Key, spacing);
                    else Status = "Enter a number for spacing (for example, 2.5).";
                }
                GUILayout.EndHorizontal(); GUILayout.EndVertical();
            }
            GUILayout.Label("Cones include corners and endpoints. The last gap may be shorter. Use separate closed loops for inner and outer boundaries.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Place / Update Cones"))
            {
                // An un-applied text edit must not silently use the old value.
                bool valid = true;
                var values = new Dictionary<string, float>();
                foreach (ConeGroup group in groups.ToArray())
                {
                    if (!float.TryParse(spacingText[group.Key], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    { Status = "Enter a number for spacing."; valid = false; break; }
                    if (!ConeLayout.IsFinite(value) || value < ConeLayout.MinimumSpacing || value > 10000)
                    { Status = "Spacing must be between 0.1 and 10000 m."; valid = false; break; }
                    values[group.Key] = value;
                }
                if (valid)
                {
                    BeforeEdit?.Invoke();
                    foreach (var pair in values) layout.SetGroupSpacing(pair.Key, pair.Value, out _);
                    Changed(); PlaceCones();
                }
            }
            if (GUILayout.Button("Clear Cones", GUILayout.Width(100))) ClearCones();
            GUILayout.EndHorizontal();
            bool visible = GUILayout.Toggle(showGuides, "Keep lines and IDs visible");
            if (visible != showGuides) { BeforeEdit?.Invoke(); showGuides = visible; SetGuideVisibility(); }
            GUILayout.Label("Cones: " + PlacedConeCount + (NeedsPlacement ? " (changed; update required)" : ""));
            GUILayout.Space(7); GUILayout.Label("Layout File (JSON)");
            filePath = GUILayout.TextField(FilePath, GUILayout.MinWidth(0), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) SaveLayout();
            if (GUILayout.Button("Load")) { LoadLayout(); GUIUtility.ExitGUI(); }
            GUILayout.EndHorizontal();
            GUILayout.Label(Status);
            DrawRosControls();
        }

        void DrawRosControls()
        {
            PollRosService();
            GUILayout.Space(10);
            GUILayout.Label("ROS 2 Service (20 group slots)");
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && !IsSendingToRos;
            showRosSettings = GUILayout.Toggle(showRosSettings, "Connection Settings", "Button");
            if (showRosSettings)
            {
                if (rosPortText == null) rosPortText = rosService.port.ToString(CultureInfo.InvariantCulture);
                GUILayout.Label("Host (dedicated cone endpoint)");
                string host = GUILayout.TextField(rosService.host ?? "");
                GUILayout.Label("Port");
                rosPortText = GUILayout.TextField(rosPortText, 5);
                int.TryParse(rosPortText, out int port);
                GUILayout.Label("Service");
                string service = GUILayout.TextField(rosService.serviceName ?? "");
                GUILayout.Label("Frame ID");
                string frame = GUILayout.TextField(rosService.frameId ?? "");
                if (host != rosService.host || port != rosService.port || service != rosService.serviceName || frame != rosService.frameId)
                {
                    BeforeEdit?.Invoke();
                    rosService.host = host; rosService.port = port;
                    rosService.serviceName = service; rosService.frameId = frame;
                }
                GUILayout.Label("ROS FLU: X forward, Y left, Z up (m). Frame Origin and timeout are in the Inspector.");
            }
            if (GUILayout.Button("Send Layout to ROS 2")) SendToRos();
            GUI.enabled = wasEnabled;
            if (IsSendingToRos && GUILayout.Button("Stop Waiting")) CancelRosService();
            GUILayout.Label(RosStatus);
        }

        public bool SendToRos()
        {
            if (IsSendingToRos) return false;
            foreach (ConeGroup group in groups)
            {
                if (!spacingText.TryGetValue(group.Key, out string text) ||
                    !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
                    value != float.Parse(group.Spacing.ToString("0.###", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture))
                { RosStatus = "Spacing input has changed. Click Place / Update Cones before sending."; return false; }
            }
            if (!TryGetPlacedGroups(out var snapshot, out string error)) { RosStatus = error; return false; }
            Transform origin = rosService.frameOrigin;
            if (!ConeRosRequestBuilder.TryBuild(snapshot, rosService.frameId,
                origin != null ? origin.position : Vector3.zero,
                origin != null ? origin.rotation : Quaternion.identity, out var request, out error))
            { RosStatus = error; return false; }
            try
            {
                rosCancellation = new CancellationTokenSource();
                rosTask = ConeRosServiceClient.SendAsync(rosService.host, rosService.port, rosService.serviceName,
                    request, rosService.timeoutSeconds, rosCancellation.Token);
                RosStatus = "Sending once; waiting for one response...";
                return true;
            }
            catch (Exception e)
            {
                rosCancellation?.Dispose(); rosCancellation = null;
                RosStatus = e.Message; return false;
            }
        }

        // EditorWindow pumps completion while the scene is stopped; runtime Update does the same.
        public void PollRosService()
        {
            if (rosTask == null || !rosTask.IsCompleted) return;
            try
            {
                SetConeLayoutResponse response = rosTask.GetAwaiter().GetResult();
                RosStatus = (response.success ? "Accepted" : "Rejected") + ": " + response.message +
                    " | Groups: " + response.group_count + " | Cone entries: " + response.cone_count +
                    " | Response received. Sending finished.";
            }
            catch (OperationCanceledException) { RosStatus = "Waiting stopped. The receiver may have applied the layout. No automatic retry."; }
            catch (Exception e) { RosStatus = "No response confirmed: " + e.Message; }
            finally
            {
                rosTask = null;
                rosCancellation?.Dispose(); rosCancellation = null;
            }
        }

        public void CancelRosService()
        {
            rosCancellation?.Cancel();
            rosCancellation?.Dispose(); rosCancellation = null;
        }

        public void HandleSceneClick(Vector3 point)
        {
            if (moveNextClick && SelectedId >= 0) { MoveVertex(SelectedId, point); moveNextClick = false; }
            else AddVertex(point);
        }

        void OnDisable()
        {
            CancelRosService();
            SetEditing(false);
            if (Active == this) Active = null;
            ClearRoot(ref guidesRoot); ClearRoot(ref conesRoot);
        }

        void OnDestroy()
        {
            DestroyObject(coneMesh); DestroyObject(coneMaterial); DestroyObject(lineMaterial);
            DestroyObject(skin); DestroyObject(uiFont);
        }
    }
}
