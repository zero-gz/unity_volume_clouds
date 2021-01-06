using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class clouds_render : MonoBehaviour
{
    Camera camera;
    Material _UpscaleMaterial;
    public RenderTexture clouds_rt;
    public float small_scale = 2.0f;
    public Texture2D black_tex;
    // Start is called before the first frame update
    void Start()
    {
        clouds_rt = new RenderTexture((int)(Screen.width / small_scale), (int)(Screen.height / small_scale), 24, RenderTextureFormat.Default);
        clouds_rt.name = "clouds_rt";

        camera = GetComponent<Camera>();
        //camera.targetTexture = clouds_rt;

        _UpscaleMaterial = new Material(Shader.Find("Hidden/CloudBlender"));
        _UpscaleMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnPostRender()
    {
        RenderTexture activeRT = RenderTexture.active;
        RenderTexture.active = clouds_rt;

        Graphics.Blit(black_tex, clouds_rt);

        // 获取clouds层的所有GameObject
        var obs = GenerateClouds.cloudsObjects;

        int num = obs.Count;
        Matrix4x4[] mat_list = new Matrix4x4[num];
        GameObject tmp = obs[0];

        Mesh mesh = tmp.GetComponent<MeshFilter>().mesh; ;
        Material mtl = tmp.GetComponent<MeshRenderer>().material;
        mtl.SetPass(0);
        for (int i = 0; i != num; i++)
        {
            GameObject ob = obs[i];
            Matrix4x4 mat = ob.transform.localToWorldMatrix;
            mat_list[i] = mat;
            //Graphics.DrawMeshNow(mesh, mat, 0);
        }

        Graphics.DrawMeshInstanced(mesh, 0, mtl, mat_list);
        RenderTexture.active = activeRT;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _UpscaleMaterial.SetTexture("_Clouds", clouds_rt);
        Graphics.Blit(source, destination, _UpscaleMaterial, 0);
    }
}
