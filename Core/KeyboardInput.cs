using System.Runtime.InteropServices;
using ReBind360.Models;

namespace ReBind360.Core
{
    public sealed class KeyboardInput
    {
        private const int VkLeft = 0x25;
        private const int VkUp = 0x26;
        private const int VkRight = 0x27;
        private const int VkDown = 0x28;
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        private static bool IsDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

        public ControllerState GetState()
        {
            var state = new ControllerState { Name = "Keyboard", Id = "keyboard" };
            for (int keyCode = 7; keyCode <= 255; keyCode++)
                state.Buttons[keyCode] = IsDown(keyCode);

            state.Axes[0] = (IsDown('D') ? 1.0 : 0.0) - (IsDown('A') ? 1.0 : 0.0);
            state.Axes[1] = (IsDown('S') ? 1.0 : 0.0) - (IsDown('W') ? 1.0 : 0.0);
            state.Axes[2] = (IsDown('L') ? 1.0 : 0.0) - (IsDown('J') ? 1.0 : 0.0);
            state.Axes[3] = (IsDown('K') ? 1.0 : 0.0) - (IsDown('I') ? 1.0 : 0.0);
            state.Axes[4] = IsDown('F') ? 1.0 : -1.0;
            state.Axes[5] = IsDown('G') ? 1.0 : -1.0;
            state.Hats[0] = (
                (IsDown(VkRight) ? 1 : 0) - (IsDown(VkLeft) ? 1 : 0),
                (IsDown(VkUp) ? 1 : 0) - (IsDown(VkDown) ? 1 : 0));
            return state;
        }
    }
}