Shader "Hue Shift Sprites/HSV Diffuse"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_H("Hue", Range(-180,180)) = 0
		_S("Saturation", Range(0,2)) = 1.0
		_V("Value", Range(0,2)) = 1.0
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert vertex:vert nofog keepalpha
		#pragma multi_compile _ PIXELSNAP_ON

		sampler2D _MainTex;
		
		struct Input
		{
			float2 uv_MainTex;
			fixed4 color;
		};
		
		void vert (inout appdata_full v, out Input o)
		{
			#if defined(PIXELSNAP_ON)
			v.vertex = UnityPixelSnap (v.vertex);
			#endif
			
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.color = v.color;
		}

		float3 r2h(float3 i)
		{
			float4 a = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			float4 b = lerp(float4(i.bg, a.wz), float4(i.gb, a.xy), step(i.b, i.g));
			float4 c = lerp(float4(b.xyw, i.r), float4(i.r, b.yzx), step(b.x, i.r));

			float d = c.x - min(c.w, c.y);
			float e = 1.0e-10;
			return float3(abs(c.z + (c.w - c.y) / (6.0*d + e)), d / (c.x + e), c.x);
		}

		float3 h2r(float3 i)
		{
			float4 a = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			float3 b = abs(((i.xxx + a.xyz) - floor(i.xxx + a.xyz)) * 6.0 - a.www);
			return i.z * lerp(a.xxx, clamp(b - a.xxx, 0.0, 1.0), i.y);
		}

		float _H;
		float _S;
		float _V;

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
			float3 hsv = r2h(c.rgb);
			hsv.x += _H / 360;
			hsv.y *= _S;
			hsv.z *= _V;
			c.rgb = h2r(hsv);
			o.Albedo = c.rgb * c.a;
			o.Alpha = c.a;
		}
		ENDCG
	}

Fallback "Transparent/VertexLit"
}
