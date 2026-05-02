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
    public void HandleMessage_DocumentPage_RequiresAllChunksBeforeEvent()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        DocumentPageMessage observed = null;
        _manager.OnDocumentPage += msg => observed = msg;

        const string chunk0 = "{\"type\":\"document-page\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQI=\"}";
        _manager.HandleMessage(chunk0);

        Assert.IsNull(observed);

        const string chunk1 = "{\"type\":\"document-page\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":1,\"totalChunks\":2,\"data\":\"AwQ=\"}";
        _manager.HandleMessage(chunk1);

        Assert.NotNull(observed);
        Assert.AreEqual(1, observed.pageIndex);
        Assert.AreEqual(1, observed.totalChunks);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, observed.data);
        Assert.AreEqual(1, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentPage_OutOfOrderChunks_AssemblesCorrectly()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        DocumentPageMessage observed = null;
        _manager.OnDocumentPage += msg => observed = msg;

        const string chunk1 = "{\"type\":\"document-page\",\"pageIndex\":2,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":1,\"totalChunks\":2,\"data\":\"AwQ=\"}";
        const string chunk0 = "{\"type\":\"document-page\",\"pageIndex\":2,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQI=\"}";

        _manager.HandleMessage(chunk1);
        Assert.IsNull(observed);

        _manager.HandleMessage(chunk0);

        Assert.NotNull(observed);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, observed.data);
        Assert.AreEqual(2, observed.pageIndex);
    }

    [Test]
    public void HandleMessage_DuplicateChunk_DoesNotDoubleCount()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        var pageEventCount = 0;
        _manager.OnDocumentPage += _ => pageEventCount++;

        const string chunk0 = "{\"type\":\"document-page\",\"pageIndex\":3,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQI=\"}";
        const string chunk0dup = "{\"type\":\"document-page\",\"pageIndex\":3,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"BQY=\"}";
        const string chunk1 = "{\"type\":\"document-page\",\"pageIndex\":3,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":1,\"totalChunks\":2,\"data\":\"AwQ=\"}";

        _manager.HandleMessage(chunk0);
        _manager.HandleMessage(chunk0dup);
        _manager.HandleMessage(chunk1);

        Assert.AreEqual(1, pageEventCount);
        Assert.AreEqual(3, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentPage_ForWrongDocument_IsIgnored()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        var pageCalled = false;
        _manager.OnDocumentPage += _ => pageCalled = true;

        const string pageJson = "{\"type\":\"document-page\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":1,\"data\":\"AQI=\"}";
        _manager.HandleMessage(pageJson);

        Assert.IsFalse(pageCalled);
        Assert.AreEqual(0, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentClose_InvokesCloseEventAndClearsState()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        DocumentCloseMessage observed = null;
        _manager.OnDocumentClose += msg => observed = msg;

        const string closeJson = "{\"type\":\"document-close\",\"documentName\":\"Transformer Manual\"}";
        _manager.HandleMessage(closeJson);

        Assert.NotNull(observed);

        Assert.IsFalse(_manager.IsDocumentOpen);
        Assert.IsNull(_manager.CurrentDocumentName);
        Assert.AreEqual(0, _manager.TotalPages);
        Assert.AreEqual(-1, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_UnknownType_DoesNotInvokeEvents()
    {
        var pageCalled = false;
        var closeCalled = false;

        _manager.OnDocumentClose += _ => closeCalled = true;

        _manager.HandleMessage("{\"type\":\"document-unknown\"}");

        Assert.IsFalse(pageCalled);
        Assert.IsFalse(closeCalled);
    }

}
