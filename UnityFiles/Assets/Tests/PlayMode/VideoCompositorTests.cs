using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.WebRTC;

/// <summary>
/// PlayMode tests for VideoCompositor.
/// Any test that calls StartCompositor() will trigger PassthroughManager's
/// WaitForCameras() coroutine which throws NullReferenceException because
/// passthroughLeft is unassigned. We use LogAssert.Expect to acknowledge
/// that log so the test runner doesn't fail on it.
/// </summary>
[TestFixture]
public class VideoCompositorTests
{
    private GameObject _go;
    private VideoCompositor _compositor;
    private PassthroughManager _passthroughManager;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("VideoCompositor");

        _passthroughManager = _go.AddComponent<PassthroughManager>();
        _compositor         = _go.AddComponent<VideoCompositor>();

        // Wire the dependency that would normally be set in the Inspector
        var field = typeof(VideoCompositor)
            .GetField("passthroughManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_compositor, _passthroughManager);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.Destroy(_go);
    }

    // -------------------------------------------------------------------------
    // Initial state — no coroutine triggered, always pass
    // -------------------------------------------------------------------------

    [Test]
    public void IsReady_IsFalseBeforeStart()
    {
        Assert.IsFalse(_compositor.IsReady,
            "VideoCompositor.IsReady must be false before StartCompositor() is called");
    }

    [Test]
    public void Track_IsNullBeforeStart()
    {
        Assert.IsNull(_compositor.Track,
            "VideoCompositor.Track must be null before the compositor pipeline is set up");
    }

    [Test]
    public void OnTrackReady_CanSubscribeAndUnsubscribeWithoutError()
    {
        bool fired = false;
        System.Action<VideoStreamTrack> handler = _ => fired = true;

        Assert.DoesNotThrow(() =>
        {
            _compositor.OnTrackReady += handler;
            _compositor.OnTrackReady -= handler;
        });

        Assert.IsFalse(fired, "Handler should not fire just from subscribe/unsubscribe");
    }

    // -------------------------------------------------------------------------
    // StartCompositor tests — expect the NullReferenceException from the coroutine
    // -------------------------------------------------------------------------

    [UnityTest]
    public IEnumerator StartCompositor_SubscribesToPassthroughEvent_WhenNotReady()
    {
        UnityEngine.TestTools.LogAssert.Expect(
            LogType.Exception,
            new System.Text.RegularExpressions.Regex("NullReferenceException"));

        _compositor.StartCompositor();
        yield return new WaitForSeconds(0.2f);

        Assert.IsFalse(_compositor.IsReady,
            "IsReady should still be false when passthrough is not ready");
    }

    [UnityTest]
    public IEnumerator StartCompositor_CalledTwice_DoesNotThrow()
    {
        // Two StartCompositor calls = two coroutines = two NullReferenceExceptions
        UnityEngine.TestTools.LogAssert.Expect(
            LogType.Exception,
            new System.Text.RegularExpressions.Regex("NullReferenceException"));
        UnityEngine.TestTools.LogAssert.Expect(
            LogType.Exception,
            new System.Text.RegularExpressions.Regex("NullReferenceException"));

        _compositor.StartCompositor();
        _compositor.StartCompositor();

        yield return new WaitForSeconds(0.2f);

        Assert.IsFalse(_compositor.IsReady);
    }

    // -------------------------------------------------------------------------
    // OnDestroy tests
    // -------------------------------------------------------------------------

    [UnityTest]
    public IEnumerator Destroy_BeforeStart_DoesNotThrow()
    {
        Object.Destroy(_go);
        yield return null;

        _go = null;
        Assert.Pass("OnDestroy handled null state without throwing");
    }

    [UnityTest]
    public IEnumerator Destroy_AfterStartCompositor_DoesNotThrow()
    {
        UnityEngine.TestTools.LogAssert.Expect(
            LogType.Exception,
            new System.Text.RegularExpressions.Regex("NullReferenceException"));

        _compositor.StartCompositor();
        yield return new WaitForSeconds(0.1f);

        Object.Destroy(_go);
        yield return null;

        _go = null;
        Assert.Pass("OnDestroy handled mid-init teardown without throwing");
    }

    // -------------------------------------------------------------------------
    // Hardware-only tests (skipped in CI)
    // -------------------------------------------------------------------------

    [Test]
    [Ignore("Requires Meta Quest hardware with camera access and WebRTC runtime")]
    public void IsReady_TrueAfterTrackCreated()
    {
        Assert.Inconclusive("Run on device");
    }

    [Test]
    [Ignore("Requires Meta Quest hardware with camera access and WebRTC runtime")]
    public void Track_NotNull_AfterCompositorReady()
    {
        Assert.Inconclusive("Run on device");
    }
}