using System;

namespace JacobiApp.Models
{
    public sealed class JacobiResult
    {
        public JacobiResult(double[] solution, int iterations, double residualNorm, TimeSpan elapsed, bool converged)
        {
            Solution = solution ?? throw new ArgumentNullException(nameof(solution));
            Iterations = iterations;
            ResidualNorm = residualNorm;
            Elapsed = elapsed;
            Converged = converged;
        }

        public double[] Solution { get; }

        public int Iterations { get; }

        public double ResidualNorm { get; }

        public TimeSpan Elapsed { get; }

        public bool Converged { get; }
    }
}
