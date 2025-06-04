using UnityEngine;
using System;

class Constraint
{
    // public int[] pointIndexes = new();
}

public class SoftBody : MonoBehaviour
{
    public Vector3[] points;
    public float[] masses;
    public int[] indexes;
    public ValueTuple<int, int>[] edges;

    Mesh mesh;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        Utils.UniquePoints(mesh.vertices, out points, out indexes);
        Utils.UniqueEdges(mesh.triangles, indexes, out edges);
        masses = new float[points.Length];
        for (int i = 0; i < masses.Length; ++i)
            masses[i] = 1;

        mesh.MarkDynamic();
    }

    // Update is called once per frame
    void Update()
    {
        // logic goes here
        foreach (ref var point in points.AsSpan())
            point += new Vector3(0, 1, 0) * Time.deltaTime;

        // logic ends here
        CommitMesh();
    }

    void CommitMesh()
    {
        Utils.Apply(points, indexes, out var newVertices);
        mesh.vertices = newVertices;
        mesh.UploadMeshData(false);
    }
}
