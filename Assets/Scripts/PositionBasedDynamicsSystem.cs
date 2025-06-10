using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public interface xpbdConstraint
{
  public int[] Points { get; }
  public int Count { get; }
  public float InvStiffness { get; }
  public float Evaluate(Vector3[] positions);
  public void EvaluateGradient(Vector3[] positions, ref Vector3[] gradient);
}

[Serializable]
public struct DistanceConstraint : xpbdConstraint
{
  public int[] points;
  public readonly int[] Points { get => points; }
  public int Count { get => Points.Length; }

  public float length;

  public float invStiffness;
  public float InvStiffness { get => invStiffness; }

  public float Evaluate(Vector3[] positions)
  {
    var p1 = positions[points[0]];
    var p2 = positions[points[1]];

    var deltaX = (p1 - p2).magnitude - length;
    return deltaX;

  }

  public void EvaluateGradient(Vector3[] positions, ref Vector3[] gradient)
  {
    var p1 = positions[points[0]];
    var p2 = positions[points[1]];

    var v = p1 - p2;
    var unitV = v.normalized;

    // grad of x1
    gradient[0] = unitV;
    gradient[1] = -unitV;
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
  public float[] invMasses;
  public DistanceConstraint[] constraints;

  public int Count => positions.Length;
}


public class PositionBasedDynamicsSystem
{
  static void Swap<T>(ref T lhs, ref T rhs)
  {
    (rhs, lhs) = (lhs, rhs);
  }

  static Vector3 gravity = new Vector3(0, -9.81f, 0);
  static Vector3[] scratchGradient = new Vector3[2];
  static Vector3[] positionCache = new Vector3[2];
  static float[] scratchLagrange = new float[3];


  public static void SimulateTimestep(ref SoftBodySystem system, float dtSeconds)
  {
    var dtSecondsSq = dtSeconds * dtSeconds;

    if (positionCache.Length < system.positions.Length)
      positionCache = new Vector3[system.positions.Length];

    for (int i = 0; i < system.positions.Length; ++i)
      positionCache[i] = system.positions[i];

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

    var positions = system.positions;
    var invMasses = system.invMasses;

    const int iterations = 20;

    if (scratchLagrange.Length < system.constraints.Length)
      scratchLagrange = new float[system.constraints.Length];

    var lagrange = scratchLagrange;
    foreach (ref var lag in lagrange.AsSpan())
      lag = 0f;

    for (int i = 0; i < iterations; ++i)
    {
      for (int j = 0; j < system.constraints.Length; ++j)
      {
        var constraint = system.constraints[j];
        var points = constraint.Points;

        if (scratchGradient.Length < constraint.Count)
          scratchGradient = new Vector3[constraint.Count];

        var c = constraint.Evaluate(positions);
        constraint.EvaluateGradient(positions, ref scratchGradient);
        var dc = scratchGradient;

        var myInvMasses = points.Select((p, _) => invMasses[p]);

        var delLagrange = Equation18(c, dc, myInvMasses, lagrange[j], constraint, dtSecondsSq);
        var delX = Equation17(dc, myInvMasses, delLagrange);

        lagrange[j] += delLagrange; // update lagrange
        foreach (var _ in points.Zip(delX, (p, dx) => positions[p] += dx)) ; // update X, disgusting code to force evaluate the linq expr
      }
    }

    Swap(ref system.prevPositions, ref positionCache);
  }

  public static float Equation18(float c, Vector3[] dc, IEnumerable<float> myInvMasses, float lagrange, xpbdConstraint constraint, float dtSecondsSq)
  {
    var alphaTilde = constraint.InvStiffness / dtSecondsSq; // paragrah after Equation 4
    var denom = myInvMasses
                  .Zip(dc, (invMass, grad) => invMass * Vector3.Dot(grad, grad))
                  .Sum();

    return (-c - alphaTilde * lagrange) / (denom + alphaTilde);
  }

  public static Vector3[] Equation17(Vector3[] dc, IEnumerable<float> myInvMasses, float delLagrange)
  {
    return myInvMasses.Zip(dc, (invMass, grad) => invMass * grad * delLagrange).ToArray();
  }
}