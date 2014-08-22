using UnityEngine;
using System.Collections;
using System.Reflection;

public static class UnityInternals 
{
	static Assembly unityAssembly;
	static MethodInfo method_GUI_DoTextField;
	static MethodInfo method_GUIGridSizer_GetRect;

	static UnityInternals() {
		
		var publicStatic = BindingFlags.Static | BindingFlags.Public;
		var nonPublicStatic = BindingFlags.Static | BindingFlags.NonPublic;
		
		unityAssembly = Assembly.GetAssembly(typeof(GUI));
		method_GUIGridSizer_GetRect = unityAssembly.GetType("UnityEngine.GUIGridSizer").GetMethod("GetRect", publicStatic);
		method_GUI_DoTextField = typeof(GUI).GetMethod("DoTextField", nonPublicStatic);
	}
	
	public static
	void GUI_DoTextField(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style) {
		method_GUI_DoTextField.Invoke(null, new object[] {position, id, content, multiline, maxLength, style});
	}
	
	public static
	Rect GUIGridSizer_GetRect(GUIContent [] content, int xCount, GUIStyle style, GUILayoutOption [] options) {
		return (Rect)method_GUIGridSizer_GetRect.Invoke(null, new object[] {content, xCount, style, options});
	}
}
