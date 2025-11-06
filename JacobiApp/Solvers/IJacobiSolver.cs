using JacobiApp.Models;

namespace JacobiApp.Solvers
{
    public interface IJacobiSolver
    {
        JacobiResult Solve(LinearSystem system, double tolerance, int maxIterations, double[]? initialGuess = null);
    }
}
