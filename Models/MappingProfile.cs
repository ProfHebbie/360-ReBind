using System.Collections.Generic;

namespace ReBind360.Models
{
    public class AxisConfig
    {
        public int SourceAxis { get; set; } = -1; // -1 means unbound
        public double Deadzone { get; set; } = 0.15;
        public double Sensitivity { get; set; } = 1.0;
        public bool Invert { get; set; } = false;
    }

    public class MappingProfile
    {
        public string Name { get; set; } = "Default";

        // Button Name -> Physical Button Index (-1 for unmapped)
        public Dictionary<string, int> Buttons { get; set; } = new()
        {
            ["a"] = 0,
            ["b"] = 1,
            ["x"] = 2,
            ["y"] = 3,
            ["lb"] = 4,
            ["rb"] = 5,
            ["back"] = 6,
            ["start"] = 7,
            ["ls"] = 8,
            ["rs"] = 9,
            ["guide"] = 10,
            ["dpad_up"] = -1,
            ["dpad_down"] = -1,
            ["dpad_left"] = -1,
            ["dpad_right"] = -1,
        };

        public Dictionary<string, int> KeyboardButtons { get; set; } = new()
        {
            ["a"] = 0x20,
            ["b"] = 0x11,
            ["x"] = 0x45,
            ["y"] = 0x51,
            ["lb"] = 0x10,
            ["rb"] = 0x52,
            ["back"] = 0x09,
            ["start"] = 0x0D,
            ["ls"] = 0x43,
            ["rs"] = 0x56,
            ["guide"] = 0x70,
            ["dpad_up"] = 0x26,
            ["dpad_down"] = 0x28,
            ["dpad_left"] = 0x25,
            ["dpad_right"] = 0x27,
        };

        public AxisConfig LeftStickX { get; set; } = new() { SourceAxis = 0, Invert = false };
        public AxisConfig LeftStickY { get; set; } = new() { SourceAxis = 1, Invert = true };
        public AxisConfig RightStickX { get; set; } = new() { SourceAxis = 2, Invert = false };
        public AxisConfig RightStickY { get; set; } = new() { SourceAxis = 3, Invert = true };

        // Triggers can be analog axes or digital buttons
        public AxisConfig LeftTrigger { get; set; } = new() { SourceAxis = 4, Invert = false };
        public AxisConfig RightTrigger { get; set; } = new() { SourceAxis = 5, Invert = false };
        public int LeftTriggerButton { get; set; } = -1;
        public int RightTriggerButton { get; set; } = -1;

        public string DPadMode { get; set; } = "hat"; // "hat" or "buttons"
        public int DPadFromHat { get; set; } = 0;

        public void ApplyDirectInputDefaults()
        {
            Buttons["a"] = 0;
            Buttons["b"] = 1;
            Buttons["x"] = 2;
            Buttons["y"] = 3;
            Buttons["lb"] = 4;
            Buttons["rb"] = 5;
            Buttons["back"] = 6;
            Buttons["start"] = 7;
            Buttons["ls"] = 8;
            Buttons["rs"] = 9;
            Buttons["guide"] = 10;
            Buttons["dpad_up"] = -1;
            Buttons["dpad_down"] = -1;
            Buttons["dpad_left"] = -1;
            Buttons["dpad_right"] = -1;

            LeftStickX = new() { SourceAxis = 0, Invert = false };
            LeftStickY = new() { SourceAxis = 1, Invert = true };
            RightStickX = new() { SourceAxis = 2, Invert = false };
            RightStickY = new() { SourceAxis = 3, Invert = true };

            LeftTrigger = new() { SourceAxis = 4, Invert = false };
            RightTrigger = new() { SourceAxis = 5, Invert = false };
            LeftTriggerButton = -1;
            RightTriggerButton = -1;

            DPadMode = "hat";
            DPadFromHat = 0;
        }

        public void ApplyPlayStationDefaults()
        {
            // PS4/PS5 controllers via DirectInput:
            // Cross=1, Circle=2, Square=0, Triangle=3, L1=4, R1=5, L2=6, R2=7, Share=8, Options=9, L3=10, R3=11, PS=12
            Buttons["a"] = 1; // Cross
            Buttons["b"] = 2; // Circle
            Buttons["x"] = 0; // Square
            Buttons["y"] = 3; // Triangle
            Buttons["lb"] = 4; // L1
            Buttons["rb"] = 5; // R1
            Buttons["back"] = 8; // Share
            Buttons["start"] = 9; // Options
            Buttons["ls"] = 10; // L3
            Buttons["rs"] = 11; // R3
            Buttons["guide"] = 12; // PS button

            LeftStickX = new() { SourceAxis = 0, Invert = false };
            LeftStickY = new() { SourceAxis = 1, Invert = true };
            RightStickX = new() { SourceAxis = 2, Invert = false };
            RightStickY = new() { SourceAxis = 5, Invert = true };

            LeftTrigger = new() { SourceAxis = 3, Invert = false };
            RightTrigger = new() { SourceAxis = 4, Invert = false };
            LeftTriggerButton = 6; // L2
            RightTriggerButton = 7; // R2

            DPadMode = "hat";
            DPadFromHat = 0;
        }
    }
}
