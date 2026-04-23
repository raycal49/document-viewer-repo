using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;

/// <summary>
/// EditMode tests for the message-parsing logic inside SignalingClient.
///
/// Because HandleMessage() is private, we extract and test the LOGIC directly
/// by replicating its parsing steps here. This is intentionally simple —
/// we're testing the data contract, not the MonoBehaviour lifecycle.
/// </summary>
[TestFixture]
public class SignalingMessageTests
{
    // -------------------------------------------------------------------------
    // Mirrors SignalingClient.HandleMessage() parsing logic
    // Returns the inner data dict, or null if the message should be ignored.
    // -------------------------------------------------------------------------

    private static Dictionary<string, object> ParseMessage(string raw, string localRoom)
    {
        var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(raw);
        if (msg == null || !msg.ContainsKey("data")) return null;

        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            msg["data"].ToString());

        if (data == null || !data.ContainsKey("type")) return null;

        // Ignore messages meant for a different room
        if (data.ContainsKey("room") && data["room"]?.ToString() != localRoom)
            return null;

        return data;
    }

    // -------------------------------------------------------------------------
    // Helper — builds a raw WS message string
    // -------------------------------------------------------------------------

    private static string MakeRaw(object dataPayload)
    {
        return JsonConvert.SerializeObject(new
        {
            from = "server",
            data = JsonConvert.SerializeObject(dataPayload)
        });
    }

    private const string MyRoom = "quest-abc123";

    // -------------------------------------------------------------------------
    // Structural guard tests
    // -------------------------------------------------------------------------

    [Test]
    public void MessageWithoutDataKey_IsIgnored()
    {
        var raw = JsonConvert.SerializeObject(new { from = "server" });
        var result = ParseMessage(raw, MyRoom);
        Assert.IsNull(result, "Messages missing 'data' key should be ignored");
    }

    [Test]
    public void MessageWithoutTypeField_IsIgnored()
    {
        var raw = MakeRaw(new { room = MyRoom, sdp = "v=0..." }); // no type
        var result = ParseMessage(raw, MyRoom);
        Assert.IsNull(result, "Messages missing 'type' field should be ignored");
    }

    [Test]
    public void EmptyJsonMessage_IsIgnoredWithoutThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            var result = ParseMessage("{}", MyRoom);
            Assert.IsNull(result);
        });
    }

    // -------------------------------------------------------------------------
    // Room-filter tests
    // -------------------------------------------------------------------------

    [Test]
    public void MessageForDifferentRoom_IsIgnored()
    {
        var raw = MakeRaw(new { type = "call-accepted", room = "quest-OTHER" });
        var result = ParseMessage(raw, MyRoom);
        Assert.IsNull(result, "Messages for a different room must be ignored");
    }

    [Test]
    public void MessageForCorrectRoom_IsNotIgnored()
    {
        var raw = MakeRaw(new { type = "call-accepted", room = MyRoom });
        var result = ParseMessage(raw, MyRoom);
        Assert.IsNotNull(result, "Messages for the correct room should be processed");
    }

    [Test]
    public void MessageWithNoRoomField_IsNotFilteredOut()
    {
        // Some server messages (e.g. system acks) have no room field — those
        // should still pass through the room filter
        var raw = MakeRaw(new { type = "ack" });
        var result = ParseMessage(raw, MyRoom);
        Assert.IsNotNull(result, "Messages with no room field should not be filtered out");
    }

    // -------------------------------------------------------------------------
    // Type recognition tests
    // -------------------------------------------------------------------------

    [Test]
    public void CallAccepted_TypeIsRecognized()
    {
        var raw    = MakeRaw(new { type = "call-accepted", room = MyRoom });
        var result = ParseMessage(raw, MyRoom);

        Assert.IsNotNull(result);
        Assert.AreEqual("call-accepted", result["type"].ToString());
    }

    [Test]
    public void CallDeclined_TypeIsRecognized()
    {
        var raw    = MakeRaw(new { type = "call-declined", room = MyRoom });
        var result = ParseMessage(raw, MyRoom);

        Assert.IsNotNull(result);
        Assert.AreEqual("call-declined", result["type"].ToString());
    }

    [Test]
    public void AnswerMessage_ExtractsSdpCorrectly()
    {
        const string fakeSdp = "v=0\r\no=- 12345 2 IN IP4 127.0.0.1\r\n";
        var raw    = MakeRaw(new { type = "answer", room = MyRoom, sdp = fakeSdp });
        var result = ParseMessage(raw, MyRoom);

        Assert.IsNotNull(result);
        Assert.AreEqual("answer", result["type"].ToString());
        Assert.AreEqual(fakeSdp, result["sdp"].ToString(),
            "SDP string must survive round-trip serialization unchanged");
    }

    [Test]
    public void IceCandidate_TypeIsRecognized()
    {
        var raw = MakeRaw(new
        {
            type      = "ice-candidate",
            room      = MyRoom,
            candidate = new { candidate = "candidate:abc", sdpMid = "0", sdpMLineIndex = 0 }
        });
        var result = ParseMessage(raw, MyRoom);

        Assert.IsNotNull(result);
        Assert.AreEqual("ice-candidate", result["type"].ToString());
        Assert.IsTrue(result.ContainsKey("candidate"),
            "ice-candidate message must carry a 'candidate' field");
    }

    // -------------------------------------------------------------------------
    // Edge cases
    // -------------------------------------------------------------------------

    [Test]
    public void UnknownType_ParsesWithoutError()
    {
        // Unknown types should not throw — SignalingClient uses a switch that falls through
        var raw    = MakeRaw(new { type = "some-future-message-type", room = MyRoom });
        var result = ParseMessage(raw, MyRoom);

        Assert.IsNotNull(result, "Unknown type should still parse (switch fall-through)");
        Assert.AreEqual("some-future-message-type", result["type"].ToString());
    }

    [Test]
    public void AnswerMessage_MissingSdp_DoesNotThrow()
    {
        // Simulate a malformed answer with no sdp key
        var raw = MakeRaw(new { type = "answer", room = MyRoom });
        Assert.DoesNotThrow(() =>
        {
            var result = ParseMessage(raw, MyRoom);
            // result will be non-null (type is present), but sdp key is missing
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ContainsKey("sdp"),
                "Malformed answer should have no sdp key");
        });
    }
}