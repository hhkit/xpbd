using UnityEngine;
using System;

class Constraint
{
    // public int[] pointIndexes = new();
}


public class SoftBody : MonoBehaviour
{
    public SoftBodySystem system;
    public int[] indexes;
    public ValueTuple<int, int>[] edges;
    public bool solveOnCPU = true;

    Mesh mesh;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        Utils.UniquePoints(mesh.vertices, out system.points, out indexes);
        Utils.UniqueEdges(mesh.triangles, indexes, out edges);
        system.masses = new float[system.points.Length];
        foreach (ref var mass in system.masses.AsSpan())
            mass = 1;

        mesh.MarkDynamic();
    }

    // Consider doing FixedUpdate instead?
    void Update()
    {
        if (solveOnCPU)
        {
            PositionBasedDynamicsSystem.Solve(ref system, Time.deltaTime);
            CommitMesh();
        }
    }

    void CommitMesh()
    {
        Utils.Apply(system.points, indexes, out var newVertices);
        mesh.vertices = newVertices;
        mesh.UploadMeshData(false);
    }
}
