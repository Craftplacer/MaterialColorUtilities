
using MaterialColorUtilities.Utils;

using System;
using System.Diagnostics;

namespace MaterialColorUtilities.HCT;

public record Cam16(
    double hue,
    double chroma,
    double j,
    double q,
    double m,
    double s,
    double jstar,
    double astar,
    double bstar
    )
{
    /// CAM16 instances also have coordinates in the CAM16-UCS space, called J*,
    /// a*, b*, or jstar, astar, bstar in code. CAM16-UCS is included in the CAM16
    /// specification, and should be used when measuring distances between colors.
    public double distance(Cam16 other)
    {
        var dJ = jstar - other.jstar;
        var dA = astar - other.astar;
        var dB = bstar - other.bstar;
        var dEPrime = Math.Sqrt(dJ * dJ + dA * dA + dB * dB);
        var dE = 1.41 * Math.Pow(dEPrime, 0.63);
        return dE;
    }

    /// Convert [argb] to CAM16, assuming the color was viewed in default viewing
    /// conditions.
    public static Cam16 FromInt(int argb)
    {
        return fromIntInViewingConditions(argb, ViewingConditions.sRgb);
    }

    /// Given [viewingConditions], convert [argb] to CAM16.
    public static Cam16 fromIntInViewingConditions(
        int argb, ViewingConditions viewingConditions)
    {
        // Transform ARGB int to XYZ
        var xyz = ColorUtils.xyzFromArgb(argb);
        var x = xyz[0];
        var y = xyz[1];
        var z = xyz[2];

        // Transform XYZ to 'cone'/'rgb' responses

        var rC = 0.401288 * x + 0.650173 * y - 0.051461 * z;
        var gC = -0.250268 * x + 1.204414 * y + 0.045854 * z;
        var bC = -0.002079 * x + 0.048952 * y + 0.953127 * z;

        // Discount illuminant
        var rD = viewingConditions.rgbD[0] * rC;
        var gD = viewingConditions.rgbD[1] * gC;
        var bD = viewingConditions.rgbD[2] * bC;

        // chromatic adaptation
        var rAF = Math.Pow(viewingConditions.fl * Math.Abs(rD) / 100.0, 0.42);
        var gAF = Math.Pow(viewingConditions.fl * Math.Abs(gD) / 100.0, 0.42);
        var bAF = Math.Pow(viewingConditions.fl * Math.Abs(bD) / 100.0, 0.42);
        var rA = MathUtils.signum(rD) * 400.0 * rAF / (rAF + 27.13);
        var gA = MathUtils.signum(gD) * 400.0 * gAF / (gAF + 27.13);
        var bA = MathUtils.signum(bD) * 400.0 * bAF / (bAF + 27.13);

        // redness-greenness
        var a = (11.0 * rA + -12.0 * gA + bA) / 11.0;
        // yellowness-blueness
        var b = (rA + gA - 2.0 * bA) / 9.0;

        // auxiliary components
        var u = (20.0 * rA + 20.0 * gA + 21.0 * bA) / 20.0;
        var p2 = (40.0 * rA + 20.0 * gA + bA) / 20.0;

        // hue
        var atan2 = Math.Atan2(b, a);
        var atanDegrees = atan2 * 180.0 / Math.PI;
        var hue = atanDegrees < 0
            ? atanDegrees + 360.0
            : atanDegrees >= 360
                ? atanDegrees - 360
                : atanDegrees;
        var hueRadians = hue * Math.PI / 180.0;
        Debug.Assert(hue >= 0 && hue < 360, $"hue was really {hue}");

        // achromatic response to color
        var ac = p2 * viewingConditions.nbb;

        // CAM16 lightness and brightness
        var J = 100.0 *
            Math.Pow(ac / viewingConditions.aw,
                viewingConditions.c * viewingConditions.z);
        var Q = 4.0 / viewingConditions.c *
            Math.Sqrt(J / 100.0) *
            (viewingConditions.aw + 4.0) *
            viewingConditions.fLRoot;

        var huePrime = hue < 20.14 ? hue + 360 : hue;
        var eHue =
            1.0 / 4.0 * (Math.Cos(huePrime * Math.PI / 180.0 + 2.0) + 3.8);
        var p1 =
            50000.0 / 13.0 * eHue * viewingConditions.nC * viewingConditions.ncb;
        var t = p1 * Math.Sqrt(a * a + b * b) / (u + 0.305);
        var alpha = Math.Pow(t, 0.9) *
            Math.Pow(
                1.64 - Math.Pow(0.29, viewingConditions.backgroundYTowhitePointY),
                0.73);
        // CAM16 chroma, colorfulness, chroma
        var C = alpha * Math.Sqrt(J / 100.0);
        var M = C * viewingConditions.fLRoot;
        var s = 50.0 *
            Math.Sqrt(alpha * viewingConditions.c / (viewingConditions.aw + 4.0));

        // CAM16-UCS components
        var jstar = (1.0 + 100.0 * 0.007) * J / (1.0 + 0.007 * J);
        var mstar = Math.Log(1.0 + 0.0228 * M) / 0.0228;
        var astar = mstar * Math.Cos(hueRadians);
        var bstar = mstar * Math.Sin(hueRadians);
        return new Cam16(hue, C, J, Q, M, s, jstar, astar, bstar);
    }

    /// Create a CAM16 color from lightness [j], chroma [c], and hue [h],
    /// assuming the color was viewed in default viewing conditions.
    public static Cam16 fromJch(double j, double c, double h)
    {
        return fromJchInViewingConditions(j, c, h, ViewingConditions.sRgb);
    }

    /// Create a CAM16 color from lightness [j], chroma [c], and hue [h],
    /// in [viewingConditions].
    public static Cam16 fromJchInViewingConditions(
        double J, double C, double h, ViewingConditions viewingConditions)
    {
        var Q = 4.0 / viewingConditions.c *
            Math.Sqrt(J / 100.0) *
            (viewingConditions.aw + 4.0) *
            viewingConditions.fLRoot;
        var M = C * viewingConditions.fLRoot;
        var alpha = C / Math.Sqrt(J / 100.0);
        var s = 50.0 *
            Math.Sqrt(alpha * viewingConditions.c / (viewingConditions.aw + 4.0));

        var hueRadians = h * Math.PI / 180.0;
        var jstar = (1.0 + 100.0 * 0.007) * J / (1.0 + 0.007 * J);
        var mstar = 1.0 / 0.0228 * Math.Log(1.0 + 0.0228 * M);
        var astar = mstar * Math.Cos(hueRadians);
        var bstar = mstar * Math.Sin(hueRadians);
        return new Cam16(h, C, J, Q, M, s, jstar, astar, bstar);
    }

    /// Create a CAM16 color from CAM16-UCS coordinates [jstar], [astar], [bstar].
    /// assuming the color was viewed in default viewing conditions.
    public static Cam16 fromUcs(double jstar, double astar, double bstar)
    {
        return fromUcsInViewingConditions(
            jstar, astar, bstar, ViewingConditions.standard);
    }

    /// Create a CAM16 color from CAM16-UCS coordinates [jstar], [astar], [bstar].
    /// in [viewingConditions].
    public static Cam16 fromUcsInViewingConditions(double jstar, double astar,
        double bstar, ViewingConditions viewingConditions)
    {
        var a = astar;
        var b = bstar;
        var m = Math.Sqrt(a * a + b * b);
        var M = (Math.Exp(m * 0.0228) - 1.0) / 0.0228;
        var c = M / viewingConditions.fLRoot;
        var h = Math.Atan2(b, a) * (180.0 / Math.PI);
        if (h < 0.0)
        {
            h += 360.0;
        }
        var j = jstar / (1 - (jstar - 100) * 0.007);

        return Cam16.fromJchInViewingConditions(j, c, h, viewingConditions);
    }

    /// ARGB representation of color, assuming the color was viewed in default
    /// viewing conditions.
    int viewedInSRgb => viewed(ViewingConditions.sRgb);

    /// ARGB representation of a color, given the color was viewed in
    /// [viewingConditions]
    public int viewed(ViewingConditions viewingConditions)
    {
        var alpha =
            chroma == 0.0 || j == 0.0 ? 0.0 : chroma / Math.Sqrt(j / 100.0);

        var t = Math.Pow(
            alpha /
                Math.Pow(
                    1.64 -
                        Math.Pow(0.29, viewingConditions.backgroundYTowhitePointY),
                    0.73),
            1.0 / 0.9);
        var hRad = hue * Math.PI / 180.0;

        var eHue = 0.25 * (Math.Cos(hRad + 2.0) + 3.8);
        var ac = viewingConditions.aw *
            Math.Pow(j / 100.0, 1.0 / viewingConditions.c / viewingConditions.z);
        var p1 =
            eHue * (50000.0 / 13.0) * viewingConditions.nC * viewingConditions.ncb;

        var p2 = ac / viewingConditions.nbb;

        var hSin = Math.Sin(hRad);
        var hCos = Math.Cos(hRad);

        var gamma = 23.0 *
            (p2 + 0.305) *
            t /
            (23.0 * p1 + 11 * t * hCos + 108.0 * t * hSin);
        var a = gamma * hCos;
        var b = gamma * hSin;
        var rA = (460.0 * p2 + 451.0 * a + 288.0 * b) / 1403.0;
        var gA = (460.0 * p2 - 891.0 * a - 261.0 * b) / 1403.0;
        var bA = (460.0 * p2 - 220.0 * a - 6300.0 * b) / 1403.0;

        var rCBase = Math.Max(0, 27.13 * Math.Abs(rA) / (400.0 - Math.Abs(rA)));
        var rC = MathUtils.signum(rA) *
            (100.0 / viewingConditions.fl) *
            Math.Pow(rCBase, 1.0 / 0.42);
        var gCBase = Math.Max(0, 27.13 * Math.Abs(gA) / (400.0 - Math.Abs(gA)));
        var gC = MathUtils.signum(gA) *
            (100.0 / viewingConditions.fl) *
            Math.Pow(gCBase, 1.0 / 0.42);
        var bCBase = Math.Max(0, 27.13 * Math.Abs(bA) / (400.0 - Math.Abs(bA)));
        var bC = MathUtils.signum(bA) *
            (100.0 / viewingConditions.fl) *
            Math.Pow(bCBase, 1.0 / 0.42);
        var rF = rC / viewingConditions.rgbD[0];
        var gF = gC / viewingConditions.rgbD[1];
        var bF = bC / viewingConditions.rgbD[2];

        var x = 1.86206786 * rF - 1.01125463 * gF + 0.14918677 * bF;
        var y = 0.38752654 * rF + 0.62144744 * gF - 0.00897398 * bF;
        var z = -0.01584150 * rF - 0.03412294 * gF + 1.04996444 * bF;

        var argb = ColorUtils.argbFromXyz(x, y, z);
        return argb;
    }
}
