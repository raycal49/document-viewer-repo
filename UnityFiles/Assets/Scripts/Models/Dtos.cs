using System;
using UnityEngine;

[Serializable] public class NegotiateResponse { public string url; }
[Serializable] public class IceConfigResponse { public IceServerData[] iceServers; }
[Serializable] public class IceServerData { public string[] urls; public string username; public string credential; }
[Serializable] public class AnnotationPoint { public float x; public float y; }
[Serializable] public class AnnotationData {
  public string Type {get; set;}
  public float[][] Vector {get; set;}
  public int[] Color {get;set;}
  public string StrokeType {get;set;}
  public float FadeDuration {get;set;}
  }
[Serializable] public class ParsedAnnotation {
  public AnnotationPoint[] points;
  public string color;
  public bool isFading;
  public float FadeDurationSeconds;
  }

[Serializable] public class DocumentStartMessage
{
    public string documentId;
    public string documentName;
    public int totalPages;
}

[Serializable] public class DocumentPageMessage
{
    public string documentId;
    public int pageIndex;
    public int totalPages;
    public int width;
    public int height;
    public int chunkIndex;
    public int totalChunks;
    public byte[] data;
}

[Serializable] public class DocumentCloseMessage
{
    public string documentId;
}

[Serializable] public class DocumentRequestPageMessage
{
    public string documentId;
    public int pageIndex;
}

[Serializable] public class DocumentNavigateMessage
{
    public string documentId;
    public int pageIndex;
    public string source;
}
