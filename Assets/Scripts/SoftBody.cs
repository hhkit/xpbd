using UnityEngine;
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
    public ValueTuple<int, int>[] edges;
    public bool solveOnCPU = true;

    Mesh mesh;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        Utils.UniquePoints(mesh.vertices, out system.positions, out indexes);
        Utils.UniqueEdges(mesh.triangles, indexes, out edges);
        system.prevPositions = system.positions.ToArray();
        system.masses = new float[system.positions.Length];
        foreach (ref var mass in system.masses.AsSpan())
            mass = 1;

        mesh.MarkDynamic();
    }

    // Consider doing FixedUpdate instead?
    void Update()
    {
        if (solveOnCPU)
        {
            PositionBasedDynamicsSystem.SimulateTimestep(ref system, Time.deltaTime);
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
}
