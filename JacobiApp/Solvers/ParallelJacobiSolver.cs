using System;
using System.Diagnostics;
using System.Threading;
using JacobiApp.Models;

namespace JacobiApp.Solvers
{
    public sealed class ParallelJacobiSolver : IJacobiSolver
    {
        private readonly int _threadCount;

        public ParallelJacobiSolver(int threadCount)
        {
            if (threadCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadCount), "Кількість потоків має бути додатною.");
            }

            _threadCount = threadCount;
        }

        public int ThreadCount => _threadCount;

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

            var threadsToUse = Math.Min(_threadCount, n);

            var stopwatch = Stopwatch.StartNew();
            var threadDiffs = new double[threadsToUse];
            var threads = new Thread[threadsToUse];
            var shouldStop = 0;
            var iteration = 0;
            var maxDelta = double.MaxValue;

            var barrier = new Barrier(threadsToUse, _ =>
            {
                maxDelta = 0;
                for (var i = 0; i < threadDiffs.Length; i++)
                {
                    if (threadDiffs[i] > maxDelta)
                    {
                        maxDelta = threadDiffs[i];
                    }
                }

                Array.Copy(next, current, n);
                iteration++;

                if (iteration >= maxIterations || maxDelta <= tolerance)
                {
                    Volatile.Write(ref shouldStop, 1);
                }
            });

            var rowsPerThread = n / threadsToUse;
            var remainder = n % threadsToUse;

            for (var threadIndex = 0; threadIndex < threadsToUse; threadIndex++)
            {
                var localIndex = threadIndex;
                var startRow = localIndex * rowsPerThread + Math.Min(localIndex, remainder);
                var rowCount = rowsPerThread + (localIndex < remainder ? 1 : 0);
                var endRow = startRow + rowCount;

                threads[localIndex] = new Thread(() =>
                {
                    while (true)
                    {
                        if (Volatile.Read(ref shouldStop) == 1)
                        {
                            break;
                        }

                        var localMaxDelta = 0d;

                        for (var i = startRow; i < endRow; i++)
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
                            if (delta > localMaxDelta)
                            {
                                localMaxDelta = delta;
                            }
                        }

                        threadDiffs[localIndex] = localMaxDelta;

                        barrier.SignalAndWait();
                    }
                })
                {
                    IsBackground = false,
                    Name = $"ПотікЯкобі-{localIndex + 1}"
                };
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            barrier.Dispose();
            stopwatch.Stop();

            var residual = system.ComputeResidualNorm(current);
            var converged = maxDelta <= tolerance;

            return new JacobiResult((double[])current.Clone(), iteration, residual, stopwatch.Elapsed, converged);
        }
    }
}
