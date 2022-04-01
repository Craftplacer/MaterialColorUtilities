using MaterialColorUtilities.HCT;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MaterialColorUtilities.Palettes
{
    public class TonalPalette
    {
        /// Commonly-used tone values.
        public static readonly int[] commonTones = new int[]{
          0,
          10,
          20,
          30,
          40,
          50,
          60,
          70,
          80,
          90,
          95,
          99,
          100,
        };

        public static readonly int commonSize = commonTones.Length;

        private double? Hue { get; }
        private double? Chroma { get; }

        private Dictionary<int, int> _cache;

        public List<int> AsList => commonTones.Select(get).ToList();

        public TonalPalette(double hue, double chroma)
        {
            Hue = hue;
            Chroma = chroma;
            _cache = new Dictionary<int, int>();
        }

        private TonalPalette(Dictionary<int, int> cache)
        {
            _cache = cache;
        }

        public static TonalPalette FromList(IEnumerable<int> colors)
        {
            Debug.Assert(colors.Count() == commonSize);
            var cache = new Dictionary<int, int>();

            for (int i = 0; i < commonTones.Length; i++)
            {
                var toneValue = commonTones[i];
                cache[toneValue] = colors.ElementAt(i);
            }

            return new TonalPalette(cache);
        }

        public int get(int tone)
        {
            if (Hue == null || Chroma == null)
            {
                if (!_cache.ContainsKey(tone))
                {
                    throw new ArgumentException(
                      nameof(tone),
                      $"When a TonalPalette is created with {nameof(FromList)}, tone must be one of {nameof(commonTones)}"
                    );
                }
                else
                {
                    return _cache[tone]!;
                }
            }
            var chroma = tone >= 90.0 ? Math.Min(Chroma.Value, 40.0) : Chroma!;

            if (_cache.ContainsKey(tone))
            {
                return _cache[tone];
            }
            else
            {
                return _cache[tone] = HctColor.From(Hue.Value, Chroma.Value, tone);
            }
        }
    }
}
