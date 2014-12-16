using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Extensions;
using System.Runtime.Serialization;

public class PropertyDrawer
{
	public struct DrawerArgs
	{
		public string name;
		public Type type;
		public object value;
		public int depth;
		public NullPolicy policy;

		public void Set(string tname, Type ttype, object tvalue, int tdepth, NullPolicy anullPolicy = NullPolicy.InstantiateNullFields)
		{
			name = tname;
			type = ttype;
			value = tvalue;
			depth = tdepth;
			policy = anullPolicy;
		}
		public void Set(string tname, Type ttype, object tvalue, NullPolicy anullPolicy = NullPolicy.InstantiateNullFields)
		{
			name = tname;
			type = ttype;
			value = tvalue;
			policy = anullPolicy;
		}
		public void Set(string tname, object tvalue, NullPolicy anullPolicy = NullPolicy.InstantiateNullFields)
		{
			name = tname;
			value = tvalue;
			policy = anullPolicy;
		}
	}

	public static string translationNull = "'Null'";
	public static string translationInstantiate = "Create";
	public static string translationSize = "Size";
	public static string translationEdit = "Edit";
	public static string translationOK = "OK";
	public static string translationUp = "▲";
	public static string translationDown = "▼";
	public static string translationInsert = "+";
	public static string translationRemove = "-";

	private static Dictionary<Type, PropertyDrawer> knownTargets = new Dictionary<Type, PropertyDrawer>();
	private static Dictionary<Type, PropertyDrawer> drawers = new Dictionary<Type, PropertyDrawer>();
	private static PropertyDrawer defaultDrawer = (PropertyDrawer)Common.CreateInstance(typeof(ObjectDrawer));

	static void Reload()
	{
		var types = Common.GetExtendedTypes(typeof(PropertyDrawer));
		foreach (var type in types) {
			if (!drawers.ContainsKey(type) && !type.IsAbstract && !type.IsGenericTypeDefinition && type != typeof(ObjectDrawer)) {
				drawers.Add(type, (PropertyDrawer)Common.CreateInstance(type));
			}
		}
	}
	public static PropertyDrawer GetForTarget(Type target)
	{
		PropertyDrawer result = null;
		if (!knownTargets.TryGetValue(target, out result)) {
			result = null;
			bool retry = false;
			while (true) {
				foreach (var keyval in drawers) {
					if (keyval.Value.Supports(target)) {
						result = keyval.Value;
						break;
					}
				}
				if (result == null && retry == false) {
					Reload();
					retry = true;
				} else {
					break;
				}
			}
			if (result == null) {
				result = defaultDrawer;
			}
			knownTargets.Add(target, result);
		}
		return result;
	}
	public static PropertyDrawer GetDrawer(Type type)
	{
		PropertyDrawer result = null;
		if (!drawers.TryGetValue(type, out result)) {
			Reload();
			if (!drawers.TryGetValue(type, out result)) {
				Debug.LogError("No drawers of type " + type.Name);
			}
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
			args.Set(name, value.GetType(), value, string.IsNullOrEmpty(name) ? -1 : 0, nullPolicy);
			var result = GetForTarget(args.type).OnTitleAndValue(ref args);
			return result;
		} else {
			return false;
		}
	}
	public static bool Draw<T>(ref T value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		return Draw(null, ref value, nullPolicy);
	}
	public static bool Draw<T>(string name, ref T value, NullPolicy nullPolicy = NullPolicy.InstantiateNullFields)
	{
		var args = new DrawerArgs();
		args.Set(name, typeof(T), value, string.IsNullOrEmpty(name) ? -1 : 0, nullPolicy);
		var result = GetForTarget(args.type).OnTitleAndValue(ref args);
		if (result) {
			value = (T)args.value;
		}
		return result;
	}

	protected PropertyDrawer()
	{
	}

	public virtual bool Supports(Type type)
	{
		return false;
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
}

public enum NullPolicy
{
	HideNullFields,
	ShowNullFields,
	InstantiateNullFields
}

public class IntRange : Attribute
{
	public int min;
	public int max;

	public IntRange(int tmin, int tmax)
	{
		min = tmin;
		max = tmax;
	}
}

public class FloatRange : Attribute
{
	public float min;
	public float max;

	public FloatRange(float tmin, float tmax)
	{
		min = tmin;
		max = tmax;
	}
}

[System.Serializable]
public abstract class DynamicEnum
{
	private static Dictionary<Type, string[]> cachedNames = new Dictionary<Type, string[]>();

	public int value = 0;
	public virtual string Name { get { return GetNames()[value]; } }
	public static implicit operator int(DynamicEnum d) { return d.value; }

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

public class DynamicEnumProvider
{
	public virtual string[] GetNames()
	{
		return new string[0];
	}
}