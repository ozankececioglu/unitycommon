#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Extensions;

public class QualitySettingsManager : MonoBehaviour 
{
	void Awake() 
	{
		QualitySettings.SetQualityLevel(5, true);
	}
}
