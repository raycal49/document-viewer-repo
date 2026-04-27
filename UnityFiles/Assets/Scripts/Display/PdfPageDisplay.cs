using UnityEngine;

public class PdfPageDisplay : MonoBehaviour
{
    [Header("Dev bootstrap")]
    [Tooltip("When enabled, the component auto-renders a test source in Start().")]
    [SerializeField] private bool _isDevMode;
    [SerializeField] private Material pageMaterialTemplate;
    [Header("Display surface")]
    [Tooltip("Renderer that displays decoded PDF page textures (typically PdfPageQuad's MeshRenderer).")]
    [SerializeField] private Renderer pageRenderer;
    [Tooltip("Optional Texture2D test source. If assigned, this takes priority in dev mode.")]
    [SerializeField] private Texture2D _testTexture;
    [Tooltip("Optional raw JPEG bytes test source (TextAsset). Used when Test Texture is not assigned.")]
    [SerializeField] private TextAsset _testJpegBytes;

    private Material _material;
    private Texture2D _currentTexture;
    private bool _ownsCurrentTexture;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        if (!_isDevMode)
            return;

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
            return;

        if (!EnsureInitialized())
            return;

        ReleaseOwnedTextureIfAny();

        _currentTexture = tex;
        _ownsCurrentTexture = false;
        _material.mainTexture = tex;
        pageRenderer.gameObject.SetActive(true);
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

        if (!EnsureInitialized())
        {
            Destroy(decodedTexture);
            return;
        }

        ReleaseOwnedTextureIfAny();
        _currentTexture = decodedTexture;
        _ownsCurrentTexture = true;
        _material.mainTexture = decodedTexture;
        pageRenderer.gameObject.SetActive(true);
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
            ShowFromBytes(_testJpegBytes.bytes, 0, 0);
    }

    private bool EnsureInitialized()
    {
        if (pageRenderer == null)
        {
            Debug.LogError("PdfPageDisplay: pageRenderer is not assigned. Assign PdfPageQuad renderer in Inspector.");
            return false;
        }

        if (_material != null)
            return true;

        if (pageMaterialTemplate != null)
        {
            _material = new Material(pageMaterialTemplate);
        }
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                Debug.LogError("[PdfPageDisplay] Missing shader and no pageMaterialTemplate assigned.");
                return false;
            }
            _material = new Material(shader);
        }

        pageRenderer.material = _material;
        pageRenderer.gameObject.SetActive(false);
        return true;
    }

    private void ReleaseOwnedTextureIfAny()
    {
        if (!_ownsCurrentTexture || _currentTexture == null)
            return;

        Destroy(_currentTexture);
        _currentTexture = null;
        _ownsCurrentTexture = false;
    }

    private void OnDestroy()
    {
        ReleaseOwnedTextureIfAny();

        if (_material != null)
            Destroy(_material);
    }
}
