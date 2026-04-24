using UnityEngine;

public class PdfPageDisplay : MonoBehaviour
{
    [Header("Dev bootstrap")]
    [Tooltip("When enabled, the component auto-renders a test source in Start().")]
    [SerializeField] private bool _isDevMode;
    [Tooltip("Optional Texture2D test source. If assigned, this takes priority in dev mode.")]
    [SerializeField] private Texture2D _testTexture;
    [Tooltip("Optional raw JPEG bytes test source (TextAsset). Used when Test Texture is not assigned.")]
    [SerializeField] private TextAsset _testJpegBytes;

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
        if (!_isDevMode)
        {
            return;
        }

        if (_testTexture != null)
        {
            Show(_testTexture);
            return;
        }

        if (_testJpegBytes != null)
        {
            ShowFromBytes(_testJpegBytes.bytes, 0, 0);
            return;
        }

        Debug.LogWarning("PdfPageDisplay: Dev mode is enabled but no test source is assigned (Test Texture or Test Jpeg Bytes).");
    }

    public void Show(Texture2D tex)
    {
        if (tex == null)
        {
            return;
        }

        _currentTexture = tex;
        _material.mainTexture = tex;
        _quad.SetActive(true);
    }

    public void ShowFromBytes(byte[] jpegBytes, int width, int height)
    {
        if (!TryDecodeJpegBytes(jpegBytes, out var decodedTexture))
        {
            Debug.LogError("PdfPageDisplay: failed to decode JPEG byte payload.");
            return;
        }

        if (width > 0 && height > 0 && (decodedTexture.width != width || decodedTexture.height != height))
        {
            Debug.LogWarning($"PdfPageDisplay: decoded dimensions ({decodedTexture.width}x{decodedTexture.height}) do not match payload metadata ({width}x{height}).");
        }

        Show(decodedTexture);
    }

    public static bool TryDecodeJpegBytes(byte[] jpegBytes, out Texture2D decodedTexture)
    {
        decodedTexture = null;

        if (jpegBytes == null || jpegBytes.Length == 0)
        {
            Debug.LogError("PdfPageDisplay: received empty JPEG byte payload.");
            return false;
        }

        var candidateTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!candidateTexture.LoadImage(jpegBytes, markNonReadable: false))
        {
            Destroy(candidateTexture);
            return false;
        }

        decodedTexture = candidateTexture;
        return true;
    }

    [ContextMenu("Test Show")]
    private void TestShow()
    {
        if (_testTexture != null)
        {
            Show(_testTexture);
            return;
        }

        if (_testJpegBytes != null)
        {
            ShowFromBytes(_testJpegBytes.bytes, 0, 0);
        }
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
