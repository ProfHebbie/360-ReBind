using System;
using System.Collections.Generic;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace ReBind360.Core
{
    public class VirtualController
    {
        private ViGEmClient? _client;
        private IXbox360Controller? _controller;
        public bool IsConnected => _controller != null;

        public bool Connect()
        {
            try
            {
                if (_client == null)
                    _client = new ViGEmClient();

                if (_controller == null)
                {
                    var controller = _client.CreateXbox360Controller();
                    controller.Connect();
                    _controller = controller;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VirtualController] Failed to connect: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            var controller = _controller;
            _controller = null;
            if (controller != null)
            {
                try
                {
                    controller.Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VirtualController] Failed to disconnect: {ex.Message}");
                }
            }
        }

        public void Update(
            Dictionary<string, bool> buttons,
            (double x, double y) leftStick,
            (double x, double y) rightStick,
            double leftTrigger,
            double rightTrigger)
        {
            if (_controller == null) return;

            try
            {
                _controller.SetButtonState(Xbox360Button.A, buttons.GetValueOrDefault("a"));
                _controller.SetButtonState(Xbox360Button.B, buttons.GetValueOrDefault("b"));
                _controller.SetButtonState(Xbox360Button.X, buttons.GetValueOrDefault("x"));
                _controller.SetButtonState(Xbox360Button.Y, buttons.GetValueOrDefault("y"));
                _controller.SetButtonState(Xbox360Button.LeftShoulder, buttons.GetValueOrDefault("lb"));
                _controller.SetButtonState(Xbox360Button.RightShoulder, buttons.GetValueOrDefault("rb"));
                _controller.SetButtonState(Xbox360Button.Back, buttons.GetValueOrDefault("back"));
                _controller.SetButtonState(Xbox360Button.Start, buttons.GetValueOrDefault("start"));
                _controller.SetButtonState(Xbox360Button.Guide, buttons.GetValueOrDefault("guide"));
                _controller.SetButtonState(Xbox360Button.LeftThumb, buttons.GetValueOrDefault("ls"));
                _controller.SetButtonState(Xbox360Button.RightThumb, buttons.GetValueOrDefault("rs"));
                _controller.SetButtonState(Xbox360Button.Up, buttons.GetValueOrDefault("dpad_up"));
                _controller.SetButtonState(Xbox360Button.Down, buttons.GetValueOrDefault("dpad_down"));
                _controller.SetButtonState(Xbox360Button.Left, buttons.GetValueOrDefault("dpad_left"));
                _controller.SetButtonState(Xbox360Button.Right, buttons.GetValueOrDefault("dpad_right"));

                short ToAxis(double val) => (short)(Math.Max(-1.0, Math.Min(1.0, val)) * (val < 0 ? 32768 : 32767));
                byte ToTrigger(double val) => (byte)(Math.Max(0.0, Math.Min(1.0, val)) * 255);

                _controller.SetAxisValue(Xbox360Axis.LeftThumbX, ToAxis(leftStick.x));
                _controller.SetAxisValue(Xbox360Axis.LeftThumbY, ToAxis(leftStick.y));
                _controller.SetAxisValue(Xbox360Axis.RightThumbX, ToAxis(rightStick.x));
                _controller.SetAxisValue(Xbox360Axis.RightThumbY, ToAxis(rightStick.y));

                _controller.SetSliderValue(Xbox360Slider.LeftTrigger, ToTrigger(leftTrigger));
                _controller.SetSliderValue(Xbox360Slider.RightTrigger, ToTrigger(rightTrigger));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VirtualController] Failed to update: {ex.Message}");
                Disconnect();
            }
        }

        public void Reset()
        {
            if (_controller == null) return;
            Update(new Dictionary<string, bool>(), (0, 0), (0, 0), 0, 0);
        }
    }
}
