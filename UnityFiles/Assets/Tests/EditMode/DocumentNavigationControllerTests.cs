using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;

[TestFixture]
public class DocumentNavigationControllerTests
{
    private GameObject _go;
    private DocumentManager _documentManager;
    private DocumentNavigationController _navigationController;
    private DocumentNavigationChannel _navigationChannel;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("DocumentNavigationControllerTests");
        _documentManager = _go.AddComponent<DocumentManager>();
        _navigationChannel = _go.AddComponent<DocumentNavigationChannel>();
        _navigationController = _go.AddComponent<DocumentNavigationController>();
        _navigationController.Configure(_documentManager, _navigationChannel);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
    }

    [Test]
    public void NavigateNext_EmitsOutboundNavigateAndUpdatesCurrentPage()
    {
        _documentManager.HandleMessage("{\"type\":\"document-start\",\"documentName\":\"doc-123\",\"documentName\":\"Manual\",\"totalPages\":4}");

        string outboundJson = null;
        _navigationChannel.OnOutboundMessageSerialized += json => outboundJson = json;

        var moved = _navigationController.NavigateNext("quest-next-button");

        Assert.IsTrue(moved);
        Assert.AreEqual(1, _documentManager.CurrentPageIndex);
        Assert.NotNull(outboundJson);

        var root = JObject.Parse(outboundJson);
        Assert.AreEqual("document-navigate", (string)root["type"]);
        Assert.AreEqual("doc-123", (string)root["documentName"]);
        Assert.AreEqual(1, (int)root["pageIndex"]);
        Assert.AreEqual("quest-next-button", (string)root["source"]);
    }

    [Test]
    public void InboundNavigateMessage_IsAppliedByController()
    {
        _documentManager.HandleMessage("{\"type\":\"document-start\",\"documentName\":\"doc-123\",\"documentName\":\"Manual\",\"totalPages\":4}");

        _navigationChannel.TryHandleInboundMessage("{\"type\":\"document-navigate\",\"documentName\":\"doc-123\",\"pageIndex\":99,\"source\":\"remote\"}");

        Assert.AreEqual(3, _documentManager.CurrentPageIndex);
    }

    [Test]
    public void InboundRequestPage_DoesNotRebroadcast()
    {
        _documentManager.HandleMessage("{\"type\":\"document-start\",\"documentName\":\"doc-123\",\"documentName\":\"Manual\",\"totalPages\":4}");

        var outboundCount = 0;
        _navigationChannel.OnOutboundMessageSerialized += _ => outboundCount++;

        _navigationChannel.TryHandleInboundMessage("{\"type\":\"document-request-page\",\"documentName\":\"doc-123\",\"pageIndex\":2}");

        Assert.AreEqual(2, _documentManager.CurrentPageIndex);
        Assert.AreEqual(0, outboundCount);
    }
}
