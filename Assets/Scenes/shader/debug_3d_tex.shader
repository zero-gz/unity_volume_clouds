// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "my_shader/debug_3d_tex" {
	Properties{
		_Volume("Texture", 3D) = "" {}
		_slice_z("Slice_z", Range(0, 1) ) = 0.0
	}
		SubShader{

		Cull Off ZWrite Off ZTest Always
		Pass {

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag


		#include "UnityCG.cginc"

		struct vs_input {
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct ps_input {
			float4 pos : SV_POSITION;
			float3 uv : TEXCOORD0;
		};

		float _slice_z;


		ps_input vert(vs_input v)
		{
			ps_input o;
			o.pos = UnityObjectToClipPos(v.vertex);
			//o.uv = float3(v.uv, fmod(_Time.y, 1.0f) );
			o.uv = float3(v.uv, _slice_z);
			return o;
		}

		sampler3D _Volume;

		float4 frag(ps_input i) : COLOR
	{

			float3 color = tex3D(_Volume, i.uv);
			return float4(color, 1.0);
		}

		ENDCG

		}
	}

		Fallback "VertexLit"
}