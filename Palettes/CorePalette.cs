using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using MaterialColorUtilities.HCT;

namespace MaterialColorUtilities.Palettes;

/// An intermediate concept between the key color for a UI theme, and a full
/// color scheme. 5 tonal palettes are generated, all except one use the same
/// hue as the key color, and all vary in chroma.
public class CorePalette
{
    /// The number of generated tonal palettes.
    const int size = 5;

    public TonalPalette Primary { get; }
    public TonalPalette Secondary { get; }
    public TonalPalette Tertiary { get; }
    public TonalPalette Neutral { get; }
    public TonalPalette NeutralVariant { get; }
    public TonalPalette Error { get; } = new TonalPalette(25, 84);

    /// Create a [CorePalette] from a source ARGB color.
    public static CorePalette of(int argb)
    {
        var cam = Cam16.FromInt(argb);
        return new CorePalette(cam.hue, cam.chroma);
    }

    private CorePalette(double hue, double chroma)
    {
        Primary = new TonalPalette(hue, Math.Max(48, chroma));
        Secondary = new TonalPalette(hue, 16);
        Tertiary = new TonalPalette(hue + 60, 24);
        Neutral = new TonalPalette(hue, 4);
        NeutralVariant = new TonalPalette(hue, 8);
    }

    /// Create a [CorePalette] from a fixed-size list of ARGB color ints
    /// representing concatenated tonal palettes.
    ///
    /// Inverse of [asList].
    public CorePalette(IEnumerable<int> colors)
    {
        Debug.Assert(colors.Count() == size * TonalPalette.commonSize);
        Primary = TonalPalette.FromList(
        getPartition(colors, 0, TonalPalette.commonSize));
        Secondary = TonalPalette.FromList(
        getPartition(colors, 1, TonalPalette.commonSize));
        Tertiary = TonalPalette.FromList(
        getPartition(colors, 2, TonalPalette.commonSize));
        Neutral = TonalPalette.FromList(
        getPartition(colors, 3, TonalPalette.commonSize));
        NeutralVariant = TonalPalette.FromList(
        getPartition(colors, 4, TonalPalette.commonSize));
    }

    /// Returns a list of ARGB color <see cref="int"/>s from concatenated tonal palettes.
    ///
    /// Inverse of <see cref="CorePalette(IEnumerable{int})"/>.
    List<int> AsList
    {
        get
        {
            var palettes = new[] { Primary, Secondary, Tertiary, Neutral, NeutralVariant, Error };
            return palettes.SelectMany(tp => tp.AsList).ToList();
        }
    }

    public override string ToString()
    {
        return $@"primary: {Primary}
            secondary: {Secondary}
            tertiary: {Tertiary}
            neutral: {Neutral}
            neutralVariant: {NeutralVariant}
            error: {Error}";
    }

    // Returns a partition from a list.
    //
    // For example, given a list with 2 partitions of size 3.
    // range = [1, 2, 3, 4, 5, 6];
    //
    // range.getPartition(0, 3) // [1, 2, 3]
    // range.getPartition(1, 3) // [4, 5, 6]
    private List<int> getPartition(IEnumerable<int> list, int partitionNumber, int partitionSize)
    {
        return list
            .Skip(partitionNumber * partitionSize)
            .Take((partitionNumber + 1) * partitionSize)
            .ToList();
    }
}
