using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Extensions;
using System.Runtime.Serialization;

public class ListDrawer : PropertyDrawer
{
	private static IList editModeList = null;
	private static List<bool> editModeSelection = new List<bool>();
	private static int editModeCountField = 0;
	public const float smallButtonWidth = 30f;

	private enum EditOp
	{
		None,
		OK,
		Up,
		Down,
		Remove,
		Resize,
		Insert
	}

	struct ListOp
	{
		public IList list;
		public Type listType;
		public bool isArray;
		public Type elementType;

		public ListOp(IList alist)
		{
			list = alist;
			listType = list.GetType();
			isArray = listType.IsArray;
			elementType = isArray ? listType.GetElementType() : listType.GetGenericArguments()[0];
		}

		public int Count { get { return list.Count; } }
		public void Resize(int count)
		{
			if (isArray) {
				var array = list as Array;
				var parameters = new object[] { array, count };
				typeof(Array).GetMethod("Resize").MakeGenericMethod(elementType).Invoke(null, parameters);
				array = parameters[0] as Array;
				list = array;

			} else if (count < list.Count) {
				listType.GetMethod("RemoveRange").Invoke(list, new object[] { count, list.Count - count });
			} else if (count > list.Count) {
				listType.GetMethod("AddRange").Invoke(list, new object[] { Array.CreateInstance(elementType, count - list.Count) });
			}
		}
		public void Insert(int index)
		{
			if (isArray) {
				Resize(list.Count + 1);
				for (int i = list.Count - 1; i > index; i--) {
					list[i] = list[i - 1];
				}
				list[index] = Common.CreateInstance(elementType);
			} else {
				list.Insert(index, Common.CreateInstance(elementType));
			}
		}
		public void RemoveAt(int index)
		{
			if (isArray) {
				for (; index < list.Count - 1; index++) {
					list[index] = list[index + 1];
				}
				Resize(list.Count - 1);
			} else {
				list.RemoveAt(index);
			}
		}
	}

	public override bool OnTitleAndValue(ref DrawerArgs args)
	{
		var list = args.value as IList;

		Type elementType = args.type.IsArray ? args.type.GetElementType() : args.type.GetGenericArguments()[0];
		var drawer = PropertyDrawer.GetForTarget(elementType);
		if (drawer == null)
			return false;

		var targs = new DrawerArgs();
		targs.type = elementType;
		targs.depth = args.depth + 1;
		targs.policy = args.policy;

		var changed = false;
		if (list == null) {
			if (args.policy == NullPolicy.InstantiateNullFields) {
				list = (IList)Common.CreateInstance(args.type);
				args.value = list;
				changed = true;

			} else if (args.policy == NullPolicy.ShowNullFields) {
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

		var editEnabled = editModeList == list;
		if (editEnabled && list.Count != editModeSelection.Count) {
			Debug.LogError("noli");
		}
		var editOp = EditOp.None;
		GUILayout.BeginHorizontal();
		GUICommon.FieldLabel(args.name, args.depth);
		if (editEnabled) {
			if (GUILayout.Button(translationSize, GUILayout.ExpandWidth(false))) {
				editOp = EditOp.Resize;
			}
			GUICommon.IntField(ref editModeCountField, GUILayout.MinWidth(30f));
			if (GUILayout.Button(translationInsert, GUILayout.ExpandWidth(false))) {
				editOp = EditOp.Insert;
			}
			bool hasSelection = editModeSelection.Any(selection => selection);
			if (hasSelection) {
				GUILayout.Label(" |  ");
				if (GUILayout.Button(translationUp, GUILayout.ExpandWidth(false))) {
					editOp = EditOp.Up;
				}
				if (GUILayout.Button(translationDown, GUILayout.ExpandWidth(false))) {
					editOp = EditOp.Down;
				}
				if (GUILayout.Button(translationRemove, GUILayout.ExpandWidth(false))) {
					editOp = EditOp.Remove;
				}
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(translationOK, GUILayout.ExpandWidth(false))) {
				editOp = EditOp.OK;
			}
		} else {
			GUILayout.Label(translationSize + ": " + list.Count.ToString());
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(translationEdit, GUILayout.ExpandWidth(false))) {
				editModeList = list;
				editModeSelection.SetCount(list.Count, false);
				editModeCountField = list.Count;
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginVertical();
		for (int index = 0; index < list.Count; index++) {
			if (editEnabled) {
				GUILayout.BeginHorizontal();
				editModeSelection[index] = GUILayout.Toggle(editModeSelection[index], "", GUILayout.ExpandWidth(false));
			}
			targs.Set("#" + index, list[index]);
			if (drawer.OnTitleAndValue(ref targs)) {
				list[index] = targs.value;
				changed = true;
			}
			if (editEnabled) {
				GUILayout.EndHorizontal();
			}
		}
		GUILayout.EndVertical();
		
		if (editOp == EditOp.Remove) {
			editOp = EditOp.Remove;
		}
		ListOp listOp = new ListOp(list);
		switch (editOp) {
			case EditOp.None:
				break;
			case EditOp.OK:
				editModeList = null;
				editModeSelection.Clear();
				break;
			case EditOp.Resize:
				listOp.Resize(editModeCountField);
				editModeSelection.SetCount(editModeCountField);
				break;
			case EditOp.Insert:
				listOp.Insert(editModeCountField);
				editModeSelection.Insert(editModeCountField, false);
				break;
			default: {
					for (int index = 0; index < listOp.Count; index++) {
						switch (editOp) {
							case EditOp.Up:
								if (editModeSelection[index] && index > 0) {
									var temp = list[index - 1];
									list[index - 1] = list[index];
									list[index] = temp;
									editModeSelection[index] = editModeSelection[index - 1];
									editModeSelection[index - 1] = true;
								}
								break;
							case EditOp.Down:
								// Reverse order since op swaps down
								var tindex = (list.Count - 1) - index;
								if (editModeSelection[tindex] && tindex < list.Count - 1) {
									var temp = list[tindex + 1];
									list[tindex + 1] = list[tindex];
									list[tindex] = temp;
									editModeSelection[tindex] = editModeSelection[tindex + 1];
									editModeSelection[tindex + 1] = true;
								}
								break;
							case EditOp.Remove:
								if (editModeSelection[index]) {
									listOp.RemoveAt(index);
									editModeSelection.RemoveAt(index);
									index--;
								}
								break;
							default:
								break;
						}
					}
				}
				break;
		}

		args.value = listOp.list;
		if (editEnabled && editOp != EditOp.OK) {
			editModeList = listOp.list;
		}

		return changed || editOp != EditOp.None && editOp != EditOp.OK;
	}
	public override bool Supports(Type type)
	{
		return type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
	}
}
