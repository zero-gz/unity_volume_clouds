using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateClouds : MonoBehaviour
{
    //public Transform cloudPrefab;
    public Mesh clouds_mesh;
    public Material clouds_mtl;
    public float cloudScale = 17f;
    public int density = 6;
    public float height = 10f;

    public float offset_x = 0.0f;
    public float offset_y = 0.0f;
    public float offset_z = 4f;

    public float flow_speed = 1.0f;
    public float clouds_fade = 1.0f;

    public bool refreshing = false;

    void Awake()
    {
        GenerateOneClouds();
    }

    void Update()
    {
        if (refreshing)
        {
            foreach (Transform child in transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            GenerateOneClouds();
        }
    }

    void GenerateOneClouds()
    {
        Vector3 position;
        Vector3 scale;
        Random.InitState(20);

        if (clouds_mtl)
        {
            clouds_mtl.SetFloat("_flow_speed", flow_speed);
            clouds_mtl.SetFloat("_clouds_fade", clouds_fade);
        }

        for (int x = 0; x < density; x++)
        {
            for (int y = 0; y < density; y++)
            {
                //Transform cloud = Instantiate(cloudPrefab);
                GameObject cloud = new GameObject();
                cloud.AddComponent<MeshFilter>();
                cloud.GetComponent<MeshFilter>().mesh = clouds_mesh;

                cloud.AddComponent<MeshRenderer>();
                MeshRenderer render = cloud.GetComponent<MeshRenderer>();
                render.material = clouds_mtl;

                // no need for clouds to cast or receive shadows
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                render.receiveShadows = false;

                position.x = x + Random.RandomRange(-offset_x, offset_x);
                position.z = y + Random.RandomRange(-offset_y, offset_y);
                position.y = ((float)Random.Range(-255, 256) / 512f) * offset_z + height;
                float xRand = ((float)Random.Range(-127, 128) / 512f);
                float zRand = ((float)Random.Range(-127, 128) / 512f);
                float yRand = Mathf.Min(xRand, zRand);//make the cloud look more flat
                float scaleRand = ((float)Random.Range(-127, 128) / 512f);
                float currentScale = cloudScale * (scaleRand + 1f);
                scale.x = currentScale * (xRand + 1f);
                scale.z = currentScale * (zRand + 1f);
                scale.y = currentScale * (yRand + 1f) * 0.8f;// * 0.8f is to make the cloud look more flat
                cloud.transform.localPosition = position;
                cloud.transform.localScale = scale;
                cloud.transform.localRotation = Quaternion.Euler(0, (float)Random.Range(0, 180), 0);
                // join the newly created cloud into the CloudGroup parent object
                cloud.transform.SetParent(transform, false);

            }
        }
    }
}
