Shader "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass"
{
	Properties
	{
		[HideInInspector] _Control("Control (RGBA)", 2D) = "black" {}
		[HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}
		[HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
	}
	
	CGINCLUDE
	#pragma surface surf Lambert decal:add vertex:SplatmapVert finalcolor:myfinal exclude_path:prepass exclude_path:deferred
	#pragma multi_compile_fog
	#define TERRAIN_SPLAT_ADDPASS
	#include "TerrainSplatmapCommon.cginc"

	float _AlphaCutoff;
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
		fixed totalOpacity = 1 - transApplication;
		
		if (totalOpacity > _AlphaCutoff)
			o.Albedo = o.Albedo / totalOpacity;
		else
			o.Albedo = float3(0, 0, 0);
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
			/*"SplatCount" = "4"
			"Queue" = "Geometry-99"
			"IgnoreProjector"="True"
			"RenderType" = "Opaque"*/
			
			"SplatCount" = "4"
			"Queue" = "Transparent"
			"IgnoreProjector"="True"
			"RenderType" = "Transparent"
		}
		Cull Off // comment out this line to only show the front face of terrain surfaces
		
		// TODO: Seems like "#pragma target 3.0 _TERRAIN_NORMAL_MAP" can't fallback correctly on less capable devices?
		// Use two sub-shaders to simulate different features for different targets and still fallback correctly.
		SubShader
		{ // for sm3.0+ targets
			CGPROGRAM
				#pragma target 3.0
				#pragma multi_compile __ _TERRAIN_NORMAL_MAP
			ENDCG
		}
		SubShader
		{ // for sm2.0 targets
			CGPROGRAM
			ENDCG
		}
	}

	Fallback off
}






/*Shader "Hidden/TerrainEngine/Splatmap/Lightmap-AddPass"
{
	Properties
	{
		_Control ("Control (RGBA)", 2D) = "black" {}
		_Splat3 ("Layer 3 (A)", 2D) = "white" {}
		_Splat2 ("Layer 2 (B)", 2D) = "white" {}
		_Splat1 ("Layer 1 (G)", 2D) = "white" {}
		_Splat0 ("Layer 0 (R)", 2D) = "white" {}
	}

	// without no-shadows-mode
	SubShader
	{
		LOD 1001
		Tags
		{
			"SplatCount" = "4"
			"Queue" = "Geometry+1"
			"IgnoreProjector"="True"
			"RenderType" = "Transparent"
		}
		Cull Off // comment out this line to only show the front face of terrain surfaces
		
		CGPROGRAM
		#pragma surface surf Lambert decal:add exclude_path:prepass
		#include "TerrainSplatmapCommon.cginc"

		float _CutoutModeHideAlpha;
		sampler2D _TransparencyMap;
		
		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 splat_control = tex2D(_Control, IN.tc_Control);
			fixed3 col;
			col  = splat_control.r * tex2D(_Splat0, IN.uv_Splat0).rgb;
			col += splat_control.g * tex2D(_Splat1, IN.uv_Splat1).rgb;
			col += splat_control.b * tex2D(_Splat2, IN.uv_Splat2).rgb;
			col += splat_control.a * tex2D(_Splat3, IN.uv_Splat3).rgb;
			o.Albedo = col;
			o.Alpha = 0.0;
			
			fixed transApplication = tex2D(_TransparencyMap, IN.tc_Control).a;
			fixed totalOpacity = 1 - transApplication;
			
			if (_CutoutModeHideAlpha != -1) // cutout mode enabled
			{
				if (totalOpacity > _CutoutModeHideAlpha)
					o.Albedo = col / totalOpacity;
				else
					o.Albedo = float3(0, 0, 0);
			}
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
			"Queue" = "AlphaTest+101"
			"IgnoreProjector"="True"
			"RenderType" = "Transparent"
		}
		Cull Off // comment out this line to only show the front face of terrain surfaces
		
		CGPROGRAM
		#pragma surface surf Lambert decal:add exclude_path:prepass
		#include "TerrainSplatmapCommon.cginc"

		float _CutoutModeHideAlpha;
		sampler2D _TransparencyMap;
		
		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 splat_control = tex2D (_Control, IN.tc_Control);
			fixed3 col;
			col  = splat_control.r * tex2D (_Splat0, IN.uv_Splat0).rgb;
			col += splat_control.g * tex2D (_Splat1, IN.uv_Splat1).rgb;
			col += splat_control.b * tex2D (_Splat2, IN.uv_Splat2).rgb;
			col += splat_control.a * tex2D (_Splat3, IN.uv_Splat3).rgb;
			o.Albedo = col;
			o.Alpha = 0.0;
			
			fixed transApplication = tex2D(_TransparencyMap, IN.tc_Control).a;
			fixed totalOpacity = 1 - transApplication;
			
			if (_CutoutModeHideAlpha != -1) // cutout mode enabled
			{
				if (totalOpacity > _CutoutModeHideAlpha)
					o.Albedo = col / totalOpacity;
				else
					o.Albedo = float3(0, 0, 0);
			}
		}
		ENDCG
	}
	
	Fallback Off
}*/