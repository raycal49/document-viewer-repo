# AR Troubleshooting Application — Web App

The AR Troubleshooting Application enables real-time remote support between a field technician wearing a Meta Quest 3 headset (User A) and a remote expert using a web browser (User B). User A streams their passthrough camera feed; User B can annotate the live video, share documents, and record the session. All annotations appear as AR overlays in User A's field of view for hands-free guidance.

## Core Features

- **Live video streaming** — Meta Quest to web browser via WebRTC
- **Real-time annotations** — User B draws overlays displayed for User A
- **Document sharing** — User B sends manuals and reference materials to the headset
- **Contextual overlays** — Object labeling
- **Session recording**
- **Hands-free navigation** — Voice or gestures for reference materials

## Wiki

[AR Application for Troubleshooting in the field](https://dev.azure.com/salvadorgonzales111/0394f3bf-cf33-40cb-b416-7c90b252a6a8/_wiki/wikis/f036607e-529b-40e0-abf0-473ddd135c98?pagePath=%2FAR%20Application%20for%20Troubleshooting%20in%20the%20field)

---

## Running the Web App

### Prerequisites

- Node.js 20.19+ or 22.12+ (Vite 7 requirement)
- npm

### Commands

```bash
cd WebApp
npm install
npm run dev
```

Open `http://localhost:5173` in your browser.

To build for production:

```bash
npm run build
```

---

## Testing WebRTC Without Unity/Quest

Use the built-in test caller to simulate a Quest device:

1. Run the web app (`npm run dev`).
2. Open `http://localhost:5173` and click **Connect**.
3. In a second tab, open `http://localhost:5173/webrtc-caller-test.html`.
4. Click **Start Call** on the test caller.
5. The incoming call appears on the web app — click the green phone icon to accept.
6. The test pattern video from the caller should display in the main app.

The test caller uses a synthetic video stream (no camera) and can optionally use a webcam.

---

## Azure Connection

The web app connects to the signaling server at:

```
https://ar-signaling-server.azurewebsites.net
```

To use a different server, set the `VITE_SIGNALING_SERVER_URL` environment variable before building.

The server provides:

- **Negotiation** — PubSub WebSocket URL and auth token
- **ICE config** — TURN/STUN credentials (Azure Communication Services)
- **Active calls** — REST endpoint for calls that were ringing before connect

Signaling uses `sendToGroup` so messages go directly between clients in the lobby and room; no server event handler is required for basic operation.

---

## Project Structure

```
WebApp/
├── public/
│   └── webrtc-caller-test.html   Test caller (simulates Quest)
├── src/
│   ├── hooks/
│   │   └── useWebRTC.ts          Orchestrates signaling + WebRTC
│   ├── scenes/
│   │   └── videoCallPage/
│   │       └── index.tsx         Video call UI (connect, calls, video feed, log)
│   ├── services/
│   │   ├── signalingService.ts   Azure Web PubSub connection
│   │   └── webrtcService.ts      RTCPeerConnection (receive-only)
│   ├── types/
│   │   └── webrtc.ts             ConnectionStatus, IncomingCall, SignalingMessage
│   ├── App.tsx
│   ├── main.tsx
│   └── theme.ts
├── package.json
└── vite.config.ts
```

---

## Integration with Unity/Quest

When the Unity/Quest WebRTC client is ready:

1. The Quest connects to the same signaling server and negotiates with `role=caller` and a unique room ID.
2. The Quest joins the lobby and its private room, then broadcasts `call-request` to the lobby.
3. The web app (already in the lobby) sees the incoming call and can accept.
4. SDP offer/answer and ICE candidates are exchanged through the room.
5. Once WebRTC connects, media flows peer-to-peer.

The `signaling-server/` folder contains an alternative, modular server implementation. The deployed server may be Sal's `SignalingServer/` — both support the same client protocol.
