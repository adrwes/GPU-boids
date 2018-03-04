//Modified version of shader from https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html

Shader "Instanced/BoidShader" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setup

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0 //

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		struct Boid
		{
			float3 position;
			float3 velocity;
			float3 acceleration;
			float mass;
			uint type;
			float padding;
		};

	#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		StructuredBuffer<Boid> boids;
	#endif

		void setup()
		{
		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			if (boids[unity_InstanceID].type == 1)
			{
				unity_ObjectToWorld._11_21_31_41 = float4(0.3f, 0, 0, 0);
				unity_ObjectToWorld._12_22_32_42 = float4(0, 0.3f, 0, 0);
				unity_ObjectToWorld._13_23_33_43 = float4(0, 0, 0.3f, 0);
			}
			else
			{
				unity_ObjectToWorld._11_21_31_41 = float4(3.0f, 0, 0, 0);
				unity_ObjectToWorld._12_22_32_42 = float4(0, 3.0f, 0, 0);
				unity_ObjectToWorld._13_23_33_43 = float4(0, 0, 3.0f, 0);
			}

			unity_ObjectToWorld._14_24_34_44 = float4(boids[unity_InstanceID].position, 1);
			unity_WorldToObject = unity_ObjectToWorld;
			unity_WorldToObject._14_24_34 *= -1;
			unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
		#endif
		}

		half _Glossiness;
		half _Metallic;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

//dot(boids[unity_InstanceID].velocity, boids[unity_InstanceID].velocity)