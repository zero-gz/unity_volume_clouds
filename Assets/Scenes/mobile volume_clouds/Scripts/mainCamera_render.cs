using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mainCamera_render : MonoBehaviour
{
    Camera camera;
    Material _UpscaleMaterial;
    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();

        _UpscaleMaterial = new Material(Shader.Find("Hidden/CloudBlender"));
        _UpscaleMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        GameObject obj = GameObject.Find("clouds_camera");
        if(obj)
        {
            RenderTexture rt = obj.GetComponent<cloudsCamera>().clouds_rt;
            _UpscaleMaterial.SetTexture("_Clouds", rt);
            Graphics.Blit(source, destination, _UpscaleMaterial, 0);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
