# Document Harness Demo Setup

A pre-wired prefab is available at:

- `Assets/Prefabs/DocumentNavigationHarnessRig.prefab`

It already includes and wires:

- `PdfPageDisplay`
- `DocumentManager`
- `DocumentNavigationChannel`
- `DocumentNavigationController`
- `DocumentNavigationControls`
- `DocumentPanelToolbar` (toolbar wiring for Prev/Next + page label + UI-only zoom label/buttons)
- `AutoDocumentPageSenderHarness`

## Recommended panel hierarchy

Create/author your visual hierarchy in-editor under the rig root (world transform remains editor-owned):

```
DocumentPanelRoot
├── BackplatePlane / BackplateQuad
├── PdfPageQuad
└── ToolbarCanvas
    ├── PreviousButton
    ├── PageNumberLabel
    ├── NextButton
    ├── ZoomOutButton
    ├── ZoomPercentLabel
    └── ZoomInButton
```

`PdfPageDisplay` now supports assigning the renderer explicitly (recommended). If no renderer is assigned/found, it can still create a legacy fallback quad.

## What you still need to furnish

1. Put your manual page assets in `Assets/Manuals/ExecSummary/`.
2. Ensure filenames include page suffixes like `exec_summary_page-0001.bytes`.
3. In the prefab instance, assign those files to `AutoDocumentPageSenderHarness.unorderedAssets`.

## Runtime controls

- **A button** (`OVRInput.Button.One`): next page
- **B button** (`OVRInput.Button.Two`): previous page
- Existing UI/button hooks continue to work via `DocumentNavigationControls` methods.

## Plan A microgesture setup (Meta XR v85+)

Add `DocumentMicrogestureSwipeControls` to your document rig GameObject and assign:

- `navigationController` -> `DocumentNavigationController`
- `leftHandGestures` -> GameObject with `OVRMicrogestureEventSource` + left `OVRHand`
- `rightHandGestures` -> GameObject with `OVRMicrogestureEventSource` + right `OVRHand`

Then choose gesture mappings in Inspector:

- `previousPageGesture` -> gesture you want for previous page
- `nextPageGesture` -> gesture you want for next page

Use cooldown + optional logging in the component to reduce false repeats.

## JSON observability

`AutoDocumentPageSenderHarness` logs both sent and received JSON traffic:

- sent document payloads (`document-start`, `document-page`, `document-close`)
- sent/received navigation channel payloads
- received payloads observed by `DocumentManager.HandleMessage`

Use these inspector fields on `AutoDocumentPageSenderHarness`:

- `logAllJsonTraffic` (enable/disable full traffic logs)
- `prettyPrintJsonLogs` (indented JSON for readability)
- `maxJsonCharacters` (truncate huge payloads, `0` = no truncation)

## Expected flow

1. Enter Play Mode.
2. Harness auto-starts (`autoStartSessionOnEnable`) and sends `document-start` + page 1.
3. Press A/B or perform mapped microgesture to navigate.
4. Harness auto-sends chunked `document-page` payloads from local assets for each page change.
5. Observe traffic in Console with `[AutoDocumentPageSenderHarness][sent|received][route]` prefixes.
