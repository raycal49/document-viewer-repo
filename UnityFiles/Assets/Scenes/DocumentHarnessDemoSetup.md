# Document Harness Demo Setup

A pre-wired prefab is available at:

- `Assets/Prefabs/DocumentNavigationHarnessRig.prefab`

It already includes and wires:

- `PdfPageDisplay`
- `DocumentManager`
- `DocumentNavigationChannel`
- `DocumentNavigationController`
- `DocumentNavigationControls`
- `AutoDocumentPageSenderHarness`

## What you still need to furnish

1. Put your manual page assets in `Assets/Manuals/ExecSummary/`.
2. Ensure filenames include page suffixes like `exec_summary_page-0001.bytes`.
3. In the prefab instance, assign those files to `AutoDocumentPageSenderHarness.unorderedAssets`.

## Runtime controls

- **A button** (`OVRInput.Button.One`): next page
- **B button** (`OVRInput.Button.Two`): previous page
- Existing UI/button hooks continue to work via `DocumentNavigationControls` methods.

## Expected flow

1. Enter Play Mode.
2. Harness auto-starts (`autoStartSessionOnEnable`) and sends `document-start` + page 1.
3. Press A/B to navigate.
4. Harness auto-sends chunked `document-page` payloads from local assets for each page change.
