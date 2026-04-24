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
    public void HandleMessage_DocumentStart_InvokesStartEventAndUpdatesState()
    {
        DocumentStartMessage observed = null;
        _manager.OnDocumentStart += msg => observed = msg;

        const string json = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(json);

        Assert.NotNull(observed);
        Assert.AreEqual("doc-123", observed.documentId);
        Assert.AreEqual("Transformer Manual", observed.documentName);
        Assert.AreEqual(12, observed.totalPages);

        Assert.IsTrue(_manager.IsDocumentOpen);
        Assert.AreEqual("doc-123", _manager.CurrentDocumentId);
        Assert.AreEqual("Transformer Manual", _manager.CurrentDocumentName);
        Assert.AreEqual(12, _manager.TotalPages);
        Assert.AreEqual(0, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentPage_ForActiveDocument_InvokesPageEventAndUpdatesCurrentPage()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        DocumentPageMessage observed = null;
        _manager.OnDocumentPage += msg => observed = msg;

        const string pageJson = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQID\"}";
        _manager.HandleMessage(pageJson);

        Assert.NotNull(observed);
        Assert.AreEqual("doc-123", observed.documentId);
        Assert.AreEqual(1, observed.pageIndex);
        Assert.AreEqual(2, observed.totalChunks);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, observed.data);

        Assert.AreEqual(1, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentPage_ForWrongDocument_IsIgnored()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        var pageCalled = false;
        _manager.OnDocumentPage += _ => pageCalled = true;

        const string pageJson = "{\"type\":\"document-page\",\"documentId\":\"doc-OTHER\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQID\"}";
        _manager.HandleMessage(pageJson);

        Assert.IsFalse(pageCalled);
        Assert.AreEqual(0, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentClose_InvokesCloseEventAndClearsState()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        DocumentCloseMessage observed = null;
        _manager.OnDocumentClose += msg => observed = msg;

        const string closeJson = "{\"type\":\"document-close\",\"documentId\":\"doc-123\"}";
        _manager.HandleMessage(closeJson);

        Assert.NotNull(observed);
        Assert.AreEqual("doc-123", observed.documentId);

        Assert.IsFalse(_manager.IsDocumentOpen);
        Assert.IsNull(_manager.CurrentDocumentId);
        Assert.IsNull(_manager.CurrentDocumentName);
        Assert.AreEqual(0, _manager.TotalPages);
        Assert.AreEqual(-1, _manager.CurrentPageIndex);
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
