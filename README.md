# StickLab

Controller aim training MVP for Unity.

## What is in the workspace

- `Assets/Scripts/Core/InputHandler.cs`
- `Assets/Scripts/Core/CursorController.cs`
- `Assets/Scripts/Core/GameLoop.cs`
- `Assets/Scripts/Drills/CircleDrill.cs`
- `Assets/Scripts/Drills/LineDrill.cs`
- `Assets/Scripts/Drills/ShapeDrill.cs`
- `Assets/Scripts/Rendering/PathRenderer.cs`
- `Assets/Scripts/Scoring/*.cs`
- `Assets/Scripts/Utils/VectorUtils.cs`

## Unity setup

1. Create or open a Unity LTS project on Windows.
2. Install the **Input System** package.
3. Set **Active Input Handling** to **Input System Package** or **Both**.
4. Import or keep the provided action asset at [Assets/InputActions/StickLabControls.inputactions](Assets/InputActions/StickLabControls.inputactions).
5. Create an empty scene.
6. Add one GameObject with `TrainingSceneBootstrap`.
7. Press Play and use the controller Start button to begin.
8. Press F1 to toggle the live settings panel.

If you prefer manual wiring, you can instead place:

- `InputHandler`
- `CursorController`
- `CursorIndicator`
- `PathRenderer`
- `CircleDrill`
- `LineDrill`
- `ShapeDrill`
- `TrainingSessionManager`
- `TrainingHud`
- `TrainingMenu`
- `TrainingSettingsPanel`
- `GameLoop`

For `InputHandler`, you can assign the action asset and use the `Gameplay/RightStick` action, or leave it empty and let the game fall back to the connected controller.

## Current MVP behavior

- reads right stick input
- moves a cursor with frame-rate independent motion
- runs a drill session with circle, line, and shape stages
- auto-progresses to thinner and harder paths
- shows live accuracy, smoothness, speed, and total score in a HUD

## Next build steps

- add a drill selector screen
- add fail/retry states
- add Bezier and spiral drills
- add heatmap logging