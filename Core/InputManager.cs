using System;
using System.Collections.Generic;
using System.Linq;
using ReBind360.Models;
using SharpDX.DirectInput;

namespace ReBind360.Core
{
    public class InputManager : IDisposable
    {
        private readonly DirectInput _directInput;
        private readonly Dictionary<Guid, Joystick> _joysticks = new();

        public InputManager()
        {
            _directInput = new DirectInput();
        }

        public List<(Guid Id, string Name)> GetDeviceList()
        {
            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            return devices.Select(d => (d.InstanceGuid, d.InstanceName)).ToList();
        }

        public void SetActiveDevice(Guid id)
        {
            if (_joysticks.ContainsKey(id)) return;
            Joystick? joystick = null;
            try
            {
                joystick = new Joystick(_directInput, id);

                // Set axis range to standardized -32768 to 32767 across all axes
                try
                {
                    foreach (var deviceObject in joystick.GetObjects())
                    {
                        if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
                        {
                            try
                            {
                                joystick.GetObjectPropertiesById(deviceObject.ObjectId).Range = new InputRange(-32768, 32767);
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                joystick.Acquire();
                _joysticks[id] = joystick;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InputManager] Failed to acquire joystick: {ex.Message}");
                if (joystick != null)
                {
                    try { joystick.Unacquire(); } catch { }
                    joystick.Dispose();
                }
            }
        }

        public ControllerState GetState(Guid? id)
        {
            var state = new ControllerState();
            if (id == null) return state;
            if (!_joysticks.ContainsKey(id.Value)) SetActiveDevice(id.Value);
            if (!_joysticks.TryGetValue(id.Value, out var joystick)) return state;

            try
            {
                joystick.Poll();
                var data = joystick.GetCurrentState();

                state.Name = joystick.Information.InstanceName;
                state.Id = id.Value.ToString();

                // Buttons (read up to 128 buttons)
                var buttons = data.Buttons;
                for (int i = 0; i < buttons.Length; i++)
                {
                    state.Buttons[i] = buttons[i];
                }

                // Normalized axes (-1.0 to +1.0)
                double Norm(int raw)
                {
                    double v = Math.Clamp(raw / 32768.0, -1.0, 1.0);
                    return Math.Abs(v) < 0.02 ? 0.0 : v;
                }

                state.Axes[0] = Norm(data.X);
                state.Axes[1] = Norm(data.Y);
                state.Axes[2] = Norm(data.Z);
                state.Axes[3] = Norm(data.RotationX);
                state.Axes[4] = Norm(data.RotationY);
                state.Axes[5] = Norm(data.RotationZ);

                if (data.Sliders != null)
                {
                    if (data.Sliders.Length > 0) state.Axes[6] = Norm(data.Sliders[0]);
                    if (data.Sliders.Length > 1) state.Axes[7] = Norm(data.Sliders[1]);
                }

                // Hats (POV Controllers)
                var pov = data.PointOfViewControllers;
                for (int i = 0; i < pov.Length; i++)
                {
                    int povVal = pov[i];
                    if (povVal == -1)
                    {
                        state.Hats[i] = (0, 0);
                    }
                    else
                    {
                        int hx = 0;
                        int hy = 0;
                        // POV is in hundredths of degree (0 = Up, 9000 = Right, 18000 = Down, 27000 = Left)
                        if (povVal >= 4500 && povVal <= 13500) hx = 1;
                        else if (povVal >= 22500 && povVal <= 31500) hx = -1;

                        if (povVal <= 4500 || povVal >= 31500) hy = 1;      // Up
                        else if (povVal >= 13500 && povVal <= 22500) hy = -1; // Down

                        state.Hats[i] = (hx, hy);
                    }
                }
            }
            catch (SharpDX.SharpDXException)
            {
                // Device disconnected or lost
                try { joystick.Unacquire(); } catch { }
                try { joystick.Dispose(); } catch { }
                _joysticks.Remove(id.Value);
            }

            return state;
        }

        public void Dispose()
        {
            foreach (var joystick in _joysticks.Values)
            {
                try { joystick.Unacquire(); } catch { }
                try { joystick.Dispose(); } catch { }
            }
            _joysticks.Clear();
            _directInput.Dispose();
        }
    }
}
