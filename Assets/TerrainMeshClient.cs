using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Shape;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.TmsMsgDb;

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshCollider))]
public class TerrainMeshClient : MonoBehaviour
{
    ROSConnection ros;

    public Material material;
    public string serviceName = "/tms_ur_construction_terrain_mesh/output/terrain/mesh_srv";
    // public float scale = 1;
    // public float addPoseY = 0f;
    public float requestInterval = 5.0f;

    // Mesh
    private static ColoredMeshMsg meshMsg;
    private MeshTriangleMsg[] meshTriangleMsgs;
    private PointMsg[] pointMsgs;
    private ColorRGBAMsg[] colorRGBAMsgs;
    private Vector3Msg[] normalVector3Msgs;
    // private Mesh mesh;

    private Vector3[] vertices;
    private Color[] vertexColors;
    private int[] triangles;
    private Vector3[] vertexNormals;

    private float awaitingResponseUntilTimestamp;
    static bool meshReceived = false;

    // Start is called before the first frame update
    void Start()
    {
        // Send Colored Mesh Service only one time
        if (meshMsg != null) return;

        awaitingResponseUntilTimestamp = -requestInterval;

        meshMsg = new ColoredMeshMsg();
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterRosService<ColoredMeshSrvRequest, ColoredMeshSrvResponse>(serviceName);
    }

    // Update is called once per frame
    void Update()
    {
        // if (!OVRPlayerController.MeshCreated && Time.time > awaitingResponseUntilTimestamp && TerrainMeshClientInversed.MeshCreated)
      //  if (OVRPlayerController.MeshCreated) return;

        if (meshReceived)
        {
            ColoredMeshSrvResponse response = new ColoredMeshSrvResponse();
            response.colored_mesh = meshMsg;
            CallbackFunc(response);
            return;
        }

        if (Time.time > awaitingResponseUntilTimestamp)
        {
            ColoredMeshSrvRequest coloredMeshSrvRequest = new ColoredMeshSrvRequest();
            ros.SendServiceMessage<ColoredMeshSrvResponse>(serviceName, coloredMeshSrvRequest, CallbackFunc);
            awaitingResponseUntilTimestamp = Time.time + requestInterval;
            Debug.Log("Service Requested");
        }
    }

    void CallbackFunc(ColoredMeshSrvResponse response)
    {
        // if (OVRPlayerController.MeshCreated) {
        //     // if PointCloud2Msg has already received, do nothing
        //     // Debug.Log(received);
        //     return;
        // }
        meshReceived = true;

        meshMsg = response.colored_mesh;

        Mesh mesh = new Mesh();

        // Increse displayable point num
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        // Set Vertices and Triangles
        mesh = SetMesh(mesh);
        // mesh.RecalculateNormals();

        // // Create Inverted Mesh
        // Mesh InvertedMesh = new Mesh();
        // Mesh invertedMesh = CreateInvertedMesh(mesh);

        // // Combine Meshes
        // mesh = CombineMeshes(mesh, invertedMesh);

        // Mesh Collider
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh   = mesh;

        // Mesh Filter
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh       = mesh;

        // Mesh Renderer
        MeshRenderer meshRenderer   = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        // Transform transform = GetComponent<Transform>();
        // transform.localScale = new Vector3(scale, scale, scale);

      //  OVRPlayerController.MeshCreated = true;
    }

    // Get Triangles
    private void GetTriangles()
    {
        triangles = new int[meshTriangleMsgs.Length * 3];

        int i = 0;
        foreach (MeshTriangleMsg meshTriangleMsg in meshTriangleMsgs)
        {
            // foreach (uint vertex_index in meshTriangleMsg.vertex_indices)
            // {
            //     triangles[i] = (int)vertex_index;
            //     i++;
            // }
            triangles[i] = (int)meshTriangleMsg.vertex_indices[0];
            triangles[i+1] = (int)meshTriangleMsg.vertex_indices[1];
            triangles[i+2] = (int)meshTriangleMsg.vertex_indices[2];
            i = i + 3;
        }
    }

    // Get Vertices
    private void GetVertices()
    {
        vertices = new Vector3[pointMsgs.Length];
        for (int i = 0; i < pointMsgs.Length; i++)
        {
            float x = (float)pointMsgs[i].y * (-1.0f);
            // float y = (float)pointMsgs[i].z + addPoseY;
            float y = (float)pointMsgs[i].z;
            float z = (float)pointMsgs[i].x;
            vertices[i] = new Vector3(x, y, z);
        }

        // return vertices;
    }

    // Get Vertex Colors
    private void GetVertexColors()
    {
        vertexColors = new Color[colorRGBAMsgs.Length];
        for (int i = 0; i < colorRGBAMsgs.Length; i++)
        {
            vertexColors[i] = new Color(colorRGBAMsgs[i].r, colorRGBAMsgs[i].g, colorRGBAMsgs[i].b);
        }
    }

    private void GetNormals()
    {
        vertexNormals = new Vector3[normalVector3Msgs.Length];
        for (int i = 0; i < normalVector3Msgs.Length; i++)
        {
            vertexNormals[i] = new Vector3((float)normalVector3Msgs[i].x, (float)normalVector3Msgs[i].y, (float)normalVector3Msgs[i].z);
        }
    }

    // Set Vertices and Triangles
    private Mesh SetMesh(Mesh mesh)
    {
        pointMsgs        = meshMsg.vertices;
        if (ColoredGroundMeshSubscriber.coloredMeshMsg == null) {
            colorRGBAMsgs = meshMsg.vertex_colors;
        } else {
            colorRGBAMsgs = ColoredGroundMeshSubscriber.coloredMeshMsg.vertex_colors;
        }
        meshTriangleMsgs = meshMsg.triangles;
        normalVector3Msgs = meshMsg.vertex_normals;

        GetVertices();
        GetVertexColors();
        GetTriangles();
        GetNormals();

        mesh.SetVertices(vertices);
        mesh.SetColors(vertexColors);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(vertexNormals);

        return mesh;
    }

    // private Mesh CreateInvertedMesh(Mesh mesh)
    // {
    //     Vector3[] invertedNormals = new Vector3[mesh.normals.Length];
    //     for (int i = 0; i < mesh.normals.Length; i++)
    //     {
    //         invertedNormals[i] = -mesh.normals[i];
    //     }

    //     Vector4[] invertedTangents = new Vector4[mesh.tangents.Length];
    //     for (int i = 0; i < mesh.tangents.Length; i ++)
    //     {
    //         invertedTangents[i] = mesh.tangents[i];
    //         invertedTangents[i].w = -invertedTangents[i].w;
    //     }

    //     Mesh invertedMesh = new Mesh();
    //     invertedMesh.SetVertices(mesh.vertices);
    //     invertedMesh.SetColors(mesh.colors);
    //     invertedMesh.SetTriangles(mesh.triangles.Reverse().ToArray(), 0);
    //     invertedMesh.SetNormals(invertedNormals);
    //     invertedMesh.SetTangents(invertedTangents);

    //     return invertedMesh;
    // }

    // private Mesh CombineMeshes(Mesh mesh, Mesh invertedMesh)
    // {
    //     CombineInstance[] combineInstancies = new CombineInstance[2]
    //     {
    //         new CombineInstance(){mesh = invertedMesh, transform = Matrix4x4.identity},
    //         new CombineInstance(){mesh = mesh, transform = Matrix4x4.identity}
    //     };
    //     // if (_combineOrder == CombineOrder.OriginalThenInverted)
    //     // {
    //     //     combineInstancies = combineInstancies.Reverse().ToArray();
    //     // }
    //     Mesh combinedMesh = new Mesh();
    //     combinedMesh.CombineMeshes(combineInstancies);
    //     return combinedMesh;
    // }
}
