#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using System.Text;


public class Test : MonoBehaviour 
{
#if UNITY_EDITOR
	[MenuItem("Test/Test1")]
	static void Test1()
	{
		Debug.Log (FabricDB.Instance.fabrics.Max(fabric => fabric.difColorAverage));
		Debug.Log (FabricDB.Instance.fabrics.Min(fabric => fabric.difColorAverage));
		Debug.Log (FabricDB.Instance.fabrics.Max(fabric => fabric.tileRatio));
		Debug.Log (FabricDB.Instance.fabrics.Min(fabric => fabric.tileRatio));
		Debug.Log (FabricDB.Instance.fabrics.Max(fabric => fabric.specular));
		Debug.Log (FabricDB.Instance.fabrics.Min(fabric => fabric.specular));
		Debug.Log (FabricDB.Instance.fabrics.Max(fabric => fabric.shininess));
		Debug.Log (FabricDB.Instance.fabrics.Min(fabric => fabric.shininess));
		Debug.Log (FabricDB.Instance.fabrics.Max(fabric => fabric.fresnel));
		Debug.Log (FabricDB.Instance.fabrics.Min(fabric => fabric.fresnel));
	}
	
	[MenuItem("Test/Test2")]
	static void Test2()
	{
		foreach (var product in ProductDB.Instance.Products) {
			for (int imisfit = 0; imisfit < product.MisfitModels.Count; imisfit++) {
				product.MisfitModels[imisfit] = ProductDB.Instance.Products.FirstOrDefault(tproduct => tproduct.PrefabName.Equals(product.MisfitModels[imisfit])).Name;
			}
		}
		ProductDB.Instance.Save();
	}
	
	[MenuItem("Test/Test3")]
	static void Test3()
	{
	}
	
	[MenuItem("Test/Test4")]
	static void Test4()
	{
	}
#endif

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
