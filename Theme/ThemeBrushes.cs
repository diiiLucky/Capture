using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CodexCapture.Theme;

public static class ThemeBrushes
{
    // Primary
    public static Brush Primary => Brush("PrimaryBrush");
    public static Brush PrimaryHover => Brush("PrimaryHoverBrush");
    public static Brush PrimaryPressed => Brush("PrimaryPressedBrush");
    public static Brush PrimarySubtle => Brush("PrimarySubtleBrush");
    public static Brush PrimaryMuted => Brush("PrimaryMutedBrush");
    public static Brush PrimaryLight => Brush("PrimaryLightBrush");

    // Legacy accent (mapped to Primary)
    public static Brush Accent => Brush("AccentBrush");
    public static Brush AccentHover => Brush("AccentHoverBrush");
    public static Brush AccentSubtle => Brush("AccentSubtleBrush");

    // Surfaces
    public static Brush Surface => Brush("SurfaceBrush");
    public static Brush SurfaceSecondary => Brush("SurfaceSecondaryBrush");
    public static Brush Panel => Brush("PanelBrush");

    // Text
    public static Brush TextPrimary => Brush("TextPrimaryBrush");
    public static Brush TextSecondary => Brush("TextSecondaryBrush");
    public static Brush TextTertiary => Brush("TextTertiaryBrush");
    public static Brush Ink => Brush("InkBrush");
    public static Brush Muted => Brush("MutedBrush");
    public static Brush Label => Brush("LabelBrush");
    public static Brush Hint => Brush("HintBrush");

    // Borders
    public static Brush BorderDefault => Brush("BorderDefaultBrush");
    public static Brush BorderFocus => Brush("BorderFocusBrush");
    public static Brush BorderSubtle => Brush("BorderSubtleBrush");
    public static Brush Border => Brush("BorderBrush");

    // Semantic
    public static Brush Success => Brush("SuccessBrush");
    public static Brush Warning => Brush("WarningBrush");
    public static Brush Error => Brush("ErrorBrush");
    public static Brush Info => Brush("InfoBrush");

    // Glass
    public static Brush FrostedLight => Brush("FrostedLightBrush");
    public static Brush FrostedMedium => Brush("FrostedMediumBrush");
    public static Brush Frosted => Brush("FrostedBrush");

    // Overlay
    public static Brush OverlayDim => Brush("OverlayDimBrush");
    public static Brush OverlayHighlight => Brush("OverlayHighlightBrush");
    public static Brush SelectionFill => Brush("SelectionFillBrush");
    public static Brush SelectionReviewFill => Brush("SelectionReviewFillBrush");
    public static Brush SelectionBorder => Brush("SelectionBorderBrush");
    public static Brush HudBackground => Brush("HudBackgroundBrush");
    public static Brush HandleFill => Brush("HandleFillBrush");
    public static Brush HandleStroke => Brush("HandleStrokeBrush");

    // Convenience
    public static Brush White => Brushes.White;
    public static Brush Transparent => Brushes.Transparent;

    // Backward compat (old names used in existing window code)
    public static Brush SelectionHighlightBorder => Brush("SelectionBorderBrush");
    public static Brush SelectionHighlightFill => Brush("SelectionFillBrush");
    public static Brush SuccessMintLight => Brush("PrimarySubtleBrush");
    public static Brush SuccessMintLighter => Brush("PrimaryLightBrush");
    public static Brush SuccessMint => Brush("PrimaryMutedBrush");
    public static Brush OverlayMint => Brush("SelectionFillBrush");

    // Colors
    public static Color ShadowColor => Color("ShadowColor");
    public static Color WidgetInnerGradientStart => Color("WidgetInnerGradientStart");
    public static Color WidgetInnerGradientEnd => Color("WidgetInnerGradientEnd");

    // ===== Radius =====
    public static class Radius
    {
        public static CornerRadius XS => Res("RadiusXS");
        public static CornerRadius SM => Res("RadiusSM");
        public static CornerRadius MD => Res("RadiusMD");
        public static CornerRadius LG => Res("RadiusLG");
        public static CornerRadius XL => Res("RadiusXL");
        public static CornerRadius Full => Res("RadiusFull");

        // Backward compat aliases
        public static CornerRadius XSmall => XS;
        public static CornerRadius Small => SM;
        public static CornerRadius Medium => MD;
        public static CornerRadius Large => LG;
        public static CornerRadius XLarge => XL;
    }

    // ===== Duration =====
    public static class Duration
    {
        public static TimeSpan Fast => Time("DurationFast");
        public static TimeSpan Normal => Time("DurationNormal");
        public static TimeSpan Slow => Time("DurationSlow");
        public static TimeSpan EntranceSpring => Time("DurationEntranceSpring");
        public static TimeSpan EntranceBounce => Time("DurationEntranceBounce");
        public static TimeSpan Hover => Time("DurationHover");
        public static TimeSpan Press => Time("DurationPress");
        public static TimeSpan Exit => Time("DurationExit");
        public static TimeSpan ValueChange => Time("DurationValueChange");
        public static TimeSpan Breath => Time("DurationBreath");

        // Backward compat aliases
        public static TimeSpan EntranceSlide => EntranceSpring;
        public static TimeSpan HoverScale => Hover;
    }

    // ===== Easing =====
    public static class Easing
    {
        public static IEasingFunction EntranceFade => new CircleEase { EasingMode = EasingMode.EaseOut };
        public static IEasingFunction EntranceSpring => new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
        public static IEasingFunction EntranceBounce => new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };
        public static IEasingFunction ExitFade => new CircleEase { EasingMode = EasingMode.EaseIn };
        public static IEasingFunction Hover => new CircleEase { EasingMode = EasingMode.EaseOut };
        public static IEasingFunction Press => new CircleEase { EasingMode = EasingMode.EaseOut };
        public static IEasingFunction ValueChange => new CircleEase { EasingMode = EasingMode.EaseOut };

        // Backward compat aliases
        public static IEasingFunction EntranceOpacity => EntranceFade;
        public static IEasingFunction EntranceSlide => EntranceSpring;
        public static IEasingFunction HoverScale => Hover;
    }

    // ===== Shadow presets =====
    public static class Shadow
    {
        public static System.Windows.Media.Effects.DropShadowEffect XS => new()
        {
            BlurRadius = 4, ShadowDepth = 1, Opacity = 0.06, Color = ShadowColor
        };
        public static System.Windows.Media.Effects.DropShadowEffect SM => new()
        {
            BlurRadius = 8, ShadowDepth = 2, Opacity = 0.08, Color = ShadowColor
        };
        public static System.Windows.Media.Effects.DropShadowEffect MD => new()
        {
            BlurRadius = 16, ShadowDepth = 4, Opacity = 0.12, Color = ShadowColor
        };
        public static System.Windows.Media.Effects.DropShadowEffect LG => new()
        {
            BlurRadius = 24, ShadowDepth = 6, Opacity = 0.16, Color = ShadowColor
        };
        public static System.Windows.Media.Effects.DropShadowEffect XL => new()
        {
            BlurRadius = 32, ShadowDepth = 8, Opacity = 0.20, Color = ShadowColor
        };
    }

    // ===== Helpers =====
    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];
    private static Color Color(string key) => (Color)Application.Current.Resources[key];
    private static CornerRadius Res(string key) => (CornerRadius)Application.Current.Resources[key];
    private static TimeSpan Time(string key) => ((System.Windows.Duration)Application.Current.Resources[key]).TimeSpan;
}
