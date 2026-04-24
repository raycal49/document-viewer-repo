using System;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class PdfPageDisplayTests
{
    [Test]
    public void TryDecodeJpegBytes_ReturnsFalse_ForEmptyPayload()
    {
        var result = PdfPageDisplay.TryDecodeJpegBytes(Array.Empty<byte>(), out var texture);

        Assert.IsFalse(result);
        Assert.IsNull(texture);
    }

    [Test]
    public void TryDecodeJpegBytes_ReturnsTrue_ForValidJpegPayload()
    {
        // 1x1 JPEG
        var jpegBytes = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAkGBxAQEBUQEA8VFRUVFRUVFRUVFRUVFRUXFhUVFRUYHSggGBolGxUVITEhJSkrLi4uFx8zODMsNygtLisBCgoKDg0OGxAQGy0fHyUtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLf/AABEIAAEAAQMBEQACEQEDEQH/xAAXAAEBAQEAAAAAAAAAAAAAAAAAAQID/8QAFhEBAQEAAAAAAAAAAAAAAAAAABEB/9oADAMBAAIQAxAAAAGfAqf/xAAVEAEBAAAAAAAAAAAAAAAAAAABAP/aAAgBAQABBQJf/8QAFhEBAQEAAAAAAAAAAAAAAAAAABEB/9oACAEDAQE/AV//xAAVEQEBAAAAAAAAAAAAAAAAAAABEP/aAAgBAgEBPwFf/8QAFBABAAAAAAAAAAAAAAAAAAAAEP/aAAgBAQAGPwJf/8QAFBABAAAAAAAAAAAAAAAAAAAAEP/aAAgBAQABPyFf/9k=");

        var result = PdfPageDisplay.TryDecodeJpegBytes(jpegBytes, out var texture);

        Assert.IsTrue(result);
        Assert.NotNull(texture);
        Assert.Greater(texture.width, 0);
        Assert.Greater(texture.height, 0);

        UnityEngine.Object.DestroyImmediate(texture);
    }
}
