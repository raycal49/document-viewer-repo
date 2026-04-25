# Document Harness Demo Scene Setup

Use any existing scene and add these components to a GameObject:

- `PdfPageDisplay`
- `DocumentManager`
- `DocumentNavigationChannel`
- `DocumentNavigationController`
- `DocumentNavigationControls` (optional button bindings)
- `AutoDocumentPageSenderHarness`

## Reference wiring

- `DocumentManager.pdfPageDisplay` -> `PdfPageDisplay`
- `DocumentNavigationController.documentManager` -> `DocumentManager`
- `DocumentNavigationController.navigationChannel` -> `DocumentNavigationChannel`
- `DocumentNavigationControls.navigationController` -> `DocumentNavigationController`
- `AutoDocumentPageSenderHarness.documentManager` -> `DocumentManager`
- `AutoDocumentPageSenderHarness.navigationController` -> `DocumentNavigationController`

## Harness configuration

- `documentId`: `exec_summary`
- `documentName`: `Exec Summary`
- `autoSortFromNames`: enabled
- `unorderedAssets`: add files in `Assets/Manuals/ExecSummary`
- `sendFirstPageOnSessionStart`: enabled

## Demo flow

1. Enter Play Mode.
2. In `AutoDocumentPageSenderHarness` context menu, run **Start Session + First Page**.
3. Trigger `Next`/`Prev`/`Jump` through your controls.
4. The harness auto-sends chunked `document-page` payloads matching current page index.
