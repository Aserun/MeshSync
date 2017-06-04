using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class NormalEditor : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] float m_vertexSize = 0.005f;
    [SerializeField] float m_normalSize = 0.15f;
    [SerializeField] Color m_vertexColor = Color.blue;
    [SerializeField] Color m_normalColor = Color.yellow;
    [SerializeField] Color m_tangentColor = Color.cyan;

    [SerializeField] Mesh m_meshTarget;
    [SerializeField] Mesh m_meshCube;
    [SerializeField] Mesh m_meshLine;
    [SerializeField] Material m_matVertices;
    [SerializeField] Material m_matNormals;
    [SerializeField] List<Vector3> m_points = new List<Vector3>();
    [SerializeField] List<Vector3> m_normals = new List<Vector3>();
    [SerializeField] List<Vector4> m_tangents = new List<Vector4>();

    ComputeBuffer m_cbArg;
    ComputeBuffer m_cbPoints;
    ComputeBuffer m_cbNormals;
    ComputeBuffer m_cbTangents;
    CommandBuffer m_cmdDraw;


    public Mesh mesh { get { return m_meshTarget; } }


    void SetupResources()
    {
        if (m_meshCube == null)
        {
            float l = 0.5f;
            var p = new Vector3[] {
                new Vector3(-l,-l, l),
                new Vector3( l,-l, l),
                new Vector3( l,-l,-l),
                new Vector3(-l,-l,-l),

                new Vector3(-l, l, l),
                new Vector3( l, l, l),
                new Vector3( l, l,-l),
                new Vector3(-l, l,-l),
            };

            m_meshCube = new Mesh();
            m_meshCube.vertices = new Vector3[] {
                p[0], p[1], p[2], p[3],
                p[7], p[4], p[0], p[3],
                p[4], p[5], p[1], p[0],
                p[6], p[7], p[3], p[2],
                p[5], p[6], p[2], p[1],
                p[7], p[6], p[5], p[4],
            };
            m_meshCube.SetIndices(new int[] {
                3, 1, 0, 3, 2, 1,
                7, 5, 4, 7, 6, 5,
                11, 9, 8, 11, 10, 9,
                15, 13, 12, 15, 14, 13,
                19, 17, 16, 19, 18, 17,
                23, 21, 20, 23, 22, 21,
            }, MeshTopology.Triangles, 0);
        }

        if (m_meshLine == null)
        {
            m_meshLine = new Mesh();
            m_meshLine.vertices = new Vector3[2] { Vector3.zero, Vector3.zero };
            m_meshLine.uv = new Vector2[2] { Vector2.zero, Vector2.one };
            m_meshLine.SetIndices(new int[2] { 0, 1 }, MeshTopology.Lines, 0);
        }

        if (m_matVertices == null)
        {
            m_matVertices = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/UTJ/NormalEditor/Shaders/VertexVisualizer.shader"));
        }
        if (m_matNormals == null)
        {
            m_matNormals = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/UTJ/NormalEditor/Shaders/NormalVisualizer.shader"));
        }


        if (m_meshTarget == null ||
            m_meshTarget.vertexCount != GetComponent<MeshFilter>().sharedMesh.vertexCount)
        {
            m_meshTarget = GetComponent<MeshFilter>().sharedMesh;
            m_points = new List<Vector3>(m_meshTarget.vertices);
            m_normals = new List<Vector3>(m_meshTarget.normals);
            m_tangents = new List<Vector4>(m_meshTarget.tangents);
            ReleaseComputeBuffers();
        }

        if (m_cbPoints == null && m_points.Count > 0)
        {
            m_cbPoints = new ComputeBuffer(m_points.Count, 12);
            m_cbPoints.SetData(m_points.ToArray());
        }
        if (m_cbNormals == null && m_normals.Count > 0)
        {
            m_cbNormals = new ComputeBuffer(m_normals.Count, 12);
            m_cbNormals.SetData(m_normals.ToArray());
        }
        if (m_cbTangents == null && m_tangents.Count > 0)
        {
            m_cbTangents = new ComputeBuffer(m_tangents.Count, 16);
            m_cbTangents.SetData(m_tangents.ToArray());
        }

        if (m_cbArg == null && m_points.Count > 0)
        {
            m_cbArg = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            m_cbArg.SetData(new uint[5] { m_meshCube.GetIndexCount(0), (uint)m_points.Count, 0, 0, 0 });
        }

        if (m_cmdDraw == null)
        {
            m_cmdDraw = new CommandBuffer();
            m_cmdDraw.name = "NormalEditor";
            m_cmdDraw.Clear();
            if (m_points.Count > 0)
                m_cmdDraw.DrawMeshInstancedIndirect(m_meshCube, 0, m_matVertices, 0, m_cbArg);
            if (m_normals.Count > 0)
                m_cmdDraw.DrawMeshInstancedIndirect(m_meshLine, 0, m_matNormals, 0, m_cbArg);
            if (m_tangents.Count > 0)
                m_cmdDraw.DrawMeshInstancedIndirect(m_meshLine, 0, m_matNormals, 1, m_cbArg);
        }
    }

    void ReleaseComputeBuffers()
    {
        if (m_cbArg != null) { m_cbArg.Release(); m_cbArg = null; }
        if (m_cbPoints != null) { m_cbPoints.Release(); m_cbPoints = null; }
        if (m_cbNormals != null) { m_cbNormals.Release(); m_cbNormals = null; }
        if (m_cbTangents != null) { m_cbTangents.Release(); m_cbTangents = null; }
        if (m_cmdDraw != null) { m_cmdDraw.Release(); m_cmdDraw = null; }
    }


    void OnEnable()
    {
        SetupResources();
    }

    void OnDisable()
    {
        ReleaseComputeBuffers();
    }

    void Reset()
    {
        SetupResources();
    }

    void LateUpdate()
    {
        var trans = GetComponent<Transform>();
        var bounds = new Bounds(trans.position, Vector3.one * 100.0f);

        m_matVertices.SetMatrix("_Transform", trans.localToWorldMatrix);
        m_matVertices.SetFloat("_Size", m_vertexSize);
        m_matVertices.SetColor("_Color", m_vertexColor);
        if (m_cbPoints != null) m_matVertices.SetBuffer("_Points", m_cbPoints);

        m_matNormals.SetMatrix("_Transform", trans.localToWorldMatrix);
        m_matNormals.SetFloat("_Size", m_normalSize);
        m_matNormals.SetColor("_NormalColor", m_normalColor);
        m_matNormals.SetColor("_TangentColor", m_tangentColor);
        if (m_cbPoints != null) m_matNormals.SetBuffer("_Points", m_cbPoints);
        if (m_cbNormals != null) m_matNormals.SetBuffer("_Normals", m_cbNormals);
        if (m_cbTangents != null) m_matNormals.SetBuffer("_Tangents", m_cbTangents);

        //Graphics.DrawMeshInstancedIndirect(m_meshCube, 0, m_matVertices, bounds, m_cbArg);

    }

    private void OnDrawGizmos()
    {
        Graphics.ExecuteCommandBuffer(m_cmdDraw);
    }


    public void ApplyNewNormals()
    {
        m_meshTarget.SetNormals(m_normals);
    }
#endif
}