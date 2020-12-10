using UnityEngine;
using UnityEditor;

//[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CloudScript: MonoBehaviour
{
    [HeaderAttribute("Debugging")]
    public bool debugNoLowFreqNoise = false;
    public bool debugNoHighFreqNoise = false;
    public bool debugNoHG = false;
    public bool debugNoPowderEffect = false;
    public bool debugNoBeer = false;
    public bool debugNoGradient = false;
    public bool debugNoLightCone = false;

    [HeaderAttribute("Performance")]
    [Range(1, 256)]
    public int steps = 128;
    public bool adjustDensity = true;
    public AnimationCurve stepDensityAdjustmentCurve = new AnimationCurve(new Keyframe(0.0f, 3.019f), new Keyframe(0.25f, 1.233f), new Keyframe(0.5f, 1.0f), new Keyframe(1.0f, 0.892f));
    [Range(1, 8)]
    public int downSample = 1;

    [HeaderAttribute("Cloud modeling")]
    public Texture3D _cloudShapeTexture;
    public Texture3D _cloudDetailTexture;
    public Gradient gradientLow;
    public Gradient gradientMed;
    public Gradient gradientHigh;
    public float startHeight = 1500.0f;
    public float thickness = 4000.0f;
    public float planetSize = 35000.0f;
    public Vector3 planetZeroCoordinate = new Vector3(0.0f, 0.0f, 0.0f);
    [Range(0.0f, 1.0f)]
    public float scale = 0.3f;
    [Range(0.0f, 32.0f)]
    public float detailScale = 13.9f;
    [Range(0.0f, 1.0f)]
    public float lowFreqMin = 0.366f;
    [Range(0.0f, 1.0f)]
    public float lowFreqMax = 0.8f;
    [Range(0.0f, 1.0f)]
    public float highFreqModifier = 0.21f;
    [Range(0.0f, 1.0f)]
    public float weatherScale = 0.1f;

    public bool use_system_weather_texture = true;
    public Texture2D weatherTexture;
    [Range(0.0f, 2.0f)]
    public float coverage = 0.92f;
    [Range(0.0f, 2.0f)]
    public float cloudSampleMultiplier = 1.0f;

    [HeaderAttribute("Cloud Lighting")]
    public Light sunLight;
    public Color cloudBaseColor = new Color32(199, 220, 255, 255);
    public Color cloudTopColor = new Color32(255, 255, 255, 255);
    [Range(0.0f, 1.0f)]
    public float ambientLightFactor = 0.551f;
    [Range(0.0f, 1.5f)]
    public float sunLightFactor = 0.79f;
    public Color highSunColor = new Color32(255, 252, 210, 255);
    public Color lowSunColor = new Color32(255, 174, 0, 255);
    [Range(0.0f, 1.0f)]
    public float henyeyGreensteinGForward = 0.4f;
    [Range(0.0f, 1.0f)]
    public float henyeyGreensteinGBackward = 0.179f;
    [Range(0.0f, 200.0f)]
    public float lightStepLength = 64.0f;
    [Range(0.0f, 1.0f)]
    public float lightConeRadius = 0.4f;
    [Range(0.0f, 4.0f)]
    public float density = 1.0f;

    [HeaderAttribute("Animating")]
    public float globalMultiplier = 1.0f;
    public float windDirection = -22.4f;
    public float coverageWindSpeed = 25.0f;
    public float coverageWindDirection = 5.0f;

    private Vector3 _windOffset;
    private Vector2 _coverageWindOffset;
    private Vector3 _windDirectionVector;

    public Material CloudMaterial
    {
        get
        {
            if (!_CloudMaterial)
            {
                _CloudMaterial = new Material(Shader.Find("Hidden/Clouds"));
                _CloudMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _CloudMaterial;
        }
    }
    private Material _CloudMaterial;

    public Material UpscaleMaterial
    {
        get
        {
            if (!_UpscaleMaterial)
            {
                _UpscaleMaterial = new Material(Shader.Find("Hidden/CloudBlender"));
                _UpscaleMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _UpscaleMaterial;
        }
    }
    private Material _UpscaleMaterial;

    void Reset()
    {
    }

    void Awake()
    {
        Reset();
    }

    void Start()
    {
        if (_CloudMaterial)
            DestroyImmediate(_CloudMaterial);
        if (_UpscaleMaterial)
            DestroyImmediate(_UpscaleMaterial);
        _windOffset = new Vector3(0.0f, 0.0f, 0.0f);
        _coverageWindOffset = new Vector3(0.5f / (weatherScale * 0.00025f), 0.5f / (weatherScale * 0.00025f));
    }

    private void Update()
    {
        // updates wind offsets
        float angleWind = windDirection * Mathf.Deg2Rad;
        _windDirectionVector = new Vector3(Mathf.Cos(angleWind), -0.25f, Mathf.Sin(angleWind));

        float angleCoverage = coverageWindDirection * Mathf.Deg2Rad;
        Vector2 coverageDirecton = new Vector2(Mathf.Cos(angleCoverage), Mathf.Sin(angleCoverage));
        _coverageWindOffset += coverageWindSpeed * globalMultiplier * coverageDirecton * Time.deltaTime;

        if(Input.GetKey(KeyCode.Space))
        {
            save_3d_tex();
        }
    }

    private void OnDestroy()
    {
        if (_CloudMaterial)
            DestroyImmediate(_CloudMaterial);
    }

    public Camera CurrentCamera
    {
        get
        {
            if (!_CurrentCamera)
                _CurrentCamera = GetComponent<Camera>();
            return _CurrentCamera;
        }
    }
    private Camera _CurrentCamera;

    private Vector4 gradientToVector4(Gradient gradient)
    {
        if (gradient.colorKeys.Length != 4)
        {
            return new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        }
        float x = gradient.colorKeys[0].time;
        float y = gradient.colorKeys[1].time;
        float z = gradient.colorKeys[2].time;
        float w = gradient.colorKeys[3].time;
        return new Vector4(x, y, z, w);
    }

    [ImageEffectOpaque]
    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (CloudMaterial == null) // if some script public parameters are missing, do nothing
        {
            Graphics.Blit(source, destination); // do nothing
            return;
        }

        Vector3 cameraPos = CurrentCamera.transform.position;

        float sunLightFactorUpdated = sunLightFactor;
        float ambientLightFactorUpdated = ambientLightFactor;
        float sunAngle = sunLight.transform.eulerAngles.x;
        Color sunColor = highSunColor;
        float henyeyGreensteinGBackwardLerp = henyeyGreensteinGBackward;

        float noiseScale = 0.00001f + scale * 0.0004f;

        if (sunAngle > 170.0f) // change sunlight color based on sun's height.
        {
            float gradient = Mathf.Max(0.0f, (sunAngle - 330.0f) / 30.0f);
            float gradient2 = gradient * gradient;
            sunLightFactorUpdated *= gradient;
            ambientLightFactorUpdated *= gradient;
            henyeyGreensteinGBackwardLerp *= gradient2 * gradient;
            ambientLightFactorUpdated = Mathf.Max(0.02f, ambientLightFactorUpdated);
            sunColor = Color.Lerp(lowSunColor, highSunColor, gradient2);
        }

        updateMaterialKeyword(debugNoLowFreqNoise, "DEBUG_NO_LOW_FREQ_NOISE");
        updateMaterialKeyword(debugNoHighFreqNoise, "DEBUG_NO_HIGH_FREQ_NOISE");

        updateMaterialKeyword(debugNoBeer, "DEBUG_NO_BEER");
        updateMaterialKeyword(debugNoPowderEffect, "DEBUG_NO_POWDER_EFFECT");
        updateMaterialKeyword(debugNoHG, "DEBUG_NO_HG");
        updateMaterialKeyword(debugNoGradient, "DEBUG_NO_GRADIENT");
        updateMaterialKeyword(debugNoLightCone, "DEBUG_NO_LIGHT_CONE");
        
        CloudMaterial.SetVector("_SunDir", sunLight.transform ? (-sunLight.transform.forward).normalized : Vector3.up);
        CloudMaterial.SetVector("_PlanetCenter", planetZeroCoordinate - new Vector3(0, planetSize, 0));
        CloudMaterial.SetVector("_ZeroPoint", planetZeroCoordinate);
        CloudMaterial.SetColor("_SunColor", sunColor);

        CloudMaterial.SetColor("_CloudBaseColor", cloudBaseColor);
        CloudMaterial.SetColor("_CloudTopColor", cloudTopColor);
        CloudMaterial.SetFloat("_AmbientLightFactor", ambientLightFactorUpdated);
        CloudMaterial.SetFloat("_SunLightFactor", sunLightFactorUpdated);

        CloudMaterial.SetTexture("_ShapeTexture", _cloudShapeTexture);
        CloudMaterial.SetTexture("_DetailTexture", _cloudDetailTexture);
        CloudMaterial.SetVector("_Randomness", new Vector4(Random.value, Random.value, Random.value, Random.value));

        CloudMaterial.SetFloat("_LightConeRadius", lightConeRadius);
        CloudMaterial.SetFloat("_LightStepLength", lightStepLength);
        CloudMaterial.SetFloat("_SphereSize", planetSize);
        CloudMaterial.SetVector("_CloudHeightMinMax", new Vector2(startHeight, startHeight + thickness));
        CloudMaterial.SetFloat("_Thickness", thickness);
        CloudMaterial.SetFloat("_Scale", noiseScale);
        CloudMaterial.SetFloat("_DetailScale", detailScale * noiseScale);
        CloudMaterial.SetVector("_LowFreqMinMax", new Vector4(lowFreqMin, lowFreqMax));
        CloudMaterial.SetFloat("_HighFreqModifier", highFreqModifier);
        CloudMaterial.SetFloat("_WeatherScale", weatherScale * 0.00025f);
        CloudMaterial.SetFloat("_Coverage", 1.0f - coverage);
        CloudMaterial.SetFloat("_HenyeyGreensteinGForward", henyeyGreensteinGForward);
        CloudMaterial.SetFloat("_HenyeyGreensteinGBackward", -henyeyGreensteinGBackwardLerp);
        if (adjustDensity)
        {
            CloudMaterial.SetFloat("_SampleMultiplier", cloudSampleMultiplier * stepDensityAdjustmentCurve.Evaluate(steps / 256.0f));
        }
        else
        {
            CloudMaterial.SetFloat("_SampleMultiplier", cloudSampleMultiplier);
        }
        

        CloudMaterial.SetFloat("_Density", density);
        if(!use_system_weather_texture)
            CloudMaterial.SetTexture("_WeatherTexture", weatherTexture);

        CloudMaterial.SetVector("_WindDirection", _windDirectionVector);
        CloudMaterial.SetVector("_WindOffset", _windOffset);
        CloudMaterial.SetVector("_CoverageWindOffset", _coverageWindOffset);
        
        CloudMaterial.SetVector("_Gradient1", gradientToVector4(gradientLow));
        CloudMaterial.SetVector("_Gradient2", gradientToVector4(gradientMed));
        CloudMaterial.SetVector("_Gradient3", gradientToVector4(gradientHigh));

        CloudMaterial.SetInt("_Steps", steps);

        CloudMaterial.SetMatrix("_FrustumCornersES", GetFrustumCorners(CurrentCamera));
        CloudMaterial.SetMatrix("_CameraInvViewMatrix", CurrentCamera.cameraToWorldMatrix);
        CloudMaterial.SetVector("_CameraWS", cameraPos);
        CloudMaterial.SetFloat("_FarPlane", CurrentCamera.farClipPlane);

        // get cloud render texture and render clouds to it
        RenderTexture rtClouds = RenderTexture.GetTemporary((int)(source.width / ((float)downSample)), (int)(source.height / ((float)downSample)), 0, source.format, RenderTextureReadWrite.Default, source.antiAliasing);
        CustomGraphicsBlit(source, rtClouds, CloudMaterial, 0);

        UpscaleMaterial.SetTexture("_Clouds", rtClouds);
        
        // Apply clouds to background
        Graphics.Blit(source, destination, UpscaleMaterial, 0);
        RenderTexture.ReleaseTemporary(rtClouds);
    }

    private void updateMaterialKeyword(bool b, string keyword)
    {
        if (b != CloudMaterial.IsKeywordEnabled(keyword))
        {
            if (b)
            {
                CloudMaterial.EnableKeyword(keyword);
            }
            else
            {
                CloudMaterial.DisableKeyword(keyword);
            }
        }
    }

    private Matrix4x4 GetFrustumCorners(Camera cam)
    {
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        float tan_fov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 toRight = Vector3.right * tan_fov * camAspect;
        Vector3 toTop = Vector3.up * tan_fov;

        Vector3 topLeft = (-Vector3.forward - toRight + toTop);
        Vector3 topRight = (-Vector3.forward + toRight + toTop);
        Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
        Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        return frustumCorners;
    }

    static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
    {
        RenderTexture.active = dest;

        //fxMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho(); // Note: z value of vertices don't make a difference because we are using ortho projection

        fxMaterial.SetPass(passNr);

        GL.Begin(GL.QUADS);

        // Here, GL.MultitexCoord2(0, x, y) assigns the value (x, y) to the TEXCOORD0 slot in the shader.
        // GL.Vertex3(x,y,z) queues up a vertex at position (x, y, z) to be drawn.  Note that we are storing
        // our own custom frustum information in the z coordinate.
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

        GL.End();
        GL.PopMatrix();
    }

    void save_3d_tex()
    {
        if (_cloudShapeTexture) // if shape texture is missing load it in
        {
            AssetDatabase.CreateAsset(_cloudShapeTexture, "Assets/Textures/perlin_worley_3d.asset");
            Debug.Log("perlin worley saved ok!");
        }

        if (_cloudDetailTexture) // if detail texture is missing load it in
        {
            AssetDatabase.CreateAsset(_cloudDetailTexture, "Assets/Textures/detail_worley_3d.asset");
            Debug.Log("detail worley saved ok!!!!");
        }

        AssetDatabase.Refresh();
    }
}
