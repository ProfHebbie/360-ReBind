using System.Collections.Generic;
using System.Linq;

namespace ReBind360.Models
{
    public class ControllerState
    {
        public Dictionary<int, bool> Buttons { get; set; } = new();
        public Dictionary<int, double> Axes { get; set; } = new();
        public Dictionary<int, (int x, int y)> Hats { get; set; } = new();
        public string Name { get; set; } = "Unknown";
        public string Id { get; set; } = string.Empty;

        public bool IsAnyInput()
        {
            return Buttons.Values.Any(b => b) || Axes.Values.Any(v => Math.Abs(v) > 0.1);
        }
    }
}
