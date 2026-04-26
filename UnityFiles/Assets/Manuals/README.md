# Manual page test assets

Place local page byte assets used by `AutoDocumentPageSenderHarness` here.

## Expected location

- `Assets/Manuals/ExecSummary/`

## Expected naming

Use page-number suffixes so auto-sort works:

- `exec_summary_page-0001.bytes`
- `exec_summary_page-0002.bytes`
- `exec_summary_page-0003.bytes`
- `exec_summary_page-0004.bytes`
- `exec_summary_page-0005.bytes`

## Notes

- Files should import as `TextAsset` (recommended `.bytes` extension).
- Use `Assets/Prefabs/DocumentNavigationHarnessRig.prefab` and assign these files to `unorderedAssets`.
- Keep `autoSortFromNames = true`.


## Traffic logging

The pre-wired prefab also includes `DocumentJsonTrafficLogger` so you can observe raw JSON payloads during local harness runs.
