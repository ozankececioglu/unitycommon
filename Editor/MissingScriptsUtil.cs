using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Extensions;

[ExecuteInEditMode]
[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour))]
public class MissingScriptsUtil : Editor
{
	class ScriptInfo
	{
		public Dictionary<string, FieldInfo> fields;
		public MonoScript script;
	}

	static List<ScriptInfo> infos = null;
	static List<string> prefabPaths;
	static int iprefab;
	static int ifixFailed;

	void OnEnable()
	{
		if (infos == null) {
			return;
		}

		List<string> properties = new List<string>();
		foreach (var target in targets) {
			var seri = new SerializedObject(target);
			var iter = seri.GetIterator();
			var go = (target as Component).gameObject;

			iter.NextVisible(true);
			if (iter.name.Equals("m_Script") && iter.objectReferenceValue == null) {
				properties.Clear();
				while (iter.NextVisible(false)) {
					properties.Add(iter.name);
				}

			} else {
				Debug.LogError("Not a missing monoscript", go);
				ifixFailed++;
				continue;
			}

			ScriptInfo found = null;
			foreach (var info in infos) {
				if (properties.All(property => info.fields.ContainsKey(property))) {
					found = info;
					break;
				}
			}

			if (found == null) {
				Debug.LogError("No suitable scipts", go);
				ifixFailed++;
				continue;

			} else {
				iter.Reset();
				iter.NextVisible(true);
				iter.objectReferenceValue = found.script;
				seri.ApplyModifiedProperties();
				seri.UpdateIfDirtyOrScript();
			}
		}

		LoadNextPrefabWithMissingScript();
	}

	[MenuItem("UnityCommon/List All Scripts", priority = 498)]
	public static void ListAllScripts()
	{
		// Project loaded scripts
		var allScripts = Resources.FindObjectsOfTypeAll(typeof(MonoScript)).Cast<MonoScript>().Where(script => script.hideFlags == 0);
		// Load dlls
		var dllPaths = new DirectoryInfo(Application.dataPath).GetFiles("*.dll", SearchOption.AllDirectories);
		foreach (var path in dllPaths) {
			var assetPath = path.FullName.Replace('\\', '/').Replace(Application.dataPath, "Assets");
			allScripts = allScripts.Union(AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(obj => obj is MonoScript).Cast<MonoScript>());
		}
		var verifiedScripts = allScripts.Where(script => script != null && script.GetClass() != null);

		var log = "verifiedScripts #" + verifiedScripts.Count() + ": ";
		foreach (var script in verifiedScripts) { log += script.GetClass().Name + ", "; }
		Debug.Log(log);
	}

	[MenuItem("UnityCommon/Find Missing Scripts", priority = 499)]
	public static void FindMissingScripts()
	{
		var missings = new List<GameObject>();

		// Get prefab list
		var prefabPaths = new DirectoryInfo(Application.dataPath).GetFiles("*.prefab", SearchOption.AllDirectories)
			.Select<FileInfo, string>(f => f.FullName.Replace('\\', '/').Replace(Application.dataPath, "Assets"))
			.ToList();

		foreach (var path in prefabPaths) {
			Resources.LoadAssetAtPath(path, typeof(GameObject));
		}

		var gos = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
		foreach (GameObject go in gos) {
			if (FindMissingScriptInGo(go)) {
				missings.Add(go);
			}
		}

		Selection.objects = missings.ToArray();

		var log = "";
		foreach (var go in missings) {
			if (EditorCommon.IsPrefab(go)) {
				if (PrefabUtility.FindPrefabRoot(go) == go) {
					log += AssetDatabase.GetAssetPath(go) + "\r\n";
				}
			} else {
				log += go.transform.ScenePath() + "\r\n";
			}
		}
		File.WriteAllText("missing.txt", log);
	}

	[MenuItem("UnityCommon/Find And Fix Missing Scripts", priority = 500)]
	public static void FixMissingScripts()
	{
		Resources.UnloadUnusedAssets();

		// Project loaded scripts
		var allScripts = Resources.FindObjectsOfTypeAll(typeof(MonoScript)).Cast<MonoScript>().Where(script => script.hideFlags == 0);
		// Load dlls
		var dllPaths = new DirectoryInfo(Application.dataPath).GetFiles("*.dll", SearchOption.AllDirectories);
		foreach (var path in dllPaths) {
			var assetPath = path.FullName.Replace('\\', '/').Replace(Application.dataPath, "Assets");
			allScripts = allScripts.Union(AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(obj => obj is MonoScript).Cast<MonoScript>());
		}

		// Process all scripts
		infos = new List<ScriptInfo>();
		foreach (var script in allScripts) {
			if (script.GetClass() != null) {
				var fields = script.GetClass().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(p => p.IsPublic || (!p.IsPublic && p.IsDefined(typeof(SerializeField), false)))
					.ToDictionary(p => p.Name);
				infos.Add(new ScriptInfo { script = script, fields = fields });
			}
		}

		ifixFailed = 0;
		iprefab = 0;
		// Get prefab list
		prefabPaths = new DirectoryInfo(Application.dataPath).GetFiles("*.prefab", SearchOption.AllDirectories)
			.Select<FileInfo, string>(f => f.FullName.Replace('\\', '/').Replace(Application.dataPath, "Assets"))
			.ToList();

		// Get loaded gameobjects
		var missings = new List<GameObject>();
		//		var gos = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
		//		foreach (GameObject go in gos) {
		//			if (FindMissingScriptInGo(go)) {
		//				missings.Add(go);
		//			}
		//		}

		Debug.Log(String.Format("Missing scripts fix operation about to start with {0} gameobjects, {1} prefabs and {2} scripts", missings.Count, prefabPaths.Count, infos.Count));

		if (missings.Count > 0) {
			Selection.objects = missings.ToArray();
		} else {
			LoadNextPrefabWithMissingScript();
		}
	}

	static bool FindMissingScriptInGo(GameObject go)
	{
		Component[] components = go.GetComponents<Component>();
		for (int i = 0; i < components.Length; i++) {
			if (components[i] == null) {
				return true;
			}
		}
		return false;
	}

	static bool LoadNextPrefabWithMissingScript()
	{
		while (iprefab < prefabPaths.Count) {
			EditorApplication.SaveAssets();
			Resources.UnloadUnusedAssets();
			var go = Resources.LoadAssetAtPath(prefabPaths[iprefab], typeof(GameObject)) as GameObject;
			iprefab++;
			var children = go.transform.Children().Select<Transform, GameObject>(t => t.gameObject).Where(c => FindMissingScriptInGo(c));
			if (children.Any()) {
				Debug.Log(String.Format("Will fix prefab no {0} named {1}", iprefab, go.name));
				Selection.objects = children.ToArray();
				return true;
			}
		}

		Debug.Log("Missing script fixing operation done" + (ifixFailed > 0 ? (", errors: " + ifixFailed) : ""));
		infos = null;
		prefabPaths = null;
		iprefab = 0;
		EditorApplication.SaveAssets();
		EditorApplication.SaveScene();
		return false;
	}
}
