using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class BundleUtility 
{
	private static Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle> ();	
	public static AssetBundle GetBundle (string name, bool cache = true)
	{
		AssetBundle bundle;
		if (bundles.ContainsKey (name))
			bundle = bundles [name];
		else {
			string path = Application.streamingAssetsPath + "/" + name + ".unity3d";
			bundle = AssetBundle.CreateFromFile (path);
			if (bundle == null) {
				Common.Log("Bundle not found at path " + path);
				return null;
			}
			
			if (cache) {
				bundles.Add (name, bundle);
			}
		}
		
		return bundle;
	}

	public static GameObject InstantiateBundle (string name, string assetName = null)
	{
		AssetBundle bundle = GetBundle (name);
		if (bundle == null) {
			return null;
		}
		UnityEngine.Object source = assetName == null || assetName == "" || assetName == bundle.mainAsset.name ? bundle.mainAsset : bundle.Load (assetName);
		GameObject result = UnityEngine.Object.Instantiate(source) as GameObject;
		result.name = source.name;
		return result;
	}
	
	public static GameObject InstantiateBundleForOnce(string name, string assetName = null) 
	{
		AssetBundle bundle = GetBundle (name, false);
		if (bundle == null) {
			return null;
		}
		UnityEngine.Object source = assetName == null || assetName == "" || assetName == bundle.mainAsset.name ? bundle.mainAsset : bundle.Load (assetName);
		GameObject result = UnityEngine.Object.Instantiate(source) as GameObject;
		result.name = source.name;
		bundle.Unload(false);
		return result;
	}
	
	public static void UnloadBundle(string name, bool deleteSceneCopies) 
	{
		if (bundles.ContainsKey(name)) {
			AssetBundle bundle = bundles[name];
			bundle.Unload(deleteSceneCopies);
			bundles.Remove(name);
		}
	}

	public static void UnloadAllBundles (bool deleteSceneCopies)
	{
		foreach (KeyValuePair<string, AssetBundle> pair in bundles) {
			pair.Value.Unload (deleteSceneCopies);
		}
		bundles.Clear();
	}
}

