using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshCombinerMaterialMerger
{
	private static readonly string[] MainTexturePropertyNames =
	{
		"_TextureSample1",
		"_BaseMap",
		"_MainTex",
	};

	private static readonly string[] RampTexturePropertyNames =
	{
		"_TextureRamp",
		"_Ramp",
	};

	private static readonly string[] NormalTexturePropertyNames =
	{
		"_BumpMap",
		"_NormalMap",
	};

	public static bool TryCombineMaterials(MeshCombiner meshCombiner, out string message)
	{
		message = string.Empty;

		if(meshCombiner == null)
		{
			message = "MeshCombiner reference is missing.";
			return false;
		}

		var meshFilter = meshCombiner.GetComponent<MeshFilter>();
		var meshRenderer = meshCombiner.GetComponent<MeshRenderer>();

		if(meshFilter == null || meshRenderer == null)
		{
			message = "MeshFilter or MeshRenderer is missing.";
			return false;
		}

		var sourceMesh = meshFilter.sharedMesh;
		if(sourceMesh == null)
		{
			message = "MeshFilter has no mesh assigned.";
			return false;
		}

		var sourceMaterials = meshRenderer.sharedMaterials;
		if(sourceMaterials == null || sourceMaterials.Length <= 1)
		{
			message = "MeshRenderer already uses a single material.";
			return false;
		}

		if(sourceMaterials.Any(material => material == null))
		{
			message = "MeshRenderer has null material slots. Remove or assign them first.";
			return false;
		}

		var subMeshCount = sourceMesh.subMeshCount;
		if(subMeshCount <= 0)
		{
			message = "Mesh has no submeshes.";
			return false;
		}

		if(sourceMaterials.Length != subMeshCount)
		{
			message = "Material count ("+sourceMaterials.Length+") does not match submesh count ("+subMeshCount+").";
			return false;
		}

		if(!AllMaterialsUseCompatibleShader(sourceMaterials, out var referenceShader))
		{
			message = "Materials use different shaders. Combine only materials with the same shader.";
			return false;
		}

		if(sourceMaterials.All(material => material == sourceMaterials[0]))
		{
			Undo.RecordObject(meshRenderer, "Combine Materials");
			meshRenderer.sharedMaterials = new[] { sourceMaterials[0] };
			message = "All slots already use the same material instance. Collapsed to a single slot.";
			EditorUtility.SetDirty(meshRenderer);
			return true;
		}

		var subMeshSlots = BuildSubMeshSlots(sourceMaterials, subMeshCount, out var packRampTextures, out var packNormalTextures);
		if(subMeshSlots == null)
		{
			message = "Failed to resolve textures for all submeshes.";
			return false;
		}

		if(TryCollapseWithoutAtlas(meshRenderer, subMeshSlots, packRampTextures, packNormalTextures, out var collapseMessage))
		{
			message = collapseMessage;
			EditorUtility.SetDirty(meshRenderer);
			return true;
		}

		var workingMesh = GetWritableMesh(meshFilter, sourceMesh);
		var mainTextures = CreateReadableCopies(subMeshSlots.Select(slot => slot.MainTexture));
		if(!TryPackTextureSet(mainTextures, meshCombiner.MaterialAtlasMaxSize, meshCombiner.MaterialAtlasPadding,
			out var mainAtlas, out var mainAtlasRects, out var mainPackError))
		{
			message = mainPackError;
			return false;
		}

		Texture2D rampAtlas = null;
		if(packRampTextures)
		{
			var rampTextures = CreateReadableCopies(subMeshSlots.Select(slot => slot.RampTexture));
			if(!TryPackTextureSet(rampTextures, meshCombiner.MaterialAtlasMaxSize, meshCombiner.MaterialAtlasPadding,
				out rampAtlas, out _, out var rampPackError))
			{
				message = rampPackError;
				return false;
			}
		}

		Texture2D normalAtlas = null;
		if(packNormalTextures)
		{
			var normalTextures = subMeshSlots
				.Select(slot => slot.NormalTexture != null ? CreateReadableTextureCopy(slot.NormalTexture) : CreateNormalFallbackTexture())
				.ToArray();
			if(!TryPackTextureSet(normalTextures, meshCombiner.MaterialAtlasMaxSize, meshCombiner.MaterialAtlasPadding,
				out normalAtlas, out _, out var normalPackError))
			{
				message = normalPackError;
				return false;
			}
		}

		Undo.RecordObject(workingMesh, "Combine Materials UV");
		if(!TryRemapMeshUvs(workingMesh, subMeshSlots, mainAtlasRects, out var uvError))
		{
			message = uvError;
			return false;
		}

		var combinedMaterial = CreateCombinedMaterial(sourceMaterials[0], referenceShader, mainAtlas, rampAtlas, normalAtlas);
		var assetPaths = SaveGeneratedAssets(meshCombiner, workingMesh, mainAtlas, rampAtlas, normalAtlas, combinedMaterial,
			out var savedMaterial);

		Undo.RecordObject(meshRenderer, "Combine Materials");
		meshRenderer.sharedMaterials = new[] { savedMaterial != null ? savedMaterial : combinedMaterial };

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		message = "Combined "+sourceMaterials.Length+" materials into one. Assets saved under Assets/"+meshCombiner.FolderPath+".";
		if(assetPaths.Length > 0)
		{
			var pingTarget = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPaths[0]);
			if(pingTarget != null)
			{
				EditorGUIUtility.PingObject(pingTarget);
			}
		}

		EditorUtility.SetDirty(meshCombiner);
		EditorUtility.SetDirty(meshRenderer);
		EditorUtility.SetDirty(workingMesh);
		return true;
	}

	private static bool TryCollapseWithoutAtlas(MeshRenderer meshRenderer, List<SubMeshTextureSlot> slots,
		bool packRampTextures, bool packNormalTextures, out string message)
	{
		message = string.Empty;

		if(packRampTextures || packNormalTextures)
		{
			return false;
		}

		if(slots.Select(slot => slot.MainTexture).Distinct().Count() != 1)
		{
			return false;
		}

		foreach(var slot in slots)
		{
			if(slot.MainScale != Vector2.one || slot.MainOffset != Vector2.zero)
			{
				return false;
			}
		}

		Undo.RecordObject(meshRenderer, "Combine Materials");
		meshRenderer.sharedMaterials = new[] { slots[0].Material };
		message = "All submeshes share the same texture setup. Collapsed to a single material without atlas baking.";
		return true;
	}

	private static bool AllMaterialsUseCompatibleShader(Material[] materials, out Shader referenceShader)
	{
		referenceShader = materials[0].shader;
		for(var i = 1; i < materials.Length; i++)
		{
			if(materials[i].shader != referenceShader)
			{
				return false;
			}
		}

		return referenceShader != null;
	}

	private static Mesh GetWritableMesh(MeshFilter meshFilter, Mesh sourceMesh)
	{
		if(EditorUtility.IsPersistent(sourceMesh))
		{
			var meshInstance = UnityEngine.Object.Instantiate(sourceMesh);
			meshInstance.name = sourceMesh.name+"_MaterialsCombined";
			Undo.RecordObject(meshFilter, "Assign Materials Combined Mesh");
			meshFilter.sharedMesh = meshInstance;
			return meshInstance;
		}

		return sourceMesh;
	}

	private static List<SubMeshTextureSlot> BuildSubMeshSlots(Material[] materials, int subMeshCount,
		out bool packRampTextures, out bool packNormalTextures)
	{
		packRampTextures = false;
		packNormalTextures = false;

		var slots = new List<SubMeshTextureSlot>(subMeshCount);
		Texture2D referenceRamp = null;

		for(var subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
		{
			var material = materials[subMeshIndex];
			var mainTexture = GetMaterialTexture(material, MainTexturePropertyNames);
			if(mainTexture == null)
			{
				return null;
			}

			var rampTexture = GetMaterialTexture(material, RampTexturePropertyNames);
			var normalTexture = GetMaterialTexture(material, NormalTexturePropertyNames);

			if(rampTexture != null)
			{
				if(referenceRamp == null)
				{
					referenceRamp = rampTexture;
				}
				else if(rampTexture != referenceRamp)
				{
					packRampTextures = true;
				}
			}

			if(normalTexture != null)
			{
				packNormalTextures = true;
			}

			slots.Add(new SubMeshTextureSlot
			{
				SubMeshIndex = subMeshIndex,
				Material = material,
				MainTexture = mainTexture,
				MainScale = GetMaterialScale(material, MainTexturePropertyNames),
				MainOffset = GetMaterialOffset(material, MainTexturePropertyNames),
				RampTexture = rampTexture,
				NormalTexture = normalTexture,
			});
		}

		return slots;
	}

	private static Texture2D[] CreateReadableCopies(IEnumerable<Texture2D> textures)
	{
		return textures.Select(texture => texture != null ? CreateReadableTextureCopy(texture) : CreateFallbackTexture()).ToArray();
	}

	private static bool TryPackTextureSet(Texture2D[] textures, int maxAtlasSize, int padding, out Texture2D atlas, out Rect[] atlasRects, out string error)
	{
		atlas = null;
		atlasRects = null;
		error = string.Empty;

		if(textures == null || textures.Length == 0)
		{
			error = "No textures to pack.";
			return false;
		}

		atlas = new Texture2D(2, 2, TextureFormat.RGBA32, true);
		atlasRects = atlas.PackTextures(textures, padding, maxAtlasSize, false);

		for(var i = 0; i < textures.Length; i++)
		{
			if(textures[i] != null && !EditorUtility.IsPersistent(textures[i]))
			{
				UnityEngine.Object.DestroyImmediate(textures[i]);
			}
		}

		if(atlasRects == null || atlasRects.Length != textures.Length)
		{
			error = "Texture atlas packing failed. Reduce source texture sizes or increase Material Atlas Max Size.";
			return false;
		}

		return true;
	}

	private static bool TryRemapMeshUvs(Mesh mesh, List<SubMeshTextureSlot> slots, Rect[] mainAtlasRects, out string error)
	{
		error = string.Empty;
		var uvs = new List<Vector2>();
		mesh.GetUVs(0, uvs);

		if(uvs.Count == 0)
		{
			error = "Mesh UV channel 0 is empty.";
			return false;
		}

		for(var slotIndex = 0; slotIndex < slots.Count; slotIndex++)
		{
			var slot = slots[slotIndex];
			var triangles = mesh.GetTriangles(slot.SubMeshIndex);
			var atlasRect = mainAtlasRects[slotIndex];

			for(var triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
			{
				var vertexIndex = triangles[triangleIndex];
				if(vertexIndex < 0 || vertexIndex >= uvs.Count)
				{
					continue;
				}

				var uv = uvs[vertexIndex];
				uv = ApplyScaleOffset(uv, slot.MainScale, slot.MainOffset);
				uvs[vertexIndex] = RemapUvToAtlas(uv, atlasRect);
			}
		}

		mesh.SetUVs(0, uvs);
		return true;
	}

	private static Vector2 ApplyScaleOffset(Vector2 uv, Vector2 scale, Vector2 offset)
	{
		return new Vector2(uv.x * scale.x + offset.x, uv.y * scale.y + offset.y);
	}

	private static Vector2 RemapUvToAtlas(Vector2 uv, Rect atlasRect)
	{
		return new Vector2(
			Mathf.Lerp(atlasRect.xMin, atlasRect.xMax, uv.x),
			Mathf.Lerp(atlasRect.yMin, atlasRect.yMax, uv.y));
	}

	private static Material CreateCombinedMaterial(Material template, Shader shader, Texture2D mainAtlas, Texture2D rampAtlas, Texture2D normalAtlas)
	{
		var combinedMaterial = new Material(template);
		combinedMaterial.shader = shader;
		combinedMaterial.name = template.name+"_Combined";

		AssignTextureToMaterial(combinedMaterial, MainTexturePropertyNames, mainAtlas);
		if(rampAtlas != null)
		{
			AssignTextureToMaterial(combinedMaterial, RampTexturePropertyNames, rampAtlas);
		}

		if(normalAtlas != null)
		{
			AssignTextureToMaterial(combinedMaterial, NormalTexturePropertyNames, normalAtlas);
		}

		return combinedMaterial;
	}

	private static void AssignTextureToMaterial(Material material, string[] propertyNames, Texture2D texture)
	{
		foreach(var propertyName in propertyNames)
		{
			if(material.HasProperty(propertyName))
			{
				material.SetTexture(propertyName, texture);
				material.SetTextureScale(propertyName, Vector2.one);
				material.SetTextureOffset(propertyName, Vector2.zero);
			}
		}
	}

	private static string[] SaveGeneratedAssets(MeshCombiner meshCombiner, Mesh mesh, Texture2D mainAtlas, Texture2D rampAtlas,
		Texture2D normalAtlas, Material combinedMaterial, out Material savedMaterial)
	{
		savedMaterial = null;
		var folderPath = EnsureAssetFolder(meshCombiner.FolderPath);
		var assetName = MakeSafeAssetName(meshCombiner.gameObject.name);
		var savedPaths = new List<string>();

		if(mesh != null && !EditorUtility.IsPersistent(mesh))
		{
			var meshPath = GetUniqueAssetPath(folderPath, assetName+"_MaterialsCombined", ".asset");
			AssetDatabase.CreateAsset(mesh, meshPath);
			savedPaths.Add(meshPath);
		}

		if(mainAtlas != null)
		{
			var mainAtlasPath = GetUniqueAssetPath(folderPath, assetName+"_AlbedoAtlas", ".asset");
			AssetDatabase.CreateAsset(mainAtlas, mainAtlasPath);
			savedPaths.Add(mainAtlasPath);
		}

		if(rampAtlas != null)
		{
			var rampAtlasPath = GetUniqueAssetPath(folderPath, assetName+"_RampAtlas", ".asset");
			AssetDatabase.CreateAsset(rampAtlas, rampAtlasPath);
			savedPaths.Add(rampAtlasPath);
		}

		if(normalAtlas != null)
		{
			var normalAtlasPath = GetUniqueAssetPath(folderPath, assetName+"_NormalAtlas", ".asset");
			AssetDatabase.CreateAsset(normalAtlas, normalAtlasPath);
			savedPaths.Add(normalAtlasPath);
		}

		if(combinedMaterial != null)
		{
			var materialPath = GetUniqueAssetPath(folderPath, assetName+"_Combined", ".mat");
			AssetDatabase.CreateAsset(combinedMaterial, materialPath);
			savedPaths.Add(materialPath);
			savedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
		}

		return savedPaths.ToArray();
	}

	private static string EnsureAssetFolder(string folderPath)
	{
		folderPath = folderPath.Replace('\\', '/').Trim('/');
		if(string.IsNullOrWhiteSpace(folderPath))
		{
			folderPath = "Prefabs/CombinedMeshes";
		}

		if(!AssetDatabase.IsValidFolder("Assets/"+folderPath))
		{
			var folderNames = folderPath.Split('/').Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
			var currentPath = "Assets";
			foreach(var folderName in folderNames)
			{
				var nextPath = currentPath+"/"+folderName;
				if(!AssetDatabase.IsValidFolder(nextPath))
				{
					AssetDatabase.CreateFolder(currentPath, folderName);
				}

				currentPath = nextPath;
			}
		}

		return folderPath;
	}

	private static string GetUniqueAssetPath(string folderPath, string assetName, string extension)
	{
		var assetPath = "Assets/"+folderPath+"/"+assetName+extension;
		var assetNumber = 1;

		while(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
		{
			assetPath = "Assets/"+folderPath+"/"+assetName+" ("+assetNumber+")"+extension;
			assetNumber++;
		}

		return assetPath;
	}

	private static string MakeSafeAssetName(string name)
	{
		foreach(var invalidChar in System.IO.Path.GetInvalidFileNameChars())
		{
			name = name.Replace(invalidChar, '_');
		}

		return string.IsNullOrWhiteSpace(name) ? "Combined" : name;
	}

	private static Texture2D GetMaterialTexture(Material material, string[] propertyNames)
	{
		foreach(var propertyName in propertyNames)
		{
			if(material != null && material.HasProperty(propertyName))
			{
				var texture = material.GetTexture(propertyName) as Texture2D;
				if(texture != null)
				{
					return texture;
				}
			}
		}

		return null;
	}

	private static Vector2 GetMaterialScale(Material material, string[] propertyNames)
	{
		foreach(var propertyName in propertyNames)
		{
			if(material != null && material.HasProperty(propertyName))
			{
				return material.GetTextureScale(propertyName);
			}
		}

		return Vector2.one;
	}

	private static Vector2 GetMaterialOffset(Material material, string[] propertyNames)
	{
		foreach(var propertyName in propertyNames)
		{
			if(material != null && material.HasProperty(propertyName))
			{
				return material.GetTextureOffset(propertyName);
			}
		}

		return Vector2.zero;
	}

	private static Texture2D CreateReadableTextureCopy(Texture2D source)
	{
		if(source == null)
		{
			return null;
		}

		if(source.isReadable)
		{
			return UnityEngine.Object.Instantiate(source);
		}

		var renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
		var previousRenderTexture = RenderTexture.active;

		try
		{
			Graphics.Blit(source, renderTexture);
			RenderTexture.active = renderTexture;

			var readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, source.mipmapCount > 1,
				GraphicsSettings.lightsUseLinearIntensity);
			readableTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
			readableTexture.Apply(true, false);
			return readableTexture;
		}
		finally
		{
			RenderTexture.active = previousRenderTexture;
			RenderTexture.ReleaseTemporary(renderTexture);
		}
	}

	private static Texture2D CreateFallbackTexture()
	{
		var fallbackTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
		var pixels = Enumerable.Repeat(Color.white, 16).ToArray();
		fallbackTexture.SetPixels(pixels);
		fallbackTexture.Apply(false, false);
		return fallbackTexture;
	}

	private static Texture2D CreateNormalFallbackTexture()
	{
		var fallbackTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
		var flatNormal = new Color(0.5f, 0.5f, 1f, 1f);
		var pixels = Enumerable.Repeat(flatNormal, 16).ToArray();
		fallbackTexture.SetPixels(pixels);
		fallbackTexture.Apply(false, false);
		return fallbackTexture;
	}

	private sealed class SubMeshTextureSlot
	{
		public int SubMeshIndex;
		public Material Material;
		public Texture2D MainTexture;
		public Vector2 MainScale;
		public Vector2 MainOffset;
		public Texture2D RampTexture;
		public Texture2D NormalTexture;
	}
}
