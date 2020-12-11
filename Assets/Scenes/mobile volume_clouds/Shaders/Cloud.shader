Shader "Custom/Cloud" {
Properties {
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_flow_speed("flow speed", Range(0, 3)) = 0.4
	_clouds_fade("_clouds_fade", Range(0, 6)) = 0.5
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 100
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha

    Cull Off

    Pass {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
			#pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"
            // containing perlin noise generating function cnoise()
            #include "./NoiseShader/ClassicNoise2D.hlsl"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed3 normal : NORMAL;
                // gpu instancing
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed3 worldNormal : TEXCOORD1;
                float3 worldViewDir : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float _clouds_fade;
			half _flow_speed;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = _WorldSpaceCameraPos.xyz - worldPos;
                return o;
            }

            fixed procedualTex(fixed2 texcoord)
            {
                // holistic transparency, set to 0.2 to make clouds look less stuffy when looked from outside
                const fixed trasparency = 0.20;
                // to make texcoords vary from the range of [-0.5, 0.5]
                fixed x = texcoord.x - 0.5;
                fixed y = texcoord.y - 0.5;
                // make this piece of cloud dense in the middle and fade out towards the edge.
                // By writting (x * x + y * y) I wanted to represent the radius, but we don't have to be so rigorous so I left out the square-root,
                // and doing square-root is computationally intensive for shaders, by the way.
                fixed attenuation = max(((0.25 - (x * x + y * y)) * 4.0 * trasparency), 0.0);
                // generate perlin noise in real-time
                fixed perlinNoise = (cnoise(texcoord * 4.0 + _Time.x * _flow_speed*10) + 1) * 0.5;
                return perlinNoise * attenuation + attenuation * attenuation;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // when this piece of cloud is looked from aside, it looks like a sharp piece, which is not supposed to exist in a real cloud
                // we can let this piece of cloud fade out if it's normal is perpendicular to our view direction
                
                fixed3 worldNormal = normalize(i.worldNormal);
                float3 worldViewDir = normalize(i.worldViewDir);
                float rim = abs(dot(worldViewDir, worldNormal));

                const half cutFade = 20.0;
				half _NearClipPlane = 0.3;
                half viewDistance = length(i.worldViewDir);
                fixed cut = smoothstep(_NearClipPlane, _NearClipPlane + cutFade, viewDistance);

                fixed alpha = procedualTex(i.texcoord);
				alpha *= (rim / _clouds_fade)*cut;

				fixed4 final_color = tex2D(_MainTex, i.texcoord);
				final_color.a = alpha;

				// add light failed! many meshes are backto light
				//fixed3 L = normalize(_WorldSpaceLightPos0.xyz);
				//final_color.rgb *= max(dot(worldNormal, L)*0.5+0.5, 0.1);
				return final_color;
            }
        ENDCG
    }
}

}
