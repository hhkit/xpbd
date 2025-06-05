using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using MathNet;
using MathNet.Numerics.LinearAlgebra;


public interface xpbdConstraint
{
  public float Stiffness { get; }
  public float Evaluate(Vector<float> positions);
  public Vector<float> EvaluateGradient(Vector<float> positions);
}

public struct DistanceConstraint : xpbdConstraint
{
  public int first, second; // public int indexes[2];
  public float lengthSq;

  public float Stiffness { get; set; }

  public float Evaluate(Vector<float> positions)
  {
    var p1 = new Vector3(positions[first * 3], positions[first * 3 + 1], positions[first * 3 + 2]);
    var p2 = new Vector3(positions[second * 3], positions[second * 3 + 1], positions[second * 3 + 2]);

    return (p1 - p2).sqrMagnitude - lengthSq; // okay this is definitely wrong

  }

  public Vector<float> EvaluateGradient(Vector<float> positions)
  {
    var Vf = Vector<float>.Build;

    var grad = Vf.Sparse(positions.Count);

    var x1 = positions[first * 3];
    var y1 = positions[first * 3 + 1];
    var z1 = positions[first * 3 + 2];

    var x2 = positions[second * 3];
    var y2 = positions[second * 3 + 1];
    var z2 = positions[second * 3 + 2];

    // grad of x1
    grad[first * 3] = 2 * (x2 - x1);
    grad[first * 3 + 1] = 2 * (y2 - y1);
    grad[first * 3 + 2] = 2 * (z2 - z1);

    grad[second * 3] = 2 * (x1 - x2);
    grad[second * 3 + 1] = 2 * (y1 - y2);
    grad[second * 3 + 2] = 2 * (z1 - z2);
    return grad;
  }
}

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
  public xpbdConstraint[] constraints;

  public int Count => positions.Length;
}


public class PositionBasedDynamicsSystem
{
  static Vector3 gravity = new Vector3(0, -9.81f, 0);

  public static void SimulateTimestep(ref SoftBodySystem system, float dtSeconds)
  {
    var Vf = Vector<float>.Build;
    var Mf = Matrix<float>.Build;

    var startPos = system.positions.ToArray();

    // simulate gravity
    for (int i = 0; i < system.Count; ++i)
    {
      var vel = (system.positions[i] - system.prevPositions[i]) / dtSeconds;
      system.positions[i] += vel * dtSeconds + (gravity * 0.5f * dtSeconds * dtSeconds);
    }

    var invMasses = Vf.Dense(system.masses.Select((v, i) => 1f / v).Duplicate(3).ToArray());
    var positions = Vf.Dense(system.positions.Reinterpret<Vector3, float>().ToArray());

    const int iterations = 20;
    var lagrange = Vf.Sparse(system.constraints.Length); // all zeroes... hopefully
    for (int i = 0; i < iterations; ++i)
    {
      for (int j = 0; j < system.constraints.Length; ++j)
      {
        var constraint = system.constraints[j];

        var c = constraint.Evaluate(positions);
        var dc = constraint.EvaluateGradient(positions);

        var delLagrange = Equation18(c, dc, invMasses, lagrange[j], constraint);
        var delX = Equation17(dc, invMasses, delLagrange, constraint);
        lagrange[j] += delLagrange;
        positions += delX;
      }
    }

    system.positions = positions.AsArray().Reinterpret<float, Vector3>().ToArray();
    system.prevPositions = startPos;
  }

  public static float Equation18(float c, Vector<float> dc, Vector<float> invMasses, float lagrange, xpbdConstraint constraint)
  {
    return (-c - constraint.Stiffness * lagrange) / (dc.DotProduct(invMasses.PointwiseMultiply(dc)) + constraint.Stiffness);
  }

  public static Vector<float> Equation17(Vector<float> dc, Vector<float> invMasses, float delLagrange, xpbdConstraint constraint)
  {
    return invMasses.PointwiseMultiply(dc) * delLagrange;
  }
}