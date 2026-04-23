using NUnit.Framework;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

[TestFixture]
public class AnnotationManagerPlayModeTests: MonoBehaviour
{
    private GameObject _go;
    private AnnotationManager _manager;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("AnnotationGetterTest");
        _manager = _go.AddComponent<AnnotationManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
    }

    [Test]
    public void HandleMessage_ValidJsonWithPointsAndColor_CorrectDataIsSentWhenEventFired()
    {
        ParsedAnnotation result = null;
        _manager.OnAnnotationReceived += a => result = a;
        
        var json = "{\"Vector\":[[1.0,2.0]],\"Color\":[255,0,0]}";
        _manager.HandleMessage(json);

        Assert.That(result.color, Is.EqualTo("#FF0000"));
        Assert.That(result.points[0].x, Is.EqualTo(1.0f));
        Assert.That(result.points[0].y, Is.EqualTo(2.0f));
    }
}
