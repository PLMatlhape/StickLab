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
3. Set **Active Input Handling** to **Both** so the dashboard buttons and controller can both work.
4. Import or keep the provided action asset at [Assets/InputActions/StickLabControls.inputactions](Assets/InputActions/StickLabControls.inputactions).
5. Create an empty scene.
6. Add one GameObject with `TrainingSceneBootstrap`.
7. Press Play.
8. Use the controller Start button or the on-screen Start button to begin.
9. Hold LT for precision/ADS mode and use RT as the fire input.
10. Press F1 to toggle the dashboard.

If you prefer manual wiring, you can instead place:

- `InputHandler`
- `CursorController`
- `CursorIndicator`
- `PathRenderer`
- `CircleDrill`
- `LineDrill`
- `ShapeDrill`
- `TrainingSessionManager`
- `TrainingDashboardUI`
- `GameLoop`

For `InputHandler`, assign [Assets/InputActions/StickLabControls.inputactions](Assets/InputActions/StickLabControls.inputactions) and use the `Gameplay` map. The dashboard supports rebinding `RightStick`, `Aim`, `Fire`, `Confirm`, and `Cancel`.

## Exact scene wiring

If you want to wire it by hand instead of using the bootstrap:

1. Create a root GameObject named `StickLab Bootstrap`.
2. Add `TrainingSceneBootstrap` to it.
3. The bootstrap creates the following runtime objects:
	- `Main Camera`
	- `Cursor`
	- `Circle Path`
	- `Line Drill`
	- `Shape Drill`
	- `Circle Drill`
	- `Game Loop`
	- `Training Session`
	- `Training Dashboard`
4. If wiring manually, assign:
	- `InputHandler` to `GameLoop`
	- `CursorController` to `GameLoop` and `TrainingSessionManager`
	- `CircleDrill`, `LineDrill`, and `ShapeDrill` to `TrainingSessionManager`
	- `TrainingDashboardUI` to the same `InputHandler`, `CursorController`, and `TrainingSessionManager`
5. Attach `PathRenderer` to each drill object.
6. Attach a `LineRenderer` to the cursor object for `CursorIndicator`.
7. Point `InputHandler` at the `Gameplay` map in [Assets/InputActions/StickLabControls.inputactions](Assets/InputActions/StickLabControls.inputactions).

## UI layout

The dashboard is a dark, card-based layout inspired by the reference image:

- left sidebar for sections and controller status
- top bar for connection and profile status
- center panel for the current drill and action buttons
- right panel for score and live performance bars
- bottom strip for quick tips and drill progress
- quick settings and rebind slots inside the sidebar/right panel

## Current MVP behavior

- reads right stick input
- moves a cursor with frame-rate independent motion
- runs a drill session with circle, line, and shape stages
- auto-progresses to thinner and harder paths
- shows live accuracy, smoothness, speed, and total score in a HUD
- records heatmap samples during sessions
- supports replay of recorded cursor paths for analytics

## Vector and SVG workflow for custom drills

For the best trace quality, complex images should be prepared as vector paths before being used as drills.

Recommended workflow:

1. Start with the original artwork or the cleanest raster source available.
2. Trace the outline in a vector editor such as Inkscape or Illustrator.
3. Simplify the path so it has fewer anchor points and cleaner geometry.
4. Split complex multi-part artwork into separate layers or path groups.
5. Export as SVG or another path-friendly vector format.
6. Convert the SVG paths into drill path data for the project.

Best practice for shapes like the butterfly reference:
- use the outer silhouette as one drill path
- use each wing segment as a separate trace segment if needed
- avoid overly dense internal points unless they are important for gameplay
- keep the path closed and readable for accurate scoring

If a folder of images or SVGs is added to the workspace, they can be batch converted into project-ready drill paths.

## Next build steps

- add a drill selector screen
- add fail/retry states
- add Bezier and spiral drills
- expand analytics with session comparisons and trends
- add heatmap logging and replay
- add custom SVG/image import pipeline