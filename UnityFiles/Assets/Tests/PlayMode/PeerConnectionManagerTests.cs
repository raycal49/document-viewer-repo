using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Unity.WebRTC;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class PeerConnectionManagerTests
{
    private GameObject _go;
    private PeerConnectionManager _manager;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("PeerConnectionManager");
        _manager = _go.AddComponent<PeerConnectionManager>();
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
    public IEnumerator SetupPeerConnection_WithNullIceConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _manager.SetupPeerConnection(null));

        yield break;
    }

    [UnityTest]
    public IEnumerator SetupPeerConnection_WithInvalidIceConfig_ThrowsArgumentException()
    {
        var iceConfig = new IceConfigResponse
        {
            iceServers = Array.Empty<IceServerData>()
        };

        Assert.Throws<ArgumentException>(() => _manager.SetupPeerConnection(iceConfig));

        yield break;
    }

    [UnityTest]
    public IEnumerator SetupPeerConnection_WithValidIceConfig_SetsIsReadyTrue()
    {
        try
        {
            var probe = new RTCConfiguration
            {
                iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
            };
            using var c = new RTCPeerConnection(ref probe);
            c.Close();
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Skipped: native WebRTC is not available in this environment. ({ex.Message})");
            yield break;
        }
 
        var iceConfig = new IceConfigResponse
        {
            iceServers = new[]
            {
                new IceServerData
                {
                    urls       = new[] { "stun:stun.l.google.com:19302" },
                    username   = string.Empty,
                    credential = string.Empty
                }
            }
        };

        _manager.SetupPeerConnection(iceConfig);

        Assert.IsTrue(_manager.IsReady);

        yield break;
    }
}
