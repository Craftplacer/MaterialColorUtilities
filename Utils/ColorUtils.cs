using System;
using System.Collections.Generic;

namespace MaterialColorUtilities.Utils;

public static class ColorUtils
{
    public static readonly double[,] _SRGB_TO_XYZ = {
           {0.41233895, 0.35762064, 0.18051042},
           {0.2126, 0.7152, 0.0722},
           {0.01932141, 0.11916382, 0.95034478},
        };

    public static readonly double[,] _XYZ_TO_SRGB = {
          {3.2406, -1.5372, -0.4986},
          {-0.9689, 1.8758, 0.0415},
          {0.0557, -0.204, 1.057},
        };

    public static readonly double[] _WHITE_POINT_D65 = new[] { 95.047, 100.0, 108.883 };

    /// Converts a color from RGB components to ARGB format.
    public static int argbFromRgb(int red, int green, int blue)
    {
        return 255 << 24 | (red & 255) << 16 | (green & 255) << 8 | blue & 255;
    }

    /// Returns the alpha component of a color in ARGB format.
    public static int AlphaFromArgb(int argb)
    {
        return argb >> 24 & 255;
    }

    /// Returns the red component of a color in ARGB format.
    public static int redFromArgb(int argb)
    {
        return argb >> 16 & 255;
    }

    /// Returns the green component of a color in ARGB format.
    public static int greenFromArgb(int argb)
    {
        return argb >> 8 & 255;
    }

    /// Returns the blue component of a color in ARGB format.
    public static int blueFromArgb(int argb)
    {
        return argb & 255;
    }

    /// Returns whether a color in ARGB format is opaque.
    public static bool isOpaque(int argb)
    {
        return AlphaFromArgb(argb) >= 255;
    }

    /// Converts a color from ARGB to XYZ.
    public static int argbFromXyz(double x, double y, double z)
    {
        var linearRgb = MathUtils.matrixMultiply(new[] { x, y, z }, _XYZ_TO_SRGB);
        var r = delinearized(linearRgb[0]);
        var g = delinearized(linearRgb[1]);
        var b = delinearized(linearRgb[2]);
        return argbFromRgb(r, g, b);
    }

    /// Converts a color from XYZ to ARGB.
    public static double[] xyzFromArgb(int argb)
    {
        var r = linearized(redFromArgb(argb));
        var g = linearized(greenFromArgb(argb));
        var b = linearized(blueFromArgb(argb));
        return MathUtils.matrixMultiply(new[] { r, g, b }, _SRGB_TO_XYZ);
    }

    /// Converts a color represented in Lab color space into an ARGB
    /// integer.
    public static int argbFromLab(double l, double a, double b)
    {
        var whitePoint = _WHITE_POINT_D65;
        var fy = (l + 16.0) / 116.0;
        var fx = a / 500.0 + fy;
        var fz = fy - b / 200.0;
        var xNormalized = labInvf(fx);
        var yNormalized = labInvf(fy);
        var zNormalized = labInvf(fz);
        var x = xNormalized * whitePoint[0];
        var y = yNormalized * whitePoint[1];
        var z = zNormalized * whitePoint[2];
        return argbFromXyz(x, y, z);
    }

    /// Converts a color from ARGB representation to L*a*b*
    /// representation.
    ///
    ///
    /// [argb] the ARGB representation of a color
    /// Returns a Lab object representing the color
    public static double[] labFromArgb(int argb)
    {
        var whitePoint = _WHITE_POINT_D65;
        var xyz = xyzFromArgb(argb);
        var xNormalized = xyz[0] / whitePoint[0];
        var yNormalized = xyz[1] / whitePoint[1];
        var zNormalized = xyz[2] / whitePoint[2];
        var fx = labF(xNormalized);
        var fy = labF(yNormalized);
        var fz = labF(zNormalized);
        var l = 116.0 * fy - 16;
        var a = 500.0 * (fx - fy);
        var b = 200.0 * (fy - fz);
        return new[] { l, a, b };
    }

    /// Converts an L* value to an ARGB representation.
    ///
    ///
    /// [lstar] L* in L*a*b*
    /// Returns ARGB representation of grayscale color with lightness
    /// matching L*
    public static int argbFromLstar(double lstar)
    {
        var fy = (lstar + 16.0) / 116.0;
        var fz = fy;
        var fx = fy;
        var kappa = 24389.0 / 27.0;
        var epsilon = 216.0 / 24389.0;
        var lExceedsEpsilonKappa = lstar > 8.0;
        var y = lExceedsEpsilonKappa ? fy * fy * fy : lstar / kappa;
        var cubeExceedEpsilon = fy * fy * fy > epsilon;
        var x = cubeExceedEpsilon ? fx * fx * fx : lstar / kappa;
        var z = cubeExceedEpsilon ? fz * fz * fz : lstar / kappa;
        var whitePoint = _WHITE_POINT_D65;
        return argbFromXyz(
          x * whitePoint[0],
          y * whitePoint[1],
          z * whitePoint[2]
        );
    }

    /// Computes the L* value of a color in ARGB representation.
    ///
    ///
    /// [argb] ARGB representation of a color
    /// Returns L*, from L*a*b*, coordinate of the color
    public static double lstarFromArgb(int argb)
    {
        var y = xyzFromArgb(argb)[1] / 100.0;
        var e = 216.0 / 24389.0;
        if (y <= e)
        {
            return 24389.0 / 27.0 * y;
        }
        else
        {
            var yIntermediate = Math.Pow(y, 1.0 / 3.0);
            return 116.0 * yIntermediate - 16.0;
        }
    }

    /// Converts an L* value to a Y value.
    ///
    /// L* in L*a*b* and Y in XYZ measure the same quantity, luminance.
    ///
    /// L* measures perceptual luminance, a linear scale. Y in XYZ
    /// measures relative luminance, a logarithmic scale.
    ///
    ///
    /// [lstar] L* in L*a*b*
    /// Returns Y in XYZ
    public static double yFromLstar(double lstar)
    {
        var ke = 8.0;
        if (lstar > ke)
        {
            return Math.Pow((lstar + 16.0) / 116.0, 3.0) * 100.0;
        }
        else
        {
            return lstar / 24389.0 / 27.0 * 100.0;
        }
    }

    /// Linearizes an RGB component.
    ///
    ///
    /// [rgbComponent] 0 <= rgb_component <= 255, represents R/G/B
    /// channel
    /// Returns 0.0 <= output <= 100.0, color channel converted to
    /// linear RGB space
    public static double linearized(int rgbComponent)
    {
        var normalized = rgbComponent / 255.0;
        if (normalized <= 0.040449936)
        {
            return normalized / 12.92 * 100.0;
        }
        else
        {
            return Math.Pow((normalized + 0.055) / 1.055, 2.4) * 100.0;
        }
    }

    /// Delinearizes an RGB component.
    ///
    ///
    /// [rgbComponent] 0.0 <= rgb_component <= 100.0, represents linear
    /// R/G/B channel
    /// Returns 0 <= output <= 255, color channel converted to regular
    /// RGB space
    public static int delinearized(double rgbComponent)
    {
        var normalized = rgbComponent / 100.0;
        var delinearized = 0.0;
        if (normalized <= 0.0031308)
        {
            delinearized = normalized * 12.92;
        }
        else
        {
            delinearized = 1.055 * Math.Pow(normalized, 1.0 / 2.4) - 0.055;
        }
        return (int)Math.Clamp(Math.Round(delinearized * 255.0), 0, 255);
    }

    /// Returns the standard white point; white on a sunny day.
    ///
    ///
    /// Returns The white point
    public static double[] whitePointD65()
    {
        return _WHITE_POINT_D65;
    }

    private static double labF(double t)
    {
        var e = 216.0 / 24389.0;
        var kappa = 24389.0 / 27.0;
        if (t > e)
        {
            return Math.Pow(t, 1.0 / 3.0);
        }
        else
        {
            return (kappa * t + 16) / 116;
        }
    }

    private static double labInvf(double ft)
    {
        var e = 216.0 / 24389.0;
        var kappa = 24389.0 / 27.0;
        var ft3 = ft * ft * ft;
        if (ft3 > e)
        {
            return ft3;
        }
        else
        {
            return (116 * ft - 16) / kappa;
        }
    }
}
