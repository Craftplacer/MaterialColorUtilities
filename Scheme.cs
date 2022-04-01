using MaterialColorUtilities.Palettes;

namespace MaterialColorUtilities
{
    public record Scheme(
        int Primary,
        int OnPrimary,
        int PrimaryContainer,
        int OnPrimaryContainer,
        int Secondary,
        int OnSecondary,
        int SecondaryContainer,
        int OnSecondaryContainer,
        int Teritiary,
        int OnTertiary,
        int TertiaryContainer,
        int OnTertiaryContainer,
        int Error,
        int OnError,
        int ErrorContainer,
        int OnErrorContainer,
        int Background,
        int OnBackground,
        int Surface,
        int OnSurface,
        int SurfaceVariant,
        int OnSurfaceVariant,
        int Outline,
        int Shadow,
        int InverseSurface,
        int InverseOnSurface,
        int InversePrimary
        )
    {
        public static Scheme light(int color) => lightFromCorePalette(CorePalette.of(color));

        public static Scheme dark(int color) => darkFromCorePalette(CorePalette.of(color));

        public static Scheme lightFromCorePalette(CorePalette palette) => new(
          palette.Primary.get(40),
          palette.Primary.get(100),
          palette.Primary.get(90),
          palette.Primary.get(10),
          palette.Secondary.get(40),
          palette.Secondary.get(100),
          palette.Secondary.get(90),
          palette.Secondary.get(10),
          palette.Tertiary.get(40),
          palette.Tertiary.get(100),
          palette.Tertiary.get(90),
          palette.Tertiary.get(10),
          palette.Error.get(40),
          palette.Error.get(100),
          palette.Error.get(90),
          palette.Error.get(10),
          palette.Neutral.get(99),
          palette.Neutral.get(10),
          palette.Neutral.get(99),
          palette.Neutral.get(10),
          palette.NeutralVariant.get(90),
          palette.NeutralVariant.get(30),
          palette.NeutralVariant.get(50),
          palette.Neutral.get(0),
          palette.Neutral.get(20),
          palette.Neutral.get(95),
          palette.Primary.get(80)
        );

        public static Scheme darkFromCorePalette(CorePalette palette) => new(
          palette.Primary.get(80),
          palette.Primary.get(20),
          palette.Primary.get(30),
          palette.Primary.get(90),
          palette.Secondary.get(80),
          palette.Secondary.get(20),
          palette.Secondary.get(30),
          palette.Secondary.get(90),
          palette.Tertiary.get(80),
          palette.Tertiary.get(20),
          palette.Tertiary.get(30),
          palette.Tertiary.get(90),
          palette.Error.get(80),
          palette.Error.get(20),
          palette.Error.get(30),
          palette.Error.get(80),
          palette.Neutral.get(10),
          palette.Neutral.get(90),
          palette.Neutral.get(10),
          palette.Neutral.get(90),
          palette.NeutralVariant.get(30),
          palette.NeutralVariant.get(80),
          palette.NeutralVariant.get(60),
          palette.Neutral.get(0),
          palette.Neutral.get(90),
          palette.Neutral.get(20),
          palette.Primary.get(40)
        );
    }
}
