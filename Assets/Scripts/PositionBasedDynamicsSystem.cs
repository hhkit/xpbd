using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using MathNet;
using MathNet.Numerics.LinearAlgebra;


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
    var startPos = system.positions.ToArray();

    // simulate gravity
    for (int i = 0; i < system.Count; ++i)
    {
      var vel = (system.positions[i] - system.prevPositions[i]) / dtSeconds;
      system.positions[i] += vel * dtSeconds + (gravity * 0.5f * dtSeconds * dtSeconds);
    }

    var masses = Vector<float>.Build.Dense(system.masses.Select((v, _) => new float[3] { v, v, v }).SelectMany(i => i).ToArray());
    var points = Vector<float>.Build.Dense(MemoryMarshal.Cast<Vector3, float>(system.positions.AsSpan()).ToArray());

    var ma = masses.PointwiseMultiply(points);
    Debug.Log($"mass * points: {ma}");

    system.prevPositions = startPos;
  }
}