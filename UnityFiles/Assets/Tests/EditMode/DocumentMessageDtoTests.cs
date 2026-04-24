using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class DocumentMessageDtoTests
{
    [Test]
    public void DocumentStartMessage_ParsesFromJson()
    {
        const string json = "{\"documentId\":\"doc-1\",\"documentName\":\"Switchgear Guide\",\"totalPages\":99}";

        var parsed = JsonUtility.FromJson<DocumentStartMessage>(json);

        Assert.NotNull(parsed);
        Assert.AreEqual("doc-1", parsed.documentId);
        Assert.AreEqual("Switchgear Guide", parsed.documentName);
        Assert.AreEqual(99, parsed.totalPages);
    }

    [Test]
    public void DocumentPageMessage_ParsesAndDecodesBase64Data()
    {
        const string json = "{\"documentId\":\"doc-1\",\"pageIndex\":3,\"totalPages\":10,\"width\":1024,\"height\":768,\"chunkIndex\":1,\"totalChunks\":4,\"data\":\"AQIDBA==\"}";

        var parsed = JsonUtility.FromJson<DocumentPageMessage>(json);

        Assert.NotNull(parsed);
        Assert.AreEqual("doc-1", parsed.documentId);
        Assert.AreEqual(3, parsed.pageIndex);
        Assert.AreEqual(10, parsed.totalPages);
        Assert.AreEqual(1024, parsed.width);
        Assert.AreEqual(768, parsed.height);
        Assert.AreEqual(1, parsed.chunkIndex);
        Assert.AreEqual(4, parsed.totalChunks);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, parsed.data);
    }

    [Test]
    public void DocumentCloseMessage_ParsesFromJson()
    {
        const string json = "{\"documentId\":\"doc-1\"}";

        var parsed = JsonUtility.FromJson<DocumentCloseMessage>(json);

        Assert.NotNull(parsed);
        Assert.AreEqual("doc-1", parsed.documentId);
    }

    [Test]
    public void OutboundMessages_SerializeToExpectedJsonFields()
    {
        var request = new DocumentRequestPageMessage
        {
            documentId = "doc-1",
            pageIndex = 7
        };

        var navigate = new DocumentNavigateMessage
        {
            documentId = "doc-1",
            pageIndex = 8,
            source = "quest"
        };

        var requestJson = JsonUtility.ToJson(request);
        var navigateJson = JsonUtility.ToJson(navigate);

        StringAssert.Contains("\"documentId\":\"doc-1\"", requestJson);
        StringAssert.Contains("\"pageIndex\":7", requestJson);

        StringAssert.Contains("\"documentId\":\"doc-1\"", navigateJson);
        StringAssert.Contains("\"pageIndex\":8", navigateJson);
        StringAssert.Contains("\"source\":\"quest\"", navigateJson);
    }
}
