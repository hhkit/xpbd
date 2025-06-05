using System;
using UnityEngine;

/// <summary>
/// A struct of array representation of a set of point masses
/// TODO: Define constraints struct?
/// </summary>
[Serializable]
public struct SoftBodySystem
{
  public Vector3[] positions;
  public Vector3[] prevPositions;
  public float[] masses;
}

public class PositionBasedDynamicsSystem
{
  public static void SimulateTimestep(ref SoftBodySystem system, float dtSeconds)
  {
    // simulate gravity
    foreach (ref var point in system.positions.AsSpan())
      point += new Vector3(0, -9.81f, 0) * dtSeconds;


  }
}