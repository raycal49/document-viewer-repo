# Project Overview
- Game Title: Document Viewer
- High-Level Concept: A VR/XR document viewer that renders PDF pages onto a 3D surface within a World Space UI, featuring navigation and zoom.
- Players: Single player (VR/XR)
- Target Platform: StandaloneWindows64 (XR compatible)
- Render Pipeline: URP

# UI Hierarchy Roadmap
The following hierarchy represents the target structure. 
- [x] DocumentPanelRoot
- [ ] GrabBar (Phase 3)
- [x] CanvasRoot (World Space)
  - [x] UIBackplate
    - [ ] HeaderRow (Phase 2)
      - [ ] DocumentTitleLabel
      - [ ] AnchorFollowToggle
    - [x] PageDisplay (Verified Working)
      - [x] PdfPageDisplay (Script)
      - [x] PdfScreen (Quad)
    - [ ] FooterRow (Phase 1)
      - [ ] NavGroup
        - [ ] PrevButton
        - [ ] PageStatusLabel (e.g., "12 / 84")
        - [ ] NextButton
      - [ ] ZoomGroup
        - [ ] ZoomOutButton
        - [ ] ZoomPercentLabel (e.g., "125%")
        - [ ] ZoomInButton

# Key Asset & Context
- `PdfPageDisplay.cs`: Manages texture application to the `PdfScreen` quad.
- `DocumentManager.cs`: Handles document session state and page assembly.
- `AutoDocumentPageSenderHarness`: Simulated document source for testing.

# Implementation Steps

## Phase 0: Script Cleanup (DONE)
Remove programmatic scaling from `PdfPageDisplay` to allow manual editor-based sizing of the quad.
- [x] Remove the code block in `Show(Texture2D tex)` that sets `_targetRenderer.transform.localScale`.

## Phase 1: Footer Row & Basic Navigation
Create the footer UI and wire it to the `DocumentManager` for page switching.
1. Create `FooterRow` GameObject under `UIBackplate`.
2. Add `NavGroup` with "Previous" and "Next" buttons and a "Page Status" label.
3. Add `ZoomGroup` with zoom controls (layout only for now).
4. Implement UI event listeners to call `DocumentManager` navigation methods.
5. Update `DocumentManager` to broadcast page status changes (current/total) to the UI.

## Phase 2: Header Row
Implement the top-level info and anchoring controls.
1. Create `HeaderRow` under `UIBackplate`.
2. Add `DocumentTitleLabel` and wire it to `DocumentManager` document name.
3. Add `AnchorFollowToggle` for UI behavior.

## Phase 3: Interaction & Zoom
1. Implement `GrabBar` for moving the panel.
2. Add "Zoom" logic to `PdfPageDisplay` (scaling the quad relative to its base size).
3. Final visual polish and Meta SDK integration.

# Verification & Testing
- **Navigation Test**: Click UI buttons and verify the PDF page changes via the Harness logs.
- **Layout Test**: Verify UI elements are visible and properly positioned in World Space.
