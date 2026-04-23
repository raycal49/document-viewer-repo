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