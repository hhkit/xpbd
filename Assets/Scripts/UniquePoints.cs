using UnityEngine;
using System;
using System.Collections.Generic;

public partial class Utils
{
  public static void UniquePoints(Vector3[] points, out Vector3[] uniquePoints, out int[] uniqueIndexes)
  {
    Dictionary<Vector3, int> lut = new();
    uniqueIndexes = new int[points.Length];

    int i = 0;
    foreach (var point in points)
    {
      if (!lut.ContainsKey(point))
      {
        lut.Add(point, lut.Count);
      }

      uniqueIndexes[i++] = lut[point];
    }

    uniquePoints = new Vector3[lut.Count];
    foreach (var (point, ind) in lut)
    {
      uniquePoints[ind] = point;
    }
  }

  public static void Apply(Vector3[] uniquePoints, int[] uniqueIndexes, out Vector3[] restoredPoints)
  {
    restoredPoints = new Vector3[uniqueIndexes.Length];

    int i = 0;
    foreach (var ind in uniqueIndexes)
    {
      restoredPoints[i++] = uniquePoints[ind];
    }
  }

  private static long HashEdge(int ind0, int ind1)
  {
    long long0 = ind0;
    long long1 = ind1;
    return ind0 < ind1 ? (long0 << 32 | long1) : (long1 << 32 | long0);
  }

  public static void UniqueEdges(int[] triangles, int[] uniqueIndexes, out int[] uniqueEdges)
  {
    HashSet<long> edgeLut = new();

    for (int i = 0; i < triangles.Length; i += 3)
    {
      var p0 = uniqueIndexes[triangles[i + 0]];
      var p1 = uniqueIndexes[triangles[i + 1]];
      var p2 = uniqueIndexes[triangles[i + 2]];

      var edges = new long[] { HashEdge(p0, p1), HashEdge(p0, p2), HashEdge(p1, p2) };

      foreach (var edge in edges)
      {
        if (!edgeLut.Contains(edge))
          edgeLut.Add(edge);
      }
    }

    uniqueEdges = new int[edgeLut.Count * 2];
    {
      var i = 0;
      foreach (var edge in edgeLut)
      {
        var ind0 = (int)(edge & 0xFFFFFFFF);
        var ind1 = (int)((edge >> 32) & 0xFFFFFFFF);

        uniqueEdges[i++] = ind0;
        uniqueEdges[i++] = ind1;
      }
    }
  }
}