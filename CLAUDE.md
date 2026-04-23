# Document Viewer — Project Context

## What we're building

A field technician wearing a Quest 3 headset is on a call with an expert at HQ using the web app. The expert pulls up a PDF (design/repair manual) from Azure storage, and both people see the same page — the Quest user in AR, the web user in-browser. Either side can flip forward/backward; the other side follows. The Quest user decides where the document hangs in their physical space.

There is no PDF support, no Azure Blob integration, and no in-headset 2D UI pattern in the codebase today. Most of the plumbing (HTTP client, WebSocket signaling, RenderTexture-on-a-Quad, Meta XR Interaction SDK) is already present.

---

## Architecture

### Rasterization: server-side (Option A — chosen)

Unity cannot render PDF natively. The existing cloud-deployed backend converts PDF to PNG images per page; Unity downloads them as textures via `UnityWebRequest`. This reuses the existing `UnityWebRequest` pattern, avoids Unity-side PDF libraries and IL2CPP/license issues, and is cheap on Quest CPU.

### Storage (Azure Blob)

- Private container(s) — one for source PDFs, one for rasterized page PNGs (or `documents/{id}/page-{n}.png`).
- Access via short-lived SAS URLs minted by the backend. Never hand blob keys to the Quest or the browser.

### Backend (`ar-signalingserver.azurewebsites.net`)

Rasterization lives here (not a separate Azure Function). When a PDF is uploaded or listed and not yet rasterized, the app shells out to `pdftoppm` (poppler) or uses a .NET lib like PdfPig to produce page PNGs and writes them back to Blob.

New endpoints:
- `GET /documents` — list available docs `{ id, title, pageCount, thumbnailUrl }`
- `GET /documents/{id}/pages/{n}` — returns SAS URL (or 302) to that page PNG
- `POST /documents/select` — accepts `{ documentId, page }` from either client; server records state and fans out via Web PubSub to both peers
- `POST /documents/upload` (later) — accept PDF, trigger rasterization

### Signaling (WebSocket — already wired)

One new JSON message type, broadcast server → both peers:

```json
{ "type": "document.show", "documentId": "...", "page": 0, "totalPages": 12, "pageUrl": "<SAS URL>" }
```

Page changes reuse the same message. The server is the source of truth for current page — last write wins, both clients re-sync from the broadcast.

### Unity (Quest) side

New `PdfPageDisplay.cs` MonoBehaviour that:
- Owns a Quad with an `Unlit/Texture` material.
- On `document.show`, downloads the PNG via `UnityWebRequestTexture`, assigns it to the material, resizes the Quad to the page aspect ratio.
- On first show, places the Quad ~1.5m in front of the user's head, then stops following (virtual whiteboard behavior).
- Is grabbable/movable by the Quest user via Meta XR Interaction SDK `Grabbable` / `HandGrabInteractable`. Position persists for the session (no Spatial Anchors needed for v1).

Quest-side page nav: two 3D buttons (small Quads or cubes with `PokeInteractable` or controller raycast) parented near the Quad. On press, POST to `/documents/select` — server re-broadcasts and both sides update.

Pattern to copy from: `Assets/Scripts/Camera/VideoCompositor.cs` (RenderTexture-to-Quad). For grab/button interactions: Meta XR Interaction SDK samples in Packages.

### Web app side

New `DocumentPicker.tsx` component:
- Lists docs from `GET /documents` with thumbnails.
- Prev/next page buttons once a doc is selected.
- Each action POSTs to `/documents/select`.
- Displays the same PNG the Quest is seeing (subscribed to `document.show`).

---

## Key decisions

| Topic | Decision |
|---|---|
| Page sync | Either side can flip; server owns current state and fans out. Same view on both. |
| Users | 1 Quest + 1 web. No multi-party concerns. |
| Annotations on PDF | Out of scope for v1. `AnnotationRenderer` stays video-only. |
| Placement | Quest user grabs and places the PDF; persists for the session. No Spatial Anchors (cross-session is out of scope). |
| Page count | No hard limit. Lazy-load pages on demand (one PNG per `document.show`). |
| Zoom / pan | Not implemented. "Walk closer" is the zoom UX. |
| Rasterization host | Existing cloud backend, not a separate Azure Function. |

---

## Suggested build order

1. **Hello-PDF, no infrastructure.** Manually rasterize one PDF, drop PNGs in a public Blob container, hardcode a URL in Unity, get a Quad showing page 1. No backend changes, no web, no signaling.
2. **Lock down storage + backend endpoints.** Private container, `GET /documents` + page endpoint with SAS URLs. Unity fetches via backend.
3. **Signaling + two-way paging.** Add `document.show` broadcast and `POST /documents/select`. Wire both web prev/next and Quest prev/next.
4. **Web picker.** Build `DocumentPicker.tsx`. End-to-end flow for pre-rasterized PDFs.
5. **Grab & place on Quest.** Add `Grabbable` to the Quad so the tech can position the manual.
6. **Upload + auto-rasterize.** Add `POST /documents/upload` with `pdftoppm`/PdfPig inside the existing app.

Each milestone is shippable on its own — don't build them in parallel.

---

## Risks to watch

- **Meta XR Interaction SDK ergonomics.** No prior 3D UI in the codebase. Budget spike time before committing to grab and poke-button stories.
- **Page image sizing.** Rasterize at ~1800×2400 per letter page. Too big wastes bandwidth; too small looks blurry when the tech walks up to it.
- **SAS URL expiry during long sessions.** Mint fresh URLs per broadcast, not per list. A URL minted at list time will expire before a 20-min session ends.

---

## Critical existing files

| File | Why it matters |
|---|---|
| `UnityFiles/Assets/Scripts/Network/SignalingHttpClient.cs` | Pattern for new `POST /documents/select` call |
| `UnityFiles/Assets/Scripts/Network/SignalingClient.cs` | Where to add the `document.show` handler |
| `UnityFiles/Assets/Scripts/Camera/VideoCompositor.cs` | Pattern for displaying a texture on a 3D surface |
| `UnityFiles/Assets/Scripts/Models/Dtos.cs` | Where the new message DTO lives |
| `WebApp/src/components/` | Where `DocumentPicker.tsx` will live |
| Meta XR Interaction SDK (Packages) | `Grabbable` / `PokeInteractable` for placing and paging the Quad |

---

## Unity user stories

**Critical finding:** The Meta XR Interaction SDK is imported in `manifest.json` (v85.0) but never used anywhere in the codebase. No `Grabbable`, no `PokeInteractable`, no sample prefabs. Stories US-10 and US-13 are genuinely new ground — a bounded spike (US-10) is required before committing to those estimates.

### Phase 1 — Render a PNG on a Quad (no backend, no signaling)

**US-01 — Create `PdfPageDisplay` scaffold** · S · no deps
- New MonoBehaviour that spawns a Quad with `Unlit/Texture` material in `Awake` and exposes `Show(Texture2D tex)`.
- File: `UnityFiles/Assets/Scripts/Display/PdfPageDisplay.cs`
- AC: `[ContextMenu("Test Show")]` with a `_testTexture` assigned renders the texture on the Quad in Play Mode. ✅ Done (`e75be7e`)

**US-02 — PNG-URL-to-Texture download helper** · S · no deps
- New static helper `DownloadPngAsync(string url, Action<Texture2D> onReady, Action<string> onError)` wrapping `UnityWebRequestTexture.GetTexture`.
- File: `UnityFiles/Assets/Scripts/Network/TextureDownloader.cs`
- AC: Good URL fires `onReady` with valid texture; bad URL fires `onError`. Mirrors error-handling in `SignalingHttpClient.cs:43-46`.

**US-03 — End-to-end render with hardcoded URL** · S · US-01, US-02
- Wire US-02 into US-01. Add `[SerializeField] string _testUrl` and `[SerializeField] bool _isDevMode`; on `Start()` (behind the bool) download and display.
- File: `PdfPageDisplay.cs` (edit)
- AC: Press Play → hardcoded PNG from Blob appears on the Quad within ~1s on device.

### Phase 2 — Wire up the signaling message

**US-04 — Add `DocumentShowMessage` DTO** · S · no deps
- Append `[Serializable] public class DocumentShowMessage` with `string documentId`, `int page`, `int totalPages`, `string pageUrl` to `Dtos.cs`. Match existing style.
- File: `UnityFiles/Assets/Scripts/Models/Dtos.cs` (append)
- AC: `JsonUtility.FromJson<DocumentShowMessage>(...)` roundtrips correctly.

**US-05 — Dispatch `document.show` in `SignalingClient`** · S · US-04
- Add `case "document.show":` in the message-type switch (~line 178). Declare `public event Action<DocumentShowMessage> OnDocumentShow`. Parse and invoke.
- File: `UnityFiles/Assets/Scripts/Network/SignalingClient.cs`
- AC: Fake `document.show` payload fires the event with correct DTO values. Existing message types still work.

**US-06 — Connect event to display via `WebRTC_Connector`** · S · US-03, US-05
- Add `[SerializeField] PdfPageDisplay _pdfDisplay` to `WebRTC_Connector.cs`. Subscribe to `OnDocumentShow` and call `_pdfDisplay.ShowFromUrl(msg.pageUrl)`.
- File: `UnityFiles/Assets/Scripts/Core/WebRTC_Connector.cs`
- AC: Fake `document.show` via WebSocket updates the Quad end-to-end.

**US-07 — Editor-only dev harness** · S · US-06
- Small Editor helper (ContextMenu or Editor window) that fires `OnDocumentShow` without backend.
- File: `UnityFiles/Assets/Scripts/Editor/PdfTestHarness.cs`
- AC: Clicking the harness button in Play Mode updates the Quad to a typed URL. Unlocks all further Unity dev from backend dependency.

### Phase 3 — Place it in AR space

**US-08 — Aspect-ratio-aware Quad sizing** · S · US-01
- On each `Show`, read `texture.width / texture.height` and rescale the Quad (width fixed to 0.8m, height derived).
- File: `PdfPageDisplay.cs` (edit)
- AC: Portrait and landscape pages both render without distortion.

**US-09 — First-show placement in front of head** · S · US-08
- On first `Show` of a session, position Quad ~1.5m in front of `Camera.main`, then stop following.
- File: `PdfPageDisplay.cs` (edit)
- AC: Quad appears directly in front of user on first message; stays in world space after head turns.

**US-10 — Meta XR Interaction SDK spike (timeboxed, 1 day max)** · M · no deps
- Throwaway branch/scene only. Get one `PokeInteractable` button logging "pressed" and one `Grabbable` cube working with hand tracking. Write internal "how to add an interactable" note.
- AC: Personally demonstrate hand-poke and hand-grab on device. Do this before committing to US-11 and US-13 estimates.

**US-11 — Grab-and-place the Quad** · M · US-09, US-10
- Attach `Grabbable` + `HandGrabInteractable` to the PDF Quad. User pinch-grabs, moves, releases — stays in place.
- Files: Quad prefab; minor edits to `PdfPageDisplay.cs`
- AC: On device, user places document, releases — it stays. Subsequent page updates don't reset position.

### Phase 4 — Send page changes back to the server

**US-12 — POST helper in `SignalingHttpClient`** · S · no deps
- Add `PostJsonAsync<TRequest, TResponse>(string url, TRequest body, ...)` using `UnityWebRequest` with `UploadHandlerRaw` + `DownloadHandlerBuffer`.
- File: `UnityFiles/Assets/Scripts/Network/SignalingHttpClient.cs` (append)
- AC: POST to test endpoint returns parsed response; failure fires `onError`.

**US-13 — Prev/Next 3D buttons** · M · US-10, US-12
- Two `PokeInteractable` GameObjects parented to the Quad prefab. Next/Prev bounded by `totalPages`. On press, POST `{ documentId, page }` to `/documents/select`. No optimistic update — wait for server broadcast.
- Files: `UnityFiles/Assets/Scripts/Display/PageNavButtons.cs`; Quad prefab edits
- AC: Pressing Next triggers POST → server broadcasts new page → Quad updates. Prev on page 0 and Next on last page are no-ops.

### Phase 5 — Production readiness

**US-14 — Texture lifecycle + loading + errors** · M · US-03, US-06
- Three concerns in one refactor pass on `PdfPageDisplay.cs`:
  1. Destroy old texture before assigning new one (avoid per-page memory leak).
  2. Show placeholder/grey Quad while PNG is downloading.
  3. Show error texture on `TextureDownloader` failure; log to `Debug.LogError`.
- AC: Flip through 20 pages quickly — no memory growth in profiler. Disconnect wifi, fire fake message — error texture appears, no crash.

---

## Story status

| ID | Title | Status |
|---|---|---|
| US-01 | `PdfPageDisplay` scaffold | ✅ Done |
| US-02 | PNG-URL-to-Texture download helper | — |
| US-03 | End-to-end render with hardcoded URL | — |
| US-04 | `DocumentShowMessage` DTO | — |
| US-05 | Dispatch `document.show` in `SignalingClient` | — |
| US-06 | Connect event to display | — |
| US-07 | Editor-only dev harness | — |
| US-08 | Aspect-ratio-aware Quad sizing | — |
| US-09 | First-show placement in front of head | — |
| US-10 | Meta XR SDK spike | — |
| US-11 | Grab-and-place the Quad | — |
| US-12 | POST helper in `SignalingHttpClient` | — |
| US-13 | Prev/Next 3D buttons | — |
| US-14 | Texture lifecycle + loading + errors | — |
