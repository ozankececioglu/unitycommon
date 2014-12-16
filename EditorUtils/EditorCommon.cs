#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using System;
using Extensions;

public class EditorCommon
{
#if UNITY_EDITOR
	public static void ApplyChangeToPrefab(GameObject go) {
		var selection = Selection.objects;
		Selection.activeObject = go;
		EditorApplication.ExecuteMenuItem("GameObject/Apply Changes To Prefab");
		Selection.objects = selection;
	}
	public static bool IsPrefab(UnityEngine.Object obj)
	{
		return PrefabUtility.GetPrefabParent(obj) == null && PrefabUtility.GetPrefabObject(obj) != null;
	}
	public static bool IsGameObject(UnityEngine.Object obj)
	{
		return obj is GameObject ? !IsPrefab(obj) : false;
	}

	[MenuItem("SimBT/Apply All Prefabs", priority = 500)]
	public static void ApplyChangesToPrefabs()
	{
		var gos = Selection.gameObjects;
		foreach (var go in gos) {
			Selection.activeGameObject = go;
			EditorApplication.ExecuteMenuItem("GameObject/Apply Changes To Prefab");
		}
		Selection.objects = gos;
	}

	[MenuItem("SimBT/Build Bundles From Selection", priority = 500)]
	public static void BuildBundlesFromSelection ()
	{
        ApplyChangesToPrefabs();
		
		if (Selection.activeObject == null) {
			Debug.LogError ("Select an object, no objects are selected");
			return;
		}
		
		Directory.CreateDirectory (Application.streamingAssetsPath + "/Bundles");
		
		List<string> tempPrefabNames = new List<string> ();
		
		BuildAssetBundleOptions options = BuildAssetBundleOptions.CollectDependencies |
				BuildAssetBundleOptions.CompleteAssets |
				BuildAssetBundleOptions.UncompressedAssetBundle;
		
		foreach (var obj in Selection.objects) {
			UnityEngine.Object content;
            if (EditorCommon.IsGameObject(obj))
            {
				GameObject go = obj as GameObject;
				go.SendMessage ("PreBuildBundle", null, SendMessageOptions.DontRequireReceiver);
				string prefabName = obj.name + ".prefab";
				tempPrefabNames.Add (prefabName);
				content = PrefabUtility.CreatePrefab ("Assets/StreamingAssets/Bundles/" + prefabName, go);
			} else {
				content = obj;
			}
			
			if (!BuildPipeline.BuildAssetBundle (content, null, "Assets/StreamingAssets/Bundles/" + obj.name + ".unity3d", options, BuildTarget.StandaloneWindows)) {
				Debug.LogError ("Cant build asset bundle " + obj.name);
			}
		}
		
		foreach (string prefapName in tempPrefabNames) {
			string file = Application.streamingAssetsPath + "/Bundles/" + prefapName;
			File.Delete (file);
			File.Delete (file + ".meta");
		}
	}

	[MenuItem("SimBT/List Bundle Content", priority=500)]
	static void ListBundleContent ()
	{
		string path;
        if (Selection.activeObject && !IsGameObject(Selection.activeObject))
        {
			path = AssetDatabase.GetAssetOrScenePath (Selection.activeObject);
		} else {
			path = EditorUtility.OpenFilePanel ("Load Resource", "Assets/StreamingAssets/Bundles", "unity3d");
		}
		
		AssetBundle bundle = AssetBundle.CreateFromFile (path);
		if (bundle == null) {
			Debug.LogError (path + " is not an asset bundle");
		}
		
		UnityEngine.Object [] objects = bundle.LoadAll ();
		string result = (bundle.mainAsset ? "Main Asset: " + bundle.mainAsset.name : "") + "\n";
		foreach (UnityEngine.Object obj in objects) {
			string str = obj.ToString ();
			if (str == null || str == "")
				result += obj.name + "\n";
			else
				result += str + "\n";
		}
		
		Debug.Log (result);
		bundle.Unload (true);
	}

	[MenuItem("SimBT/Change Guid", priority = 500)]
	public static void ChangeGuid() 
	{
		if (Selection.activeObject == null) {
			Debug.LogError("Select an asset first");
			return;
		}
		
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if (path == null || path == "") {
			Debug.LogError("Select an asset first");
			return;
		}
		
		string replacePath = AssetDatabase.GenerateUniqueAssetPath(path);
		AssetDatabase.CopyAsset(path, replacePath);
		AssetDatabase.DeleteAsset(path);
		AssetDatabase.Refresh();
		AssetDatabase.MoveAsset(replacePath, path);
		AssetDatabase.Refresh();
	}
	
	static int go_count, components_count, missing_count;
	static List<UnityEngine.Object> selection = new List<UnityEngine.Object>();
	[MenuItem("SimBT/Find Missing Scripts", priority=500)]
    public static void FindMissingScripts()
    {
        GameObject[] go = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
        go_count = 0;
		components_count = 0;
		missing_count = 0;
		
		selection.Clear();
		
        foreach (GameObject g in go) {
   			FindInGO(g);
        }
		
		Selection.objects = selection.ToArray();
		
        Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", go_count, components_count, missing_count));
    }
	
    private static void FindInGO(GameObject g)
    {
        go_count++;
        Component[] components = g.GetComponents<Component>();
		bool addedToSelection = false;
        for (int i = 0; i < components.Length; i++)
        {
            components_count++;
            if (components[i] == null)
            {
                missing_count++;
                string s = g.name;
                Transform t = g.transform;
                while (t.parent != null) 
                {
                    s = t.parent.name +"/"+s;
                    t = t.parent;
                }
                Debug.Log (s + " has an empty script attached in position: " + i, g);
				
				if (!addedToSelection) {
					selection.Add(g);
					addedToSelection = true;
				}
            }
        }
    }
	
//	[MenuItem("SimBT/Remove Animations", false, 500)]
//	static void RemoveAnimations ()
//	{
//		if (Selection.activeGameObject == null) {
//			Debug.LogError ("Select an object", "No objects selected", "ok");
//			return;
//		}
//		
//		GameObject go = Selection.activeGameObject;
//		Animator [] animators = go.GetComponentsInChildren<Animator> ();
//		foreach (Animator anim in animators) {
//			UnityEngine.Object.DestroyImmediate (anim);
//		}
//	}
#endif	
}
