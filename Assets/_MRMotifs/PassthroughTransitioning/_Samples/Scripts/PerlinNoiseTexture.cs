using UnityEngine;

public class PerlinNoiseTexture : MonoBehaviour
{
    [Header("Texture Resolution")]
    [Tooltip("Width of the texture in pixels.")]
    [SerializeField] private int pixWidth = 1024;

    [Tooltip("Height of the texture in pixels.")]
    [SerializeField] private int pixHeight = 1024;

    [Header("Texture Pattern")]
    [Tooltip("The x origin of the sampled area in the plane.")]
    [SerializeField] private float xOrg = 0.2f;

    [Tooltip("The y origin of the sampled area in the plane.")]
    [SerializeField] private float yOrg = 0.5f;

    [Tooltip("The number of cycles of the basic noise pattern that are repeated over the width and height of the texture.")]
    [SerializeField] private float scale = 10.0f;

    private Texture2D _noiseTex;
    private Color[] _pix;
    private Renderer _rend;
    private static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");

    private void Awake()
    {
        _rend = GetComponent<Renderer>();

        _noiseTex = new Texture2D(pixWidth, pixHeight);
        _pix = new Color[_noiseTex.width * _noiseTex.height];

        CalcNoise();
        _noiseTex.SetPixels(_pix);
        _noiseTex.Apply();

        _rend.material.SetTexture(NoiseTex, _noiseTex);
    }

    private void CalcNoise()
    {
        for (var y = 0; y < _noiseTex.height; y++)
        {
            for (var x = 0; x < _noiseTex.width; x++)
            {
                var xCoord = xOrg + x / (float)_noiseTex.width * scale;
                var yCoord = yOrg + y / (float)_noiseTex.height * scale;
                var sample = Mathf.PerlinNoise(xCoord, yCoord);
                _pix[y * _noiseTex.width + x] = new Color(sample, sample, sample);
            }
        }
    }
}