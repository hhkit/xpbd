using System;
using System.Linq;
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

  public int Count => positions.Length;
}

public class PositionBasedDynamicsSystem
{
  static Vector3 gravity = new Vector3(0, -9.81f, 0);

  public static void SimulateTimestep(ref SoftBodySystem system, float dtSeconds)
  {
    // simulate gravity
    for (int i = 0; i < system.Count; ++i)
    {
      var vel = (system.positions[i] - system.prevPositions[i]) / dtSeconds;
      system.positions[i] += vel * dtSeconds + (gravity * 0.5f * dtSeconds * dtSeconds);
    }

    for (int i = 0; i < system.Count; ++i)
      system.prevPositions[i] = system.positions[i];
  }
}