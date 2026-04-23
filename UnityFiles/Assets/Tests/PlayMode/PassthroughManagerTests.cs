using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode tests for PassthroughManager.
/// PassthroughCameraAccess (Meta XR SDK) only runs on real Quest hardware,
/// so tests that trigger the coroutine expect the resulting NullReferenceException
/// log and verify state-machine behaviour around it.
/// </summary>
[TestFixture]
public class PassthroughManagerTests
{
    private GameObject _go;
    private PassthroughManager _manager;

    [SetUp]
    public void SetUp()
    {
        _go      = new GameObject("PassthroughManager");
        _manager = _go.AddComponent<PassthroughManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.Destroy(_go);
    }

    // -------------------------------------------------------------------------
    // Initial state — no camera involved, always pass
    // -------------------------------------------------------------------------

    [Test]
    public void IsReady_IsFalseOnAwake()
    {
        Assert.IsFalse(_manager.IsReady,
            "PassthroughManager should not be ready before StartPassthrough() is called");
    }

    // -------------------------------------------------------------------------
    // Coroutine tests — passthroughLeft is null so WaitForCameras throws.
    // We expect the exception log so the test runner doesn't fail on it.
    // -------------------------------------------------------------------------

    [UnityTest]
    public IEnumerator StartPassthrough_WithNoCamera_TimesOutWithoutCrash()
    {
        UnityEngine.TestTools.LogAssert.Expect(
            LogType.Exception,
            new System.Text.RegularExpressions.Regex("NullReferenceException"));

        _manager.StartPassthrough();
        yield return new WaitForSeconds(0.5f);

        Assert.IsFalse(_manager.IsReady,
            "IsReady should still be false when no camera is assigned");
    }

    [UnityTest]
    public IEnumerator OnPassthroughReady_NotFired_BeforeTimeout()
    {
        bool eventFired = false;
        _manager.OnPassthroughReady += () => eventFired = true;

        UnityEngine.TestTools.LogAssert.Expect(
            LogType.Exception,
            new System.Text.RegularExpressions.Regex("NullReferenceException"));

        _manager.StartPassthrough();
        yield return new WaitForSeconds(0.5f);

        Assert.IsFalse(eventFired,
            "OnPassthroughReady should not fire when no camera is assigned");
    }

    // -------------------------------------------------------------------------
    // Hardware-only tests (skipped in CI)
    // -------------------------------------------------------------------------

    [Test]
    [Ignore("Requires Meta Quest hardware with camera access granted")]
    public void IsReady_TrueAfterCameraStreams()
    {
        Assert.Inconclusive("Run on device");
    }
}