/*
HOW IT WORKs:
1. Each Quest user gets a unique room ID: ex "quest-abc123"
2. Quest joins its private room AND the shared "lobby" group
3. Quest sends call-request--> server broadcasts it to the "lobby" group
4. All web clients are subscribed to "lobby" and see ALL incoming calls
5. Web client clicks Answer --> joins the specific Quest's private room
6. Server marks that call as "claimed" so other web clients see it disappear
7. SDP offer/answer and ICE candidates flow only in the private room
8. Once WebRTC connects, connection becomes peer-to-peer
*/


require("dotenv").config();
const express = require("express");
const { WebPubSubServiceClient } = require("@azure/web-pubsub");
const { WebPubSubEventHandler } = require("@azure/web-pubsub-express");
const { CommunicationIdentityClient } = require("@azure/communication-identity");

const app = express();
app.use(express.json());

const cors = require('cors');
app.use(cors({ origin: '*', methods: ['GET', 'POST'] }));

app.options('/eventhandler', (req, res) => {
  res.setHeader('WebHook-Allowed-Origin', '*');
  res.status(200).send();
});

//  Azure stuff
const connectionString = process.env.AZURE_WEBPUBSUB_CONNECTION_STRING;
const hubName = process.env.WEBPUBSUB_HUB_NAME || "AR_STREAM";
if (!connectionString) { console.error("AZURE_WEBPUBSUB_CONNECTION_STRING not set"); process.exit(1); }

const acsConnectionString = process.env.AZURE_COMMUNICATION_CONNECTION_STRING;
if (!acsConnectionString) { console.error("AZURE_COMMUNICATION_CONNECTION_STRING not set"); process.exit(1); }

const identityClient = new CommunicationIdentityClient(acsConnectionString);
const serviceClient  = new WebPubSubServiceClient(connectionString, hubName);

//  In memory call state 
// { [room]: { callerId, callerName, claimedBy: userId | null } }
const activeCalls = new Map();

const handler = new WebPubSubEventHandler(hubName, {
  path: "/eventhandler",

  handleConnect: async (req, res) => {
    console.log(`Client connected: ${req.context.userId || req.context.connectionId}`);
    res.success();
  },

  handleUserEvent: async (req, res) => {
    const data     = req.dataType === "json" ? req.data : JSON.parse(req.data);
    const senderId = req.context.userId || req.context.connectionId;
    const { room, type } = data;

    console.log(`[${room || 'lobby'}] "${type}" from ${senderId}`);

    switch (type) {

      //  Quest advertising itself to all web clients 
      case "call-request": {
        activeCalls.set(room, {
          callerId:   senderId,
          callerName: data.callerName || "Meta Quest User",
          claimedBy:  null,
        });
        await serviceClient.group("lobby").sendToAll(
          { type: "call-request", senderId, room, callerName: data.callerName || "Meta Quest User" },
          { contentType: "application/json" }
        );
        break;
      }

      //  Web client accepted 
      // The web client has ALREADY joined the private room client-side before
      // sending this, so the offer will reach it immediately after we notify
      // the Quest:  no server side addConnection race possible.
      case "call-accepted": {
        const call = activeCalls.get(room);
        if (!call) {
          console.warn(`call-accepted: no active call for room ${room}`);
          res.success();
          return;
        }

        if (call.claimedBy) {
          await serviceClient.sendToUser(senderId,
            { type: "call-already-claimed", room },
            { contentType: "application/json" }
          );
          res.success();
          return;
        }

        call.claimedBy = senderId;

        // Tell the Quest to proceed with the offer
        await serviceClient.group(room).sendToAll(
          { type: "call-accepted", senderId, room },
          { contentType: "application/json",
            filter: `connectionId ne '${req.context.connectionId}'` }
        );

        // Tell lobby this call is now taken
        await serviceClient.group("lobby").sendToAll(
          { type: "call-claimed", room, claimedBy: senderId },
          { contentType: "application/json" }
        );
        break;
      }

      case "call-declined": {
        await serviceClient.group(room).sendToAll(
          { type: "call-declined", senderId, room },
          { contentType: "application/json" }
        );
        break;
      }

      case "offer": {
        await serviceClient.group(room).sendToAll(
          { type: "offer", sdp: data.sdp, senderId, room },
          { contentType: "application/json",
            filter: `connectionId ne '${req.context.connectionId}'` }
        );
        break;
      }

      case "answer": {
        await serviceClient.group(room).sendToAll(
          { type: "answer", sdp: data.sdp, senderId, room },
          { contentType: "application/json",
            filter: `connectionId ne '${req.context.connectionId}'` }
        );
        break;
      }

      case "ice-candidate": {
        await serviceClient.group(room).sendToAll(
          { type: "ice-candidate", candidate: data.candidate, senderId, room },
          { contentType: "application/json",
            filter: `connectionId ne '${req.context.connectionId}'` }
        );
        break;
      }

      case "call-ended": {
        activeCalls.delete(room);
        await serviceClient.group("lobby").sendToAll(
          { type: "call-ended", room },
          { contentType: "application/json" }
        );
        break;
      }

      default:
        console.warn(`Unknown message type: ${type}`);
    }

    res.success();
  },
});
app.use(handler.getMiddleware());

app.get("/negotiate", async (req, res) => {
  const { userId, room, role = "viewer" } = req.query;
  if (!userId) return res.status(400).json({ error: "userId is required" });

  try {
    let roles;

    if (role === "caller" && room) {
      roles = [
        `webpubsub.joinLeaveGroup.${room}`,
        `webpubsub.sendToGroup.${room}`,
        `webpubsub.joinLeaveGroup.lobby`,
        `webpubsub.sendToGroup.lobby`,
      ];
    } else {
      // Viewer: wildcard join/send so it can enter any private room on accept
      roles = [
        `webpubsub.joinLeaveGroup`,
        `webpubsub.sendToGroup`,
      ];
    }

    const token = await serviceClient.getClientAccessToken({
      userId,
      roles,
      expirationTimeInMinutes: 60,
    });

    console.log(`Token issued — userId: ${userId}, role: ${role}`);
    res.json({ url: token.url, userId });

  } catch (err) {
    console.error("Failed to generate token:", err);
    res.status(500).json({ error: "Failed to generate access token" });
  }
});

app.get("/ice-config", async (req, res) => {
  try {
    const user = await identityClient.createUser();
    const expiresOn = new Date(Date.now() + 60 * 60 * 1000);
    const tokenResponse = await identityClient.getToken(user, ["voip"], { expiresOn });

    res.json({
      iceServers: [
        {
          urls: [
            "turn:relay.communication.microsoft.com:3478",
            "turn:relay.communication.microsoft.com:3478?transport=tcp",
            "turn:relay.communication.microsoft.com:443?transport=tcp",
          ],
          username: user.communicationUserId,
          credential: tokenResponse.token,
        },
        { urls: ["stun:stun.azure.com:3478"] },
      ],
    });
  } catch (err) {
    console.error("Failed to generate ICE config:", err);
    res.status(500).json({ error: "Failed to generate ICE configuration" });
  }
});

app.get("/active-calls", (req, res) => {
  const calls = [];
  for (const [room, call] of activeCalls.entries()) {
    if (!call.claimedBy) {
      calls.push({ room, callerName: call.callerName, callerId: call.callerId });
    }
  }
  res.json({ calls });
});

app.get("/health", (req, res) => res.json({ status: "ok", hub: hubName, activeCalls: activeCalls.size }));

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`\nSignaling server on http://localhost:${PORT}  hub=${hubName}\n`));
