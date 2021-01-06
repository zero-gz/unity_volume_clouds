using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class cloudsCamera : MonoBehaviour
{
    Camera camera;
    public RenderTexture clouds_rt;
    public float small_scale = 2.0f;
    // Start is called before the first frame update
    void Start()
    {
        clouds_rt = new RenderTexture((int)(Screen.width/small_scale), (int)(Screen.height/small_scale), 24, RenderTextureFormat.Default);
        clouds_rt.name = "clouds_rt";

        camera = GetComponent<Camera>();
        camera.targetTexture = clouds_rt;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //private void OnPostRender()
    //{
    //    RenderTexture activeRT = RenderTexture.active;
    //    RenderTexture.active = clouds_rt;


    //    for (int i = 0, n = obs.Count; i != n; i++)
    //    {
    //        var ob = obs[i];
    //        if (ob != null && ob.rendering && ob.mesh != null)
    //        {
    //            velocityMaterial.SetMatrix("_CurrM", ob.localToWorldCurr);
    //            velocityMaterial.SetMatrix("_PrevM", ob.localToWorldPrev);
    //            velocityMaterial.SetPass(ob.meshSmrActive ? kVerticesSkinned : kVertices);

    //            for (int j = 0; j != ob.mesh.subMeshCount; j++)
    //            {
    //                Graphics.DrawMeshNow(ob.mesh, Matrix4x4.identity, j);
    //            }
    //        }
    //    }

    //    RenderTexture.active = activeRT;
    //}

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);
    }
}
