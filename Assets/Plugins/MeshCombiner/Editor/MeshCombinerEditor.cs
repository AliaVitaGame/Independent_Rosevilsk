using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshCombiner))]
public class MeshCombinerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		MeshCombiner meshCombiner = (MeshCombiner)target;
		Mesh mesh = meshCombiner.GetComponent<MeshFilter>().sharedMesh;

		#region Script:
		GUI.enabled = false;
		EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MeshCombiner)target), typeof(MeshCombiner), false);
		GUI.enabled = true;
		#endregion Script.

		#region MeshFiltersToSkip array:
		SerializedProperty meshFiltersToSkip = serializedObject.FindProperty("meshFiltersToSkip");
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(meshFiltersToSkip, true);
		if(EditorGUI.EndChangeCheck())
		{
			serializedObject.ApplyModifiedProperties();
		}
		#endregion MeshFiltersToSkip array.

		#region Button which combine Meshes into one Mesh & Toggles with combine options:
		EditorGUI.BeginChangeCheck();

		meshCombiner.CreateMultiMaterialMesh = GUILayout.Toggle(meshCombiner.CreateMultiMaterialMesh, "Create Multi-Material Mesh");
		meshCombiner.CombineInactiveChildren = GUILayout.Toggle(meshCombiner.CombineInactiveChildren, "Combine Inactive Children");

		meshCombiner.DeactivateCombinedChildren = GUILayout.Toggle(meshCombiner.DeactivateCombinedChildren, "Deactivate Combined Children");
		meshCombiner.DeactivateCombinedChildrenMeshRenderers = GUILayout.Toggle(meshCombiner.DeactivateCombinedChildrenMeshRenderers,
			"Deactivate Combined Children's MeshRenderers");

		meshCombiner.GenerateUVMap = GUILayout.Toggle(meshCombiner.GenerateUVMap, new GUIContent("Generate UV Map", "It is a slow operation that "+
			"generates a UV map (required for the lightmap).\n\nCan be used only in the Editor."));

		// The last (6) "Destroy Combined Children" Toggle:
		GUIStyle style = new GUIStyle(EditorStyles.toggle);
		if(meshCombiner.DestroyCombinedChildren)
		{
			style.onNormal.textColor = new Color(1, 0.15f, 0);
		}
		meshCombiner.DestroyCombinedChildren = GUILayout.Toggle(meshCombiner.DestroyCombinedChildren,
			new GUIContent("Destroy Combined Children", "In the editor this operation is registered in Undo together with Combine Meshes."), style);

		if(EditorGUI.EndChangeCheck())
		{
			EditorUtility.SetDirty(meshCombiner);
		}

		if(GUILayout.Button("Combine Meshes"))
		{
			meshCombiner.CombineMeshes(true);
			EditorUtility.SetDirty(meshCombiner);
		}

		EditorGUILayout.Space(4);
		meshCombiner.MaterialAtlasMaxSize = EditorGUILayout.IntField("Material Atlas Max Size", meshCombiner.MaterialAtlasMaxSize);
		meshCombiner.MaterialAtlasPadding = EditorGUILayout.IntField("Material Atlas Padding", meshCombiner.MaterialAtlasPadding);

		if(GUILayout.Button(new GUIContent("Combine Materials", "Packs submesh textures into one atlas and assigns a single material. "+
			"Does not merge geometry. Remaps UV0 and saves mesh/material assets under Folder path.")))
		{
			if(MeshCombinerMaterialMerger.TryCombineMaterials(meshCombiner, out var combineMaterialsMessage))
			{
				Debug.Log("<color=#00cc00><b>"+combineMaterialsMessage+"</b></color>");
			}
			else
			{
				Debug.LogWarning("<color=#ff9900><b>"+combineMaterialsMessage+"</b></color>");
			}

			EditorUtility.SetDirty(meshCombiner);
		}
		#endregion Button which combine Meshes into one Mesh & Toggles with combine options.

		#region Path to the folder where combined Meshes will be saved:
		// Create Labels:
		GUILayout.Label("");
		GUILayout.Label(new GUIContent("Folder path:", "Folder path to save combined Mesh."));

		// Create style wherein text color will be red if folder path is not valid:
		style = new GUIStyle(EditorStyles.textField);
		bool isValidPath = IsValidPath(meshCombiner.FolderPath);
		if(!isValidPath)
		{
			style.normal.textColor = Color.red;
			style.focused.textColor = Color.red;
		}

		// Create TextField with custom style:
		meshCombiner.FolderPath = EditorGUILayout.TextField(meshCombiner.FolderPath, style);
		#endregion Path to the folder where combined Meshes will be saved.

		#region Button which save/show combined Mesh:
		bool meshIsSaved = (mesh != null && AssetDatabase.Contains(mesh));
		GUI.enabled = (mesh != null && (isValidPath || meshIsSaved)); // Valid path is required for not saved Mesh.
		string saveMeshButtonText = (meshIsSaved) ? "Show Saved Combined Mesh" : "Save Combined Mesh";

		if(GUILayout.Button(saveMeshButtonText))
		{
			meshCombiner.FolderPath = SaveCombinedMesh(meshCombiner, mesh, meshCombiner.FolderPath);
		}
		GUI.enabled = true;
		#endregion Button which save/show combined Mesh.
	}

	private bool IsValidPath(string folderPath)
	{
		string pattern = "[:*?\"<>|]"; // Prohibited characters.
		Regex regex = new Regex(pattern);
		return (!regex.IsMatch(folderPath));
	}

	private string SaveCombinedMesh(MeshCombiner meshCombiner, Mesh mesh, string folderPath)
	{
		bool meshIsSaved = AssetDatabase.Contains(mesh); // If is saved then only show it in the project view.
		string meshPath = meshIsSaved ? AssetDatabase.GetAssetPath(mesh) : null;

		#region Create directories if Mesh and path doesn't exists:
		folderPath = folderPath.Replace('\\', '/');
		if(!meshIsSaved && !AssetDatabase.IsValidFolder("Assets/"+folderPath))
		{
			string[] folderNames = folderPath.Split('/');
			folderNames = folderNames.Where((folderName) => !folderName.Equals("")).ToArray();
			folderNames = folderNames.Where((folderName) => !folderName.Equals(" ")).ToArray();

			folderPath = "/"; // Reset folder path.
			for(int i = 0; i < folderNames.Length; i++)
			{
				folderNames[i] = folderNames[i].Trim();
				if(!AssetDatabase.IsValidFolder("Assets"+folderPath+folderNames[i]))
				{
					string folderPathWithoutSlash = folderPath.Substring(0, folderPath.Length-1); // Delete last "/" character.
					AssetDatabase.CreateFolder("Assets"+folderPathWithoutSlash, folderNames[i]);
				}
				folderPath += folderNames[i]+"/";
			}
			folderPath = folderPath.Substring(1, folderPath.Length-2); // Delete first and last "/" character.
		}
		#endregion Create directories if Mesh and path doesn't exists.

		#region Save Mesh:
		if(!meshIsSaved)
		{
			meshPath = "Assets/"+folderPath+"/"+mesh.name+".asset";
			int assetNumber = 1;
			while(AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) != null) // If Mesh with same name exists, change name.
			{
				meshPath = "Assets/"+folderPath+"/"+mesh.name+" ("+assetNumber+").asset";
				assetNumber++;
			}

			AssetDatabase.CreateAsset(mesh, meshPath);
			AssetDatabase.SaveAssets();
			Debug.Log("<color=#ff9900><b>Mesh \""+mesh.name+"\" was saved in the \""+folderPath+"\" folder.</b></color>"); // Show info about saved mesh.
		}
		#endregion Save Mesh.

		Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
		if(savedMesh != null)
		{
			MeshFilter meshFilter = meshCombiner.GetComponent<MeshFilter>();
			Undo.RecordObject(meshFilter, "Assign Combined Mesh");
			meshFilter.sharedMesh = savedMesh;
			EditorUtility.SetDirty(meshFilter);
			EditorUtility.SetDirty(meshCombiner.gameObject);
		}

		EditorGUIUtility.PingObject(savedMesh != null ? savedMesh : mesh); // Show Mesh in the project view.
		return folderPath;
	}
}
