using System;
using System.Diagnostics;
using JacobiApp.Models;

namespace JacobiApp.Solvers
{
    public sealed class JacobiSolver : IJacobiSolver
    {
        public JacobiResult Solve(LinearSystem system, double tolerance, int maxIterations, double[]? initialGuess = null)
        {
            ArgumentNullException.ThrowIfNull(system);

            if (system.HasZeroOnDiagonal())
            {
                throw new InvalidOperationException("Метод Якобі вимагає ненульові елементи на головній діагоналі.");
            }

            var n = system.Size;
            var current = initialGuess != null ? (double[])initialGuess.Clone() : new double[n];
            var next = new double[n];
            var stopwatch = Stopwatch.StartNew();

            var iteration = 0;
            var maxDelta = double.MaxValue;

            while (iteration < maxIterations && maxDelta > tolerance)
            {
                maxDelta = 0;

                for (var i = 0; i < n; i++)
                {
                    double sum = 0;
                    var aii = system.Coefficients[i, i];

                    for (var j = 0; j < n; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        sum += system.Coefficients[i, j] * current[j];
                    }

                    var value = (system.Constants[i] - sum) / aii;
                    next[i] = value;

                    var delta = Math.Abs(value - current[i]);
                    if (delta > maxDelta)
                    {
                        maxDelta = delta;
                    }
                }

                iteration++;
                Array.Copy(next, current, n);
            }

            stopwatch.Stop();
            var residual = system.ComputeResidualNorm(current);
            var converged = maxDelta <= tolerance;

            return new JacobiResult((double[])current.Clone(), iteration, residual, stopwatch.Elapsed, converged);
        }
    }
}
