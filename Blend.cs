using MaterialColorUtilities.HCT;
using MaterialColorUtilities.Utils;

using System;

namespace MaterialColorUtilities;

public static class Blend
{
    /// <summary>
    /// Shifts <paramref name="designColor"/>'s hue towards <paramref name="sourceColor"/>'s, creating a slightly
    /// warmer/coolor variant of <paramref name="designColor"/>. Hue will shift up to 15 degrees.
    /// </summary>
    /// <param name="designColor"></param>
    /// <param name="sourceColor"></param>
    /// <returns></returns>
    static int Harmonize(int designColor, int sourceColor)
    {
        var fromHct = HctColor.FromInt(designColor);
        var toHct = HctColor.FromInt(sourceColor);
        var differenceDegrees =
            MathUtils.differenceDegrees(fromHct.Hue, toHct.Hue);
        var rotationDegrees = Math.Min(differenceDegrees * 0.5, 15.0);
        var outputHue = MathUtils.sanitizeDegreesDouble(fromHct.Hue +
            rotationDegrees * getRotationDirection(fromHct.Hue, toHct.Hue));
        return HctColor.From(outputHue, fromHct.Chroma, fromHct.Tone);
    }

    /// Sign of direction change needed to travel from one angle to another.
    ///
    /// [from] is the angle travel starts from in degrees. [to] is the ending
    /// angle, also in degrees.
    ///
    /// The return value is -1 if decreasing [from] leads to the shortest travel
    /// distance,  1 if increasing from leads to the shortest travel distance.
    private static double getRotationDirection(double from, double to)
    {
        var a = to - from;
        var b = to - from + 360.0;
        var c = to - from - 360.0;

        var aAbs = Math.Abs(a);
        var bAbs = Math.Abs(b);
        var cAbs = Math.Abs(c);

        if (aAbs <= bAbs && aAbs <= cAbs)
        {
            return a >= 0.0 ? 1 : -1;
        }
        else if (bAbs <= aAbs && bAbs <= cAbs)
        {
            return b >= 0.0 ? 1 : -1;
        }
        else
        {
            return c >= 0.0 ? 1 : -1;
        }
    }
}
