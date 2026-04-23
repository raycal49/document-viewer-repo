using UnityEngine;

public class PdfPageDisplay : MonoBehaviour
{
    [SerializeField] private Texture2D _testTexture;
    [SerializeField] private string _testUrl;
    [SerializeField] private bool _isDevMode;

    private GameObject _quad;
    private Material _material;
    private Texture2D _currentTexture;

    private void Awake()
    {
        _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _quad.name = "PdfPageQuad";
        _quad.transform.SetParent(transform);
        _quad.transform.localPosition = Vector3.zero;
        _quad.transform.localRotation = Quaternion.identity;
        _quad.transform.localScale = new Vector3(0.8f, 1.067f, 1f); // portrait default (letter ratio)

        _material = new Material(Shader.Find("Unlit/Texture"));
        _quad.GetComponent<Renderer>().material = _material;
        _quad.SetActive(false);
    }

    private void Start()
    {
        if (_isDevMode && !string.IsNullOrEmpty(_testUrl))
            ShowFromUrl(_testUrl);
    }

    public void Show(Texture2D tex)
    {
        if (tex == null) return;
        _currentTexture = tex;
        _material.mainTexture = tex;
        _quad.SetActive(true);
    }

    public void ShowFromUrl(string url)
    {
        StartCoroutine(TextureDownloader.Download(
            url,
            Show,
            err => Debug.LogError($"PdfPageDisplay: texture download failed — {err}")));
    }

    [ContextMenu("Test Show")]
    private void TestShow()
    {
        if (_testTexture != null)
            Show(_testTexture);
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
