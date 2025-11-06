using System;
using JacobiApp.Models;

namespace JacobiApp.Utilities
{
    public static class DiagonalDominantMatrixGenerator
    {
        private static readonly Random GlobalRandom = new();

        public static LinearSystem Create(int size, double minDiagonalGap = 5.0, int? seed = null)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Розмір має бути додатним.");
            }

            if (minDiagonalGap <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minDiagonalGap), "Значення відступу має бути додатним.");
            }

            var coefficients = new double[size, size];
            var constants = new double[size];

            var random = seed.HasValue ? new Random(seed.Value) : null;

            lock (GlobalRandom)
            {
                for (var i = 0; i < size; i++)
                {
                    double rowSum = 0;

                    for (var j = 0; j < size; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        var value = NextDouble(-10, 10, random);
                        coefficients[i, j] = value;
                        rowSum += Math.Abs(value);
                    }

                    coefficients[i, i] = rowSum + minDiagonalGap;
                    constants[i] = NextDouble(-20, 20, random);
                }
            }

            return new LinearSystem(coefficients, constants);
        }

        private static double NextDouble(double minValue, double maxValue, Random? scopedRandom)
        {
            if (scopedRandom != null)
            {
                return scopedRandom.NextDouble() * (maxValue - minValue) + minValue;
            }

            return GlobalRandom.NextDouble() * (maxValue - minValue) + minValue;
        }
    }
}
