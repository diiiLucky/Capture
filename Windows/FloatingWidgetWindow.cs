using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CodexCapture.Models;
using CodexCapture.Services;
using CodexCapture.Theme;
using Wpf.Ui.Controls;

namespace CodexCapture.Windows;

public sealed class FloatingWidgetWindow : Window
{
    private readonly AppController _controller;

    private readonly ScaleTransform _scaleTransform = new(1.0, 1.0);
    private readonly TranslateTransform _translateTransform = new(0, 0);

    private Point? _dragStartScreenPoint;
    private Point _dragStartWindowPoint;
    private bool _dragged;

    public FloatingWidgetWindow(AppController controller, AppSettings settings)
    {
        _controller = controller;
        Width = 54;
        Height = 54;
        Left = settings.FloatingWidgetState.Left;
        Top = settings.FloatingWidgetState.Top;
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = ThemeBrushes.Transparent;
        Opacity = 0;

        var circle = BuildCircle();
        Content = circle;

        Loaded += (_, _) => AnimateEntrance();
        circle.PreviewMouseLeftButtonDown += OnMouseDown;
        circle.PreviewMouseMove += OnMouseMove;
        circle.PreviewMouseLeftButtonUp += OnMouseUp;
    }

    private Border BuildCircle()
    {
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_scaleTransform);
        transformGroup.Children.Add(_translateTransform);

        var circle = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = ThemeBrushes.Radius.Full,
            Background = new LinearGradientBrush(
                ((SolidColorBrush)ThemeBrushes.Primary).Color,
                ((SolidColorBrush)ThemeBrushes.PrimaryHover).Color, 45),
            Cursor = Cursors.Hand,
            ToolTip = "点击截图 / 拖动移动",
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = transformGroup,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new SymbolIcon
            {
                Symbol = SymbolRegular.Crop20,
                FontSize = 22,
                Foreground = ThemeBrushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            }
        };

        circle.MouseEnter += OnMouseEnter;
        circle.MouseLeave += OnMouseLeave;

        return circle;
    }

    #region Mouse Events

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        BeginAnimation(LeftProperty, null);
        BeginAnimation(TopProperty, null);

        _dragStartScreenPoint = PointToScreen(e.GetPosition(this));
        _dragStartWindowPoint = new Point(Left, Top);
        _dragged = false;
        if (sender is Border circle)
        {
            circle.CaptureMouse();
            AnimatePress(circle, 0.94);
        }
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStartScreenPoint is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var currentScreenPoint = PointToScreen(e.GetPosition(this));
        var delta = GetScreenDeltaInDips(_dragStartScreenPoint.Value, currentScreenPoint);

        if (!_dragged)
        {
            if (Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) < 3)
                return;
            _dragged = true;
        }

        Left = _dragStartWindowPoint.X + delta.X;
        Top = _dragStartWindowPoint.Y + delta.Y;
        e.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border circle)
        {
            circle.ReleaseMouseCapture();
            AnimatePress(circle, 1.0);
        }
        _dragStartScreenPoint = null;
        if (!_dragged)
            _controller.BeginCapture();
        e.Handled = true;
    }

    #endregion

    #region Hover & Press

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        AnimateScale(1.08);
        _translateTransform.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(-2, ThemeBrushes.Duration.Hover) { EasingFunction = ThemeBrushes.Easing.Hover });
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        AnimateScale(1.0);
        _translateTransform.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(0, ThemeBrushes.Duration.Hover) { EasingFunction = ThemeBrushes.Easing.Hover });
    }

    #endregion

    #region Animations

    private void AnimateEntrance()
    {
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, ThemeBrushes.Duration.Normal)
        {
            FillBehavior = FillBehavior.Stop,
            EasingFunction = ThemeBrushes.Easing.EntranceFade
        });
        Opacity = 1;

        var targetTop = Top;
        BeginAnimation(TopProperty, new DoubleAnimation(targetTop + 12, targetTop, ThemeBrushes.Duration.EntranceSpring)
        {
            FillBehavior = FillBehavior.Stop,
            EasingFunction = ThemeBrushes.Easing.EntranceSpring
        });
        Top = targetTop;
    }

    private void AnimateScale(double to)
    {
        var animation = new DoubleAnimation(to, ThemeBrushes.Duration.Hover)
        {
            EasingFunction = ThemeBrushes.Easing.Hover
        };
        _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    private static void AnimatePress(Border circle, double target)
    {
        if (circle.RenderTransform is not TransformGroup group || group.Children[0] is not ScaleTransform st)
            return;

        var animation = new DoubleAnimation(target, ThemeBrushes.Duration.Press)
        {
            EasingFunction = ThemeBrushes.Easing.Press
        };
        st.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    #endregion

    #region Helpers

    private Vector GetScreenDeltaInDips(Point startScreenPoint, Point currentScreenPoint)
    {
        var delta = currentScreenPoint - startScreenPoint;
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformFromDevice.Transform(delta) ?? delta;
    }

    #endregion
}
