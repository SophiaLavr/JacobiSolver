using System;

namespace JacobiApp.Models
{
    public sealed class LinearSystem
    {
        public LinearSystem(double[,] coefficients, double[] constants)
        {
            Coefficients = coefficients ?? throw new ArgumentNullException(nameof(coefficients));
            Constants = constants ?? throw new ArgumentNullException(nameof(constants));

            var rows = coefficients.GetLength(0);
            var columns = coefficients.GetLength(1);

            if (rows != columns)
            {
                throw new ArgumentException("Матриця коефіцієнтів має бути квадратною.", nameof(coefficients));
            }

            if (rows != constants.Length)
            {
                throw new ArgumentException("Довжина вектора вільних членів має збігатися з розмірністю матриці.", nameof(constants));
            }
        }

        public double[,] Coefficients { get; }

        public double[] Constants { get; }

        public int Size => Constants.Length;

        public bool HasZeroOnDiagonal()
        {
            var n = Size;
            for (var i = 0; i < n; i++)
            {
                if (Math.Abs(Coefficients[i, i]) < double.Epsilon)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsDiagonallyDominant()
        {
            var n = Size;

            for (var i = 0; i < n; i++)
            {
                var diag = Math.Abs(Coefficients[i, i]);
                double rowSum = 0;

                for (var j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    rowSum += Math.Abs(Coefficients[i, j]);
                }

                if (diag < rowSum)
                {
                    return false;
                }
            }

            return true;
        }

        public double[] Multiply(double[] vector)
        {
            if (vector == null)
            {
                throw new ArgumentNullException(nameof(vector));
            }

            if (vector.Length != Size)
            {
                throw new ArgumentException("Розмірність вектора має збігатися з розміром системи.", nameof(vector));
            }

            var n = Size;
            var result = new double[n];

            for (var i = 0; i < n; i++)
            {
                double sum = 0;
                for (var j = 0; j < n; j++)
                {
                    sum += Coefficients[i, j] * vector[j];
                }

                result[i] = sum;
            }

            return result;
        }

        public double ComputeResidualNorm(double[] solution)
        {
            var ax = Multiply(solution);
            var n = Size;
            double norm = 0;

            for (var i = 0; i < n; i++)
            {
                var diff = ax[i] - Constants[i];
                norm += diff * diff;
            }

            return Math.Sqrt(norm);
        }

        public LinearSystem Clone()
        {
            var n = Size;
            var copy = new double[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    copy[i, j] = Coefficients[i, j];
                }
            }

            var constantsCopy = new double[n];
            Array.Copy(Constants, constantsCopy, n);

            return new LinearSystem(copy, constantsCopy);
        }
    }
}
