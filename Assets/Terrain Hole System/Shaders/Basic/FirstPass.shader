/*Shader "Nature/Terrain/Diffuse"
{
	Properties
	{
		[HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
		[HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
		// used in fallback on old cards & base map
		[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
		[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
	}
	
	CGINCLUDE
		#pragma surface surf Lambert vertex:SplatmapVert finalcolor:myfinal exclude_path:prepass exclude_path:deferred alphatest:_none
		#pragma multi_compile_fog
		#include "TerrainSplatmapCommon.cginc"

		float _CutoutModeHideAlpha;
		sampler2D _TransparencyMap;
		
		void surf(Input IN, inout SurfaceOutput o)
		{
			half4 splat_control;
			half weight;
			fixed4 mixedDiffuse;
			SplatmapMix(IN, splat_control, weight, mixedDiffuse, o.Normal);
			o.Albedo = mixedDiffuse.rgb;
			o.Alpha = weight;
			
			fixed transApplication = tex2D(_TransparencyMap, IN.tc_Control).a;
			
			if(tex2D(_Splat0, IN.uv_Splat0).a == 0)
				transApplication = splat_control.r;
			else if(tex2D(_Splat1, IN.uv_Splat1).a == 0)
				transApplication = splat_control.g;
			else if(tex2D(_Splat2, IN.uv_Splat2).a == 0)
				transApplication = splat_control.b;
			else if(tex2D(_Splat3, IN.uv_Splat3).a == 0)
				transApplication = splat_control.a;
				
			fixed totalOpacity = 1 - transApplication;
			
			if (_CutoutModeHideAlpha != -1) // cutout mode enabled
			{
				if (totalOpacity > _CutoutModeHideAlpha)
				{
					o.Albedo = o.Albedo / totalOpacity;
					o.Alpha = .5;
					
					if (totalOpacity > 0.1)
						o.Albedo = float3(1, 0, 0);
				}
			}
			else
			{				
				o.Alpha = totalOpacity;
				o.Albedo = o.Albedo / o.Alpha;
			}
		}

		void myfinal(Input IN, SurfaceOutput o, inout fixed4 color)
		{
			SplatmapApplyWeight(color, o.Alpha);
			SplatmapApplyFog(color, IN);
		}

	ENDCG

	Category
	{
		Tags
		{
			/#*"SplatCount" = "4"
			"Queue" = "Geometry-99"
			"RenderType" = "Opaque"*#/
			
			"SplatCount" = "4"			
			"Queue" = "Transparent"
			//"RenderType" = "Opaque"
			"RenderType" = "Transparent"
		}
		Blend SrcAlpha OneMinusSrcAlpha
		AlphaTest Greater [_CutoutModeHideAlpha]
		Cull Off // comment out this line to only show the front faces of terrain surfaces
		
		// TODO: Seems like "#pragma target 3.0 _TERRAIN_NORMAL_MAP" can't fallback correctly on less capable devices?
		// Use two sub-shaders to simulate different features for different targets and still fallback correctly.
		SubShader { // for sm3.0+ targets
			CGPROGRAM
				#pragma target 3.0
				#pragma multi_compile __ _TERRAIN_NORMAL_MAP
			ENDCG
		}
		SubShader { // for sm2.0 targets
			CGPROGRAM
			ENDCG
		}
	}

	Dependency "AddPassShader" = "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass"
	Dependency "BaseMapShader" = "Diffuse"
	Dependency "Details0"      = "Hidden/TerrainEngine/Details/Vertexlit"
	Dependency "Details1"      = "Hidden/TerrainEngine/Details/WavingDoublePass"
	Dependency "Details2"      = "Hidden/TerrainEngine/Details/BillboardWavingDoublePass"
	Dependency "Tree0"         = "Hidden/TerrainEngine/BillboardTree"

	Fallback "Diffuse"
}*/





Shader "TRI/THS/Basic"
{
	Properties
	{
		[HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
		// used in fallback on old cards & base map
		[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
		[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)

		// plugin-specific
		[HideInInspector] _AlphaCutoff("Alpha Cutoff", Range(-1, 1)) = .5
		[HideInInspector] _AlphaCutoff_2("Alpha Cutoff_2", Range(-1, 1)) = .5
		[HideInInspector] _TransparencyMap("Transparency Map (RGBA)", 2D) = "black" {}
	}
	
	// without no-shadows-mode
	SubShader
	{
		LOD 1001
		Tags
		{
			/*"SplatCount" = "4"
			"Queue" = "Geometry-99"
			"RenderType" = "Opaque"*/
		
			"SplatCount" = "4"
			"Queue" = "Geometry"
			"RenderType" = "TransparentCutout"
		}
		Blend SrcAlpha OneMinusSrcAlpha
		//AlphaTest Greater [_AlphaCutoff]
		Cull Off // comment out this line to only show the front faces of terrain surfaces
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:SplatmapVert finalcolor:myfinal alphatest:_AlphaCutoff_2 exclude_path:prepass
		#include "TerrainSplatmapCommon.cginc"

		float _AlphaCutoff;
		sampler2D _TransparencyMap;
		
		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 splat_control = tex2D(_Control, IN.tc_Control);
			fixed3 col;
			col = splat_control.r * tex2D(_Splat0, IN.uv_Splat0).rgb;
			col += splat_control.g * tex2D(_Splat1, IN.uv_Splat1).rgb;
			col += splat_control.b * tex2D(_Splat2, IN.uv_Splat2).rgb;
			col += splat_control.a * tex2D(_Splat3, IN.uv_Splat3).rgb;
			
			/*fixed transApplication = 0;
			if(tex2D(_Splat0, IN.uv_Splat0).a == 0)
				transApplication = splat_control.r;
			else if(tex2D(_Splat1, IN.uv_Splat1).a == 0)
				transApplication = splat_control.g;
			else if(tex2D(_Splat2, IN.uv_Splat2).a == 0)
				transApplication = splat_control.b;
			else if(tex2D(_Splat3, IN.uv_Splat3).a == 0)
				transApplication = splat_control.a;
			fixed totalOpacity = 1 - transApplication;*/

			fixed transApplication = tex2D(_TransparencyMap, IN.tc_Control).a;
			fixed totalOpacity = 1 - transApplication;
			
			if (totalOpacity > _AlphaCutoff)
			{
				o.Albedo = col / totalOpacity;
				o.Alpha = 1;
			}
		}
		
		void myfinal(Input IN, SurfaceOutput o, inout fixed4 color)
		{
			SplatmapApplyWeight(color, o.Alpha);
			SplatmapApplyFog(color, IN);
		}
		ENDCG
	}

	// with no-shadows-mode
	SubShader
	{
		LOD 1000
		Tags
		{
			"SplatCount" = "4"
			"Queue" = "AlphaTest+100"
			"RenderType" = "TransparentCutout"
		}
		Blend SrcAlpha OneMinusSrcAlpha
		//AlphaTest Greater [_AlphaCutoff]
		Cull Off // comment out this line to only show the front face of terrain surfaces
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:SplatmapVert finalcolor:myfinal alphatest:_AlphaCutoff_2 exclude_path:prepass
		#include "TerrainSplatmapCommon.cginc"
		
		float _AlphaCutoff;
		sampler2D _TransparencyMap;
		
		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 splat_control = tex2D(_Control, IN.tc_Control);
			fixed3 col;
			col = splat_control.r * tex2D(_Splat0, IN.uv_Splat0).rgb;
			col += splat_control.g * tex2D(_Splat1, IN.uv_Splat1).rgb;
			col += splat_control.b * tex2D(_Splat2, IN.uv_Splat2).rgb;
			col += splat_control.a * tex2D(_Splat3, IN.uv_Splat3).rgb;
			
			/*fixed transApplication = 0;
			if(tex2D(_Splat0, IN.uv_Splat0).a == 0)
				transApplication = splat_control.r;
			else if(tex2D(_Splat1, IN.uv_Splat1).a == 0)
				transApplication = splat_control.g;
			else if(tex2D(_Splat2, IN.uv_Splat2).a == 0)
				transApplication = splat_control.b;
			else if(tex2D(_Splat3, IN.uv_Splat3).a == 0)
				transApplication = splat_control.a;
			fixed totalOpacity = 1 - transApplication;*/

			fixed transApplication = tex2D(_TransparencyMap, IN.tc_Control).a;
			fixed totalOpacity = 1 - transApplication;
			
			if (totalOpacity > _AlphaCutoff)
			{
				o.Albedo = col / totalOpacity;
				o.Alpha = 1;
			}
		}
		
		void myfinal(Input IN, SurfaceOutput o, inout fixed4 color)
		{
			SplatmapApplyWeight(color, o.Alpha);
			SplatmapApplyFog(color, IN);
		}
		ENDCG
	}
	
	Dependency "AddPassShader" = "Hidden/TerrainEngine/Splatmap/Lightmap-AddPass"
	Dependency "BaseMapShader" = "Diffuse"
	
	Fallback "Diffuse"
}