using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JacobiApp.Solvers;
using JacobiApp.Utilities;

namespace JacobiApp;

internal static class Program
{
	private static void Main(string[] args)
	{
		var options = RunnerOptions.Parse(args);

		Console.WriteLine($"Точнiсть: {options.Tolerance}, Максимальна кiлькiсть iтерацiй: {options.MaxIterations}");
		Console.WriteLine($"Розмiри матрицi: {string.Join(", ", options.Sizes)}");
		Console.WriteLine($"Кiлькiсть потокiв: {string.Join(", ", options.ThreadCounts)}");
		if (options.Seed.HasValue)
		{
			Console.WriteLine($"Random seed: {options.Seed}");
		}
		Console.WriteLine();

		var singleThreadSolver = new JacobiSolver();
		var evaluator = new PerformanceEvaluator(singleThreadSolver);

		foreach (var size in options.Sizes)
		{
			int? seed = options.Seed.HasValue ? options.Seed.Value + size : null;
			var system = DiagonalDominantMatrixGenerator.Create(size, seed: seed);

			if (!system.IsDiagonallyDominant())
			{
				Console.WriteLine($"Згенерована система розмiру {size} не є дiагонально домiнантною. Пропуск.");
				continue;
			}

			var parallelSolvers = options.ThreadCounts
				.Where(t => t > 1)
				.Select(t => new ParallelJacobiSolver(t))
				.ToList();

			var measurements = evaluator.Evaluate(system, parallelSolvers, options.Tolerance, options.MaxIterations);

			Console.WriteLine($"Розмiр матрицi: {size}");
			Console.WriteLine("Потоки  | Час (мс) |  Iтр | Залишок | Прискорення");

			foreach (var entry in measurements.OrderBy(m => m.ThreadCount))
			{
				var time = entry.Measured.Elapsed.TotalMilliseconds;
				Console.WriteLine(
					$"{entry.ThreadCount,7} | {time,9:F2} | {entry.Measured.Iterations,4} | {entry.Measured.ResidualNorm,8:E2} | {entry.Speedup,7:F2}");
			}

			Console.WriteLine();
		}

	}

	private sealed class RunnerOptions
	{
	private RunnerOptions(
	    IReadOnlyList<int> sizes,
	    IReadOnlyList<int> threadCounts,
	    double tolerance,
	    int maxIterations,
	    int? seed)
		{
			Sizes = sizes;
			ThreadCounts = threadCounts;
			Tolerance = tolerance;
			MaxIterations = maxIterations;
	    Seed = seed;
		}

		public IReadOnlyList<int> Sizes { get; }

		public IReadOnlyList<int> ThreadCounts { get; }

		public double Tolerance { get; }

		public int MaxIterations { get; }

	public int? Seed { get; }

		public static RunnerOptions Parse(string[] args)
		{
			var sizes = new List<int> { 200, 500, 1000 };
			var threadCounts = new List<int> { 1, Environment.ProcessorCount };
			var tolerance = 1e-8;
			var maxIterations = 10_000;
	    int? seed = null;

			foreach (var arg in args)
			{
				if (!arg.StartsWith("--", StringComparison.Ordinal))
				{
					continue;
				}

				var parts = arg[2..].Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2)
				{
					continue;
				}

				var key = parts[0].Trim().ToLowerInvariant();
				var value = parts[1].Trim();

				switch (key)
				{
					case "sizes":
						sizes = ParseIntList(value, "sizes");
						break;
					case "threads":
						threadCounts = ParseIntList(value, "threads");
						break;
					case "tolerance":
						tolerance = double.Parse(value, CultureInfo.InvariantCulture);
						break;
					case "maxiterations":
						maxIterations = int.Parse(value, CultureInfo.InvariantCulture);
						break;
					case "seed":
						seed = int.Parse(value, CultureInfo.InvariantCulture);
						break;
				}
			}

			sizes = sizes.Distinct().Where(s => s > 0).OrderBy(s => s).ToList();
			threadCounts = threadCounts.Distinct().Where(t => t > 0).OrderBy(t => t).ToList();

			if (sizes.Count == 0)
			{
				throw new ArgumentException("Потрібно вказати принаймні один коректний розмір матриці.");
			}

			if (threadCounts.Count == 0)
			{
				throw new ArgumentException("Потрібно вказати принаймні одну коректну кількість потоків.");
			}

			if (!threadCounts.Contains(1))
			{
				threadCounts.Insert(0, 1);
			}

			return new RunnerOptions(sizes, threadCounts, tolerance, maxIterations, seed);
		}

		private static List<int> ParseIntList(string value, string optionName)
		{
			var result = new List<int>();
			var tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries);

			foreach (var token in tokens)
			{
				if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
				{
					throw new ArgumentException($"Не вдалося розібрати '{token}' в опції '{optionName}'.");
				}

				result.Add(parsed);
			}

			return result;
		}
	}
}
