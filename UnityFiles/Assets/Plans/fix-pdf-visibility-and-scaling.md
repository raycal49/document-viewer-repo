# Project Overview
- Goal: Display rasterized PDF pages on a 3D Quad (`PdfScreen`) within a World Space UI in VR.
- Issue: The UI is currently scaled for Screen Space (thousands of units) but rendered in World Space, making it invisible or extremely distant. Redundant components were removed, but the "wiring" and scaling logic are still mismatched.

# Game Mechanics
- `AutoDocumentPageSenderHarness` sends document messages to `DocumentManager`.
- `DocumentManager` assembles chunks and tells `PdfPageDisplay` to show the image.
- `PdfPageDisplay` updates the material on `PdfScreen`.

# UI Changes
1. **Scale Normalization:** The `Canvas` is currently 2048 meters wide because its scale is `1.0`. We will scale it down to `0.0005` (making it ~1 meter wide).
2. **Position Reset:** The `Canvas` local position will be centered `(0,0,0)` relative to the backplate, instead of `(1024, 767, 0)`.
3. **RectTransform Alignment:** Ensure `PageDisplay` (the container for the PDF) is centered.

# Key Asset & Context
- `PdfPageDisplay.cs`: Update `Show` to fit the Quad into the parent `RectTransform` area rather than using raw pixel scale.
- `AutoDocumentPageSenderHarness`: Ensure it's correctly linked and active.

# Implementation Steps

## 1. UI Transform Fix (The "Reality Check")
The current UI is giant. We need to shrink it to VR proportions.
- **Action**: Set `Canvas` localScale to `(0.0005, 0.0005, 0.0005)`.
- **Action**: Set `Canvas` localPosition to `(0, 0, 0)`.
- **Action**: Set `PageDisplay` localPosition to `(0, 0, 0)`.
- **Action**: Set `PdfPageDisplay` localPosition to `(0, 0, 0)`.
- **Action**: Set `PdfScreen` localPosition to `(0, 0, -0.01)` (to stay in front of UI).

## 2. Robust Scaling Logic (PdfPageDisplay.cs)
Instead of `localScale = (tex.width, tex.height, 1)`, which depends on the texture size, we should match the `RectTransform` size of the parent.
- **Action**: Modify `PdfPageDisplay` to get the `RectTransform` of its parent and scale the `PdfScreen` to fit those bounds.
- **Action**: Enable `_isDevMode` on the UI component so it shows something immediately even without the Harness.

## 3. Harness Verification
Ensure the `AutoDocumentPageSenderHarness` has the correct `documentManager` reference.
- **Action**: Verify the serialized reference on `DocumentNavigationHarnessRig`.

## 4. Visibility Verification
- **Action**: Ensure the `PdfScreen` MeshRenderer is using a shader that works in VR (URP/Unlit).

# Verification & Testing
1. **Editor Play**: Check if the `PdfScreen` appears in front of the camera.
2. **Scale Check**: Verify the Quad's world scale is approximately 1 meter, not 2048 meters.
3. **Harness Log**: Check the console for `[AutoDocumentPageSenderHarness][sent][document-page]` logs.
