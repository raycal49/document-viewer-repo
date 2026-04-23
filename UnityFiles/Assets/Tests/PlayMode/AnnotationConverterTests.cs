#nullable enable
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools.Utils;

// we cannot test happy path for ViewportToWorld due to hardware requirements
public sealed class ViewportToWorldTests
{
    private GameObject? _go;
    private Camera? _projectionCamera;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("test_cam");
        _projectionCamera = _go.AddComponent<Camera>();
        _projectionCamera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -5f), Quaternion.identity);
        _projectionCamera.nearClipPlane = 0.1f;
        _projectionCamera.farClipPlane = 100f;
        _projectionCamera.orthographic = false;
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
    }

    [TestCase(0.5f, 0.5f)]
    [TestCase(0.25f, 0.75f)]
    [TestCase(0.9f, 0.1f)]
    public void ViewportToWorld_WhenRaycastManagerNull_ReturnsPointWith2mDepth(float x, float y)
    {
        var expectedRay = _projectionCamera.ViewportPointToRay(new Vector3(x, 1f - y, 0f));
        var expected = expectedRay.GetPoint(2f);
        var webPoint = new AnnotationPoint();
        webPoint.x = x;
        webPoint.y = y;

        var result = AnnotationConverter.ViewportToWorld(_projectionCamera, webPoint, 2f);

        Assert.That(result, Is.EqualTo(expected).Using(Vector3ComparerWithEqualsOperator.Instance));
    }

    [Test]
    public void ViewportToWorld_Throws_WhenCameraNull()
    {
        var webPoint = new AnnotationPoint();

        webPoint.x = 0.5f;
        webPoint.y = 0.5f;

        Assert.Throws<ArgumentNullException>(() =>
            AnnotationConverter.ViewportToWorld(null, webPoint));
    }
}