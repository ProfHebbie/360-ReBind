using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ReBind360.Core;
using ReBind360.Models;

namespace ReBind360.UI
{
    public partial class MainWindow : Window
    {
        private readonly InputManager _inputManager;
        private readonly VirtualController[] _virtualControllers = { new(), new(), new() };
        private readonly Mapper[] _mappers;
        private Mapper _mapper;
        private readonly ProfileManager _profileManager;
        private readonly InputCapture _inputCapture;
        private readonly KeyboardInput _keyboardInput = new();
        private readonly DispatcherTimer _loopTimer;
        private bool _isRunning = false;
        private bool _updatingDeviceMenu;
        private bool _hasInitializedSources;
        private int _selectedSlot;
        private readonly Guid?[] _slotDevices = new Guid?[3];
        private readonly bool[] _slotKeyboard = new bool[3];
        private readonly List<InputSourceOption> _inputSources = new();

        private sealed record InputSourceOption(string Name, Guid? DeviceId, bool IsKeyboard);

        private readonly Dictionary<string, (TextBlock TargetLabel, TextBlock BindingBadge, Button ListenBtn)> _rebindRows = new();

        private static readonly (string Key, string Label, bool IsAxis)[] ControlDefinitions = new[]
        {
            ("a", "Button A", false),
            ("b", "Button B", false),
            ("x", "Button X", false),
            ("y", "Button Y", false),
            ("lb", "Left Bumper (LB)", false),
            ("rb", "Right Bumper (RB)", false),
            ("lt", "Left Trigger (LT)", true),
            ("rt", "Right Trigger (RT)", true),
            ("dpad_up", "D-Pad Up", false),
            ("dpad_down", "D-Pad Down", false),
            ("dpad_left", "D-Pad Left", false),
            ("dpad_right", "D-Pad Right", false),
            ("ls", "Left Stick Click (LS)", false),
            ("rs", "Right Stick Click (RS)", false),
            ("back", "Back / Select", false),
            ("start", "Start", false),
            ("guide", "Xbox Guide", false),
            ("left_stick_x", "Left Stick Horizontal (X)", true),
            ("left_stick_y", "Left Stick Vertical (Y)", true),
            ("right_stick_x", "Right Stick Horizontal (X)", true),
            ("right_stick_y", "Right Stick Vertical (Y)", true),
        };

        public MainWindow()
        {
            InitializeComponent();

            _inputManager = new InputManager();
            _profileManager = new ProfileManager();
            _inputCapture = new InputCapture();

            _mappers = new[]
            {
                new Mapper(_profileManager.Load("Default")),
                new Mapper(_profileManager.Load("Slot2")),
                new Mapper(_profileManager.Load("Slot3"))
            };
            _mapper = _mappers[0];
            SlotMenu.ItemsSource = new[] { "SLOT 1", "SLOT 2", "SLOT 3" };
            SlotMenu.SelectedIndex = 0;

            BuildRebindUi();
            SyncSettingsToUi();
            RefreshDevices();

            // High-frequency render loop (~60-120 FPS = 16ms)
            _loopTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
            _loopTimer.Tick += GameLoop;
            _loopTimer.Start();
        }

        private void SyncSettingsToUi()
        {
            // Sync Inversion Checkboxes
            ChkInvertLeftY.IsChecked = _mapper.Profile.LeftStickY.Invert;
            ChkInvertLeftX.IsChecked = _mapper.Profile.LeftStickX.Invert;
            ChkInvertRightY.IsChecked = _mapper.Profile.RightStickY.Invert;
            ChkInvertRightX.IsChecked = _mapper.Profile.RightStickX.Invert;
            ChkInvertLT.IsChecked = _mapper.Profile.LeftTrigger.Invert;
            ChkInvertRT.IsChecked = _mapper.Profile.RightTrigger.Invert;

            DeadzoneSlider.Value = _mapper.Profile.LeftStickX.Deadzone;
            DeadzoneVal.Text = $"{_mapper.Profile.LeftStickX.Deadzone:F2}";

            SensitivitySlider.Value = _mapper.Profile.LeftStickX.Sensitivity;
            SensitivityVal.Text = $"{_mapper.Profile.LeftStickX.Sensitivity:F2}";

            RefreshRebindBadges();
        }

        private void BuildRebindUi()
        {
            RebindListContainer.Children.Clear();
            _rebindRows.Clear();

            foreach (var (key, label, isAxis) in ControlDefinitions)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 40)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                var targetText = new TextBlock
                {
                    Text = label.ToUpper(),
                    FontWeight = FontWeights.Black,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(targetText, 0);

                var badgeBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8, 3, 8, 3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var badgeText = new TextBlock
                {
                    Text = "UNBOUND",
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 240, 255))
                };
                badgeBorder.Child = badgeText;
                Grid.SetColumn(badgeBorder, 1);

                var listenBtn = new Button
                {
                    Content = "LISTEN",
                    FontWeight = FontWeights.Black,
                    FontSize = 10,
                    Background = new SolidColorBrush(Color.FromRgb(255, 230, 0)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    BorderThickness = new Thickness(2),
                    Height = 28,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                string capturedKey = key;
                listenBtn.Click += (s, e) => StartListen(capturedKey);
                Grid.SetColumn(listenBtn, 2);

                var clearBtn = new Button
                {
                    Content = "CLEAR",
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Background = new SolidColorBrush(Color.FromRgb(255, 56, 96)),
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    BorderThickness = new Thickness(2),
                    Height = 28,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                clearBtn.Click += (s, e) => ClearBinding(capturedKey);
                Grid.SetColumn(clearBtn, 3);

                grid.Children.Add(targetText);
                grid.Children.Add(badgeBorder);
                grid.Children.Add(listenBtn);
                grid.Children.Add(clearBtn);

                card.Child = grid;
                RebindListContainer.Children.Add(card);

                _rebindRows[key] = (targetText, badgeText, listenBtn);
            }

            RefreshRebindBadges();
        }

        private void RefreshRebindBadges()
        {
            foreach (var (key, _, _) in ControlDefinitions)
            {
                if (!_rebindRows.TryGetValue(key, out var row)) continue;

                string bindingDesc = "UNBOUND";
                var p = _mapper.Profile;

                if (key == "left_stick_x") bindingDesc = p.LeftStickX.SourceAxis >= 0 ? $"Axis {p.LeftStickX.SourceAxis} {(p.LeftStickX.Invert ? "(Inv)" : "")}" : "Unbound";
                else if (key == "left_stick_y") bindingDesc = p.LeftStickY.SourceAxis >= 0 ? $"Axis {p.LeftStickY.SourceAxis} {(p.LeftStickY.Invert ? "(Inv)" : "")}" : "Unbound";
                else if (key == "right_stick_x") bindingDesc = p.RightStickX.SourceAxis >= 0 ? $"Axis {p.RightStickX.SourceAxis} {(p.RightStickX.Invert ? "(Inv)" : "")}" : "Unbound";
                else if (key == "right_stick_y") bindingDesc = p.RightStickY.SourceAxis >= 0 ? $"Axis {p.RightStickY.SourceAxis} {(p.RightStickY.Invert ? "(Inv)" : "")}" : "Unbound";
                else if (key == "lt")
                {
                    if (p.LeftTriggerButton >= 0) bindingDesc = $"Button {p.LeftTriggerButton}";
                    else if (p.LeftTrigger.SourceAxis >= 0) bindingDesc = $"Axis {p.LeftTrigger.SourceAxis}";
                    else bindingDesc = "Unbound";
                }
                else if (key == "rt")
                {
                    if (p.RightTriggerButton >= 0) bindingDesc = $"Button {p.RightTriggerButton}";
                    else if (p.RightTrigger.SourceAxis >= 0) bindingDesc = $"Axis {p.RightTrigger.SourceAxis}";
                    else bindingDesc = "Unbound";
                }
                else if (key.StartsWith("dpad_", StringComparison.Ordinal) && p.DPadMode == "hat")
                {
                    bindingDesc = $"Hat {p.DPadFromHat}";
                }
                else
                {
                    int btnIdx = _slotKeyboard[_selectedSlot]
                        ? p.KeyboardButtons.GetValueOrDefault(key, -1)
                        : p.Buttons.GetValueOrDefault(key, -1);
                    bindingDesc = btnIdx >= 0
                        ? _slotKeyboard[_selectedSlot]
                            ? System.Windows.Input.KeyInterop.KeyFromVirtualKey(btnIdx).ToString().ToUpperInvariant()
                            : $"Button {btnIdx}"
                        : "Unbound";
                }

                row.BindingBadge.Text = bindingDesc.ToUpper();
                row.BindingBadge.Foreground = bindingDesc == "Unbound" 
                    ? new SolidColorBrush(Color.FromRgb(150, 150, 160)) 
                    : new SolidColorBrush(Color.FromRgb(0, 240, 255));
            }
        }

        private void StartListen(string key)
        {
            var currentState = GetSelectedInputState();
            _inputCapture.Start(currentState, key);

            // Update button texts
            foreach (var kvp in _rebindRows)
            {
                if (kvp.Key == key)
                {
                    kvp.Value.ListenBtn.Content = "PRESS NOW...";
                    kvp.Value.ListenBtn.Background = new SolidColorBrush(Color.FromRgb(255, 230, 0));
                }
                else
                {
                    kvp.Value.ListenBtn.Content = "LISTEN";
                    kvp.Value.ListenBtn.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                }
            }
            string prompt = key.EndsWith("_x", StringComparison.Ordinal) ? "Push the stick right" :
                key.EndsWith("_y", StringComparison.Ordinal) ? "Push the stick up" :
                key.StartsWith("dpad_", StringComparison.Ordinal) ? "Press a D-pad direction or key" : "Press a button or key";
            InfoLabel.Text = $"{prompt} to bind {key.ToUpper()}...";
        }

        private void ClearBinding(string key)
        {
            var p = _mapper.Profile;
            if (key == "left_stick_x") p.LeftStickX.SourceAxis = -1;
            else if (key == "left_stick_y") p.LeftStickY.SourceAxis = -1;
            else if (key == "right_stick_x") p.RightStickX.SourceAxis = -1;
            else if (key == "right_stick_y") p.RightStickY.SourceAxis = -1;
            else if (key == "lt") { p.LeftTrigger.SourceAxis = -1; p.LeftTriggerButton = -1; }
            else if (key == "rt") { p.RightTrigger.SourceAxis = -1; p.RightTriggerButton = -1; }
            else
            {
                if (key.StartsWith("dpad_", StringComparison.Ordinal)) p.DPadMode = "buttons";
                p.Buttons[key] = -1;
                p.KeyboardButtons[key] = -1;
            }

            CancelInputCapture();
            _profileManager.Save(p);
            RefreshRebindBadges();
        }

        private void ApplyCaptureResult(CaptureResult result)
        {
            string target = result.TargetControl;
            var p = _mapper.Profile;

            if (target == "left_stick_x" && result.Kind == "axis") { p.LeftStickX.SourceAxis = result.Index; p.LeftStickX.Invert = result.Direction < 0; }
            else if (target == "left_stick_y" && result.Kind == "axis") { p.LeftStickY.SourceAxis = result.Index; p.LeftStickY.Invert = result.Direction < 0; }
            else if (target == "right_stick_x" && result.Kind == "axis") { p.RightStickX.SourceAxis = result.Index; p.RightStickX.Invert = result.Direction < 0; }
            else if (target == "right_stick_y" && result.Kind == "axis") { p.RightStickY.SourceAxis = result.Index; p.RightStickY.Invert = result.Direction < 0; }
            else if (target == "lt")
            {
                if (result.Kind == "axis") { p.LeftTrigger.SourceAxis = result.Index; p.LeftTriggerButton = -1; }
                else if (result.Kind == "button") { p.LeftTriggerButton = result.Index; p.LeftTrigger.SourceAxis = -1; }
            }
            else if (target == "rt")
            {
                if (result.Kind == "axis") { p.RightTrigger.SourceAxis = result.Index; p.RightTriggerButton = -1; }
                else if (result.Kind == "button") { p.RightTriggerButton = result.Index; p.RightTrigger.SourceAxis = -1; }
            }
            else
            {
                if (target.StartsWith("dpad_", StringComparison.Ordinal) && result.Kind == "hat")
                {
                    p.DPadMode = "hat";
                    p.DPadFromHat = result.Index;
                }
                else if (target.StartsWith("dpad_", StringComparison.Ordinal) && result.Kind == "button")
                {
                    p.DPadMode = "buttons";
                    if (_slotKeyboard[_selectedSlot]) p.KeyboardButtons[target] = result.Index;
                    else p.Buttons[target] = result.Index;
                }
                else if (result.Kind == "button")
                {
                    if (_slotKeyboard[_selectedSlot]) p.KeyboardButtons[target] = result.Index;
                    else p.Buttons[target] = result.Index;
                }
            }

            _profileManager.Save(p);
            SyncSettingsToUi();

            // Reset Listen Buttons
            foreach (var kvp in _rebindRows)
            {
                kvp.Value.ListenBtn.Content = "LISTEN";
                kvp.Value.ListenBtn.Background = new SolidColorBrush(Color.FromRgb(255, 230, 0));
            }

            InfoLabel.Text = $"Bound {target.ToUpper()} successfully!";
        }

        private void RefreshDevices()
        {
            var devices = _inputManager.GetDeviceList();
            if (!_hasInitializedSources)
            {
                if (devices.Count > 0) _slotDevices[0] = devices[0].Id;
                _slotKeyboard[1] = true;
                if (devices.Count > 1) _slotDevices[2] = devices[1].Id;
                _hasInitializedSources = true;
            }

            _inputSources.Clear();
            _inputSources.Add(new InputSourceOption("No input", null, false));
            _inputSources.Add(new InputSourceOption("Keyboard", null, true));
            _inputSources.AddRange(devices.Select(device => new InputSourceOption(device.Name, device.Id, false)));

            _updatingDeviceMenu = true;
            DeviceMenu.ItemsSource = _inputSources.ToList();
            DeviceMenu.DisplayMemberPath = nameof(InputSourceOption.Name);
            SelectCurrentInputSource();
            _updatingDeviceMenu = false;

            if (devices.Count == 0)
            {
                InfoLabel.Text = "No physical controllers found. Keyboard input is available for any slot.";
            }
            else InfoLabel.Text = $"Detected {devices.Count} physical controller(s).";
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }

        private void Selector_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void Selector_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        private void Selector_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void DeviceMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingDeviceMenu || DeviceMenu.SelectedItem is not InputSourceOption source) return;
            if (_inputCapture.IsActive)
            {
                CancelInputCapture();
            }
            _slotDevices[_selectedSlot] = source.DeviceId;
            _slotKeyboard[_selectedSlot] = source.IsKeyboard;
            RefreshRebindBadges();
            InfoLabel.Text = source.IsKeyboard
                ? $"Keyboard slot {_selectedSlot + 1}: WASD/IJKL sticks, arrows D-pad, Space/Ctrl/E/Q, Shift/R, F/G."
                : $"{source.Name} assigned to slot {_selectedSlot + 1}.";
        }

        private void SlotMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SlotMenu.SelectedIndex < 0 || SlotMenu.SelectedIndex >= _mappers.Length) return;
            if (_inputCapture.IsActive)
            {
                CancelInputCapture();
            }
            _selectedSlot = SlotMenu.SelectedIndex;
            _mapper = _mappers[_selectedSlot];
            _updatingDeviceMenu = true;
            SelectCurrentInputSource();
            _updatingDeviceMenu = false;
            SyncSettingsToUi();
            InfoLabel.Text = $"Editing slot {_selectedSlot + 1}.";
        }

        private void SelectCurrentInputSource()
        {
            int sourceIndex = _inputSources.FindIndex(source =>
                _slotKeyboard[_selectedSlot]
                    ? source.IsKeyboard
                    : !source.IsKeyboard && source.DeviceId == _slotDevices[_selectedSlot]);
            DeviceMenu.SelectedIndex = sourceIndex >= 0 ? sourceIndex : 0;
        }

        private ControllerState GetSelectedInputState()
        {
            return _slotKeyboard[_selectedSlot]
                ? _keyboardInput.GetState()
                : _inputManager.GetState(_slotDevices[_selectedSlot]);
        }

        private void CancelInputCapture()
        {
            _inputCapture.Stop();
            foreach (var row in _rebindRows.Values)
            {
                row.ListenBtn.Content = "LISTEN";
                row.ListenBtn.Background = new SolidColorBrush(Color.FromRgb(255, 230, 0));
            }
        }

        private void PresetDefault_Click(object sender, RoutedEventArgs e)
        {
            CancelInputCapture();
            _mapper.Profile.ApplyDirectInputDefaults();
            _profileManager.Save(_mapper.Profile);
            SyncSettingsToUi();
            InfoLabel.Text = "Applied DirectInput Default Preset.";
        }

        private void PresetPlayStation_Click(object sender, RoutedEventArgs e)
        {
            CancelInputCapture();
            _mapper.Profile.ApplyPlayStationDefaults();
            _profileManager.Save(_mapper.Profile);
            SyncSettingsToUi();
            InfoLabel.Text = "Applied PlayStation (DS4/DualSense) Preset.";
        }

        private void InversionChanged(object sender, RoutedEventArgs e)
        {
            var p = _mapper.Profile;
            p.LeftStickY.Invert = ChkInvertLeftY.IsChecked == true;
            p.LeftStickX.Invert = ChkInvertLeftX.IsChecked == true;
            p.RightStickY.Invert = ChkInvertRightY.IsChecked == true;
            p.RightStickX.Invert = ChkInvertRightX.IsChecked == true;
            p.LeftTrigger.Invert = ChkInvertLT.IsChecked == true;
            p.RightTrigger.Invert = ChkInvertRT.IsChecked == true;

            _profileManager.Save(p);
            RefreshRebindBadges();
        }

        private void DeadzoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mapper?.Profile == null) return;
            double dz = Math.Round(DeadzoneSlider.Value, 2);
            DeadzoneVal.Text = $"{dz:F2}";
            _mapper.Profile.LeftStickX.Deadzone = dz;
            _mapper.Profile.LeftStickY.Deadzone = dz;
            _mapper.Profile.RightStickX.Deadzone = dz;
            _mapper.Profile.RightStickY.Deadzone = dz;
        }

        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mapper?.Profile == null) return;
            double sn = Math.Round(SensitivitySlider.Value, 2);
            SensitivityVal.Text = $"{sn:F2}";
            _mapper.Profile.LeftStickX.Sensitivity = sn;
            _mapper.Profile.LeftStickY.Sensitivity = sn;
            _mapper.Profile.RightStickX.Sensitivity = sn;
            _mapper.Profile.RightStickY.Sensitivity = sn;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                bool allConnected = true;
                foreach (var controller in _virtualControllers)
                {
                    if (!controller.Connect())
                    {
                        allConnected = false;
                        break;
                    }
                }

                if (!allConnected)
                {
                    foreach (var controller in _virtualControllers) controller.Disconnect();
                    InfoLabel.Text = "Failed to create virtual controller! Ensure ViGEmBus driver is installed.";
                    return;
                }

                _isRunning = true;
                StartBtn.Content = "■ STOP EMULATION";
                StartBtn.Background = (Brush)FindResource("NeoCoralBrush");
                StartBtn.Foreground = Brushes.White;

                StatusBadge.Background = (Brush)FindResource("NeoMintBrush");
                StatusLabel.Text = "● 3 VIRTUAL XBOX 360 SLOTS ACTIVE";
                StatusLabel.Foreground = Brushes.Black;
                InfoLabel.Text = "Three virtual controller slots are active. Assign an input source to each slot.";
            }
            else
            {
                foreach (var controller in _virtualControllers) controller.Disconnect();
                _isRunning = false;
                StartBtn.Content = "▶ START EMULATION";
                StartBtn.Background = (Brush)FindResource("NeoMintBrush");
                StartBtn.Foreground = Brushes.Black;

                StatusBadge.Background = (Brush)FindResource("NeoCoralBrush");
                StatusLabel.Text = "DISCONNECTED";
                StatusLabel.Foreground = Brushes.White;

                InfoLabel.Text = "Emulation stopped.";
            }
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            for (int slot = 0; slot < _mappers.Length; slot++)
            {
                var inputState = _slotKeyboard[slot]
                    ? (DeviceMenu.IsKeyboardFocusWithin || SlotMenu.IsKeyboardFocusWithin
                        ? new ControllerState { Name = "Keyboard", Id = "keyboard" }
                        : _keyboardInput.GetState())
                    : _inputManager.GetState(_slotDevices[slot]);
                var mapped = _mappers[slot].Apply(inputState);

                if (slot == _selectedSlot)
                {
                    if (_inputCapture.IsActive)
                    {
                        var result = _inputCapture.Poll(inputState);
                        if (result != null) ApplyCaptureResult(result);
                    }

                    var pressed = string.Join(", ", inputState.Buttons.Where(b => b.Value).Select(b => $"B{b.Key}"));
                    var axesStr = string.Join(" ", inputState.Axes.Where(a => Math.Abs(a.Value) > 0.05).Select(a => $"A{a.Key}:{a.Value:F2}"));
                    var hatsStr = string.Join(" ", inputState.Hats.Where(h => h.Value.x != 0 || h.Value.y != 0).Select(h => $"H{h.Key}:({h.Value.x},{h.Value.y})"));
                    RawInputLabel.Text = $"BTNS: [{(string.IsNullOrEmpty(pressed) ? "NONE" : pressed)}]  AXES: [{(string.IsNullOrEmpty(axesStr) ? "CENTERED" : axesStr)}]  {(string.IsNullOrEmpty(hatsStr) ? "" : $"HATS: [{hatsStr}]")}";
                    ControllerModel.UpdateState(mapped.Buttons, mapped.LeftStick, mapped.RightStick, mapped.LeftTrigger, mapped.RightTrigger);
                }

                if (_isRunning && _virtualControllers[slot].IsConnected)
                {
                    _virtualControllers[slot].Update(mapped.Buttons, mapped.LeftStick, mapped.RightStick, mapped.LeftTrigger, mapped.RightTrigger);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _loopTimer.Stop();
            foreach (var controller in _virtualControllers) controller.Disconnect();
            _inputManager.Dispose();
        }
    }
}
