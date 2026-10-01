# 360 ReBind

**360 ReBind** is a Windows desktop application that maps physical gamepads and keyboard input to virtual Xbox 360 controllers. It is built with C# and WPF, reads physical controllers through DirectInput, and creates virtual Xbox 360 devices through ViGEmBus.

Use it to rebind controller buttons, tune stick response, combine different input sources across virtual controller slots, and monitor input while configuring a profile.

## Features

- Three independently configurable virtual Xbox 360 controller slots.
- Assign each slot a physical controller, the keyboard, or no input.
- Poll multiple physical DirectInput devices at once.
- Rebind controller buttons, triggers, stick axes, and D-pad inputs.
- Rebind keyboard-backed controls to keyboard keys.
- Configure stick inversion, deadzone, and sensitivity.
- Start and stop virtual controller emulation from the app.
- View live controller visualization and raw input diagnostics.
- Use built-in DirectInput and PlayStation-oriented mapping presets.
- Save each slot's mapping profile locally as JSON.

## Requirements

- Windows 10 or Windows 11.
- .NET 8 SDK to build from source; the .NET 8 Desktop Runtime is required to run a framework-dependent build.
- ViGEmBus driver installed and working. The app uses it to create virtual Xbox 360 controllers.
- A physical DirectInput-compatible controller for gamepad input. Keyboard input can be used without a physical controller.

> **Driver status:** ViGEmBus is a third-party dependency. Install it only from a trusted, maintained source and follow its installation instructions. If virtual controller creation fails, verify that the driver is installed and available to Windows.

## Build and run

Open PowerShell in the project directory and run:

```powershell
dotnet restore ReBind360.csproj
dotnet build ReBind360.csproj
dotnet run --project ReBind360.csproj
```

To build a self-contained Windows x64 executable:

```powershell
dotnet publish ReBind360.csproj -c Release -r win-x64 --self-contained true
```

The publish output is placed under `bin/Release/net8.0-windows/win-x64/publish/`. ViGEmBus is still required; publishing the app does not bundle that driver.

## Quick start

1. Install and confirm ViGEmBus is working.
2. Connect any physical gamepads you want to use.
3. Launch 360 ReBind.
4. Select a virtual **slot**, then choose its input source. Source and slot selectors are operated with the mouse.
5. Open **REBIND CONTROLS** and click **LISTEN** beside a control to capture an input from the selected source.
6. Use **PLAY & MONITOR** to inspect live input, tune inversion/deadzone/sensitivity, and choose a preset if useful.
7. Click **START EMULATION**. Windows and compatible games should see the virtual Xbox 360 controller slots.
8. Click **STOP EMULATION** before closing the app if you want to disconnect the virtual outputs explicitly.

## Slots and default input assignments

The application exposes three virtual Xbox 360 output slots. On first launch, it assigns sources as follows:

| Slot | Initial input |
| --- | --- |
| Slot 1 | First detected physical controller, if available |
| Slot 2 | Keyboard |
| Slot 3 | Second detected physical controller, if available |

These assignments are starting defaults; choose a different source for any slot in the app. Additional physical controllers can be selected from the input list. A slot set to **No input** sends neutral input.

Each slot has its own mapping profile. Profiles are stored per Windows user in:

```text
%APPDATA%\360ReBind\Profiles\
```

The default profile names used by the slots are `Default`, `Slot2`, and `Slot3`. Their JSON files are created as settings are saved.

## Keyboard input defaults

The keyboard source starts with these controls:

| Virtual control | Default key(s) |
| --- | --- |
| Left stick | W / A / S / D |
| Right stick | I / J / K / L |
| D-pad | Arrow keys |
| A / B / X / Y | Space / Ctrl / E / Q |
| Left / right bumper | Shift / R |
| Left / right trigger | F / G |
| Back / Start | Tab / Enter |
| Left / right stick click | C / V |
| Guide | F1 |

Keyboard controls are polled globally while keyboard input is assigned to a slot. While a slot or input selector has keyboard focus, keyboard input for the virtual controller is suppressed to avoid selector interaction affecting gameplay.

## Rebinding notes

- Click **LISTEN** for the control you want to change, then actuate the desired input.
- Stick-axis capture records the direction used and applies an initial inversion accordingly. You can change inversion later in **PLAY & MONITOR**.
- Trigger controls can be bound to an analog axis or a button/key.
- D-pad controls can use a physical POV hat or individual buttons/keys.
- Changing the selected slot or its input source cancels an active capture.
- Deadzone and sensitivity apply to the stick axes. The sliders update the active slot's profile.

## Technology

- .NET 8 / C#
- Windows Presentation Foundation (WPF)
- SharpDX DirectInput for physical gamepad polling
- Nefarius ViGEm.Client for virtual Xbox 360 controller output

## Troubleshooting

### Virtual controller creation fails

Confirm that ViGEmBus is installed, Windows recognizes the driver, and the app can connect to it. The application creates three virtual Xbox 360 outputs when emulation starts; failure to create any output prevents emulation from starting.

### No physical controller is listed

Connect the controller, confirm Windows recognizes it, then click **REFRESH**. The current physical-device input path uses DirectInput; a device exposed only through another API may not appear in the list.

### Input is incorrect or an axis is reversed

Use **LISTEN** to capture the relevant control again. Try the built-in preset appropriate to the controller, then check the live visualization and raw input diagnostics. Adjust inversion, deadzone, or sensitivity for the selected slot.

### The game sees duplicate controllers

The app creates three virtual outputs when emulation starts, including slots assigned to **No input**. Some games may list every virtual output even when a slot receives neutral input. Configure the slots the game should use and stop emulation when finished.

## Contributing

Bug reports and focused pull requests are welcome. Include Windows version, controller model, whether the input is physical or keyboard-based, the selected slot/source, and the steps needed to reproduce the issue. Do not include personal profile files or sensitive system information.

## License

No license file is currently included. Unless a license is added, standard copyright restrictions apply; please contact the repository owner before redistributing or reusing the code.
