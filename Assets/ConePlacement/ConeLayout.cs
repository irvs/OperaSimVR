using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OperaSimVR.ConePlacement
{
    [Serializable]
    public sealed class ConeVertex
    {
        [SerializeField] int id;
        [SerializeField] Vector3 position;
        [SerializeField] List<int> neighbors = new List<int>();
        public int Id => id;
        public Vector3 Position => position;
        public IReadOnlyList<int> Neighbors => neighbors.AsReadOnly();
        internal List<int> Links => neighbors;
        internal ConeVertex(int id, Vector3 position) { this.id = id; this.position = position; }
        internal void Move(Vector3 value) { position = value; }
    }

    public sealed class ConeGroup
    {
        public string Key { get; internal set; }
        public IReadOnlyList<int> OrderedVertexIds { get; internal set; }
        public bool IsClosed { get; internal set; }
        public float Spacing { get; internal set; }
    }

    /// <summary>Undirected paths/rings. All edits validate before changing the graph.</summary>
    [Serializable]
    public sealed class ConeLayout
    {
        public const int MaxVertices = 1000;
        public const int MaxCones = 10000;
        public const float MinimumSpacing = 0.1f;
        public const float DefaultSpacing = 2f;
        [SerializeField] int version = 1;
        [SerializeField] int nextId = 1;
        [SerializeField] List<ConeVertex> vertices = new List<ConeVertex>();
        [SerializeField] List<GroupSetting> settings = new List<GroupSetting>();
        [Serializable] sealed class GroupSetting
        {
            public string key;
            public float spacing;
        }

        public IReadOnlyList<ConeVertex> Vertices => vertices.AsReadOnly();
        public ConeVertex FindVertex(int id) => vertices.Find(v => v.Id == id);
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool ValidPosition(Vector3 p) => IsFinite(p.x) && IsFinite(p.y) && IsFinite(p.z)
            && Mathf.Abs(p.x) <= 1000000 && Mathf.Abs(p.y) <= 1000000 && Mathf.Abs(p.z) <= 1000000;

        public int AddVertex(Vector3 position)
        {
            if (!ValidPosition(position)) throw new ArgumentException("頂点の座標が不正です。");
            if (vertices.Count >= MaxVertices || nextId == int.MaxValue)
                throw new InvalidOperationException("頂点数またはIDの上限に達しました。");
            int id = nextId++;
            vertices.Add(new ConeVertex(id, position));
            return id;
        }

        public bool MoveVertex(int id, Vector3 position, out string error)
        {
            error = null;
            ConeVertex vertex = FindVertex(id);
            if (vertex == null) { error = "頂点IDが存在しません。"; return false; }
            if (!ValidPosition(position)) { error = "頂点の座標が不正です。"; return false; }
            vertex.Move(position);
            return true;
        }

        public bool RemoveVertex(int id, out string error)
        {
            error = null;
            ConeVertex vertex = FindVertex(id);
            if (vertex == null) { error = "頂点IDが存在しません。"; return false; }
            // Materialize old settings so a split can inherit the old group's spacing.
            GetGroups();
            foreach (ConeVertex other in vertices) other.Links.Remove(id);
            vertices.Remove(vertex);
            return true;
        }

        public bool SetNeighbors(int id, IEnumerable<int> neighbors, out string error)
        {
            error = null;
            ConeVertex vertex = FindVertex(id);
            if (vertex == null) { error = "頂点IDが存在しません。"; return false; }
            var proposed = (neighbors ?? Enumerable.Empty<int>()).ToList();
            if (proposed.Count > 2) { error = "隣接点は0～2個です。"; return false; }
            if (proposed.Distinct().Count() != proposed.Count) { error = "同じ隣接IDを重複指定できません。"; return false; }
            foreach (int target in proposed)
            {
                if (target == id) { error = "自分自身には接続できません。"; return false; }
                ConeVertex other = FindVertex(target);
                if (other == null) { error = "隣接ID " + target + " が存在しません。"; return false; }
                if (!other.Links.Contains(id) && other.Links.Count >= 2)
                { error = "頂点 " + target + " の隣接点が2個を超えます。"; return false; }
            }
            GetGroups();
            foreach (int old in vertex.Links.ToArray()) FindVertex(old).Links.Remove(id);
            vertex.Links.Clear();
            foreach (int target in proposed.OrderBy(n => n))
            {
                vertex.Links.Add(target);
                FindVertex(target).Links.Add(id);
                FindVertex(target).Links.Sort();
            }
            return true;
        }

        public List<ConeGroup> GetGroups()
        {
            var lookup = vertices.ToDictionary(v => v.Id);
            var visited = new HashSet<int>();
            var result = new List<ConeGroup>();
            var oldSettings = settings.ToArray();
            foreach (ConeVertex first in vertices.OrderBy(v => v.Id))
            {
                if (visited.Contains(first.Id)) continue;
                var component = new List<int>();
                var pending = new Stack<int>();
                pending.Push(first.Id);
                while (pending.Count > 0)
                {
                    int id = pending.Pop();
                    if (!visited.Add(id)) continue;
                    component.Add(id);
                    foreach (int neighbor in lookup[id].Neighbors) pending.Push(neighbor);
                }
                component.Sort();
                string key = string.Join("-", component);
                bool closed = component.Count >= 3 && component.All(id => lookup[id].Neighbors.Count == 2);
                int start = closed ? component[0] : component.First(id => lookup[id].Neighbors.Count < 2);
                var ordered = new List<int>();
                int previous = -1, current = start;
                do
                {
                    ordered.Add(current);
                    int next = lookup[current].Neighbors.Where(n => n != previous).OrderBy(n => n).DefaultIfEmpty(-1).First();
                    previous = current;
                    current = next;
                } while (current != -1 && current != start && ordered.Count < component.Count);
                GroupSetting setting = oldSettings.FirstOrDefault(s => s.key == key);
                if (setting == null)
                {
                    // Largest surviving membership wins; ties use the smallest original ID.
                    GroupSetting inherited = oldSettings.Select(s => new
                    {
                        value = s,
                        overlap = s.key.Split('-').Count(part => int.TryParse(part, out int n) && component.BinarySearch(n) >= 0),
                        firstId = int.Parse(s.key.Split('-')[0])
                    }).Where(s => s.overlap > 0).OrderByDescending(s => s.overlap).ThenBy(s => s.firstId)
                        .Select(s => s.value).FirstOrDefault();
                    setting = new GroupSetting { key = key, spacing = inherited?.spacing ?? DefaultSpacing };
                }
                result.Add(new ConeGroup { Key = key, OrderedVertexIds = ordered.AsReadOnly(), IsClosed = closed, Spacing = setting.spacing });
            }
            settings = result.Select(g => new GroupSetting { key = g.Key, spacing = g.Spacing }).ToList();
            return result;
        }

        public bool SetGroupSpacing(string groupKey, float spacing, out string error)
        {
            error = null;
            if (!IsFinite(spacing) || spacing < MinimumSpacing || spacing > 10000)
            { error = "配置間隔は0.1～10000mで入力してください。"; return false; }
            if (!GetGroups().Any(g => g.Key == groupKey)) { error = "グループが存在しません。"; return false; }
            settings.Find(s => s.key == groupKey).spacing = spacing;
            return true;
        }

        /// <summary>Keep every corner, step by the requested spacing, shorten the final gap on each edge.</summary>
        public bool TryGeneratePositions(int maxCount, out List<Vector3> positions, out string error)
        {
            positions = new List<Vector3>();
            error = null;
            maxCount = Math.Min(maxCount, MaxCones);
            var candidate = new List<Vector3>();
            var unique = new HashSet<Vector3>();
            foreach (ConeGroup group in GetGroups())
            {
                var ids = group.OrderedVertexIds;
                int edgeCount = group.IsClosed ? ids.Count : ids.Count - 1;
                for (int i = 0; i < ids.Count; i++)
                {
                    Vector3 a = FindVertex(ids[i]).Position;
                    if (unique.Add(a))
                    {
                        if (candidate.Count >= maxCount) { error = "コーン数が上限（" + maxCount + "個）を超えます。間隔を広げてください。"; return false; }
                        candidate.Add(a);
                    }
                    if (i >= edgeCount) continue;
                    Vector3 b = FindVertex(ids[(i + 1) % ids.Count]).Position;
                    double length = Vector3.Distance(a, b);
                    double steps = Math.Max(0, Math.Ceiling(length / group.Spacing - 0.000001) - 1);
                    if (steps > maxCount - candidate.Count)
                    { error = "コーン数が上限（" + maxCount + "個）を超えます。間隔を広げてください。"; return false; }
                    for (int step = 1; step <= (int)steps; step++)
                    {
                        Vector3 point = Vector3.Lerp(a, b, (float)(step * (double)group.Spacing / length));
                        if (unique.Add(point)) candidate.Add(point);
                    }
                }
            }
            positions = candidate;
            return true;
        }

        public string ToJson(bool prettyPrint = true)
        {
            GetGroups();
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static bool TryFromJson(string json, out ConeLayout layout, out string error)
        {
            layout = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json) || json.Length > 2000000)
            { error = "配置JSONが空、またはサイズ上限を超えています。"; return false; }
            try
            {
                ConeLayout parsed = JsonUtility.FromJson<ConeLayout>(json);
                if (parsed == null || parsed.version != 1 || parsed.vertices == null || parsed.settings == null
                    || parsed.vertices.Count > MaxVertices || !json.Contains("\"vertices\"") || !json.Contains("\"version\""))
                    throw new ArgumentException("配置JSONの形式・バージョンが不正です。");
                var ids = new HashSet<int>();
                foreach (ConeVertex vertex in parsed.vertices)
                {
                    if (vertex == null || vertex.Id < 1 || vertex.Id == int.MaxValue || !ids.Add(vertex.Id)
                        || !ValidPosition(vertex.Position) || vertex.Links == null || vertex.Links.Count > 2
                        || vertex.Links.Distinct().Count() != vertex.Links.Count)
                        throw new ArgumentException("頂点ID・座標・隣接関係が不正です。");
                }
                foreach (ConeVertex vertex in parsed.vertices)
                    foreach (int target in vertex.Neighbors)
                        if (target == vertex.Id || !ids.Contains(target) || !parsed.FindVertex(target).Links.Contains(vertex.Id))
                            throw new ArgumentException("隣接IDが不正、または双方向になっていません。");
                var keys = new HashSet<string>();
                foreach (GroupSetting setting in parsed.settings)
                {
                    if (setting == null || string.IsNullOrEmpty(setting.key) || !keys.Add(setting.key)
                        || !IsFinite(setting.spacing) || setting.spacing < MinimumSpacing || setting.spacing > 10000)
                        throw new ArgumentException("グループの配置間隔が不正です。");
                    var parts = setting.key.Split('-');
                    int previous = 0;
                    foreach (string part in parts)
                    {
                        if (!int.TryParse(part, out int id) || id <= previous)
                            throw new ArgumentException("グループIDが不正です。");
                        previous = id;
                    }
                }
                int minimumNext = parsed.vertices.Count == 0 ? 1 : parsed.vertices.Max(v => v.Id) + 1;
                parsed.nextId = Math.Max(minimumNext, parsed.nextId);
                if (parsed.nextId < 1) throw new ArgumentException("次の頂点IDが不正です。");
                parsed.GetGroups();
                layout = parsed;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is NullReferenceException)
            { error = "読込失敗: " + exception.Message; return false; }
        }
    }
}
