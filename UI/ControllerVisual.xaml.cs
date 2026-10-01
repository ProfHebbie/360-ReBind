using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReBind360.UI
{
    public partial class ControllerVisual : UserControl
    {
        // Neo-Brutalism Brushes
        private static readonly SolidColorBrush IdleFill = new(Color.FromRgb(42, 42, 56));
        private static readonly SolidColorBrush PressedWhite = new(Color.FromRgb(255, 255, 255));
        private static readonly SolidColorBrush BlackText = new(Color.FromRgb(0, 0, 0));
        private static readonly SolidColorBrush WhiteText = new(Color.FromRgb(255, 255, 255));

        private static readonly SolidColorBrush ColorA = new(Color.FromRgb(0, 255, 133)); // Neo Mint
        private static readonly SolidColorBrush ColorB = new(Color.FromRgb(255, 56, 96));  // Neo Coral
        private static readonly SolidColorBrush ColorX = new(Color.FromRgb(0, 240, 255));  // Neo Cyan
        private static readonly SolidColorBrush ColorY = new(Color.FromRgb(255, 230, 0));  // Neo Yellow

        private static readonly SolidColorBrush StickHeadIdle = new(Color.FromRgb(54, 54, 70));
        private static readonly SolidColorBrush StickHeadActive = new(Color.FromRgb(0, 240, 255));
        private static readonly SolidColorBrush DpadPressed = new(Color.FromRgb(255, 230, 0)); // Neo Yellow
        private static readonly SolidColorBrush DpadIdle = new(Color.FromRgb(42, 42, 56));

        public ControllerVisual()
        {
            InitializeComponent();
        }

        public void UpdateState(
            Dictionary<string, bool> buttons,
            (double x, double y) leftStick,
            (double x, double y) rightStick,
            double leftTrigger,
            double rightTrigger)
        {
            // Face Buttons
            UpdateButton(buttons.GetValueOrDefault("a"), ABtn, AHalo, AText, ColorA);
            UpdateButton(buttons.GetValueOrDefault("b"), BBtn, BHalo, BText, ColorB);
            UpdateButton(buttons.GetValueOrDefault("x"), XBtn, XHalo, XText, ColorX);
            UpdateButton(buttons.GetValueOrDefault("y"), YBtn, YHalo, YText, ColorY);

            // Bumpers
            LbShape.Fill = buttons.GetValueOrDefault("lb") ? PressedWhite : IdleFill;
            RbShape.Fill = buttons.GetValueOrDefault("rb") ? PressedWhite : IdleFill;

            // Triggers (0.0 to 1.0)
            LtProgress.Value = Math.Clamp(leftTrigger, 0.0, 1.0);
            RtProgress.Value = Math.Clamp(rightTrigger, 0.0, 1.0);

            // Back / Start / Guide
            BackBtn.Background = buttons.GetValueOrDefault("back") ? PressedWhite : IdleFill;
            StartBtn.Background = buttons.GetValueOrDefault("start") ? PressedWhite : IdleFill;

            bool guide = buttons.GetValueOrDefault("guide");
            GuideHalo.Opacity = guide ? 0.6 : 0;
            GuideBtn.Fill = guide ? PressedWhite : ColorY;

            // D-Pad Directions
            DpadUp.Fill = buttons.GetValueOrDefault("dpad_up") ? DpadPressed : DpadIdle;
            DpadDown.Fill = buttons.GetValueOrDefault("dpad_down") ? DpadPressed : DpadIdle;
            DpadLeft.Fill = buttons.GetValueOrDefault("dpad_left") ? DpadPressed : DpadIdle;
            DpadRight.Fill = buttons.GetValueOrDefault("dpad_right") ? DpadPressed : DpadIdle;

            // Sticks Movement (UP is positive Y in math/Xbox, which is NEGATIVE dy in screen canvas)
            UpdateStick(leftStick, buttons.GetValueOrDefault("ls"), LeftStickThumb, LsHalo, LsHead, 150, 130);
            UpdateStick(rightStick, buttons.GetValueOrDefault("rs"), RightStickThumb, RsHalo, RsHead, 325, 210);
        }

        private void UpdateButton(bool isPressed, System.Windows.Shapes.Ellipse btn, System.Windows.Shapes.Ellipse halo, TextBlock text, SolidColorBrush activeColor)
        {
            halo.Opacity = isPressed ? 0.6 : 0;
            btn.Fill = isPressed ? PressedWhite : activeColor;
            text.Foreground = BlackText;
        }

        private void UpdateStick((double x, double y) val, bool isClicked, Canvas thumbCanvas, System.Windows.Shapes.Ellipse halo, System.Windows.Shapes.Ellipse head, double originX, double originY)
        {
            // Max deflection is +- 18 pixels
            double dx = Math.Clamp(val.x, -1.0, 1.0) * 18.0;
            // Screen Y is down-positive, so pushing stick UP (+y) moves thumbstick UP (-dy)
            double dy = -Math.Clamp(val.y, -1.0, 1.0) * 18.0;

            Canvas.SetLeft(thumbCanvas, originX + dx);
            Canvas.SetTop(thumbCanvas, originY + dy);

            halo.Opacity = isClicked ? 0.6 : 0;
            head.Fill = isClicked ? StickHeadActive : StickHeadIdle;
        }
    }
}
