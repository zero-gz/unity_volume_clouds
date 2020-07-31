/*!
 * File: PerlinWorleyNoise.cs
 * Date: 2018/03/02 12:34
 *
 * Author: Yuan Li
 * Contact: vanish8.7@gmail.com
 *
 * Description: To generate perlin-worley noise
 *
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinWorleyNoise : NoiseTexture
{
    [SerializeField]
    private NoiseTexture _perlinNoise;
    [SerializeField]
    private NoiseTexture _worleySubNoise;
    [SerializeField]
    private NoiseTexture _worleyNoise0;
    [SerializeField]
    private NoiseTexture _worleyNoise1;
    [SerializeField]
    private NoiseTexture _worleyNoise2;

    // Utility function that maps a value from one range to another.
    float Remap(float original_value, float original_min, float original_max, float new_min, float new_max)
    {
        return new_min + (((original_value - original_min) / (original_max - original_min)) * (new_max - new_min));
    }

    protected override Color GetNoise(NoiseTools.NoiseGeneratorBase noise, float frequency, int dimension, int fractal, int x, int y, int z = 0)
    {
        if (this._perlinNoise == null || this._worleyNoise0 == null || this._worleyNoise1 == null || this._worleyNoise2 == null) return Color.black;
        float perlin = this._perlinNoise.GetNoiseData(x, y, z);
        //inverted worley noise
        //with higher frequency

        float worley_sub = this._worleySubNoise.GetNoiseData(x, y, z);
        float worley0 = this._worleyNoise0.GetNoiseData(x, y, z);
        float worley1 = this._worleyNoise1.GetNoiseData(x, y, z);
        float worley2 = this._worleyNoise2.GetNoiseData(x, y, z);

        //float worleyFBM = worley0 * 0.625f + worley1 * 0.25f + worley2 * 0.125f;

        float perlinWorley = 0;
        //from TileableVolumeNoise
        //perlinWorley = Remap(perlin, 0, 1, worleyFBM, 1);

        //this from Nubis course
        perlinWorley = Remap(perlin, 1.0f - worley_sub, 1, 0, 1);
        //perlinWorley = Remap(perlin, worley_sub, 1, 0, 1);

        return new Color(perlinWorley, worley0, worley1, worley2);
    }

}
