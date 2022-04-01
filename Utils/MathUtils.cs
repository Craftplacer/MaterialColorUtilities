using System;
using System.Collections.Generic;

namespace MaterialColorUtilities.Utils;

public static class MathUtils
{
    /// The signum function.
    ///
    ///
    /// Returns 1 if num > 0, -1 if num < 0, and 0 if num = 0
    public static int signum(double num)
    {
        if (num < 0)
        {
            return -1;
        }
        else
        {
            if (num == 0)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }

    /// The linear interpolation function.
    ///
    ///
    /// Returns start if amount = 0 and stop if amount = 1
    public static double lerp(double start, double stop, double amount)
    {
        return (1.0 - amount) * start + amount * stop;
    }

    /// Clamps an integer between two integers.
    ///
    ///
    /// Returns input when min <= input <= max, and either min or max
    /// otherwise.
    [Obsolete]
    public static int clampInt(int min, int max, int input)
    {
        if (input < min)
        {
            return min;
        }
        else
        {
            if (input > max)
            {
                return max;
            }
        }
        return input;
    }

    /// Clamps an integer between two floating-point numbers.
    ///
    ///
    /// Returns input when min <= input <= max, and either min or max
    /// otherwise.
    [Obsolete]
    public static double clampDouble(double min, double max, double input)
    {
        if (input < min)
        {
            return min;
        }
        else
        {
            if (input > max)
            {
                return max;
            }
        }
        return input;
    }

    /// <summary>
    /// Sanitizes a degree measure as an integer.
    /// </summary>
    /// <returns>
    /// Returns a degree measure between 0 (inclusive) and 360
    /// (exclusive).
    /// </returns>
    public static int sanitizeDegreesInt(int degrees)
    {
        degrees = degrees % 360;
        if (degrees < 0)
        {
            degrees = degrees + 360;
        }
        return degrees;
    }

    /// <summary>
    /// Sanitizes a degree measure as a floating-point number.
    /// </summary>
    /// <returns>
    /// Returns a degree measure between 0.0 (inclusive) and 360.0
    /// (exclusive)
    /// </returns>
    public static double sanitizeDegreesDouble(double degrees)
    {
        degrees = degrees % 360.0;
        if (degrees < 0)
        {
            degrees = degrees + 360.0;
        }
        return degrees;
    }

    /// <summary>
    /// Distance of two points on a circle, represented using degrees.
    /// </summary>
    public static double differenceDegrees(double a, double b)
    {
        return 180.0 - Math.Abs(Math.Abs(a - b) - 180.0);
    }

    /// <summary>
    /// Multiplies a 1x3 row vector with a 3x3 matrix.
    /// </summary>
    public static double[] matrixMultiply(double[] row, double[,] matrix)
    {
        var a = row[0] * matrix[0, 0 ] + row[1] * matrix[0, 1] + row[2] * matrix[0, 2];
        var b = row[0] * matrix[1, 0 ] + row[1] * matrix[1, 1] + row[2] * matrix[1, 2];
        var c = row[0] * matrix[2, 0 ] + row[1] * matrix[2, 1] + row[2] * matrix[2, 2];
        return new[] { a, b, c };
    }
}
