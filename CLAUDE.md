# Document Viewer — Project Context

## What we're building

A field technician wearing a Quest 3 headset is on a call with an expert at HQ using the web app. The expert pulls up a PDF (design/repair manual) from Azure storage, and both people can view and navigate it. The Quest user sees the document on a world-locked Quad in AR and can place it in physical space.

For v1, the document path is **PDF-only** and **peer-to-peer page transport** (web → Quest over WebRTC data channel). The backend remains in the loop for document listing and signaling, but not for page-image delivery.

---

## Architecture

### Page transport and rasterization (chosen)

- **Web app** downloads source PDFs from Azure Blob and rasterizes pages locally using `pdf.js`.
- Web app streams rendered pages to Quest over a **dedicated `documents` WebRTC data channel** (separate from annotations channel).
- Each page is JPEG, base64-encoded, chunked (~16 KB per chunk), and reassembled on Quest.
- **Quest no longer fetches page URLs** for normal operation.

### Storage (Azure Blob)

- Private container for source PDFs.
- No required v1 container for server-generated rasterized pages.
- Access control remains server-managed; clients do not receive account keys.

### Backend (`ar-signalingserver.azurewebsites.net`)

Backend responsibilities for v1:
- `GET /documents` — list available docs `{ id, title, pageCount, thumbnailUrl? }`
- Signaling broker for WebRTC setup (existing role)

Removed from v1 backend scope:
- Server-side rasterization pipeline
- `GET /documents/{id}/pages/{n}` page-image endpoint
- `POST /documents/select` server-owned page sync
- SAS page URL minting for Quest image fetch path

### Signaling and data channels

WebSocket signaling remains for connection establishment. Document interaction payloads move to the `documents` data channel.

Inbound to Quest (from web):
- `DocumentStartMessage { documentId, documentName, totalPages }`
- `DocumentPageMessage { documentId, pageIndex, totalPages, width, height, chunkIndex, totalChunks, data }`
- `DocumentCloseMessage { documentId }`

Outbound from Quest (to web):
- `DocumentRequestPageMessage { documentId, pageIndex }`
- `DocumentNavigateMessage { documentId, pageIndex, source }`

### State model

- Each side maintains its own current-page state.
- Default behavior: Quest follows expert navigation.
- Either side can disable follow and browse independently.
- Navigation sync is peer-to-peer via data channel messages (no server-owned canonical page).

### Unity (Quest) side

- `PdfPageDisplay` owns Quad + material and renders decoded bytes with:
  - `ShowFromBytes(byte[] jpegBytes, int width, int height)`
- `DocumentManager` owns:
  - chunk reassembly
  - page cache and eviction
  - routing decoded data to `PdfPageDisplay`
- Main-thread handoff uses the existing `ConcurrentQueue<string>` pattern already used by WebRTC code.
- UI is a World Space Canvas parented to the Quad:
  - document title
  - `TMP_InputField` page jump (`X / Y`)
  - Prev / Next
  - Follow toggle

### Memory / VRAM strategy

- Cache target: current page ±1.
- Destroy textures when evicted.
- Prefer lazy page loading for large PDFs:
  - Quest requests page N (`document-request-page`), web streams that page.

### Web app side

- Keep `DocumentPicker.tsx` for listing/selecting docs.
- Add `pdf.js` page render pipeline.
- Add chunked sender for `document-start/page/close` messages.
- Honor peer backpressure (`RTCDataChannel.bufferedAmount`) to avoid runaway queue growth.

---

## Key decisions

| Topic | Decision |
|---|---|
| Transport | Web rasterizes PDF pages and streams JPEG chunks over dedicated WebRTC `documents` data channel. |
| Format scope | PDF-only for v1. |
| Page sync | Independent local page state with follow toggle; no server-owned current page. |
| Users | 1 Quest + 1 web. No multi-party concerns in v1. |
| Placement | Quest user grabs and places the PDF; persists for session only. |
| Zoom / pan | Not implemented in v1; user walks closer. |
| Backend role | Document list + signaling broker only; out of page-image path. |

---

## Suggested phased build order

1. **Phase 1 — Documents channel scaffold + byte render path**
   - Open dedicated `documents` channel.
   - Hardcoded test payload renders through `ShowFromBytes`.
2. **Phase 2 — Web chunked stream from pdf.js**
   - Send `document-start/page/close` from web.
   - Quest reassembly and decode.
3. **Phase 3 — Bidirectional navigation + follow toggle**
   - `document-navigate` and `document-request-page` flows.
   - Independent page state behavior.
4. **Phase 4 — Quest document UI polish**
   - Canvas overlay, title, prev/next, jump-to-page input.
5. **Phase 5 — XR interaction integration**
   - Grab-and-place behaviors with Meta XR SDK.
6. **Phase 6 — Library hardening and resilience**
   - `GET /documents` production handling, error states, backpressure tuning, cache/eviction validation.

Each phase should remain shippable and independently demoable.

---

## Risks to watch

- **Data-channel backpressure:** unmanaged send rate can stall peers or spike memory. Gate send loop with `bufferedAmount` thresholds.
- **VRAM pressure on Quest:** page churn can leak textures without strict eviction/destruction.
- **Expert-side network instability:** architecture assumes expert web client has adequate connectivity; define degraded behavior if not.
- **Meta XR interaction ergonomics:** first real use of grab + in-world controls in this repo.

---

## Critical existing files

| File | Why it matters |
|---|---|
| `UnityFiles/Assets/Scripts/Network/PeerConnectionManager.cs` | Add/host dedicated `documents` data channel and callbacks |
| `UnityFiles/Assets/Scripts/Network/WebRTCSender.cs` | Existing `ConcurrentQueue` threading pattern to reuse |
| `UnityFiles/Assets/Scripts/Camera/VideoCompositor.cs` | Pattern for displaying texture content on 3D surface |
| `UnityFiles/Assets/Scripts/Models/Dtos.cs` | Home for document channel DTOs |
| `WebApp/src/components/DocumentPicker.tsx` | Existing document selection UX entry point |

---

## Testing philosophy (updated for data-channel architecture)

Testing stays pragmatic: validate high-risk logic and state transitions, avoid expensive mock-heavy coverage for thin wiring.

**What changed with the new architecture:**
- Focus shifts from HTTP endpoint wrappers to:
  - chunk reassembly correctness
  - navigation/follow state transitions
  - cache eviction and texture lifecycle
  - data-channel message parsing/dispatch

**Per-story check before writing tests:**
1. Is there pure logic/state behavior likely to regress silently?
2. Is the test cheaper than manual repeated validation?
3. Does this code carry memory/perf/concurrency risk?

If yes to any, add a targeted test.

**Preferred test types:**
- **EditMode tests** for DTO parsing, chunk assembly logic, page-state reducers, eviction math.
- **PlayMode tests** for MonoBehaviour lifecycle and texture replacement/destruction.
- **Manual on-device checks** for XR interaction, visual quality, real WebRTC behavior.

**Concrete high-value targets in this plan:**
- Reassembly handles out-of-order chunk arrival and duplicate chunk receipt.
- Incomplete/missing chunk sets time out cleanly and do not render corrupted textures.
- Follow toggle state machine (follow-on vs follow-off) behaves deterministically.
- Cache window (±1) evicts correctly and destroys textures.
- Data-channel message routing ignores unknown document IDs safely.

No mandatory test quota per story; test where risk and ROI justify it.

---

## Unity user stories

### Phase 1 — Byte-render foundation

**US-01 — `PdfPageDisplay` scaffold** · S · no deps
- Keep Quad/material foundation and core display ownership.
- Status: ✅ Done

**US-02 — URL texture helper (legacy/dev utility only)** · S · no deps
- Keep only if useful for isolated debug tooling.
- Not part of primary runtime path.
- Status: ✅ Done

**US-03 — End-to-end render bootstrap (bytes-only)** · S · US-01
- Build a bytes-only bootstrap path (no URL fetch) using a baked/dev-provided JPEG byte payload and `ShowFromBytes`.
- Acceptance: pressing Play renders page content through the same byte decode/render path used by runtime document messages.
- Status: 🔄 Revised (implementation update required).

**US-21 — Add `ShowFromBytes` on `PdfPageDisplay`** · S · US-01
- `ShowFromBytes(byte[] jpegBytes, int width, int height)` is the primary render API.

### Phase 2 — Documents data channel and chunk ingest

**US-15 — Dedicated `documents` data channel** · S · no deps
- Add second channel negotiation in `PeerConnectionManager`.

**US-04 — Document channel DTO set** · S · no deps
- Replace old `DocumentShowMessage` with start/page/close + outbound navigate/request DTOs.

**US-05 — Dispatch document messages from data channel** · S · US-04, US-15
- Parse and enqueue on main thread using existing queue pattern.

**US-06 — `DocumentManager` integration path** · M · US-05, US-21
- Route incoming messages to reassembly/cache/render pipeline.

**US-16 — Chunk reassembly + JPEG decode** · M · US-06
- Reassemble by `(documentId, pageIndex)` and decode into texture bytes.

**US-07 — Editor harness for chunk flow** · S · US-16
- Feed fake start/page chunks into `DocumentManager` without backend/web.

### Phase 3 — Navigation and sync semantics

**US-13 — Quest page controls wired to data channel** · M · US-05
- Prev/Next/jump send `document-navigate` or `document-request-page`.

**US-19 — `TMP_InputField` click-to-jump** · S · US-13
- Validate/bounds-clamp input and route through same navigation path.

**US-20 — Follow toggle behavior** · S · US-13
- Gate whether inbound remote navigation updates local current page.

**US-12 — HTTP navigation helper** · —
- **Dropped from scope** (no `/documents/select` flow in v1).

### Phase 4 — AR placement and memory hardening

**US-08 — Aspect-ratio-aware sizing** · S · US-21
- Keep page aspect correct regardless of source page dimensions.

**US-09 — First-show placement in front of head** · S · US-08
- One-time placement then remain world-locked.

**US-10 — Meta XR interaction spike** · M · no deps
- Validate grab + poke ergonomics on device.

**US-11 — Grab-and-place Quad** · M · US-09, US-10
- Parent Canvas with Quad so controls move together.

**US-14 — Texture lifecycle + loading + errors** · M · US-16
- Includes strict destroy/replace behavior and user-visible loading/error states.

**US-17 — Page cache ±1 eviction** · S · US-16, US-14
- Keep memory bounded and avoid stale texture buildup.

**US-18 — World Space Canvas document UI** · M · US-11, US-13
- Title + page indicator + nav controls + follow toggle.

---

## Story status

| ID | Title | Status |
|---|---|---|
| US-01 | `PdfPageDisplay` scaffold | ✅ Done |
| US-02 | URL texture helper (legacy/dev utility) | ✅ Done |
| US-03 | Bootstrap render path (bytes-only) | 🔄 Revised |
| US-04 | Document channel DTO set | — |
| US-05 | Data-channel dispatch | — |
| US-06 | Connect to `DocumentManager` | — |
| US-07 | Editor harness for chunk flow | — |
| US-08 | Aspect-ratio-aware Quad sizing | — |
| US-09 | First-show placement in front of head | — |
| US-10 | Meta XR SDK spike | — |
| US-11 | Grab-and-place the Quad | — |
| US-12 | HTTP POST helper | ❌ Dropped |
| US-13 | Quest navigation controls over data channel | — |
| US-14 | Texture lifecycle + loading + errors | — |
| US-15 | Dedicated `documents` data channel | — |
| US-16 | Chunk reassembly + JPEG decode | — |
| US-17 | Page cache ±1 eviction | — |
| US-18 | World Space Canvas UI overlay | — |
| US-19 | Click-to-jump page input | — |
| US-20 | Follow toggle behavior | — |
| US-21 | `ShowFromBytes` render API | ✅ Done |
