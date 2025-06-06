using UnityEngine;
using EasyButtons;
using System;
using System.Linq;

class Constraint
{
    // public int[] pointIndexes = new();
}


public class SoftBody : MonoBehaviour
{
    public SoftBodySystem system;
    public int[] indexes;
    public int[] edges;
    public bool solveOnCPU = true;
    [Range(0f, 0.05f)]
    public float initialInverseStiffness = 0.01f;

    [SerializeField] bool cached = false;
    Mesh mesh;

    void ApplyTransform()
    {
        var tfm = transform;
        foreach (ref var vertex in system.positions.AsSpan())
        {
            vertex = tfm.TransformPoint(vertex);
        }

        // then reset the transform
        tfm.localScale = Vector3.one;
        tfm.rotation = Quaternion.identity;
        tfm.position = Vector3.zero;
    }

    [Button]
    void ProcessMesh()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        Utils.UniquePoints(mesh.vertices, out system.positions, out indexes);
        Utils.UniqueEdges(mesh.triangles, indexes, out edges);

        system.constraints = new DistanceConstraint[edges.Length / 2];

        for (int i = 0; i < system.constraints.Length; ++i)
        {
            var i0 = edges[i * 2 + 0];
            var i1 = edges[i * 2 + 1];

            var distanceConstraint = new DistanceConstraint();
            distanceConstraint.first = i0;
            distanceConstraint.second = i1;
            distanceConstraint.length = (system.positions[i0] - system.positions[i1]).magnitude;
            distanceConstraint.invStiffness = initialInverseStiffness;
            system.constraints[i] = distanceConstraint;
        }

        ApplyTransform();

        system.masses = new float[system.positions.Length];
        foreach (ref var mass in system.masses.AsSpan())
            mass = 1;

        mesh.MarkDynamic();
        CommitMesh();
        cached = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!mesh)
            mesh = GetComponent<MeshFilter>().mesh;
        if (!cached)
            ProcessMesh();

        system.prevPositions = system.positions.ToArray();
    }

    // Consider doing FixedUpdate instead?
    void FixedUpdate()
    {
        if (solveOnCPU)
        {
            PositionBasedDynamicsSystem.SimulateTimestep(ref system, Time.fixedDeltaTime);
            CommitMesh();
        }
    }

    void CommitMesh()
    {
        Utils.Apply(system.positions, indexes, out var newVertices);
        mesh.vertices = newVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var vertex in system.positions){
            Gizmos.DrawSphere(vertex, 0.02f);
        }

        if (edges != null) {
            var lines = edges
                .Select((val, ind) => system.positions[val])
                .ToArray();
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawLineList(lines);
        }
    }
#endif
}
