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

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class Label : Attribute
{
	public string title;
	public NullPolicy policy;

	public Label(string atitle = null, NullPolicy apolicy = NullPolicy.InstantiateNullFields)
	{
		title = atitle;
		policy = apolicy;
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class LabelPolicy : Attribute
{
	public bool dontRequireLabel;

	public LabelPolicy(bool adontReqireLabel = true)
	{
		dontRequireLabel = adontReqireLabel;
	}
}

public class ObjectDrawer : PropertyDrawer
{
	public abstract class Description
	{
		protected Label label;

		public Description(Label alabel)
		{
			label = alabel;
		}

		public string Name { get { return label.title; } }
		public NullPolicy Policy { get { return label.policy; } }

		public abstract object GetValue(object target);
		public abstract void SetValue(object target, object value);
		public abstract Type GetLabelType();
	}

	public class FieldDescription : Description
	{
		public FieldInfo field;

		public FieldDescription(Label label, FieldInfo tfield)
			: base(label)
		{
			field = tfield;
			if (label.title == null) {
				label.title = field.Name;
			}
		}

		public override object GetValue(object target)
		{
			return field.GetValue(target);
		}
		public override void SetValue(object target, object value)
		{
			field.SetValue(target, value);
		}
		public override Type GetLabelType()
		{
			return field.FieldType;
		}
	}

	public class PropertyDescription : Description
	{
		public PropertyInfo property;

		public PropertyDescription(Label label, PropertyInfo tproperty)
			: base(label)
		{
			property = tproperty;
			if (label.title == null) {
				label.title = property.Name;
			}
		}

		public override object GetValue(object target)
		{
			return property.GetValue(target, null);
		}
		public override void SetValue(object target, object value)
		{
			property.SetValue(target, value, null);
		}
		public override Type GetLabelType()
		{
			return property.PropertyType;
		}
	}

	static Dictionary<Type, IEnumerable<Description>> descriptions = new Dictionary<Type, IEnumerable<Description>>();

	public static IEnumerable<Description> GetDescriptions(Type type)
	{
		IEnumerable<Description> result = null;
		if (!descriptions.TryGetValue(type, out result)) {
			var policy = (LabelPolicy) Attribute.GetCustomAttribute(type, typeof(LabelPolicy));
			var dontRequireLabel = policy == null ? false : policy.dontRequireLabel;
			List<Description> labels = new List<Description>();
			var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			foreach (var member in type.GetMembers(bindingFlags)) {
				if (member.MemberType == MemberTypes.Property) {
					var property = member as PropertyInfo;
					var label = (Label)Attribute.GetCustomAttribute(property, typeof(Label));
					if (label == null && dontRequireLabel) {
						label = new Label();
					}
					if (label != null) {
						var drawer = PropertyDrawer.GetForTarget(property.PropertyType);
						if (drawer != null) {
							labels.Add(new PropertyDescription(label, property));
						} else {
							Debug.LogWarning(type + "." + property.Name + " has Label attribute but there is no drawers for " + property.PropertyType);
						}
					}

				} else if (member.MemberType == MemberTypes.Field) {
					var field = member as FieldInfo;
					var label = (Label)Attribute.GetCustomAttribute(field, typeof(Label));
					if (label == null && dontRequireLabel) {
						label = new Label();
					}
					if (label != null) {
						var drawer = PropertyDrawer.GetForTarget(field.FieldType);
						if (drawer != null) {
							labels.Add(new FieldDescription(label, field));
						} else {
							Debug.LogWarning(type + "." + field.Name + " has Label attribute but there is no drawers for " + field.FieldType);
						}
					}
				}
			}

			result = labels.ToArray();
			descriptions.Add(type, result);
		}

		return result;
	}

	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		var changed = false;
		if (args.value == null) {
			if (args.policy == NullPolicy.InstantiateNullFields) {
				args.value = Common.CreateInstance(args.type);
				changed = true;

			} else if (args.policy == NullPolicy.ShowNullFields) {
				GUILayout.BeginHorizontal();
				GUICommon.FieldLabel(args.name, args.depth);
				GUILayout.Label(translationNull);
				if (GUILayout.Button(translationInstantiate)) {
					args.value = Common.CreateInstance(args.type);
					changed = true;
				}
				GUILayout.EndHorizontal();
				return changed;

			} else {
				return false;
			}
		}

		if (!string.IsNullOrEmpty(args.name)) {
			GUILayout.BeginHorizontal();
			GUICommon.FieldLabel(args.name, args.depth);
			GUILayout.EndHorizontal();
		}

		var descriptions = ObjectDrawer.GetDescriptions(args.type);
		var targs = new PropertyDrawer.DrawerArgs();
		targs.depth = args.depth + 1;
		targs.policy = args.policy;

		foreach (var description in descriptions) {
			var type = description.GetLabelType();
			var drawer = PropertyDrawer.GetForTarget(type);
			if (drawer != null) {
				targs.Set(description.Name, type, description.GetValue(args.value), description.Policy);
				if (drawer.OnTitleAndValue(ref targs)) {
					description.SetValue(args.value, targs.value);
					changed = true;
				}
			}
		}

		return changed;
	}
	public override bool Supports(Type type)
	{
		return true;
	}
}
