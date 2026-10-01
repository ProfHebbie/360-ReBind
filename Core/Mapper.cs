using System;
using System.Collections.Generic;
using ReBind360.Models;

namespace ReBind360.Core
{
    public class Mapper
    {
        public MappingProfile Profile { get; set; }

        public Mapper(MappingProfile profile)
        {
            Profile = profile ?? new MappingProfile();
        }

        private double ProcessAxis(ControllerState state, AxisConfig config)
        {
            if (config.SourceAxis < 0 || !state.Axes.TryGetValue(config.SourceAxis, out double raw))
                return 0.0;

            if (config.Invert)
            {
                raw = -raw;
            }

            double absRaw = Math.Abs(raw);
            if (absRaw < config.Deadzone)
                return 0.0;

            // Scale remaining range
            double magnitude = (absRaw - config.Deadzone) / (1.0 - config.Deadzone);
            magnitude = Math.Min(1.0, magnitude * config.Sensitivity);

            return raw > 0 ? magnitude : -magnitude;
        }

        private double ProcessTrigger(ControllerState state, AxisConfig axisConfig, int buttonIndex)
        {
            // If button is assigned and pressed, full 1.0 trigger
            if (buttonIndex >= 0 && state.Buttons.GetValueOrDefault(buttonIndex, false))
            {
                return 1.0;
            }

            if (axisConfig.SourceAxis < 0 || !state.Axes.TryGetValue(axisConfig.SourceAxis, out double raw))
                return 0.0;

            // On many gamepads, resting trigger is -1.0 or 0.0, pressed is 1.0
            if (raw < 0)
            {
                raw = (raw + 1.0) / 2.0; // convert -1..1 to 0..1
            }

            if (axisConfig.Invert)
            {
                raw = 1.0 - raw;
            }

            if (raw < axisConfig.Deadzone)
                return 0.0;

            double magnitude = (raw - axisConfig.Deadzone) / (1.0 - axisConfig.Deadzone);
            return Math.Clamp(magnitude * axisConfig.Sensitivity, 0.0, 1.0);
        }

        public (Dictionary<string, bool> Buttons, (double x, double y) LeftStick, (double x, double y) RightStick, double LeftTrigger, double RightTrigger) Apply(ControllerState state)
        {
            var buttons = new Dictionary<string, bool>();

            foreach (var kvp in Profile.Buttons)
            {
                int buttonIndex = state.Id == "keyboard"
                    ? Profile.KeyboardButtons.GetValueOrDefault(kvp.Key, -1)
                    : kvp.Value;
                if (buttonIndex >= 0)
                {
                    buttons[kvp.Key] = state.Buttons.GetValueOrDefault(buttonIndex, false);
                }
                else
                {
                    buttons[kvp.Key] = false;
                }
            }

            // D-Pad Hat Mode
            if (Profile.DPadMode == "hat" && state.Hats.TryGetValue(Profile.DPadFromHat, out var hat))
            {
                buttons["dpad_left"] = hat.x == -1;
                buttons["dpad_right"] = hat.x == 1;
                buttons["dpad_up"] = hat.y == 1;
                buttons["dpad_down"] = hat.y == -1;
            }

            double lx = ProcessAxis(state, Profile.LeftStickX);
            double ly = ProcessAxis(state, Profile.LeftStickY);
            double rx = ProcessAxis(state, Profile.RightStickX);
            double ry = ProcessAxis(state, Profile.RightStickY);

            double lt = ProcessTrigger(state, Profile.LeftTrigger, Profile.LeftTriggerButton);
            double rt = ProcessTrigger(state, Profile.RightTrigger, Profile.RightTriggerButton);

            return (buttons, (lx, ly), (rx, ry), lt, rt);
        }
    }
}
