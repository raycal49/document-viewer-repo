using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class DocumentManagerTests
{
    private GameObject _go;
    private DocumentManager _manager;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("DocumentManagerTests");
        _manager = _go.AddComponent<DocumentManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
    }

    [Test]
    public void HandleMessage_DocumentStart_InvokesStartEvent()
    {
        DocumentStartMessage observed = null;
        _manager.OnDocumentStart += msg => observed = msg;

        const string json = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(json);

        Assert.NotNull(observed);
        Assert.AreEqual("doc-123", observed.documentId);
        Assert.AreEqual("Transformer Manual", observed.documentName);
        Assert.AreEqual(12, observed.totalPages);
    }

    [Test]
    public void HandleMessage_DocumentPage_InvokesPageEvent()
    {
        DocumentPageMessage observed = null;
        _manager.OnDocumentPage += msg => observed = msg;

        const string json = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQID\"}";
        _manager.HandleMessage(json);

        Assert.NotNull(observed);
        Assert.AreEqual("doc-123", observed.documentId);
        Assert.AreEqual(1, observed.pageIndex);
        Assert.AreEqual(2, observed.totalChunks);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, observed.data);
    }

    [Test]
    public void HandleMessage_DocumentClose_InvokesCloseEvent()
    {
        DocumentCloseMessage observed = null;
        _manager.OnDocumentClose += msg => observed = msg;

        const string json = "{\"type\":\"document-close\",\"documentId\":\"doc-123\"}";
        _manager.HandleMessage(json);

        Assert.NotNull(observed);
        Assert.AreEqual("doc-123", observed.documentId);
    }

    [Test]
    public void HandleMessage_UnknownType_DoesNotInvokeEvents()
    {
        var startCalled = false;
        var pageCalled = false;
        var closeCalled = false;

        _manager.OnDocumentStart += _ => startCalled = true;
        _manager.OnDocumentPage += _ => pageCalled = true;
        _manager.OnDocumentClose += _ => closeCalled = true;

        _manager.HandleMessage("{\"type\":\"document-unknown\"}");

        Assert.IsFalse(startCalled);
        Assert.IsFalse(pageCalled);
        Assert.IsFalse(closeCalled);
    }
}
