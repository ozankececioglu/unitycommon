using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Extensions;
using System.Runtime.Serialization;

#region Helpers
[System.Serializable]
public abstract class DynamicEnum
{
	private static Dictionary<Type, string[]> cachedNames = new Dictionary<Type, string[]> ();
	public int value = 0;

	public virtual string Name { get { return GetNames () [value]; } }

	public static implicit operator int (DynamicEnum d)
	{
		return d.value;
	}

	
	public string[] GetNames ()
	{
		string[] result;
		var type = GetType ();
		if (!cachedNames.TryGetValue (type, out result)) {
			result = InitNames ();
			cachedNames.Add (type, result);
		}
		return result;
	}

	public abstract string[] InitNames ();
}

#endregion

public class PropertyDrawer
{
	public enum NullPolicy
	{
		HideNullFields,
		ShowNullFields,
		InstantiateNullFields
	}

	public struct DrawerArgs
	{
		public string name;
		public Type type;
		public object value;
		public int depth;
		public NullPolicy nullPolicy;

		public void Set (string tname, Type ttype, object tvalue, int tdepth, NullPolicy anullPolicy)
		{
			name = tname;
			type = ttype;
			value = tvalue;
			depth = tdepth;
			nullPolicy = anullPolicy;
		}

		public void Set (string tname, Type ttype, object tvalue, int tdepth)
		{
			name = tname;
			type = ttype;
			value = tvalue;
			depth = tdepth;
		}

		public void Set (string tname, Type ttype, object tvalue)
		{
			name = tname;
			type = ttype;
			value = tvalue;
		}

		public void Set (string tname, object tvalue)
		{
			name = tname;
			value = tvalue;
		}
	}
	
	public class Label : Attribute
	{
		public string label;
		
		public Label()
		{
			label = "";
		}
		
		public Label (string tlabel)
		{
			label = tlabel;
		}
	}

	public class CustomPropertyDrawer : Attribute
	{
		public Type type;

		public CustomPropertyDrawer (Type ttype)
		{
			type = ttype;
		}
	}
	
	public static string sizeText = "Size";
	public static string nullText = "'Null'";
	public static string createText = "Create";
	
//	private static UnityObjectDrawer unityObjectDrawer;
	private static EnumDrawer enumDrawer;
	private static ArrayDrawer arrayDrawer;
	private static ListDrawer listDrawer;
	private static ObjectDrawer objectDrawer;
	private static DynamicEnumDrawer dynamicEnumDrawer;
	private static Dictionary<Type, PropertyDrawer> drawers;
		
	static PropertyDrawer ()
	{
		drawers = new Dictionary<Type, PropertyDrawer> ();
		enumDrawer = new EnumDrawer ();
//		unityObjectDrawer = new UnityObjectDrawer();
		listDrawer = new ListDrawer ();
		arrayDrawer = new ArrayDrawer ();
		objectDrawer = new ObjectDrawer ();
		dynamicEnumDrawer = new DynamicEnumDrawer ();
		
		var types = Common.GetExtendedTypes (typeof(PropertyDrawer));
		var signature = new Type[0];
		var parameters = new object[0];

		foreach (var type in types) {
			var attr = (CustomPropertyDrawer)Attribute.GetCustomAttribute (type, typeof(CustomPropertyDrawer));
			if (attr != null) {
				var constructorInfo = type.GetConstructor (signature);
				if (constructorInfo != null) {
					var instance = (PropertyDrawer)constructorInfo.Invoke (parameters);
					drawers.Add (attr.type, instance);
				}
			}
		}
	}

	public static PropertyDrawer GetDrawer (Type type)
	{
		if (type == null) {
			return null;
		}
		
		PropertyDrawer result = null;
		if (!drawers.TryGetValue (type, out result)) {
			if (enumDrawer.SupportsType (type)) {
				result = enumDrawer;
			} else if (dynamicEnumDrawer.SupportsType (type)) {
				result = dynamicEnumDrawer;
//			} else if (unityObjectDrawer.SupportsType(type)) {
//				return unityObjectDrawer;
			} else if (arrayDrawer.SupportsType (type)) {
				result = arrayDrawer;
			} else if (listDrawer.SupportsType (type)) {
				result = listDrawer;
			} else {
				result = objectDrawer;
			}
			drawers.Add (type, result);
		}
		return result;
	}

	public static bool Draw (object value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		return Draw (null, value, nullPolicy);
	}

	public static bool Draw (string name, object value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		if (value != null) {
			var args = new DrawerArgs ();
			args.Set (name, value.GetType (), value, string.IsNullOrEmpty (name) ? -1 : 0, nullPolicy);
			var result = GetDrawer (args.type).OnGUI (ref args);
			return result;
		} else {
			return false;
		}
	}

	public static bool Draw<T> (ref T value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		return Draw("", ref value, nullPolicy);
	}

	public static bool Draw<T> (string name, ref T value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		var args = new DrawerArgs ();
		args.Set (name, typeof(T), value, string.IsNullOrEmpty (name) ? -1 : 0, nullPolicy);
		var result = GetDrawer (args.type).OnGUI (ref args);
		if (result) {
			value = (T)args.value;
		}
		return result;
	}

	public virtual bool SupportsType (Type type)
	{
		return false;
	}

	public virtual bool OnGUI (ref DrawerArgs args)
	{
		GUILayout.BeginHorizontal ();
		GUICommon.FieldLabel (args.name, args.depth);
		bool result = OnValue (ref args);
		GUILayout.EndHorizontal ();

		return result;
	}

	public virtual bool OnValue (ref DrawerArgs args)
	{
		GUILayout.FlexibleSpace ();
		return false;
	}
}

// ==================
// Drawers
// ==================

internal class EnumDrawer : PropertyDrawer
{
	static Dictionary<Type, string[]> cachedNames = new Dictionary<Type, string[]> ();

	public static string[] GetNames (Type type)
	{
		string[] result;
		if (!cachedNames.TryGetValue (type, out result)) {
			List<string> names = new List<string> ();
			var members = type.GetMembers (BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
			foreach (var member in members) {
				var label = (Label)Attribute.GetCustomAttribute (member, typeof(Label));
				names.Add (label == null ? member.Name : label.label);
			}
			result = names.ToArray ();
			cachedNames.Add (type, result);
		}

		return result;
	}

	public override bool OnValue (ref DrawerArgs args)
	{
		var values = Enum.GetValues (args.type);
		int selection = (int)Array.IndexOf<int> (values as int[], (int)args.value);
		var names = GetNames (args.type);
		if (GUICommon.ComboField (ref selection, names)) {
			args.value = values.GetValue (selection);
			return true;
		}
		return false;
	}

	public override bool SupportsType (Type type)
	{
		return type.IsEnum;
	}
}

//internal class UnityObjectDrawer : PropertyDrawer
//{
//	static Type objectType = typeof(UnityEngine.Object);
//
//	public override bool OnValue (ref AIPropertyArgs args)
//	{		
//		var clipboard = AIAuthor.Instance.clipboard;
//		if (clipboard != null && clipboard.GetType ().IsAssignableFrom (value.GetType()) && GUILayout.Button ("Paste")) {
//			value = AIAuthor.Instance.clipboard;
//			return true;
//		}
//		
//		if (value == null && GUILayout.Button ("Remove")) {
//			value = null;
//		}
//		
//		return false;
//	}
//
//	public override bool SupportsType (Type type)
//	{
//		return objectType.IsAssignableFrom (type);
//	}
//}

internal class ListDrawer : PropertyDrawer
{
	public override bool OnGUI (ref DrawerArgs args)
	{
		var list = args.value as IList;

		Type elementType = args.type.GetGenericArguments () [0];
		var drawer = PropertyDrawer.GetDrawer (elementType);
		if (drawer == null)
			return false;

		var changed = false;
		if (list == null) {
			if (args.nullPolicy == NullPolicy.InstantiateNullFields) {
				list = (IList)Common.CreateInstance (args.type);
				args.value = list;
				changed = true;

			} else if (args.nullPolicy == NullPolicy.ShowNullFields) {
				GUILayout.BeginHorizontal ();
				GUICommon.FieldLabel (args.name, args.depth);
				GUILayout.Label (PropertyDrawer.nullText);
				if (GUILayout.Button (PropertyDrawer.createText)) {
					list = (IList)Common.CreateInstance (args.type);
					args.value = list;
					changed = true;
				}
				GUILayout.EndHorizontal ();
				return changed;

			} else {
				return false;
			}
		}

		GUILayout.BeginHorizontal ();
		GUICommon.FieldLabel (args.name, args.depth);
		GUILayout.Label (PropertyDrawer.sizeText + ": ");

		int count = list.Count;
		if (GUICommon.IntField (ref count)) {
			changed = true;
			if (count < list.Count) {
				args.type.GetMethod ("RemoveRange").Invoke (list, new object[] { count, list.Count - count });
			} else if (count > list.Count) {
				args.type.GetMethod ("AddRange").Invoke (list, new object[] { Array.CreateInstance (elementType, count - list.Count) });
			}
		}
		GUILayout.EndHorizontal ();

		var targs = new DrawerArgs ();
		targs.type = elementType;
		targs.depth = args.depth + 1;
		targs.nullPolicy = args.nullPolicy;

		GUILayout.BeginVertical ();
		for (int index = 0; index < count; index++) {
			targs.Set ("#" + index, list [index]);
			if (drawer.OnGUI (ref targs)) {
				list [index] = targs.value;
				changed = true;
			}
		}
		GUILayout.EndVertical ();

		return changed;
	}

	public override bool SupportsType (Type type)
	{
		return type.IsGenericType &&
			!type.IsGenericTypeDefinition &&
			type.GetGenericTypeDefinition () == typeof(List<>);
	}
}

internal class ArrayDrawer : PropertyDrawer
{
	public override bool OnGUI (ref DrawerArgs args)
	{
		var array = args.value as Array;
		Type elementType = args.type.GetElementType ();
		var drawer = PropertyDrawer.GetDrawer (elementType);
		if (drawer == null)
			return false;

		var changed = false;
		if (array == null) {
			if (args.nullPolicy == NullPolicy.InstantiateNullFields) {
				array = Common.CreateInstanceArray (elementType, 0);
				args.value = array;
				changed = true;

			} else if (args.nullPolicy == NullPolicy.ShowNullFields) {
				GUILayout.BeginHorizontal ();
				GUICommon.FieldLabel (args.name, args.depth);
				GUILayout.Label (PropertyDrawer.nullText);
				if (GUILayout.Button (PropertyDrawer.createText)) {
					array = Common.CreateInstanceArray (elementType, 0);
					args.value = array;
					changed = true;
				}
				GUILayout.EndHorizontal ();
				return changed;

			} else {
				return false;
			}
		}

		GUILayout.BeginHorizontal ();
		GUICommon.FieldLabel (args.name, args.depth);
		GUILayout.Label (PropertyDrawer.sizeText + ": ");

		int count = array.Length;
		if (GUICommon.IntField (ref count)) {
			changed = true;
			var parameters = new object[] { array, count };
			typeof(Array).GetMethod ("Resize").MakeGenericMethod (elementType).Invoke (null, parameters);
			array = parameters [0] as Array;
			args.value = array;
		}
		GUILayout.EndHorizontal ();

		var targs = new DrawerArgs ();
		targs.type = elementType;
		targs.depth = args.depth + 1;

		GUILayout.BeginVertical ();
		for (int index = 0; index < count; index++) {
			targs.Set ("#" + index, array.GetValue (index));
			if (drawer.OnGUI (ref targs)) {
				array.SetValue (targs.value, index);
				changed = true;
			}
		}
		GUILayout.EndVertical ();

		return changed;
	}

	public override bool SupportsType (Type type)
	{
		return type.IsArray;
	}
}

internal class ObjectDrawer : PropertyDrawer
{
	public abstract class LabelDescription
	{
		public Label customLabel;

		public LabelDescription (Label label)
		{
			customLabel = label;
		}

		public string Name { get { return customLabel.label; } }

		public abstract object GetValue (object target);

		public abstract void SetValue (object target, object value);

		public abstract Type GetLabelType ();
	}

	public class FieldDescription : LabelDescription
	{
		public FieldInfo field;

		public FieldDescription (Label label, FieldInfo tfield)
			: base(label)
		{
			field = tfield;
		}

		public override object GetValue (object target)
		{
			return field.GetValue (target);
		}

		public override void SetValue (object target, object value)
		{
			field.SetValue (target, value);
		}

		public override Type GetLabelType ()
		{
			return field.FieldType;
		}
	}

	public class PropertyDescription : LabelDescription
	{
		public PropertyInfo property;

		public PropertyDescription (Label label, PropertyInfo tproperty)
			: base(label)
		{
			property = tproperty;
		}

		public override object GetValue (object target)
		{
			return property.GetValue (target, null);
		}

		public override void SetValue (object target, object value)
		{
			property.SetValue (target, value, null);
		}

		public override Type GetLabelType ()
		{
			return property.PropertyType;
		}
	}

	static Dictionary<Type, List<LabelDescription> > descriptions = new Dictionary<Type, List<LabelDescription> > ();

	public static List<LabelDescription> GetDescriptions (Type type)
	{
		List<LabelDescription> result = null;
		if (!descriptions.TryGetValue (type, out result)) {
			Type customLabelType = typeof(Label);
			List<LabelDescription> result = new List<LabelDescription> ();
			var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			foreach (var member in type.GetMembers(bindingFlags)) {
				if (member.MemberType == MemberTypes.Property) {
					var property = member as PropertyInfo;
					var customLabel = (Label)Attribute.GetCustomAttribute (property, customLabelType);
					if (customLabel != null) {
						var drawer = PropertyDrawer.GetDrawer (property.PropertyType);
						if (drawer != null) {
							result.Add (new PropertyDescription (customLabel, property));
						} else {
							Debug.LogWarning (type + "." + property.Name + " has Label attribute but there is no drawers for " + property.PropertyType);
						}
					}
				} else if (member.MemberType == MemberTypes.Field) {
					var field = member as FieldInfo;
					var customLabel = (Label)Attribute.GetCustomAttribute (field, customLabelType);
					if (customLabel != null) {
						var drawer = PropertyDrawer.GetDrawer (field.FieldType);
						if (drawer != null) {
							result.Add (new FieldDescription (customLabel, field));
						} else {
							Debug.LogWarning (type + "." + field.Name + " has Label attribute but there is no drawers for " + field.FieldType);
						}
					}
				}
			}
			
			result.TrimExcess();
			descriptions.Add (type, result);
		}

		return result;
	}

	public override bool OnGUI (ref DrawerArgs args)
	{
		var changed = false;
		if (args.value == null) {
			if (args.nullPolicy == NullPolicy.InstantiateNullFields) {
				args.value = Common.CreateInstance (args.type);
				changed = true;

			} else if (args.nullPolicy == NullPolicy.ShowNullFields) {
				GUILayout.BeginHorizontal ();
				GUICommon.FieldLabel (args.name, args.depth);
				GUILayout.Label ("'Null'");
				if (GUILayout.Button ("Create")) {
					args.value = Common.CreateInstance (args.type);
					changed = true;
				}
				GUILayout.EndHorizontal ();
				return changed;

			} else {
				return false;
			}
		}
		
		var descriptions = ObjectDrawer.GetDescriptions (args.type);
		var targs = new PropertyDrawer.DrawerArgs ();
		targs.depth = args.depth + 1;
		targs.nullPolicy = args.nullPolicy;
		
		if (descriptions.Length == 1) {
			var description = descriptions [0];
			var type = description.GetLabelType ();
			var drawer = PropertyDrawer.GetDrawer (type);
			if (drawer != null) {
				targs.Set (args.name, type, description.GetValue (args.value));
				if (drawer.OnGUI (ref targs)) {
					description.SetValue (args.value, targs.value);
					changed = true;
				}
			}
			
		} else {
			if (!string.IsNullOrEmpty (args.name)) {
				GUILayout.BeginHorizontal ();
				GUICommon.FieldLabel (args.name, args.depth);
				GUILayout.EndHorizontal ();
			}
			
			foreach (var description in descriptions) {
				var type = description.GetLabelType ();
				var drawer = PropertyDrawer.GetDrawer (type);
				if (drawer != null) {
					targs.Set (description.Name, type, description.GetValue (args.value));
					if (drawer.OnGUI (ref targs)) {
						description.SetValue (args.value, targs.value);
						changed = true;
					}
				}
			}
		}

		return changed;
	}

	public override bool SupportsType (Type type)
	{
		return true;
	}
}

internal class DynamicEnumDrawer : PropertyDrawer
{
	public override bool OnValue (ref DrawerArgs args)
	{
		var dynamicEnum = args.value as DynamicEnum;
		var changed = dynamicEnum == null;
		if (changed) {
			dynamicEnum = (DynamicEnum)Common.CreateInstance (args.type);
			args.value = dynamicEnum;
		}

		if (GUICommon.ComboField (ref dynamicEnum.value, dynamicEnum.GetNames ())) {
			return true;
		}

		return changed;
	}

	public override bool SupportsType (Type type)
	{
		return typeof(DynamicEnum).IsAssignableFrom (type);
	}
}

[CustomPropertyDrawer(typeof(string))]
internal class StringDrawer : PropertyDrawer
{
	public override bool OnValue (ref DrawerArgs args)
	{
		string svalue = args.value.ToString ();
		bool changed = svalue == null;
		if (changed) {
			svalue = "";
			args.value = svalue;
		}

		if (GUICommon.StringField (ref svalue)) {
			args.value = svalue;
			return true;
		}
		return changed;
	}
}

[CustomPropertyDrawer(typeof(int))]
internal class IntDrawer : PropertyDrawer
{
	public override bool OnGUI (ref DrawerArgs args)
	{
		int ivalue = (int)args.value;

		GUILayout.BeginHorizontal ();
		float delta = GUICommon.NumberLabel (args.name, args.depth);
		var changed = !delta.IsZero ();
		if (changed) {
			ivalue += (int)delta;
		}

		changed |= GUICommon.IntField (ref ivalue);
		GUILayout.EndHorizontal ();

		if (changed) {
			args.value = ivalue;
		}
		return changed;
	}
}

[CustomPropertyDrawer(typeof(float))]
internal class FloatDrawer : PropertyDrawer
{
	public override bool OnGUI (ref DrawerArgs args)
	{
		float fvalue = (float)args.value;

		GUILayout.BeginHorizontal ();
		float delta = GUICommon.NumberLabel (args.name, args.depth);
		bool changed = !delta.IsZero ();
		if (changed) {
			fvalue += delta * 0.1f;
		}
		changed |= GUICommon.FloatField (ref fvalue);
		GUILayout.EndHorizontal ();

		if (changed) {
			args.value = fvalue;
		}
		return changed;
	}
}

[CustomPropertyDrawer(typeof(double))]
internal class DoubleDrawer : PropertyDrawer
{
	public override bool OnGUI (ref DrawerArgs args)
	{
		double dvalue = (double)args.value;
		GUILayout.BeginHorizontal ();
		float delta = GUICommon.NumberLabel (args.name, args.depth);
		bool changed = !delta.IsZero ();
		if (changed) {
			dvalue += delta * 0.1f;
		}
		changed |= GUICommon.DoubleField (ref dvalue);
		GUILayout.EndHorizontal ();

		if (changed) {
			args.value = dvalue;
		}
		return changed;
	}
}

[CustomPropertyDrawer(typeof(bool))]
internal class BoolDrawer : PropertyDrawer
{
	public override bool OnValue (ref DrawerArgs args)
	{
		bool bvalue = (bool)args.value;
		bool tvalue = GUILayout.Toggle (bvalue, "");

		if (bvalue != tvalue) {
			args.value = tvalue;
			return true;
		} else {
			return false;
		}
	}
}

[CustomPropertyDrawer(typeof(Vector3))]
internal class Vector3Drawer : PropertyDrawer
{
	public override bool OnValue (ref DrawerArgs args)
	{
		Vector3 vvalue = (Vector3)args.value;
		if (GUICommon.Vector3Field (ref vvalue)) {
			args.value = vvalue;
			return true;
		} else {
			return false;
		}
	}
}

[CustomPropertyDrawer(typeof(Bounds))]
internal class BoundsDrawer : PropertyDrawer
{
	public override bool OnValue (ref DrawerArgs args)
	{
		Bounds bvalue = (Bounds)args.value;
		if (GUICommon.BoundsField (ref bvalue)) {
			args.value = bvalue;
			return true;
		} else {
			return false;
		}
	}
}