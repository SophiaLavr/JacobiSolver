using System;
using System.Collections.Generic;
using JacobiApp.Models;
using JacobiApp.Solvers;

namespace JacobiApp.Utilities
{
    public sealed class PerformanceEvaluator
    {
        private readonly IJacobiSolver _singleThreadSolver;

        public PerformanceEvaluator(IJacobiSolver singleThreadSolver)
        {
            _singleThreadSolver = singleThreadSolver ?? throw new ArgumentNullException(nameof(singleThreadSolver));
        }

        public IReadOnlyList<SpeedupMeasurement> Evaluate(
            LinearSystem system,
            IReadOnlyCollection<ParallelJacobiSolver> parallelSolvers,
            double tolerance,
            int maxIterations)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (parallelSolvers == null)
            {
                throw new ArgumentNullException(nameof(parallelSolvers));
            }

            var baseline = _singleThreadSolver.Solve(system, tolerance, maxIterations);
            var measurements = new List<SpeedupMeasurement>
            {
                new(system.Size, 1, baseline, baseline, 1.0)
            };

            foreach (var solver in parallelSolvers)
            {
                var parallelResult = solver.Solve(system, tolerance, maxIterations);
                var speedup = parallelResult.Elapsed.TotalMilliseconds == 0
                    ? double.PositiveInfinity
                    : baseline.Elapsed.TotalMilliseconds / parallelResult.Elapsed.TotalMilliseconds;

                measurements.Add(new SpeedupMeasurement(system.Size, solver.ThreadCount, baseline, parallelResult, speedup));
            }

            return measurements;
        }
    }

    public sealed class SpeedupMeasurement
    {
        public SpeedupMeasurement(int size, int threadCount, JacobiResult baseline, JacobiResult measured, double speedup)
        {
            Size = size;
            ThreadCount = threadCount;
            Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
            Measured = measured ?? throw new ArgumentNullException(nameof(measured));
            Speedup = speedup;
        }

        public int Size { get; }

        public int ThreadCount { get; }

        public JacobiResult Baseline { get; }

        public JacobiResult Measured { get; }

        public double Speedup { get; }
    }
}
