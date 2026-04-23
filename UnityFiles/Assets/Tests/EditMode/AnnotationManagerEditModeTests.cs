using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

[TestFixture]
public class AnnotationManagerTests
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
    public void HandleMessage_EmptyPointsArray_DoesNotFireEvent()
    {
        bool fired = false;
        _manager.OnAnnotationReceived += _ => fired = true;

        var json = "{\"Vector\":[],\"Color\":[255,0,0]}";
        _manager.HandleMessage(json);

        Assert.IsFalse(fired);
    }

    [Test]
    public void HandleMessage_NullPoints_DoesNotFireEvent()
    {
        bool fired = false;
       _manager.OnAnnotationReceived += _ => fired = true;

        var json = "{\"Vector\":null,\"Color\":[255,0,0]}";
        _manager.HandleMessage(json);

        Assert.IsFalse(fired);
    }

    [Test]
    public void HandleMessage_ValidPointsMissingColor_DoesNotFireEvent()
    {
        bool fired = false;
        _manager.OnAnnotationReceived += _ => fired = true;

        var json = "{\"Vector\":[[1.0,2.0]],\"Color\":null}";
        _manager.HandleMessage(json);

        Assert.IsFalse(fired);
    }
}