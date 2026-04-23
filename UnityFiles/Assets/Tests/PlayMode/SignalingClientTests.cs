using NUnit.Framework;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TestTools;

public class SignalingClientTests
{
    private GameObject _go;
    private SignalingClient _client;
    private ConcurrentQueue<string> _queue;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("SignalingClient");
        _client = _go.AddComponent<SignalingClient>();
        _client.Configure("room-1", "caller-1");
        _queue = new ConcurrentQueue<string>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
        {
            Object.DestroyImmediate(_go);
        }
    }

    [UnityTest]
    public IEnumerator HandleMessage_CallAcceptedMessage_InvokesOnCallAccepted()
    {
        var invoked = false;
        _client.OnCallAccepted += () => invoked = true;

        var raw =
            @"{
            ""data"": {
                ""type"": ""call-accepted"",
                ""room"": ""room-1""
            }
        }";

        _client.HandleMessage(raw);

        Assert.IsTrue(invoked);
        yield break;
    }

    [UnityTest]
    public IEnumerator HandleMessage_CallDeclinedMessage_invokesOnCallDeclined()
    {
        var invoked = false;
        _client.OnCallDeclined += () => invoked = true;

        var raw =
            @"{
            ""data"": {
                ""type"": ""call-declined"",
                ""room"": ""room-1""
            }
        }";

        _client.HandleMessage(raw);

        Assert.IsTrue(invoked);
        yield break;
    }

    [UnityTest]
    public IEnumerator HandleMessage_AnswerMessage_InvokesOnAnswerReceived()
    {
        string receivedSdp = null;
        _client.OnAnswerReceived += sdp => receivedSdp = sdp;

        var raw =
            @"{
            ""data"": {
                ""type"": ""answer"",
                ""room"": ""room-1"",
                ""sdp"": ""sdp_contents""
            }
        }";

        _client.HandleMessage(raw);

        Assert.AreEqual("sdp_contents", receivedSdp);
        yield break;
    }

    [UnityTest]
    public IEnumerator HandleMessage_IceCandidateMessage_InvokesOnIceCandidateReceived()
    {
        Dictionary<string, object> received = null;
        _client.OnIceCandidateReceived += candidate => received = candidate;

        var raw =
            @"{
            ""data"": {
                ""type"": ""ice-candidate"",
                ""room"": ""room-1"",
                ""candidate"": {
                    ""candidate"": ""candidate_info"",
                    ""sdpMid"": ""0"",
                    ""sdpMLineIndex"": 0
                }
            }
        }";

        _client.HandleMessage(raw);

        Assert.AreEqual("ice-candidate", received["type"].ToString());
        Assert.AreEqual("room-1", received["room"].ToString());
        yield break;
    }

    [UnityTest]
    public IEnumerator HandleMessage_DifferentRoomMessage_DoesNotInvokeEvents()
    {
        var acceptedInvoked = false;
        _client.OnCallAccepted += () => acceptedInvoked = true;

        var raw =
            @"{
            ""data"": {
                ""type"": ""call-accepted"",
                ""room"": ""room-2""
            }
        }";

        _client.HandleMessage(raw);

        Assert.IsFalse(acceptedInvoked);
        yield break;
    }

    [UnityTest]
    public IEnumerator HandleMessage_UnknownDataType_DoesNotInvokeAnyHandlers()
    {
        var anyHandlerInvoked = false;

        _client.OnCallAccepted += () => anyHandlerInvoked = true;
        _client.OnCallDeclined += () => anyHandlerInvoked = true;
        _client.OnAnswerReceived += _ => anyHandlerInvoked = true;
        _client.OnIceCandidateReceived += _ => anyHandlerInvoked = true;

        var raw =
            @"{
            ""data"": {
                ""type"": ""something-unknown"",
                ""room"": ""room-1""
            }
        }";

        _client.HandleMessage(raw);

        Assert.IsFalse(anyHandlerInvoked);
        yield break;
    }
}