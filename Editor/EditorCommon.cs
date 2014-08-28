using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using System;
using Extensions;

#if UNITY_EDITOR
public class EditorCommon
{
	[MenuItem("SimBT/Build Bundles From Selection", priority = 500)]
	public static void BuildBundlesFromSelection ()
	{
		Common.ApplyChangesToPrefabs ();
		
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
			if (Common.IsGameObject (obj)) {
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
		if (Selection.activeObject && !Common.IsGameObject (Selection.activeObject)) {
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

//	[MenuItem("SimBT/Verify paths", false, 0)]
//	static void VerifyPaths ()
//	{
//		GameObject aigo = GameObject.Find ("/AI");
//		SimAIPath [] paths = aigo.GetComponentsInChildren<SimAIPath> ();
//		
//		foreach (SimAIPath path in paths) {
//			if (!path.Verify ()) {
//				Selection.activeObject = path.gameObject;
//				SceneView.currentDrawingSceneView.FrameSelected();
//				return;
//			}
//		}
//		
//		Debug.Log("All paths are verified and ok");
//	}
//	
//	[MenuItem("SimBT/Select AI Path %#t", false, 0)]
//	static void SelectAIPath ()
//	{
//		GameObject aigo = GameObject.Find ("/AI");
//		if (aigo) {
//			SimAIPath path = aigo.GetComponentInChildren<SimAIPath> ();
//			Selection.activeObject = path.gameObject;
//		}
//	}
//	
//	[MenuItem("SimBT/Select Agent Root %#r", false, 0)]
//	static void SelectAgentRoot ()
//	{
//		if (Selection.activeGameObject != null) {
//			Transform transform = Selection.activeGameObject.transform;
//			while (transform != null) {
//				if (transform.GetComponent<AIAgent> () != null) {
//					Selection.activeObject = transform.gameObject;
//					return;
//				}
//
//				transform = transform.parent;
//			}
//			Debug.Log ("No AIAgent found");
//		}
//	}
	
}
#endif