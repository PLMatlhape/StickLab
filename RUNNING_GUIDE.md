# StickLab Setup & Running Guide

## Project Structure ✓
- ✅ Assets/Scripts/ (all core scripts present)
- ✅ Assets/InputActions/StickLabControls.inputactions
- ✅ Assets/Scenes/Training.unity (created with bootstrap)
- ✅ Packages/manifest.json (configured with Input System 1.7.0)
- ✅ ProjectSettings/ProjectVersion.txt (Unity 2022.3.20f1 LTS)

## Quick Start

### Option 1: Open with Unity (Recommended)
1. **Open the StickLab folder in Unity Hub**
   - File → Add project from disk
   - Select: `d:\My-Projects\StickLab`
   - If prompted about version, use **2022.3 LTS or newer**

2. **Unity will auto-import the project**
   - Wait for all imports to complete
   - Packages will auto-download (Input System, TextMeshPro)

3. **Verify Input System Configuration**
   - Edit → Project Settings → Input System Package
   - Set "Active Input Handling" to **"Both"**
   - This enables both gamepad and UI button input

4. **Open the Scene**
   - Navigate to: Assets/Scenes/Training.unity
   - Double-click to open in editor

5. **Press Play** 🎮
   - Hit the Play button in the center top of the editor
   - The dashboard should load immediately
   - You'll see the training UI with sidebar, controls, and drills

### Option 2: Manual Scene Setup (if needed)
1. Create empty scene: Assets → Create → Scene
2. Create empty GameObject: Right-click in Hierarchy → Create Empty → "StickLab Bootstrap"
3. Add Component: Inspector → Add Component → TrainingSceneBootstrap
4. Save and press Play

## Controls Once Running

**Gamepad:**
- **Left Stick / D-Pad**: Navigate buttons in dashboard
- **Start (Hamburger)**: Begin training session
- **LT**: Hold for Aim Down Sights (precision mode, 55% sensitivity)
- **RT**: Fire/Shoot input
- **Select (3-line)**: Pause/Resume during session

**Keyboard (alternative):**
- **Arrow Keys / WASD**: Navigate dashboard
- **Enter**: Confirm selection
- **Escape / P**: Pause/Resume
- **F1**: Toggle dashboard visibility (during gameplay)

## Visual Features You'll See

### Dashboard Layout:
```
┌─────────────────────────────────────────────────────┐
│ STICKLAB         [Connection Status]        F1 Menu │  ← Top Bar
├──────────┬───────────────────────────────────────────┤
│          │  [Stage Selection Cards]                  │
│ Sidebar  │  • Circle Aim                             │
│ • Deadzone │  • Line Follow                          │
│ • Sens   │  • Shape Trace                            │
│ • Diff   │                                            │  ← Center Panel
├──────────┼───────────────────────────────────────────┤
│  [Tips]  │ Score: ████████░░ 87.3 / 100              │  ← Right Panel
│          │ Peak: 92.4                                 │  ← Bottom
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│     PAUSED                                          │
│                                                     │
│  Accuracy: 87.3  |  Smoothness: 89.1               │
│  Speed: 85.2  |  Peak: 92.4                        │
│                                                     │
│  [RESUME]  [NEW SESSION]                           │  ← Overlay
└─────────────────────────────────────────────────────┘
```

### Sidebar Controls:
- **Deadzone**: Range 0.0-0.5 (default 0.18)
  - +/- buttons adjust with 0.02 steps
  - Controls stick input threshold before registering movement
  
- **Sensitivity**: Range 2.0-16.0 (default 8.0)
  - +/- buttons adjust with 0.5 steps
  - Affects cursor speed
  - Multiplied by 0.55 during ADS (LT held)

- **Difficulty Buttons**: Easy, Normal, Hard
  - Easy: 1.0× all multipliers (relaxed targets)
  - Normal: 0.97/0.92/0.90 size/thickness/tolerance
  - Hard: 0.92/0.84/0.80 (challenging targets)

### Training Drills:
1. **Circle Aim**: Hit expanding/contracting circles
   - Requires circular movements within radial threshold
   
2. **Line Follow**: Trace horizontal/vertical lines
   - Tests directional precision and steadiness
   
3. **Shape Trace**: Outline polygon shapes
   - Combines circular and linear precision

### Scoring System:
- **Accuracy**: Proximity to perfect aim (0-100)
- **Smoothness**: Consistency of movement (0-100)
- **Speed**: Efficiency without sacrificing quality (0-100)
- **Total Score**: Weighted composite (usually 0-100)
- **Peak Score**: Highest achieved during session
- **Average Score**: Running average (smoothed lerp)

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Input System errors | Make sure "Active Input Handling" = "Both" in Project Settings |
| Missing packages | Unity automatically downloads on first import; wait 2-3 minutes |
| Buttons not responding | Check if gamepad is connected (Windows Settings → Devices → Game Controllers) |
| No controller input | Verify gamepad is plugged in before opening scene; restart the editor if needed |
| Visual elements missing | Check Console (Window → General → Console) for errors; fix any missing scripts |
| Deadzone not adjusting | Ensure InputHandler getDeadzone() is correctly wired; check PlayerPrefs key "StickLab.Settings.Deadzone" |

## Persistence Features

**Saved Settings:**
- Deadzone value → `PlayerPrefs.GetFloat("StickLab.Settings.Deadzone")`
- Sensitivity value → `PlayerPrefs.GetFloat("StickLab.Settings.Sensitivity")`
- Difficulty preset → `PlayerPrefs.GetString("StickLab.Settings.DifficultyPreset")`
- Input rebinds → `InputActionAsset.SaveBindingOverridesAsJson()`

These persists across game restarts via the dashboard's LoadSettings/SaveSettings methods.

## Performance Notes

- **Resolution**: Scales to any screen size (optimized for 1920×1080)
- **Canvas Scaler**: Responsive with 0.5 width/height match
- **Rendering**: Canvas-based UI (CPU efficient)
- **Input Sampling**: 60fps polling of gamepad/keyboard per frame
- **Drill Updates**: Runs in TrainingSessionManager.Tick() at variable deltaTime

## Next Steps for Polish

If you want to refine the visuals further:

1. **Button Styling**:
   - Currently: Simple color tints and text-only
   - Option: Add outlines, rounded corners, or icons via Image components

2. **Gauge Animation**:
   - Currently: Instant fill changes
   - Option: Lerp fill smoothly (adjust speed in UpdateScoreGauge)

3. **Overlay Transitions**:
   - Currently: Instant show/hide via gameObject.SetActive()
   - Option: Fade in/out with CanvasGroup.alpha lerped over 0.3s time

4. **Difficulty Presets**:
   - Currently: 3 hardcoded profiles
   - Option: Generate UI sliders for custom multiplier knobs

5. **Stage Cards**:
   - Currently: Text layout only
   - Option: Add visual drill preview (animated circle/line/shape)

---

**Status**: Project is ready to run! Launch with File → Open Project in Unity Hub.
**Last Updated**: April 28, 2026
