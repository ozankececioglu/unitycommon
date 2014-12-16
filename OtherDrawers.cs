using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Extensions;
using System.Runtime.Serialization;

public class EnumDrawer : PropertyDrawer
{
	static Dictionary<Type, EnumInfo> cache = new Dictionary<Type, EnumInfo>();

	class EnumInfo
	{
		public string[] labels;
		public object[] values;
	}

	static EnumInfo GetType(Type type)
	{
		EnumInfo result;
		if (!cache.TryGetValue(type, out result)) {
			var evalues = Enum.GetValues(type);
			var enames = Enum.GetNames(type);

			List<string> names = new List<string>();
			List<object> values = new List<object>();
			var members = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
			foreach (var member in members) {
				var label = (Label)Attribute.GetCustomAttribute(member, typeof(Label));
				names.Add(label == null || label.title == null ? member.Name : label.title);
				values.Add(evalues.GetValue(Array.IndexOf(enames, member.Name)));
			}
			result = new EnumInfo { labels = names.ToArray(), values = values.ToArray() };
			cache.Add(type, result);
		}

		return result;
	}
	public static string[] GetNames(Type type)
	{
		return GetType(type).labels;
	}
	public static Array GetValues(Type type)
	{
		return GetType(type).values;
	}

	public override bool OnValue(ref DrawerArgs args)
	{
		var values = GetValues(args.type);
		int index = Array.IndexOf(values, args.value);
		var names = GetNames(args.type);
		if (GUICommon.ComboField(ref index, names)) {
			args.value = values.GetValue(index);
			return true;
		}
		return false;
	}
	public override bool Supports(Type type)
	{
		return type.IsEnum;
	}
}

public class DynamicEnumDrawer : PropertyDrawer
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
	public override bool Supports(Type type)
	{
		return typeof(DynamicEnum).IsAssignableFrom(type);
	}
}

public class StringDrawer : PropertyDrawer
{
	public override bool OnValue(ref DrawerArgs args)
	{
		string svalue = args.value as string;
		bool changed = svalue == null;
		if (changed) {
			svalue = "";
			args.value = svalue;
		}

		if (GUICommon.StringField(ref svalue)) {
			args.value = svalue;
			return true;
		}
		return changed;
	}
	public override bool Supports(Type type)
	{
		return type == typeof(string);
	}
}

public class IntDrawer : PropertyDrawer
{
	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		int ivalue = (int)args.value;

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
	public override bool Supports(Type type)
	{
		return type == typeof(int);
	}
}

public class FloatDrawer : PropertyDrawer
{
	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		float fvalue = (float)args.value;

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
	public override bool Supports(Type type)
	{
		return type == typeof(float);
	}
}

public class DoubleDrawer : PropertyDrawer
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
	public override bool Supports(Type type)
	{
		return type == typeof(double);
	}
}

public class BoolDrawer : PropertyDrawer
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
	public override bool Supports(Type type)
	{
		return type == typeof(bool);
	}
}

public class Vector3Drawer : PropertyDrawer
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
	public override bool Supports(Type type)
	{
		return type == typeof(Vector3);
	}
}

public class BoundsDrawer : PropertyDrawer
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
	public override bool Supports(Type type)
	{
		return type == typeof(Bounds);
	}
}

