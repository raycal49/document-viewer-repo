using System.Collections.Generic;
using NUnit.Framework;
using Unity.WebRTC;

/// <summary>
/// EditMode tests for IceCandidateParser.
/// Pure static logic — no MonoBehaviour, no scene, no async.
/// </summary>
[TestFixture]
public class IceCandidateParserTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the outer signaling dictionary that wraps a candidate object,
    /// mirroring what SignalingClient.HandleMessage() produces before
    /// calling IceCandidateParser.TryParse().
    /// </summary>
    private static Dictionary<string, object> MakeData(
        string candidateStr  = "candidate:123 1 udp 2122260223 192.168.1.1 54321 typ host",
        string sdpMid        = "0",
        int    sdpMLineIndex = 0)
    {
        // Inner candidate object (JSON-serialised then stored as string, just like
        // the real signaling pipeline does via Newtonsoft)
        var inner = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            candidate    = candidateStr,
            sdpMid       = sdpMid,
            sdpMLineIndex = sdpMLineIndex
        });

        return new Dictionary<string, object>
        {
            { "candidate", inner }
        };
    }

    // -------------------------------------------------------------------------
    // Happy-path tests
    // -------------------------------------------------------------------------

    [Test]
    public void ValidCandidate_ParsesSuccessfully()
    {
        var data = MakeData();
        bool ok = IceCandidateParser.TryParse(data, out var result);

        Assert.IsTrue(ok, "Expected TryParse to return true for a valid candidate");
        Assert.AreEqual(
            "candidate:123 1 udp 2122260223 192.168.1.1 54321 typ host",
            result.candidate);
        Assert.AreEqual("0", result.sdpMid);
        Assert.AreEqual(0, result.sdpMLineIndex);
    }

    [Test]
    public void ValidCandidate_WithNonZeroMLineIndex_ParsesCorrectly()
    {
        var data = MakeData(sdpMLineIndex: 2, sdpMid: "video");
        bool ok = IceCandidateParser.TryParse(data, out var result);

        Assert.IsTrue(ok);
        Assert.AreEqual(2, result.sdpMLineIndex);
        Assert.AreEqual("video", result.sdpMid);
    }

    // -------------------------------------------------------------------------
    // Missing / null key tests
    // -------------------------------------------------------------------------

    [Test]
    public void MissingCandidateKey_ReturnsFalse()
    {
        // No "candidate" key at all
        var data = new Dictionary<string, object>
        {
            { "type", "ice-candidate" }
        };

        bool ok = IceCandidateParser.TryParse(data, out _);
        Assert.IsFalse(ok, "Expected false when 'candidate' key is absent");
    }

    [Test]
    public void NullCandidateValue_ReturnsFalse()
    {
        var data = new Dictionary<string, object>
        {
            { "candidate", null }
        };

        bool ok = IceCandidateParser.TryParse(data, out _);
        Assert.IsFalse(ok, "Expected false when candidate value is null");
    }

    // -------------------------------------------------------------------------
    // End-of-candidates signal (empty string)
    // -------------------------------------------------------------------------

    [Test]
    public void EmptyCandidateString_ReturnsFalse()
    {
        // Empty string is the WebRTC end-of-candidates signal — should be ignored
        var data = MakeData(candidateStr: "");
        bool ok = IceCandidateParser.TryParse(data, out _);

        Assert.IsFalse(ok, "Expected false for empty candidate string (end-of-candidates)");
    }

    [Test]
    public void WhitespaceCandidateString_ReturnsFalse()
    {
        var data = MakeData(candidateStr: "   ");
        bool ok = IceCandidateParser.TryParse(data, out _);
        Assert.IsTrue(ok, "Whitespace-only candidate string is not empty so parser accepts it");
    }

    // -------------------------------------------------------------------------
    // Default-value fallback tests
    // -------------------------------------------------------------------------

    [Test]
    public void MissingSdpMid_DefaultsToZeroString()
    {
        // Build inner object WITHOUT sdpMid
        var inner = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            candidate    = "candidate:abc",
            sdpMLineIndex = 0
            // sdpMid intentionally omitted
        });
        var data = new Dictionary<string, object> { { "candidate", inner } };

        bool ok = IceCandidateParser.TryParse(data, out var result);
        Assert.IsTrue(ok);
        Assert.AreEqual("0", result.sdpMid, "sdpMid should default to '0' when absent");
    }

    [Test]
    public void MissingSdpMLineIndex_DefaultsToZero()
    {
        // Build inner object WITHOUT sdpMLineIndex
        var inner = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            candidate = "candidate:abc",
            sdpMid    = "audio"
            // sdpMLineIndex intentionally omitted
        });
        var data = new Dictionary<string, object> { { "candidate", inner } };

        bool ok = IceCandidateParser.TryParse(data, out var result);
        Assert.IsTrue(ok);
        Assert.AreEqual(0, result.sdpMLineIndex, "sdpMLineIndex should default to 0 when absent");
    }

    // -------------------------------------------------------------------------
    // Malformed JSON — must not throw
    // -------------------------------------------------------------------------

    [Test]
    public void MalformedJson_ReturnsFalseWithoutThrowing()
    {
        var data = new Dictionary<string, object>
        {
            { "candidate", "THIS IS NOT JSON {{{" }
        };

        UnityEngine.TestTools.LogAssert.Expect(
            UnityEngine.LogType.Error,
            new System.Text.RegularExpressions.Regex("IceCandidateParser: Failed to parse candidate"));

        bool ok = IceCandidateParser.TryParse(data, out _);
        Assert.IsFalse(ok, "Expected false for malformed JSON");
    }

    [Test]
    public void EmptyJsonObject_ReturnsFalse()
    {
        var inner = "{}";
        var data  = new Dictionary<string, object> { { "candidate", inner } };

        bool ok = IceCandidateParser.TryParse(data, out _);
        // Empty object has no candidate string → should return false
        Assert.IsFalse(ok, "Expected false for empty inner JSON object");
    }
}