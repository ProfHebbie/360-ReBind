using System;
using System.Collections.Generic;
using System.Linq;
using ReBind360.Models;

namespace ReBind360.Core
{
    public class CaptureResult
    {
        public string TargetControl { get; set; } = string.Empty;
        public string Kind { get; set; } = "button"; // "button", "axis", "hat"
        public int Index { get; set; } = 0;
        public int Direction { get; set; } = 0; // for hat: -1 or 1
    }

    public class InputCapture
    {
        public const double AxisThreshold = 0.55;

        private HashSet<int> _baselineButtons = new();
        private Dictionary<int, double> _baselineAxes = new();
        private Dictionary<int, (int x, int y)> _baselineHats = new();

        public bool IsActive { get; private set; } = false;
        public string? TargetControl { get; private set; }

        public void Start(ControllerState currentState, string targetControl)
        {
            TargetControl = targetControl;
            _baselineButtons = new HashSet<int>(currentState.Buttons.Where(b => b.Value).Select(b => b.Key));
            _baselineAxes = new Dictionary<int, double>(currentState.Axes);
            _baselineHats = new Dictionary<int, (int x, int y)>(currentState.Hats.Where(h => h.Value.x != 0 || h.Value.y != 0));
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
            TargetControl = null;
        }

        public CaptureResult? Poll(ControllerState currentState)
        {
            if (!IsActive || string.IsNullOrEmpty(TargetControl)) return null;

            bool acceptsAxis = TargetControl is "left_stick_x" or "left_stick_y" or "right_stick_x" or "right_stick_y" or "lt" or "rt";
            bool acceptsButton = !acceptsAxis || TargetControl is "lt" or "rt";
            bool acceptsHat = TargetControl.StartsWith("dpad_", StringComparison.Ordinal);

            // Check buttons
            if (acceptsButton)
            {
                foreach (var kvp in currentState.Buttons)
                {
                    if (kvp.Value && !_baselineButtons.Contains(kvp.Key))
                    {
                        var result = new CaptureResult { TargetControl = TargetControl, Kind = "button", Index = kvp.Key };
                        Stop();
                        return result;
                    }
                }
            }

            // Check axes
            if (acceptsAxis)
            {
                foreach (var kvp in currentState.Axes)
                {
                    double baseline = _baselineAxes.GetValueOrDefault(kvp.Key, 0.0);
                    double movement = kvp.Value - baseline;
                    if (Math.Abs(movement) > AxisThreshold && Math.Abs(kvp.Value) > AxisThreshold)
                    {
                        var result = new CaptureResult { TargetControl = TargetControl, Kind = "axis", Index = kvp.Key, Direction = movement > 0 ? 1 : -1 };
                        Stop();
                        return result;
                    }
                }
            }

            // Check hats
            if (acceptsHat)
            {
                foreach (var kvp in currentState.Hats)
                {
                    if ((kvp.Value.x != 0 || kvp.Value.y != 0) &&
                        (!_baselineHats.TryGetValue(kvp.Key, out var baseHat) || baseHat != kvp.Value))
                    {
                        var result = new CaptureResult { TargetControl = TargetControl, Kind = "hat", Index = kvp.Key };
                        Stop();
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
