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
	private static Dictionary<Type, string[]> cachedNames = new Dictionary<Type, string[]>();
	public int value = 0;

	public virtual string Name { get { return GetNames()[value]; } }

	public static implicit operator int(DynamicEnum d)
	{
		return d.value;
	}


	public string[] GetNames()
	{
		string[] result;
		var type = GetType();
		if (!cachedNames.TryGetValue(type, out result)) {
			result = InitNames();
			cachedNames.Add(type, result);
		}
		return result;
	}

	public abstract string[] InitNames();
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
		public Description description;
		public object value;
		public int depth;
		// TODO nullpolicy will be fed from description & drawer
		public NullPolicy nullPolicy;

		private Type _type;
		public Type type
		{
			get { return _type; }
			set { _type = value; _drawer = PropertyDrawer.GetDrawer(_type); }
		}

		private PropertyDrawer _drawer;
		public PropertyDrawer drawer { get { return _drawer; } }
	}

	public class Description : Attribute
	{
		public string title;
		public NullPolicy nullPolicy;

		public Description(string atitle = "", NullPolicy anullPolicy = NullPolicy.InstantiateNullFields)
		{
			title = atitle;
			nullPolicy = anullPolicy;
		}
	}

	public class IntRange : Description
	{
		public int min;
		public int max;

		public IntRange(int amin, int amax)
			: base()
		{
			min = amin;
			max = amax;
		}
		public IntRange(string atitle, int amin, int amax)
			: base(atitle)
		{
			min = amin;
			max = amax;
		}
	}

	public class FloatRange : Description
	{
		public float min;
		public float max;

		public FloatRange(float amin, float amax)
			: base()
		{
			min = amin;
			max = amax;
		}
		public FloatRange(string atitle, float amin, float amax)
			: base(atitle)
		{
			min = amin;
			max = amax;
		}
	}

	public class CustomPropertyDrawer : Attribute
	{
		public Type type;

		public CustomPropertyDrawer(Type ttype)
		{
			type = ttype;
		}
	}

	public static string translationSize = "Size";
	public static string translationNull = "'Null'";
	public static string translationInstantiate = "Create";

	//	private static UnityObjectDrawer unityObjectDrawer;
	private static EnumDrawer enumDrawer;
	private static ArrayDrawer arrayDrawer;
	private static ListDrawer listDrawer;
	private static ObjectDrawer objectDrawer;
	private static DynamicEnumDrawer dynamicEnumDrawer;
	private static Dictionary<Type, PropertyDrawer> drawers;

	static PropertyDrawer()
	{
		drawers = new Dictionary<Type, PropertyDrawer>();
		enumDrawer = new EnumDrawer();
		//		unityObjectDrawer = new UnityObjectDrawer();
		listDrawer = new ListDrawer();
		arrayDrawer = new ArrayDrawer();
		objectDrawer = new ObjectDrawer();
		dynamicEnumDrawer = new DynamicEnumDrawer();

		var types = Common.GetExtendedTypes(typeof(PropertyDrawer));
		var signature = new Type[0];
		var parameters = new object[0];

		foreach (var type in types) {
			var attr = (CustomPropertyDrawer)Attribute.GetCustomAttribute(type, typeof(CustomPropertyDrawer));
			if (attr != null) {
				var constructorInfo = type.GetConstructor(signature);
				if (constructorInfo != null) {
					var instance = (PropertyDrawer)constructorInfo.Invoke(parameters);
					drawers.Add(attr.type, instance);
				}
			}
		}
	}

	public static PropertyDrawer GetDrawer(Type type)
	{
		if (type == null) {
			return null;
		}

		PropertyDrawer result = null;
		if (!drawers.TryGetValue(type, out result)) {
			if (enumDrawer.SupportsType(type)) {
				result = enumDrawer;
			} else if (dynamicEnumDrawer.SupportsType(type)) {
				result = dynamicEnumDrawer;
				//			} else if (unityObjectDrawer.SupportsType(type)) {
				//				return unityObjectDrawer;
			} else if (arrayDrawer.SupportsType(type)) {
				result = arrayDrawer;
			} else if (listDrawer.SupportsType(type)) {
				result = listDrawer;
			} else {
				result = objectDrawer;
			}
			drawers.Add(type, result);
		}
		return result;
	}
	public static bool Draw(object value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		return Draw(null, value, nullPolicy);
	}
	public static bool Draw(string name, object value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		if (value != null) {
			var args = new DrawerArgs();
			args.name = name;
			args.type = value.GetType();
			args.value = value;
			args.depth = string.IsNullOrEmpty(name) ? -1 : 0;
			args.nullPolicy = nullPolicy;
			return args.drawer.OnTitleAndValue(ref args);
		} else {
			return false;
		}
	}
	public static bool Draw<T>(ref T value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		return Draw("", ref value, nullPolicy);
	}
	public static bool Draw<T>(string name, ref T value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		var args = new DrawerArgs();
		args.name = name;
		args.type = value.GetType();
		args.value = value;
		args.depth = string.IsNullOrEmpty(name) ? -1 : 0;
		// TODO
		args.nullPolicy = nullPolicy;
		var result = args.drawer.OnTitleAndValue(ref args);
		if (result) {
			value = (T)args.value;
		}
		return result;
	}

	public virtual bool OnTitleAndValue(ref DrawerArgs args)
	{
		GUILayout.BeginHorizontal();
		GUICommon.FieldLabel(args.name, args.depth);
		bool result = OnValue(ref args);
		GUILayout.EndHorizontal();

		return result;
	}
	public virtual bool OnValue(ref DrawerArgs args)
	{
		GUILayout.FlexibleSpace();
		return false;
	}
	public virtual NullPolicy GetNullPolicy(ref DrawerArgs args)
	{
		return NullPolicy.InstantiateNullFields;
	}
}

// ==================
// Drawers
// ==================

internal class EnumDrawer : PropertyDrawer
{
	static Dictionary<Type, string[]> cachedNames = new Dictionary<Type, string[]>();

	public static string[] GetNames(Type type)
	{
		string[] result;
		if (!cachedNames.TryGetValue(type, out result)) {
			List<string> names = new List<string>();
			var members = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
			foreach (var member in members) {
				var description = (Description)Attribute.GetCustomAttribute(member, typeof(Description));
				names.Add(description == null ? member.Name : description.title);
			}
			result = names.ToArray();
			cachedNames.Add(type, result);
		}

		return result;
	}
	public override bool OnValue(ref DrawerArgs args)
	{
		var values = Enum.GetValues(args.type);
		int selection = (int)Array.IndexOf<int>(values as int[], (int)args.value);
		var names = GetNames(args.type);
		if (GUICommon.ComboField(ref selection, names)) {
			args.value = values.GetValue(selection);
			return true;
		}
		return false;
	}
	public bool SupportsType(Type type)
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
	private static IList listToBeMarkedToDelete = null;
	public const float smallButtonWidth = 30f;

	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		var list = args.value as IList;

		var targs = new DrawerArgs();
		targs.type = args.type.GetGenericArguments()[0];
		targs.depth = args.depth + 1;
		// TODO
		targs.nullPolicy = args.nullPolicy;

		var drawer = targs.drawer;
		if (drawer == null)
			return false;

		var changed = false;
		if (list == null) {
			if (args.nullPolicy == NullPolicy.InstantiateNullFields) {
				list = (IList)Common.CreateInstance(args.type);
				args.value = list;
				changed = true;

			} else if (args.nullPolicy == NullPolicy.ShowNullFields) {
				GUILayout.BeginHorizontal();
				GUICommon.FieldLabel(args.name, args.depth);
				GUILayout.Label(PropertyDrawer.translationNull);
				if (GUILayout.Button(PropertyDrawer.translationInstantiate)) {
					list = (IList)Common.CreateInstance(args.type);
					args.value = list;
					changed = true;
				}
				GUILayout.EndHorizontal();
				return changed;

			} else {
				return false;
			}
		}

		GUILayout.BeginHorizontal();
		GUICommon.FieldLabel(args.name, args.depth);
		GUILayout.Label(PropertyDrawer.translationSize + ": ");

		int count = list.Count;
		if (GUICommon.IntField(ref count)) {
			changed = true;
			if (count < list.Count) {
				args.type.GetMethod("RemoveRange").Invoke(list, new object[] { count, list.Count - count });
			} else if (count > list.Count) {
				args.type.GetMethod("AddRange").Invoke(list, new object[] { Array.CreateInstance(targs.type, count - list.Count) });
			}
		}

		if (args.nullPolicy != NullPolicy.HideNullFields) {
			if (GUILayout.Button("+", GUILayout.Width(smallButtonWidth))) {
				if (args.nullPolicy == NullPolicy.ShowNullFields) {
					list.Add(null);
				} else {
					list.Add(Common.CreateInstance(targs.type));
				}
			}
		}

		var deleteEnabled = listToBeMarkedToDelete == list;
		if (deleteEnabled) {
			if (GUILayout.Button("O", GUILayout.Width(smallButtonWidth))) {
				listToBeMarkedToDelete = null;
			}
		} else if (list.Count > 0) {
			if (GUILayout.Button("-", GUILayout.Width(smallButtonWidth))) {
				listToBeMarkedToDelete = list;
			}
		}

		GUILayout.EndHorizontal();

		int deleteIndex = -1;
		GUILayout.BeginVertical();
		for (int index = 0; index < count; index++) {
			if (deleteEnabled) {
				GUILayout.BeginHorizontal();
			}
			targs.name = "#" + index;
			targs.value = list[index];
			if (drawer.OnTitleAndValue(ref targs)) {
				list[index] = targs.value;
				changed = true;
			}
			if (deleteEnabled) {
				if (GUILayout.Button("-", GUILayout.Width(smallButtonWidth))) {
					deleteIndex = index;
				}
				GUILayout.EndHorizontal();
			}
		}
		if (deleteEnabled && deleteIndex != -1) {
			list.RemoveAt(deleteIndex);
			changed = true;
		}

		GUILayout.EndVertical();

		return changed;
	}
	public bool SupportsType(Type type)
	{
		return type.IsGenericType &&
			!type.IsGenericTypeDefinition &&
			type.GetGenericTypeDefinition() == typeof(List<>);
	}
}

internal class ArrayDrawer : PropertyDrawer
{
	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		var array = args.value as Array;

		var targs = new DrawerArgs();
		targs.type = args.type.GetElementType();
		targs.depth = args.depth + 1;

		var drawer = targs.drawer;
		if (drawer == null)
			return false;

		var changed = false;
		if (array == null) {
			if (args.nullPolicy == NullPolicy.InstantiateNullFields) {
				array = Common.CreateInstanceArray(targs.type, 0);
				args.value = array;
				changed = true;

			} else if (args.nullPolicy == NullPolicy.ShowNullFields) {
				GUILayout.BeginHorizontal();
				GUICommon.FieldLabel(args.name, args.depth);
				GUILayout.Label(PropertyDrawer.translationNull);
				if (GUILayout.Button(PropertyDrawer.translationInstantiate)) {
					array = Common.CreateInstanceArray(targs.type, 0);
					args.value = array;
					changed = true;
				}
				GUILayout.EndHorizontal();
				return changed;

			} else {
				return false;
			}
		}

		GUILayout.BeginHorizontal();
		GUICommon.FieldLabel(args.name, args.depth);
		GUILayout.Label(PropertyDrawer.translationSize + ": ");

		int count = array.Length;
		if (GUICommon.IntField(ref count)) {
			changed = true;
			var parameters = new object[] { array, count };
			typeof(Array).GetMethod("Resize").MakeGenericMethod(targs.type).Invoke(null, parameters);
			array = parameters[0] as Array;
			args.value = array;
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginVertical();
		for (int index = 0; index < count; index++) {
			targs.name = "#" + index;
			targs.value = array.GetValue(index);
			if (drawer.OnTitleAndValue(ref targs)) {
				array.SetValue(targs.value, index);
				changed = true;
			}
		}
		GUILayout.EndVertical();

		return changed;
	}
	public bool SupportsType(Type type)
	{
		return type.IsArray;
	}
}

internal class ObjectDrawer : PropertyDrawer
{
	public abstract class AccessorInfo
	{
		public Description description;

		public AccessorInfo(Description adescription)
		{
			description = adescription;
		}

		public string Name { get { return description.title; } }
		public abstract object GetValue(object target);
		public abstract void SetValue(object target, object value);
		public abstract Type GetAccessorType();
		public virtual bool IsReadOnly() { return false; }
	}

	public class FieldAccessorInfo : AccessorInfo
	{
		public FieldInfo field;

		public FieldAccessorInfo(Description label, FieldInfo tfield)
			: base(label)
		{
			field = tfield;
		}

		public override object GetValue(object target)
		{
			return field.GetValue(target);
		}
		public override void SetValue(object target, object value)
		{
			field.SetValue(target, value);
		}
		public override Type GetAccessorType()
		{
			return field.FieldType;
		}
	}

	public class PropertyAccessorInfo : AccessorInfo
	{
		public PropertyInfo property;

		public PropertyAccessorInfo(Description label, PropertyInfo tproperty)
			: base(label)
		{
			property = tproperty;
		}

		public override object GetValue(object target)
		{
			return property.GetValue(target, null);
		}
		public override void SetValue(object target, object value)
		{
			property.SetValue(target, value, null);
		}
		public override Type GetAccessorType()
		{
			return property.PropertyType;
		}
		public override bool IsReadOnly()
		{
			return property.CanWrite;
		}
	}

	static Dictionary<Type, List<AccessorInfo>> descriptions = new Dictionary<Type, List<AccessorInfo>>();

	public static List<AccessorInfo> GetDescriptions(Type type)
	{
		List<AccessorInfo> result = null;
		if (!descriptions.TryGetValue(type, out result)) {
			Type descriptionType = typeof(Description);
			result = new List<AccessorInfo>();
			var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			foreach (var member in type.GetMembers(bindingFlags)) {
				if (member.MemberType == MemberTypes.Property) {
					var property = member as PropertyInfo;
					var customLabel = (Description)Attribute.GetCustomAttribute(property, descriptionType);
					if (customLabel != null) {
						var drawer = PropertyDrawer.GetDrawer(property.PropertyType);
						if (drawer != null) {
							result.Add(new PropertyAccessorInfo(customLabel, property));
						} else {
							Common.LogWarning(type + "." + property.Name + " has Description attribute but there is no drawers for " + property.PropertyType);
						}
					}
				} else if (member.MemberType == MemberTypes.Field) {
					var field = member as FieldInfo;
					var description = (Description)Attribute.GetCustomAttribute(field, descriptionType);
					if (description != null) {
						var drawer = PropertyDrawer.GetDrawer(field.FieldType);
						if (drawer != null) {
							result.Add(new FieldAccessorInfo(description, field));
						} else {
							Common.LogWarning(type + "." + field.Name + " has Description attribute but there is no drawers for " + field.FieldType);
						}
					}
				}
			}

			result.TrimExcess();
			descriptions.Add(type, result);
		}

		return result;
	}

	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		var changed = false;
		if (args.value == null) {
			if (args.nullPolicy == NullPolicy.InstantiateNullFields) {
				args.value = Common.CreateInstance(args.type);
				changed = true;

			} else if (args.nullPolicy == NullPolicy.ShowNullFields) {
				GUILayout.BeginHorizontal();
				GUICommon.FieldLabel(args.name, args.depth);
				GUILayout.Label("'Null'");
				if (GUILayout.Button("Create")) {
					args.value = Common.CreateInstance(args.type);
					changed = true;
				}
				GUILayout.EndHorizontal();
				return changed;

			} else {
				return false;
			}
		}

		var accessors = ObjectDrawer.GetDescriptions(args.type);
		var targs = new PropertyDrawer.DrawerArgs();
		targs.depth = args.depth + 1;
		targs.nullPolicy = args.nullPolicy;

		if (accessors.Count == 1) {
			var accessor = accessors[0];
			targs.type = accessor.GetAccessorType();
			var drawer = targs.drawer;
			if (drawer != null) {
				targs.name = args.name;
				targs.value = accessor.GetValue(args.value);
				targs.description = accessor.description;
				if (drawer.OnTitleAndValue(ref targs)) {
					accessor.SetValue(args.value, targs.value);
					changed = true;
				}
			}

		} else {
			if (!string.IsNullOrEmpty(args.name)) {
				GUILayout.BeginHorizontal();
				GUICommon.FieldLabel(args.name, args.depth);
				GUILayout.EndHorizontal();
			}

			foreach (var accessor in accessors) {
				targs.type = accessor.GetAccessorType();
				var drawer = targs.drawer;
				if (drawer != null) {
					targs.name = args.name;
					targs.value = accessor.GetValue(args.value);
					targs.description = accessor.description;
					if (drawer.OnTitleAndValue(ref targs)) {
						accessor.SetValue(args.value, targs.value);
						changed = true;
					}
				}
			}
		}

		return changed;
	}
}

internal class DynamicEnumDrawer : PropertyDrawer
{
	public override bool OnValue(ref DrawerArgs args)
	{
		var dynamicEnum = args.value as DynamicEnum;
		var changed = dynamicEnum == null;
		if (changed) {
			dynamicEnum = (DynamicEnum)Common.CreateInstance(args.type);
			args.value = dynamicEnum;
		}

		if (GUICommon.ComboField(ref dynamicEnum.value, dynamicEnum.GetNames())) {
			return true;
		}

		return changed;
	}
	public bool SupportsType(Type type)
	{
		return typeof(DynamicEnum).IsAssignableFrom(type);
	}
}

[CustomPropertyDrawer(typeof(string))]
internal class StringDrawer : PropertyDrawer
{
	public override bool OnTitleAndValue(ref PropertyDrawer.DrawerArgs args)
	{
		if (args.value == null) {
			switch (args.nullPolicy) {
				case NullPolicy.HideNullFields:
					return false;
				case NullPolicy.InstantiateNullFields:
					args.value = "";
					return true;
				case NullPolicy.ShowNullFields:
					var changed = false;
					GUILayout.BeginHorizontal();
					GUICommon.FieldLabel(args.name, args.depth);
					if (GUILayout.Button(PropertyDrawer.translationInstantiate)) {
						args.value = "";
						changed = true;
					}
					GUILayout.EndHorizontal();
					return changed;
				default:
					return false;
			}
		} else {
			var changed = false;
			GUILayout.BeginHorizontal();
			GUICommon.FieldLabel(args.name, args.depth);
			string svalue = args.value.ToString();
			if (GUICommon.StringField(ref svalue)) {
				args.value = svalue;
				changed = true;
			}
			GUILayout.EndHorizontal();
			return changed;
		}
	}
}

[CustomPropertyDrawer(typeof(int))]
internal class IntDrawer : PropertyDrawer
{
	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		int ivalue = (int)args.value;
		var intRange = args.description as IntRange;

		if (intRange != null) {
			if (GUICommon.IntSliderWithLabel(args.name, ref ivalue, intRange.min, intRange.max)) {
				args.value = ivalue;
				return true;
			}
			return false;

		} else {
			GUILayout.BeginHorizontal();
			float delta = GUICommon.NumberLabel(args.name, args.depth);
			var changed = !delta.IsZero();
			if (changed) {
				ivalue += (int)delta;
			}

			changed |= GUICommon.IntField(ref ivalue);
			GUILayout.EndHorizontal();

			if (changed) {
				args.value = ivalue;
			}
			return changed;
		}
	}
}

[CustomPropertyDrawer(typeof(float))]
internal class FloatDrawer : PropertyDrawer
{
	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		float fvalue = (float)args.value;
		var floatRange = args.description as FloatRange;

		if (floatRange != null) {
			if (GUICommon.FloatSliderWithLabel(args.name, ref fvalue, floatRange.min, floatRange.max)) {
				args.value = fvalue;
				return true;
			}
			return false;

		} else {
			GUILayout.BeginHorizontal();
			float delta = GUICommon.NumberLabel(args.name, args.depth);
			bool changed = !delta.IsZero();
			if (changed) {
				fvalue += delta * 0.1f;
			}
			changed |= GUICommon.FloatField(ref fvalue);
			GUILayout.EndHorizontal();

			if (changed) {
				args.value = fvalue;
			}
			return changed;
		}
	}
}

[CustomPropertyDrawer(typeof(double))]
internal class DoubleDrawer : PropertyDrawer
{
	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		double dvalue = (double)args.value;
		GUILayout.BeginHorizontal();
		float delta = GUICommon.NumberLabel(args.name, args.depth);
		bool changed = !delta.IsZero();
		if (changed) {
			dvalue += delta * 0.1f;
		}
		changed |= GUICommon.DoubleField(ref dvalue);
		GUILayout.EndHorizontal();

		if (changed) {
			args.value = dvalue;
		}
		return changed;
	}
}

[CustomPropertyDrawer(typeof(bool))]
internal class BoolDrawer : PropertyDrawer
{
	public override bool OnValue(ref DrawerArgs args)
	{
		bool bvalue = (bool)args.value;
		bool tvalue = GUILayout.Toggle(bvalue, "");

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
	public override bool OnValue(ref DrawerArgs args)
	{
		Vector3 vvalue = (Vector3)args.value;
		if (GUICommon.Vector3Field(ref vvalue)) {
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
	public override bool OnValue(ref DrawerArgs args)
	{
		Bounds bvalue = (Bounds)args.value;
		if (GUICommon.BoundsField(ref bvalue)) {
			args.value = bvalue;
			return true;
		} else {
			return false;
		}
	}
}
