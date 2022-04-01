
using MaterialColorUtilities.Utils;

using System;
using System.Collections.Generic;

namespace MaterialColorUtilities.HCT;

public static class Hct
{

    /// When the delta between the floor & ceiling of a binary search for chroma is
    /// less than this, the binary search terminates.
    private const double _chromaSearchEndpoint = 0.4;

    /// The maximum color distance, in CAM16-UCS, between a requested color and the
    /// color returned.
    private const double _deMax = 1.0;

    /// The maximum difference between the requested L* and the L* returned.
    private const double _dlMax = 0.2;

    /// The minimum color distance, in CAM16-UCS, between a requested color and an
    /// 'exact' match. This allows the binary search during gamut mapping to
    /// terminate much earlier when the error is infinitesimal.
    private const double _deMaxError = 0.000000001;

    /// When the delta between the floor & ceiling of a binary search for J,
    /// lightness in CAM16, is less than this, the binary search terminates.
    private const double lightnessSearchEndpoint = 0.01;

    public static Cam16 getCam16(double hue, double chroma, double lstar)
    {
        return getCam16InViewingConditions(
            hue, chroma, lstar, ViewingConditions.sRgb);
    }

    public static Cam16 getCam16InViewingConditions(double hue, double chroma, double lstar,
        ViewingConditions viewingConditions)
    {
        if (chroma < 1.0 || Math.Round(lstar) <= 0.0 || Math.Round(lstar) >= 100.0)
        {
            return Cam16.FromInt(ColorUtils.argbFromLstar(lstar));
        }

        hue = hue < 0
            ? 0
            : hue > 360
                ? 360
                : hue;

        // Perform a binary search to find a chroma low enough that lstar is
        // possible. For example, a high chroma, high L* red isn't available.

        // The highest chroma possible. Updated as binary search proceeds.
        var high = chroma;

        // The guess for the current binary search iteration. Starts off at the highest chroma, thus,
        // if a color is possible at the requested chroma, the search can stop early.
        var mid = chroma;
        var low = 0.0;
        var isFirstLoop = true;

        Cam16? answer = null;

        while (Math.Abs(low - high) >= _chromaSearchEndpoint)
        {
            // Given the current chroma guess, mid, and the desired hue, find J, lightness in CAM16 color
            // space, that creates a color with L* = `lstar` in L*a*b*
            var possibleAnswer = findCamByJ(hue, mid, lstar, viewingConditions);

            if (isFirstLoop)
            {
                if (possibleAnswer != null)
                {
                    return possibleAnswer;
                }
                else
                {
                    // If this binary search iteration was the first iteration, and this point has been reached,
                    // it means the requested chroma was not available at the requested hue and L*. Proceed to a
                    // traditional binary search, starting at the midpoint between the requested chroma and 0.

                    isFirstLoop = false;
                    mid = low + (high - low) / 2.0;
                    continue;
                }
            }

            if (possibleAnswer == null)
            {
                // There isn't a CAM16 J that creates a color with L*a*b* L*. Try a lower chroma.
                high = mid;
            }
            else
            {
                answer = possibleAnswer;
                // It is possible to create a color with L* `lstar` and `mid` chroma. Try a higher chroma.
                low = mid;
            }

            mid = low + (high - low) / 2.0;
        }

        // There was no answer: for the desired hue, there was no chroma low enough to generate a color
        // with the desired L*. All values of L* are possible when there is 0 chroma. Return a color
        // with 0 chroma, i.e. a shade of gray, with the desired L*.
        if (answer == null)
        {
            return Cam16.FromInt(ColorUtils.argbFromLstar(lstar));
        }

        return answer;
    }

    public static Cam16? findCamByJ(
        double hue, double chroma, double lstar, ViewingConditions frame)
    {
        var low = 0.0;
        var high = 100.0;
        var mid = 0.0;
        var bestdL = double.MaxValue;
        var bestdE = double.MaxValue;
        Cam16? bestCam = null;
        while (Math.Abs(low - high) > lightnessSearchEndpoint)
        {
            mid = low + (high - low) / 2;
            var camBeforeClip =
                Cam16.fromJchInViewingConditions(mid, chroma, hue, frame);
            var clipped = camBeforeClip.viewed(frame);
            var clippedLstar = ColorUtils.lstarFromArgb(clipped);
            var dL = Math.Abs(lstar - clippedLstar);
            if (dL < _dlMax)
            {
                var camClipped = Cam16.fromIntInViewingConditions(clipped, frame);
                var dE = camClipped.distance(Cam16.fromJchInViewingConditions(
                    camClipped.j, camClipped.chroma, hue, frame));
                if ((dE <= _deMax && dE < bestdE) && dL < _dlMax)
                {
                    bestdL = dL;
                    bestdE = dE;
                    bestCam = camClipped;
                }
            }

            if (bestdL == 0 && bestdE < _deMaxError)
            {
                break;
            }

            if (clippedLstar < lstar)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return bestCam;
    }
}

/// HCT, hue, chroma, and tone. A color system that provides a perceptually
/// accurate color measurement system that can also accurately render what
/// colors will appear as in different lighting environments.
public class HctColor
{
    /// A number, in degrees, representing ex. red, orange, yellow, etc.
    /// Ranges from 0 <= [hue] < 360
    public double Hue
    {
        get => _hue;
        set
        {
            var cam16 = Hct.getCam16(MathUtils.sanitizeDegreesDouble(value), _chroma, _tone);
            _argb = cam16.viewed(ViewingConditions.sRgb);
            _hue = cam16.hue;
            _chroma = cam16.chroma;
            _tone = ColorUtils.lstarFromArgb(_argb);
        }
    }

    public double Chroma
    {
        get => _chroma;

        /// 0 <= [newChroma] <= ?
        /// After setting chroma, the color is mapped from HCT to the more
        /// limited sRGB gamut for display. This will change its ARGB/integer
        /// representation. If the HCT color is outside of the sRGB gamut, chroma
        /// will decrease until it is inside the gamut.
        set
        {
            var cam16 = Hct.getCam16(_hue, value, _tone);
            _argb = cam16.viewed(ViewingConditions.sRgb);
            _hue = cam16.hue;
            _chroma = cam16.chroma;
            _tone = ColorUtils.lstarFromArgb(_argb);
        }
    }

    public double Tone {
        get => _tone;

        /// 0 <= [newTone] <= 100; invalid values are corrected.
        /// After setting tone, the color is mapped from HCT to the more
        /// limited sRGB gamut for display. This will change its ARGB/integer
        /// representation. If the HCT color is outside of the sRGB gamut, chroma
        /// will decrease until it is inside the gamut.
        set
        {
            var cam16 = Hct.getCam16(_hue, _chroma, Math.Clamp(value, 0.0, 100.0));
            _argb = cam16.viewed(ViewingConditions.sRgb);
            _hue = cam16.hue;
            _chroma = cam16.chroma;
            _tone = ColorUtils.lstarFromArgb(_argb);
        }
    }
    public int _argb;

    /// 0 <= [hue] < 360; invalid values are corrected.
    /// 0 <= [chroma] <= ?; Informally, colorfulness. The color returned may be
    ///    lower than the requested chroma. Chroma has a different maximum for any
    ///    given hue and tone.
    /// 0 <= [tone] <= 100; informally, lightness. Invalid values are corrected.
    public static HctColor From(double hue, double chroma, double tone)
    {
        return new HctColor(hue, chroma, tone);
    }

    private HctColor(double hue, double chroma, double tone)
    {
        var cam16 = Hct.getCam16(hue, chroma, tone);
        _argb = cam16.viewed(ViewingConditions.sRgb);
        _hue = cam16.hue;
        _chroma = cam16.chroma;
        _tone = ColorUtils.lstarFromArgb(_argb);
    }

    // @override
    // bool operator ==(o)
    // {
    //     if (o is !HctColor)
    //     {
    //         return false;
    //     }
    //     return o._argb == _argb;
    // }

    // @override
    // String toString()
    // {
    //     return 'H${hue.round().toString()} C${chroma.round()} T${tone.round().toString()}';
    // }

    /// HCT representation of [argb].
    public static HctColor FromInt(int argb)
    {
        var cam = Cam16.FromInt(argb);
        var tone = ColorUtils.lstarFromArgb(argb);
        return new HctColor(cam.hue, cam.chroma, tone);
    }

    int toInt()
    {
        return _argb;
    }

    public override bool Equals(object? obj)
    {
        return obj is HctColor color &&
               _argb == color._argb;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_argb);
    }

    private double _hue;
    private double _chroma;
    private double _tone;

    public static bool operator ==(HctColor? left, HctColor? right)
    {
        return EqualityComparer<HctColor>.Default.Equals(left, right);
    }

    public static bool operator !=(HctColor? left, HctColor? right)
    {
        return !(left == right);
    }

    public static implicit operator int(HctColor hct) => hct._argb;
}