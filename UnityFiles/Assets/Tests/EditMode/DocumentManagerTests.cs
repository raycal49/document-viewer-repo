using NUnit.Framework;
using Newtonsoft.Json.Linq;
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
    public void HandleMessage_DocumentPage_RequiresAllChunksBeforeEvent()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        DocumentPageMessage observed = null;
        _manager.OnDocumentPage += msg => observed = msg;

        const string chunk0 = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQI=\"}";
        _manager.HandleMessage(chunk0);

        Assert.IsNull(observed);

        const string chunk1 = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":1,\"totalChunks\":2,\"data\":\"AwQ=\"}";
        _manager.HandleMessage(chunk1);

        Assert.NotNull(observed);
        Assert.AreEqual("doc-123", observed.documentId);
        Assert.AreEqual(1, observed.pageIndex);
        Assert.AreEqual(1, observed.totalChunks);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, observed.data);
        Assert.AreEqual(1, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentPage_OutOfOrderChunks_AssemblesCorrectly()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        DocumentPageMessage observed = null;
        _manager.OnDocumentPage += msg => observed = msg;

        const string chunk1 = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":2,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":1,\"totalChunks\":2,\"data\":\"AwQ=\"}";
        const string chunk0 = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":2,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQI=\"}";

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
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        var pageEventCount = 0;
        _manager.OnDocumentPage += _ => pageEventCount++;

        const string chunk0 = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":3,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"AQI=\"}";
        const string chunk0dup = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":3,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":2,\"data\":\"BQY=\"}";
        const string chunk1 = "{\"type\":\"document-page\",\"documentId\":\"doc-123\",\"pageIndex\":3,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":1,\"totalChunks\":2,\"data\":\"AwQ=\"}";

        _manager.HandleMessage(chunk0);
        _manager.HandleMessage(chunk0dup);
        _manager.HandleMessage(chunk1);

        Assert.AreEqual(1, pageEventCount);
        Assert.AreEqual(3, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentPage_ForWrongDocument_IsIgnored()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":12}";
        _manager.HandleMessage(startJson);

        var pageCalled = false;
        _manager.OnDocumentPage += _ => pageCalled = true;

        const string pageJson = "{\"type\":\"document-page\",\"documentId\":\"doc-OTHER\",\"pageIndex\":1,\"totalPages\":12,\"width\":900,\"height\":1200,\"chunkIndex\":0,\"totalChunks\":1,\"data\":\"AQI=\"}";
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

    [Test]
    public void NavigateNext_WhenDocumentOpen_UpdatesPageAndEmitsNavigateMessage()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":3}";
        _manager.HandleMessage(startJson);

        string outboundJson = null;
        DocumentNavigateMessage observedNavigate = null;
        _manager.OnOutboundDocumentMessage += json => outboundJson = json;
        _manager.OnDocumentNavigate += msg => observedNavigate = msg;

        var sent = _manager.NavigateNext("quest-next-button");

        Assert.IsFalse(sent, "No data channel is attached in this EditMode test, so send should return false.");
        Assert.NotNull(outboundJson);
        Assert.NotNull(observedNavigate);
        Assert.AreEqual(1, _manager.CurrentPageIndex);

        var root = JObject.Parse(outboundJson);
        Assert.AreEqual("document-navigate", (string)root["type"]);
        Assert.AreEqual("doc-123", (string)root["documentId"]);
        Assert.AreEqual(1, (int)root["pageIndex"]);
        Assert.AreEqual("quest-next-button", (string)root["source"]);
    }

    [Test]
    public void NavigatePrevious_AtFirstPage_DoesNotEmitMessage()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":3}";
        _manager.HandleMessage(startJson);

        var outboundCalled = false;
        _manager.OnOutboundDocumentMessage += _ => outboundCalled = true;

        var sent = _manager.NavigatePrevious("quest-prev-button");

        Assert.IsFalse(sent);
        Assert.IsFalse(outboundCalled);
        Assert.AreEqual(0, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentNavigate_ClampsAndUpdatesPage()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":3}";
        _manager.HandleMessage(startJson);

        DocumentNavigateMessage observed = null;
        _manager.OnDocumentNavigate += msg => observed = msg;

        const string navigateJson = "{\"type\":\"document-navigate\",\"documentId\":\"doc-123\",\"pageIndex\":99,\"source\":\"peer\"}";
        _manager.HandleMessage(navigateJson);

        Assert.NotNull(observed);
        Assert.AreEqual(2, observed.pageIndex);
        Assert.AreEqual(2, _manager.CurrentPageIndex);
    }

    [Test]
    public void HandleMessage_DocumentRequestPage_ClampsAndInvokesEvent()
    {
        const string startJson = "{\"type\":\"document-start\",\"documentId\":\"doc-123\",\"documentName\":\"Transformer Manual\",\"totalPages\":3}";
        _manager.HandleMessage(startJson);

        DocumentRequestPageMessage observed = null;
        _manager.OnDocumentRequestPage += msg => observed = msg;

        const string requestJson = "{\"type\":\"document-request-page\",\"documentId\":\"doc-123\",\"pageIndex\":-7}";
        _manager.HandleMessage(requestJson);

        Assert.NotNull(observed);
        Assert.AreEqual(0, observed.pageIndex);
        Assert.AreEqual(0, _manager.CurrentPageIndex);
    }
}
