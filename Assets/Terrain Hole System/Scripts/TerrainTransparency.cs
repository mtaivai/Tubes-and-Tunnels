using UnityEngine;
using Random = System.Random;

[ExecuteInEditMode] public class TerrainTransparency : MonoBehaviour
{
	public bool disableBasemap = true;
	public float alphaCutoff = .5f;
	public bool autoUpdateTransparencyMap = true;
	
	public Texture2D transparencyMap;

	Terrain terrain;
	TerrainData tData;
	Material tMaterial;
	void Update()
	{
		terrain = GetComponent<Terrain>();
		tData = terrain ? terrain.terrainData : null;
		tMaterial = terrain ? terrain.materialTemplate : null;
		if (!terrain || !tData || !tMaterial)
			return;
		
		if(disableBasemap && !Application.isPlaying && GetComponent<Terrain>().basemapDistance != 1000000) // only reset on update in edit mode
			GetComponent<Terrain>().basemapDistance = 1000000;
		//if (tMaterial.HasProperty("_AlphaCutoff") && tMaterial.GetFloat("_AlphaCutoff") != alphaCutoff)
		{
			var alphaCutoff_final = Application.isPlaying ? alphaCutoff + .00001f : alphaCutoff; // forces property change on play-mode stop, to force material refresh, to fix that terrain would not display on play-mode stop
			tMaterial.SetFloat("_AlphaCutoff", alphaCutoff_final);
			tMaterial.SetFloat("_AlphaCutoff_2", alphaCutoff_final);
		}

		if (!transparencyMap && autoUpdateTransparencyMap)
		{
			UpdateTransparencyMap();
			ApplyTransparencyMap();
		}
		else
			ApplyTransparencyMap();
	}

	public void UpdateTransparencyMap()
	{
		var newTransparencyMapValues = new Color[tData.alphamapResolution, tData.alphamapResolution];
		for (var slotIndex = 0; slotIndex < tData.alphamapLayers; slotIndex++)
		{
			SplatPrototype slotTexture = tData.splatPrototypes[slotIndex];

			// found the transparent texture slot
			if (slotTexture.texture.name == "Transparent")
			{
				float[,,] slotApplicationMapValues = tData.GetAlphamaps(0, 0, tData.alphamapResolution, tData.alphamapResolution);
				for (var a = 0; a < tData.alphamapResolution; a++)
					for (var b = 0; b < tData.alphamapResolution; b++)
					{
						float textureStrength = slotApplicationMapValues[a, b, slotIndex];
						var newColor = new Color(0, 0, 0, textureStrength);
						newTransparencyMapValues[b, a] = newColor;
					}
				break;
			}
		}

		bool transparencyMapNeedsUpdating = !transparencyMap;
		if (!transparencyMapNeedsUpdating)
		{
			try
			{
				Color[] transparencyMap_colors = transparencyMap.GetPixels();
				if (transparencyMap.width != tData.alphamapResolution || transparencyMap.height != tData.alphamapResolution) // if line above passed (i.e. transparency-map was script-created), and size is outdated
					transparencyMapNeedsUpdating = true;
				if (!transparencyMapNeedsUpdating)
					for (var a = 0; a < tData.alphamapResolution; a++)
						for (var b = 0; b < tData.alphamapResolution; b++)
							if (transparencyMap_colors[(a * tData.alphamapResolution) + b] != newTransparencyMapValues[b, a]) //transparencyMap.GetPixel(b, a) != newTransparencyMapValues[b, a])
							{
								transparencyMapNeedsUpdating = true;
								break;
							}
			}
			catch (UnityException ex)
			{
				if (!ex.Message.Contains("is not readable")) // (ignore 'is not readable' errors; when they occur, the needs-updating flag is left as: false)
					throw;
			}
		}

		if (transparencyMapNeedsUpdating)
		{
			// if old transparency map was of a different resolution, destroy old transparency map
			if (transparencyMap)
			{
				DestroyImmediate(transparencyMap);
				transparencyMap = null;
			}
			if (!transparencyMap)
				transparencyMap = new Texture2D(tData.alphamapResolution, tData.alphamapResolution);

			for (var a = 0; a < tData.alphamapResolution; a++)
				for (var b = 0; b < tData.alphamapResolution; b++)
					transparencyMap.SetPixel(a, b, newTransparencyMapValues[a, b]);
			transparencyMap.Apply();
		}
	}
	public void ApplyTransparencyMap()
	{
		// apply our transparency map (ensure our transparency map is connected to the shader)
		tMaterial.SetTexture("_TransparencyMap", transparencyMap);
	}
}