using MaterialColorUtilities.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MaterialColorUtilities.HCT;

public record ViewingConditions(
    double[] whitePoint,
    double adaptingLuminance,
    double backgroundLstar,
    double surround,
    bool discountingIlluminant,
    double backgroundYTowhitePointY,
    double aw,
    double nbb,
    double ncb,
    double c,
    double nC,
    List<double> drgbInverse,
    List<double> rgbD,
    double fl,
    double fLRoot,
    double z)
{
    public static readonly ViewingConditions sRgb = Make();
    public static readonly ViewingConditions standard = sRgb;


    public static ViewingConditions Make(
        double[]? whitePoint = null,
        double adaptingLuminance = -1.0,
        double backgroundLstar = 50.0,
        double surround = 2.0,
        bool discountingIlluminant = false)
    {
        whitePoint ??= ColorUtils.whitePointD65();

        adaptingLuminance = (adaptingLuminance > 0.0)
            ? adaptingLuminance
            : (200.0 / Math.PI * ColorUtils.yFromLstar(50.0) / 100.0);
        backgroundLstar = Math.Max(30.0, backgroundLstar);
        // Transform test illuminant white in XYZ to 'cone'/'rgb' responses
        var xyz = whitePoint;
        var rW = xyz[0] * 0.401288 + xyz[1] * 0.650173 + xyz[2] * -0.051461;
        var gW = xyz[0] * -0.250268 + xyz[1] * 1.204414 + xyz[2] * 0.045854;
        var bW = xyz[0] * -0.002079 + xyz[1] * 0.048952 + xyz[2] * 0.953127;

        // Scale input surround, domain (0, 2), to CAM16 surround, domain (0.8, 1.0)
        Debug.Assert(surround >= 0.0 && surround <= 2.0);
        var f = 0.8 + (surround / 10.0);
        // "Exponential non-linearity"
        var c = (f >= 0.9)
            ? MathUtils.lerp(0.59, 0.69, ((f - 0.9) * 10.0))
            : MathUtils.lerp(0.525, 0.59, ((f - 0.8) * 10.0));
        // Calculate degree of adaptation to illuminant
        var d = discountingIlluminant
            ? 1.0
            : f *
                (1.0 -
                    ((1.0 / 3.6) * Math.Exp((-adaptingLuminance - 42.0) / 92.0)));
        // Per Li et al, if D is greater than 1 or less than 0, set it to 1 or 0.
        d = (d > 1.0)
            ? 1.0
            : (d < 0.0)
                ? 0.0
                : d;
        // chromatic induction factor
        var nc = f;

        // Cone responses to the whitePoint, r/g/b/W, adjusted for discounting.
        //
        // Why use 100.0 instead of the white point's relative luminance?
        //
        // Some papers and implementations, for both CAM02 and CAM16, use the Y
        // value of the reference white instead of 100. Fairchild's Color Appearance
        // Models (3rd edition) notes that this is in error: it was included in the
        // CIE 2004a report on CIECAM02, but, later parts of the conversion process
        // account for scaling of appearance relative to the white point relative
        // luminance. This part should simply use 100 as luminance.
        var rgbD = new double[] {
          d * (100.0 / rW) + 1.0 - d,
          d * (100.0 / gW) + 1.0 - d,
          d * (100.0 / bW) + 1.0 - d
        };

        // Factor used in calculating meaningful factors
        var k = 1.0 / (5.0 * adaptingLuminance + 1.0);
        var k4 = k * k * k * k;
        var k4F = 1.0 - k4;

        // Luminance-level adaptation factor
        var fl = (k4 * adaptingLuminance) +
            (0.1 * k4F * k4F * Math.Pow(5.0 * adaptingLuminance, 1.0 / 3.0));
        // Intermediate factor, ratio of background relative luminance to white relative luminance
        var n = ColorUtils.yFromLstar(backgroundLstar) / whitePoint[1];

        // Base exponential nonlinearity
        // note Schlomer 2018 has a typo and uses 1.58, the correct factor is 1.48
        var z = 1.48 + Math.Sqrt(n);

        // Luminance-level induction factors
        var nbb = 0.725 / Math.Pow(n, 0.2);
        var ncb = nbb;

        // Discounted cone responses to the white point, adjusted for post-saturationtic
        // adaptation perceptual nonlinearities.
        var rgbAFactors = new double[] {
              Math.Pow(fl * rgbD[0] * rW / 100.0, 0.42),
              Math.Pow(fl * rgbD[1] * gW / 100.0, 0.42),
              Math.Pow(fl * rgbD[2] * bW / 100.0, 0.42)
        };

        var rgbA = new double[] {
            (400.0 * rgbAFactors[0]) / (rgbAFactors[0] + 27.13),
            (400.0 * rgbAFactors[1]) / (rgbAFactors[1] + 27.13),
            (400.0 * rgbAFactors[2]) / (rgbAFactors[2] + 27.13)
        };

        var aw = (40.0 * rgbA[0] + 20.0 * rgbA[1] + rgbA[2]) / 20.0 * nbb;

        return new ViewingConditions(
              whitePoint,
              adaptingLuminance,
              backgroundLstar,
              surround,
              discountingIlluminant,
              n,
              aw,
              nbb,
              ncb,
              c,
              nc,
              new List<double> { 0.0, 0.0, 0.0 },
              rgbD.ToList(),
              fl,
              Math.Pow(fl, 0.25),
              z
          );
    }
}
