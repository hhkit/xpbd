using System;
using UnityEngine;

/// <summary>
/// A struct of array representation of a set of point masses
/// TODO: Define constraints struct?
/// </summary>
[Serializable]
public struct SoftBodySystem
{
  public Vector3[] points;
  public float[] masses;
}

public class PositionBasedDynamicsSystem
{
  public static void Solve(ref SoftBodySystem system, float dtSeconds)
  {
    // do solving here
    foreach (ref var point in system.points.AsSpan())
      point += new Vector3(0, 1, 0) * dtSeconds;
  }
}