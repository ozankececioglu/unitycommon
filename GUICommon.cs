using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Extensions;

public static class GUICommon
{
	class ActionContainer {
		public Action action;
	}
	
	public static float fieldNameWidth = 150f;
	public static float fieldDepthWidth = 20f;
	
	static GUIContent textImageContent = new GUIContent ();
	static GUIContent imageContent = new GUIContent ();
	static GUIContent textContent = new GUIContent ();
	static Stack<ActionContainer> postGuiActions = new Stack<ActionContainer>();
	//
	private static GUIStyle _fieldNameStyle = null;
	public static GUIStyle fieldNameStyle {
		get {
			if (_fieldNameStyle == null) {
				_fieldNameStyle = new GUIStyle (GUI.skin.label);
				_fieldNameStyle.clipping = TextClipping.Clip;
				_fieldNameStyle.wordWrap = false;
			}
			return _fieldNameStyle;
		}
	}
	
	static GUICommon()
	{
		
	}
	
	public static void PushPostGui() {
		postGuiActions.Push(new ActionContainer());
	}
	
	public static void PopPostGui() {
		var actionContainer = postGuiActions.Pop();
		if (actionContainer.action != null) {
			actionContainer.action();
		}
	}
	
	public static void AddPostGui(Action action) {
		var actionContainer = postGuiActions.Peek();
		actionContainer.action += action;
	}
	
	public static GUIContent TempContent (string text)
	{
		textContent.text = text;
		return textContent;
	}

	public static GUIContent TempContent (Texture image)
	{
		imageContent.image = image;
		return imageContent;
	}
	
	public static GUIContent TempContent (string text, Texture image)
	{
		textImageContent.text = text;
		textImageContent.image = image;
		return textImageContent;
	}
	
	public static GUIContent [] TempContent (string [] texts)
	{
		var result = new GUIContent[texts.Length];
		for (int itext = 0; itext < texts.Length; itext++) {
			result [itext] = new GUIContent (texts [itext]);
		}
		return result;
	}
	
	public static void ClearCache ()
	{
		comboControl = 0;
		textCacheControl = 0;
	}
	
	static int popupListHash = "Combo".GetHashCode ();
	static int comboControl = 0;
	static int comboResult = -1;
	static Rect comboRect;
	static int comboYFieldCount;
	
	public static bool ComboField (ref int selection, string [] fields, params GUILayoutOption [] options)
	{
		return ComboField (ref selection, fields, GUI.skin.box, options);
	}
	
	public static bool ComboField (ref int selection, string [] fields, GUIStyle style, params GUILayoutOption [] options)
	{
		int id = GUIUtility.GetControlID (popupListHash, FocusType.Passive);
		if (style == null)
			style = GUI.skin.button;
		
		GUILayout.Box (fields [selection], options);
		Rect boxRect = GUILayoutUtility.GetLastRect ();
		
		Event e = Event.current;
		if (e.type == EventType.MouseDown && boxRect.Contains (e.mousePosition)) {
			comboControl = id;
			var minSize = new Vector2 ();
			foreach (var field in fields) {
				var size = style.CalcSize (TempContent (field));
				if (size.x > minSize.x)
					minSize.x = size.x;
				minSize.y += size.y;
			}
			
			minSize.x += style.margin.left + style.margin.right;
			minSize.y += Mathf.Max (style.margin.top, style.margin.bottom) * (fields.Length + 1);
			comboRect.Set (Input.mousePosition.x - 1, Screen.height - Input.mousePosition.y - 1, minSize.x, minSize.y);
			
			if (comboRect.xMax > Screen.width)
				comboRect.x = Screen.width - comboRect.width;
			if (comboRect.yMax > Screen.height)
				comboRect.y = Screen.height - comboRect.height;
		}
		
		if (comboResult != -1 && comboControl == id) {
			bool result = selection != comboResult;
			selection = comboResult;
			comboResult = -1;
			comboControl = 0;
			return result;
		}
		
		int oldSelection = selection;
		if (comboControl == id) {
			AddPostGui(() => {
				var mouseUp = Event.current.type == EventType.MouseUp;
				GUI.Box(comboRect, "");
				var newSelection = GUI.SelectionGrid (comboRect, oldSelection, fields, 1);
				if (mouseUp) {
					comboResult = comboRect.Contains(Event.current.mousePosition) ? newSelection : oldSelection;
				}
			});
		}
		
		return false;
	}
	
	public static bool ColorField (ref Color color) 
	{
		return false;
	}
	
	static int textCacheControl = 0;
	static string textCache = null;
	delegate string StringerDel (object obj);
	delegate bool ParserDel (string text, out object value);
	
	static bool FormattedField (StringerDel stringer, ParserDel parser, ref object value, GUIStyle style,
		params GUILayoutOption[] options)
	{
		int controlId = GUIUtility.GetControlID (FocusType.Keyboard);
		if (controlId == textCacheControl && controlId != GUIUtility.keyboardControl) {
			textCacheControl = 0;
		}
		var inputStr = (controlId == textCacheControl && controlId == GUIUtility.keyboardControl) ? textCache : stringer (value);
		var rect = GUILayoutUtility.GetRect (TempContent (controlId == GUIUtility.keyboardControl ?
			inputStr + Input.compositionString : inputStr), style);
		var content = TempContent (inputStr);

		bool guiChanged = GUI.changed;
		GUI.changed = false;
		UnityInternals.GUI_DoTextField (rect, controlId, content, false, -1, style);
		bool tguiChanged = GUI.changed;
		GUI.changed = guiChanged;
		
		if (tguiChanged) {
			textCache = content.text;
			textCacheControl = controlId;
			object tvalue;
			if (parser (content.text, out tvalue)) {
				value = tvalue;
				return true;
			}
		}
		
		return false;
	}

	public static void FieldLabel(string name, int depth) {
		float pixelDepth = depth * GUICommon.fieldDepthWidth;
		GUILayout.Space(pixelDepth);
		GUILayout.Label(name, GUICommon.fieldNameStyle, GUILayout.Width(GUICommon.fieldNameWidth - pixelDepth));
	}
	
	public static float NumberLabel(Rect rect, string name) 
	{
		int controlId = GUIUtility.GetControlID(FocusType.Native);
		GUI.Label(rect, name, fieldNameStyle);
		if (Event.current.type == EventType.MouseDown) {
			if (rect.Contains(Event.current.mousePosition)) {
				GUIUtility.hotControl = controlId;
			}
		} else if (GUIUtility.hotControl == controlId) {
			if (Event.current.type == EventType.MouseUp) {
				GUIUtility.hotControl = 0;
			} else if (Event.current.type == EventType.MouseDrag) {
				return Input.GetKey(KeyCode.LeftShift) ? 5f * Event.current.delta.x : Event.current.delta.x;
			}
		}
		
		return 0f;
	}
	
	public static float NumberLabel(string name, params GUILayoutOption [] options)
	{
		int controlId = GUIUtility.GetControlID(FocusType.Native);
		GUILayout.Label(name, fieldNameStyle, options);
		if (Event.current.type == EventType.MouseDown) {
			Rect rect = GUILayoutUtility.GetLastRect();
			if (rect.Contains(Event.current.mousePosition)) {
				GUIUtility.hotControl = controlId;
			}
		} else if (GUIUtility.hotControl == controlId) {
			if (Event.current.type == EventType.MouseUp) {
				GUIUtility.hotControl = 0;
			} else if (Event.current.type == EventType.MouseDrag) {
				return Input.GetAxis("Mouse X");
			}
		}
		
		return 0f;
	}
	
	public static float NumberLabel(string name, int depth)
	{
		float pixelDepth = depth * GUICommon.fieldDepthWidth;
		GUILayout.Space(pixelDepth);
		return NumberLabel(name, GUILayout.Width(GUICommon.fieldNameWidth - pixelDepth));
	}
	
	public static bool StringField (ref string text, params GUILayoutOption[] options)
	{
		bool guiChanged = GUI.changed;
		GUI.changed = false;
		text = GUILayout.TextField (text, options);
		bool tguiChanged = GUI.changed;
		GUI.changed = guiChanged;
		
		return tguiChanged;
	}
	
	public static bool IntField (ref int value, params GUILayoutOption [] options)
	{
		var style = GUI.skin.textField;
		object oval = value;
		bool tresult = FormattedField (x => x.ToString (), delegate (string str, out object tvalue) {
			int val;
			bool result = Int32.TryParse (str, out val);
			tvalue = val;
			return result;
		}, ref oval, style, options);
		value = (int)oval;
		return tresult;
	}
	
	public static bool FloatField (ref float value, params GUILayoutOption [] options)
	{
		var style = GUI.skin.textField;
		object oval = value;
		bool tresult = FormattedField (x => ((float)x).ToString ("g7"), delegate (string str, out object tvalue) {
			float val;
			bool result = Single.TryParse (str, out val);
			tvalue = val;
			return result;
		}, ref oval, style, options);
		value = (float)oval;
		return tresult;
	}
	
	public static bool DoubleField (ref double value, params GUILayoutOption [] options)
	{
		var style = GUI.skin.textField;
		object oval = value;
		bool tresult = FormattedField (x => ((double)x).ToString ("g7"), delegate (string str, out object tvalue) {
			double val;
			bool result = Double.TryParse (str, out val);
			tvalue = val;
			return result;
		}, ref oval, style, options);
		value = (double)oval;
		return tresult;
	}
	
	public static bool Vector3Field (ref Vector3 value)
	{
		Rect rect = GUILayoutUtility.GetRect (TempContent ("Vector3Field"), GUI.skin.textField, GUILayout.ExpandWidth(true));
		var widthOption = GUILayout.Width(rect.width / 3);
		float delta;
		bool changed = false;
		
		GUILayout.BeginHorizontal();
		if (Event.current.type == EventType.Layout) {
			GUILayout.BeginHorizontal();
		} else {
			GUILayout.BeginHorizontal(widthOption);
		}
		delta = NumberLabel("x", GUILayout.ExpandWidth(false));
		if (!delta.IsZero()) {
			value.x += delta;
			changed = true;
		}
		changed |= GUICommon.FloatField (ref value.x, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal();
		
		if (Event.current.type == EventType.Layout) {
			GUILayout.BeginHorizontal();
		} else {
			GUILayout.BeginHorizontal(widthOption);
		}
		delta = NumberLabel("y", GUILayout.ExpandWidth(false));
		if (!delta.IsZero()) {
			value.y += delta;
			changed = true;
		}
		changed |= GUICommon.FloatField (ref value.y, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal();
		
		if (Event.current.type == EventType.Layout) {
			GUILayout.BeginHorizontal();
		} else {
			GUILayout.BeginHorizontal(widthOption);
		}
		delta = NumberLabel("z", GUILayout.ExpandWidth(false));
		if (!delta.IsZero()) {
			value.z += delta;
			changed = true;
		}
		changed |= GUICommon.FloatField (ref value.z, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal();
		GUILayout.EndHorizontal();
		
		return changed;
	}
	
	public static bool BoundsField (ref Bounds value)
	{
		var changed = false;
		GUILayout.BeginVertical ();
		GUILayout.BeginHorizontal ();
		GUILayout.Label ("Center", GUILayout.Width (GUICommon.fieldNameWidth));
		Vector3 center = value.center;
		if (Vector3Field (ref center)) {
			value.center = center;
			changed = true;
		}
		GUILayout.EndHorizontal ();
		
		GUILayout.BeginHorizontal ();
		GUILayout.Label ("Extents", GUILayout.Width (GUICommon.fieldNameWidth));
		Vector3 extents = value.extents;
		if (Vector3Field (ref extents)) {
			value.extents = extents;
			changed = true;
		}
		GUILayout.EndHorizontal ();
		GUILayout.EndVertical ();
		
		return changed;
	}

	public static bool StringFieldWithLabel(string label, ref string text)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, fieldNameStyle, GUILayout.Width(fieldNameWidth));
		bool guiChanged = GUI.changed;
		GUI.changed = false;
		text = GUILayout.TextField (text);
		bool tguiChanged = GUI.changed;
		GUI.changed = guiChanged || tguiChanged;
		GUILayout.EndHorizontal();
		
		return tguiChanged;
	}
	
	public static bool BoolFieldWithLabel(string label, ref bool value) 
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, fieldNameStyle, GUILayout.Width(fieldNameWidth));
		var tvalue = value;
		value = GUILayout.Toggle (tvalue, "");
		bool result = tvalue != value;
		GUILayout.EndHorizontal();
		
		return result;
	}
	
	public static bool IntFieldWithLabel(string label, ref int value) 
	{
		GUILayout.BeginHorizontal();
		float delta = NumberLabel(label, GUILayout.Width(fieldNameWidth));
		bool result = !delta.IsZero();
		if (result) {
			value += (int)delta;
		}
		result |= GUICommon.IntField (ref value);
		GUILayout.EndHorizontal();
		
		return result;
	}
	
	public static bool FloatFieldWithLabel(string label, ref float value) 
	{
		GUILayout.BeginHorizontal();
		float delta = NumberLabel(label, GUILayout.Width(fieldNameWidth));
		bool result = !delta.IsZero();
		if (result) {
			value += delta * 0.1f;
		}
		result |= GUICommon.FloatField (ref value);
		GUILayout.EndHorizontal();
		
		return result;
	}
	
	public static bool DoubleFieldWithLabel(string label, ref double value)
	{
		GUILayout.BeginHorizontal();
		float delta = NumberLabel(label, GUILayout.Width(fieldNameWidth));
		bool result = !delta.IsZero();
		if (result) {
			value += delta * 0.1f;
		}
		result |= GUICommon.DoubleField (ref value);
		GUILayout.EndHorizontal();
		
		return result;
	}
	
	public static bool ComboFieldWithLabel(string label, ref int selection, string [] fields)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label (label, fieldNameStyle, GUILayout.Width(fieldNameWidth));
		var result = GUICommon.ComboField (ref selection, fields);
		GUILayout.EndHorizontal();

		return result;
	}

	public static bool ImageFieldWithLabel(string label, Texture image)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, GUILayout.Width(fieldNameWidth));
		var result = image != null ? GUILayout.Button(image, GUI.skin.label, GUILayout.Height(80f)) : GUILayout.Button("<>");
		GUILayout.EndHorizontal();
		return result;
	}

	public static bool FloatSliderWithLabel(string label, ref float value, float min, float max)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, GUILayout.Width(fieldNameWidth));
		var nvalue = GUILayout.HorizontalSlider(value, min, max);
		GUILayout.EndHorizontal();
		if (!(nvalue - value).IsZero()) {
			value = nvalue;
			return true;
		}
		return false;
	}
}
