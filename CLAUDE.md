# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

F4ToPokeys is a C# WPF desktop application (.NET Framework 4.8, x86) that bridges **Falcon BMS** (an F-16 flight simulator) with **PoKeys USB hardware devices**. It reads real-time flight data from Falcon BMS shared memory and drives physical cockpit panels, gauges, lights, and stepper motors.

## Build

Open `F4ToPokeys.sln` in Visual Studio 2017+ and build, or from the command line (run from the repo root):

```
msbuild F4ToPokeys/F4ToPokeys.csproj /p:Configuration=Debug /p:Platform=x86
```

Output: `F4ToPokeys\bin\Debug\F4ToPokeys.exe` (or `Release\` for release builds).

**Platform:** `F4ToPokeys.csproj` defines `Debug|x86` and `Release|x86` configurations only. This matches the upstream `lightningstools/F4SharedMem` project so pulls from its source repo don't require local csproj edits. `SimplifiedCommon` (also upstream) only defines `AnyCPU` configs; the `ProjectReference` in `F4ToPokeys.csproj` uses `<SetPlatform>Platform=AnyCPU</SetPlatform>` to cross-request its AnyCPU build when the parent is building `x86`.

There are no automated tests — the project is validated through manual integration testing with Falcon BMS hardware.

## Solution Structure

Three projects in the solution:

| Project | Type | Role |
|---|---|---|
| `F4ToPokeys/` | WinExe (.NET 4.8) | Main WPF application |
| `lightningstools/F4SharedMem/` | Library (.NET 4.8) | Falcon BMS shared memory reader |
| `lightningstools/SimplifiedCommon/` | Library (.NET 2.0) | Win32 interop utilities |

Key external dependencies (not in NuGet — must be installed separately):
- `PoKeysDevice_DLL.dll` — from PoLabs PoKeys SDK
- `UsbWrapper.dll`, `Usc.dll` — from Pololu Maestro SDK

### Main project folder layout (`F4ToPokeys/`)

```
Core/           BindableObject, RelayCommand, IDevice (the device interface)
Configuration/  Configuration, ConfigurationViewModel
Falcon/         FalconConnector, FalconLight/Gauge/*
Hardware/
  PoKeys/       PoKeys + outputs (Digital/MemorySlot/MatrixLed/PoExtBus/SevenSegment)
  PoKeys/Stepper/  PoKeysStepper*, PoVID6066
  PololuMaestro/  PololuMaestro + servos
  DEDuino/      DEDuino (Arduino serial bridge)
Views/          Per-hardware-type DataTemplates (PoKeysTemplate, PololuMaestroTemplate, DEDuinoTemplate)
Controls/       SevenSegmentDigitControl, FloatingPointTextBox, SelectorHelper
Converters/     Value converters
Resources/      Icons, resource dictionaries (error/light_on/light_off), design data
Translations/   Resx files (English + French)
```

All source files use the `F4ToPokeys` namespace regardless of folder — the folders are organizational only. Do not change namespaces when moving files, or XML deserialization of the saved `Configuration.xml` will break.

## Architecture

### Data Flow (100ms polling loop)

1. `FalconConnector` detects Falcon BMS process via named mutex
2. `F4SharedMem.Reader` maps and marshals 8+ Windows shared memory regions into `FlightData`
3. `FalconConnector` diffs old vs. new `FlightData`, fires `FlightDataChanged` and `FlightDataLightsChanged` events
4. Light and gauge consumers receive events and write to hardware outputs

### Key Singletons

- `FalconConnector.Singleton` — the event hub; owns the polling timer and all flight data consumers
- `ConfigHolder.Singleton` — loads/saves configuration from `%LocalAppData%\F4ToPokeys\F4ToPokeys.xml`
- `PoKeysEnumerator.Singleton` — discovers and enumerates connected PoKeys devices

### Hardware Output Hierarchy

```
PoKeys (USB I/O board)
  ├── DigitalOutput      → individual on/off pins
  ├── MemorySlot         → 7-segment display values
  ├── MatrixLed          → LED matrix panels
  ├── PoExtBus           → expansion bus outputs
  └── StepperAxisOutput  → stepper motor positioning

PololuMaestro (servo controller)
  └── MaestroOutput      → PWM servo channels

DEDuino (custom Arduino)
  └── DED/caution panel outputs
```

### Falcon Light & Gauge Consumers

- `FalconLight` — represents one on/off light signal from BMS (e.g., MASTER CAUTION)
- `FalconLightConsumer` — maps a `FalconLight` to a hardware output pin
- `FalconGauge` — represents a numeric value from BMS (e.g., airspeed, altitude)
- `FalconGaugeDigit` + `FalconGaugeFormat` — extract and format individual digits for 7-segment displays

The full list of available lights and gauges is constructed in `FalconConnector.cs` using reflection over `FlightData` fields and a hardcoded list of named light enum values.

### Configuration System

Configuration is XML-serialized via `ConfigHolder`. The root `Configuration` object contains lists of devices and their consumer bindings. Version migration (v1.0 → v1.1) is handled in `Configuration.FixAfterRead()`. When adding new configurable items, follow the existing serialization pattern (public properties, `[XmlArray]`/`[XmlArrayItem]` attributes).

### UI Pattern

WPF with MVVM: `BindableObject` provides `INotifyPropertyChanged`; `ConfigurationViewModel` is the main view model. The app runs primarily as a **system tray** application — the main window is `MainWindow.xaml` which hosts a `TaskbarIcon`. Configuration is edited in `ConfigurationDialog.xaml`.

`ConfigurationDialog.xaml` is a thin shell (~115 lines) that merges per-hardware-type `ResourceDictionary` files from `Views/`. To add a new hardware type:
1. Implement `IDevice` (`Core/IDevice.cs`) on the new model class — requires `DisplayName` and `Error` properties.
2. Create `Views/MyNewHardwareTemplate.xaml` containing a single keyed `DataTemplate` (follow the pattern in `PoKeysTemplate.xaml`).
3. Add one `ResourceDictionary` entry for the new file to `ConfigurationDialog.xaml`'s `MergedDictionaries`.
4. Add one `ItemsControl` + Add button to the dialog body.
5. Register the new `.xaml` as a `<Page>` and the `.cs` as `<Compile>` in `F4ToPokeys.csproj`.

## Domain Terminology

- **Falcon BMS** — the F-16 flight simulator this app integrates with
- **PoKeys** — USB I/O boards by PoLabs (digital I/O, PWM, stepper motor control)
- **Maestro** — Pololu USB servo controller
- **DEDuino** — custom Arduino-based hardware for the DED (Data Entry Display)
- **Shared Memory** — Windows IPC mechanism BMS uses to publish flight data (`FlightData`, `FlightData2`, OSB, radio, etc.)
- **Memory Slot** — a named address in BMS shared memory used to push formatted data to hardware displays
- **Light** — a boolean cockpit indicator (e.g., gear up, master caution, afterburner)
- **Gauge** — a numeric cockpit value (altitude, airspeed, heading, etc.)
