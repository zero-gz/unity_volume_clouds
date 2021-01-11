using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class clouds_render : MonoBehaviour
{
    Camera camera;
    Material _UpscaleMaterial;
    public float small_scale = 2.0f;

    private CommandBuffer cmd = null;
    private static int _CLOUDS_RT_ID = Shader.PropertyToID("CloudsRT");

    // Start is called before the first frame update
    void Awake()
    {
        camera = GetComponent<Camera>();

        _UpscaleMaterial = new Material(Shader.Find("Hidden/CloudBlender"));
        _UpscaleMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    void OnEnable()
    {
        cmd = new CommandBuffer();
        cmd.name = "TestInstance";
        camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, cmd);
    }

    private void OnDisable()
    {
        if (cmd != null)
        {
            camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, cmd);
            cmd = null;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnPreRender()
    {
        RenderClouds();
    }

    private void RenderClouds()
    {
        if (cmd == null)
            return;

        cmd.Clear();

        // 获取clouds层的所有GameObject
        var obs = GenerateClouds.cloudsObjects;

        int num = obs.Count;
        Matrix4x4[] mat_list = new Matrix4x4[num];
        GameObject tmp = obs[0];

        Mesh mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        Material mtl = tmp.GetComponent<MeshRenderer>().sharedMaterial;
        mtl.SetPass(0);
        for (int i = 0; i != num; i++)
        {
            GameObject ob = obs[i];
            Matrix4x4 mat = ob.transform.localToWorldMatrix;
            mat_list[i] = mat;
        }

        cmd.GetTemporaryRT(_CLOUDS_RT_ID, (int)(Screen.width/small_scale), (int)(Screen.height/small_scale), 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        cmd.SetRenderTarget(_CLOUDS_RT_ID);
        cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
        cmd.SetGlobalTexture(Shader.PropertyToID("_Clouds"), _CLOUDS_RT_ID);
        cmd.DrawMeshInstanced(mesh, 0, mtl, 0, mat_list, num);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, _UpscaleMaterial, 0);
    }
}
