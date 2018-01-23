Shader "RiggedShader"
{
	Properties
	{
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Pass
	{
		Tags{ "LightMode" = "ForwardBase" }
		CGPROGRAM
		// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.5
		#include "UnityCG.cginc"
		#include "Lighting.cginc"

				// compile shader into multiple variants, with and without shadows
				// (we don't care about any lightmaps yet, so skip these variants)
		#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
				// shadow helper functions and macros
		#include "AutoLight.cginc"

				struct v2f
			{
				float2 uv : TEXCOORD0;
				SHADOW_COORDS(1) // put shadows data into TEXCOORD1
					fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				float4 pos : SV_POSITION;
			};
			v2f vert(appdata_base v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0.rgb;
				o.ambient = ShadeSH9(half4(worldNormal,1));
				//o.diff = half4(sin(vid / 10), sin(vid / 100), sin(vid / 1000), 0) * 0.5 + 0.5;
				// compute shadows data
				TRANSFER_SHADOW(o)
					return o;
			}

			sampler2D _MainTex;
			//float4x4 _BonePositions[];
			//float3 _verts[];
			//int3 _tris[];

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
			// compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
			fixed shadow = SHADOW_ATTENUATION(i);
			// darken light's illumination with shadow, keep ambient intact
			fixed3 lighting = i.diff * shadow + i.ambient;
			col.rgb *= lighting;
			return col;
			}
				ENDCG
			}

		// shadow casting support
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}