# Project Overview
- Game Title: Document Viewer
- High-Level Concept: A VR/XR document viewer that renders PDF pages onto a 3D surface within a World Space UI.
- Players: Single player (VR/XR)
- Target Platform: StandaloneWindows64 (XR compatible)
- Render Pipeline: URP
- Screen Orientation: Landscape (Canvas is 2048x1536)

# Game Mechanics
## Core Gameplay Loop
The player navigates through PDF documents. Pages are rasterized into JPEGs and sent via a data channel (likely WebRTC), then displayed on a 3D Quad (`PdfScreen`).

## Controls and Input Methods
The project uses the Meta XR Interaction SDK (PointableCanvas, PokeInteractable, RayInteractable).

# UI
The UI is a World Space Canvas mounted on a backplate. A specific `PageDisplay` RectTransform defines the area where the PDF page should appear.

# Key Asset & Context
- `PdfPageDisplay.cs`: Manages the rendering of texture onto the target renderer.
- `PdfScreen`: A MeshRenderer (Quad) used as the physical screen.
- `DocumentManager.cs`: Orchestrates receiving data and triggering the display.

# Implementation Steps
## 1. Eliminate Component Redundancy
There are two `PdfPageDisplay` components pointing to the same `PdfScreen`. This causes material instance conflicts where one component updates a material that isn't currently assigned to the renderer.
- **Action**: Remove the `PdfPageDisplay` component from the `DocumentNavigationHarnessRig` GameObject.
- **Dependency**: None

## 2. Re-link DocumentManager
The `DocumentManager` currently references the "Rig" version of the display component.
- **Action**: In the `DocumentNavigationHarnessRig`, update the `DocumentManager.pdfPageDisplay` field to reference the `PdfPageDisplay` component located under the `Canvas` hierarchy (GameObject ID: 58000).
- **Dependency**: Step 1

## 3. Scale the PdfScreen Quad
The `PdfScreen` is a 1x1 meter Quad sitting inside a 2048x1536 unit Canvas. At scale (1,1,1), it is practically invisible.
- **Action**: Update the `PdfScreen` Transform scale to match the `PageDisplay` dimensions (e.g., `(2048, 1536, 1)`) or a proportionally correct size.
- **Code Change**: Enhance `PdfPageDisplay.cs` to automatically scale the `_targetRenderer` transform based on the incoming `width` and `height` in `ShowFromBytes`.
- **Dependency**: Step 1

## 4. Fix Depth and Z-Fighting
The `PdfScreen` is likely at the exact same Z-position as the UI background.
- **Action**: Set the `PdfScreen` local position Z to a small negative value (e.g., `-0.01` or `-1` depending on unit scale) to ensure it renders in front of the backplate.
- **Dependency**: None

## 5. Script Refinement (PdfPageDisplay.cs)
Modify the script to handle scaling and ensure the renderer is enabled correctly.
- **Action**: Update `Show(Texture2D tex)` to adjust the transform scale.

# Verification & Testing
1. **Manual Check**: Verify in the Inspector that `PdfScreen` has the correct material assigned after entering Play Mode.
2. **Context Menu**: Use the "Test Show" context menu on `PdfPageDisplay` to verify rendering with the `_testJpegBytes`.
3. **Log Check**: Ensure no "Target Renderer is not assigned" or "failed to decode" errors appear in the console.
