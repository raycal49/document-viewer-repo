# Project Overview
- Goal: Fix the end-to-end flow where `AutoDocumentPageSenderHarness` triggers PDF rendering on the `PdfScreen` quad.
- Context: The system broke after moving the display components into a World Space UI hierarchy.

# Game Mechanics (The Harness Flow)
1. **Source**: `AutoDocumentPageSenderHarness` (Rig) generates JSON/Bytes.
2. **Logic**: `DocumentManager` (Rig) receives and assembles chunks.
3. **Display**: `PdfPageDisplay` (UI) receives the texture and updates the `PdfScreen` (Quad).

# Key Issues to Resolve
- **Scaling**: Canvas is currently 2km wide in World Space.
- **Positioning**: UI elements are offset for screen-space overlay instead of world-space centering.
- **Quad Fit**: The Quad scale should match the UI container, not raw pixels.

# Implementation Steps

## 1. World Space UI Transform Correction
- **Canvas**: Set `localScale` to `(0.0005, 0.0005, 0.0005)` and `localPosition` to `(0, 0, 0)`.
- **PageDisplay/PdfPageDisplay**: Set `localPosition` to `(0, 0, 0)`.
- **PdfScreen**: Set `localPosition` to `(0, 0, -0.01)` to ensure it renders in front of the background.

## 2. Update PdfPageDisplay.cs for UI Container Scaling
- **Action**: Modify the `Show` method to scale the `PdfScreen` based on its parent `RectTransform` size. This ensures the PDF always fills the intended UI slot.

## 3. Reference Verification
- **Action**: Ensure `AutoDocumentPageSenderHarness` -> `DocumentManager` -> `PdfPageDisplay` links are correct.

# Verification & Testing (Harness-Only)
1. Enter Play Mode.
2. Monitor Console for `[AutoDocumentPageSenderHarness][sent][document-page]`.
3. Verify the `PdfScreen` is active and displaying the document page in the VR view.

# Todos
- [ ] World Space UI Transform Correction
- [ ] Update PdfPageDisplay script for container scaling
- [ ] Reference Verification
- [ ] Final Verification
