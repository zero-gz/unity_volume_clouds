using UnityEngine;
using System.Collections;

//[ExecuteInEditMode]
public class WeatherScript : MonoBehaviour
{
    public int size = 512;
    public string save_path = "Assets/Scenes/textures/new_weather.png";
    private RenderTexture rt; // weather texture at the moment

    public Material SystemMaterial
    {
        get
        {
            if (!_SystemMaterial)
            {
                _SystemMaterial = new Material(Shader.Find("Hidden/WeatherSystem"));
                _SystemMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _SystemMaterial;
        }
    }
    private Material _SystemMaterial;

    public void Awake()
    {
    }

    // generates new weather texture
    public void GenerateWeatherTexture()
    {
        SystemMaterial.SetVector("_Randomness", new Vector3(Random.Range(-1000, 1000), Random.Range(-1000, 1000), Random.value * 1.5f - 0.2f));
        Graphics.Blit(null, rt, SystemMaterial, 0);

        util.save_rendertexture(save_path, rt);
        Debug.Log("save texture ok!");
    }

    void Start()
    {
        if (_SystemMaterial)
            DestroyImmediate(_SystemMaterial);

        rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        rt.wrapMode = TextureWrapMode.Mirror;
        rt.Create();

        GenerateWeatherTexture();
    }

    void Update()
    {
    }
}