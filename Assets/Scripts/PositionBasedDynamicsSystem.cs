using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using MathNet;
using MathNet.Numerics.LinearAlgebra;


public interface xpbdConstraint
{
  public float InvStiffness { get; }
  public float Evaluate(Vector<float> positions);
  public Vector<float> EvaluateGradient(Vector<float> positions);
}

[Serializable]
public struct DistanceConstraint : xpbdConstraint
{
  public int first, second; // public int indexes[2];
  public float length;

  public float invStiffness;
  public float InvStiffness { get => invStiffness; }

  public float Evaluate(Vector<float> positions)
  {
    var p1 = new Vector3(positions[first * 3], positions[first * 3 + 1], positions[first * 3 + 2]);
    var p2 = new Vector3(positions[second * 3], positions[second * 3 + 1], positions[second * 3 + 2]);

    var deltaX = (p1 - p2).magnitude - length;
    return deltaX;

  }

  public Vector<float> EvaluateGradient(Vector<float> positions)
  {
    var Vf = Vector<float>.Build;

    var grad = Vf.Sparse(positions.Count, 0f);

    var p1 = new Vector3(positions[first * 3], positions[first * 3 + 1], positions[first * 3 + 2]);
    var p2 = new Vector3(positions[second * 3], positions[second * 3 + 1], positions[second * 3 + 2]);

    var v = p1 - p2;
    var invVLength = 1f / v.magnitude;
    var unitV = v * invVLength;

    // grad of x1
    grad[first * 3] = unitV.x;
    grad[first * 3 + 1] = unitV.y;
    grad[first * 3 + 2] = unitV.z;

    grad[second * 3] = -unitV.x;
    grad[second * 3 + 1] = -unitV.y;
    grad[second * 3 + 2] = -unitV.z;
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
  public DistanceConstraint[] constraints;

  public int Count => positions.Length;
}


public class PositionBasedDynamicsSystem
{
  static Vector3 gravity = new Vector3(0, -9.81f, 0);

  public static void SimulateTimestep(ref SoftBodySystem system, float dtSeconds)
  {
    var Vf = Vector<float>.Build;
    var Mf = Matrix<float>.Build;

    var dtSecondsSq = dtSeconds * dtSeconds;
    var startPos = system.positions.ToArray();

    // simulate gravity
    for (int i = 0; i < system.Count; ++i)
    {
      var position = system.positions[i];
      var prevPosition = system.prevPositions[i];

      var vel = (position - prevPosition) / dtSeconds;
      position += vel * dtSeconds + (gravity * 0.5f * dtSeconds * dtSeconds);

      // don't drop below 0
      if (position.y < 0)
        position.y = 0;

      system.positions[i] = position;
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

        var delLagrange = Equation18(c, dc, invMasses, lagrange[j], constraint, dtSecondsSq);
        var delX = Equation17(dc, invMasses, delLagrange, constraint);
        lagrange[j] += delLagrange;
        positions += delX;
      }
    }

    system.positions = positions.AsArray().Reinterpret<float, Vector3>().ToArray();
    system.prevPositions = startPos;
  }

  public static float Equation18(float c, Vector<float> dc, Vector<float> invMasses, float lagrange, xpbdConstraint constraint, float dtSecondsSq)
  {
    var alphaTilde = constraint.InvStiffness / dtSecondsSq; // paragrah after Equation 4
    return (-c - alphaTilde * lagrange) / (dc.DotProduct(invMasses.PointwiseMultiply(dc)) + alphaTilde);
  }

  public static Vector<float> Equation17(Vector<float> dc, Vector<float> invMasses, float delLagrange, xpbdConstraint constraint)
  {
    return invMasses.PointwiseMultiply(dc) * delLagrange;
  }
}