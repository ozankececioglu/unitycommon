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
		foreach (var name in Enum.GetNames(typeof(ProductType))) {
			Debug.Log (name);
		}
	}
	
	[MenuItem("Test/Test2")]
	static void Test2()
	{
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
