using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public interface IGUIListMaster
{
	string GetContentAt (GUIList view, int index);
	int GetRowCount (GUIList view);
	void SelectionChanged (GUIList view, int prev, int cur);
}

public class GUIList
{
	public GUILayoutOption[] options;
	public Color normalColor = Color.white;
	public Color selectionColor = Color.red;
	//
	IGUIListMaster master;
	int selection = -1;
	bool focusToSelection = false;
	//
	Vector2 scrollPosition;
	Rect selectionRect;
	Rect contentRect;
	Rect viewRect;
	
	public GUIList (IGUIListMaster tmaster, GUILayoutOption [] toptions = null)
	{
		master = tmaster;
		if (toptions == null) {
			toptions = new GUILayoutOption[] {};
		}
		options = toptions;
	}
	
	public int SelectedRow {
		get { return selection; }
		set {
			if (selection != value) {
				selection = value;
				focusToSelection = true;
			}
		}
	}
	
	public void Draw ()
	{
		bool isRepaintEvent = Event.current.type == EventType.Repaint;
		
		int rowCount = master.GetRowCount (this);
		if (selection >= rowCount) {
			SelectedRow = -1;
		}

		GUILayout.BeginVertical("box");
		scrollPosition = GUILayout.BeginScrollView (scrollPosition, options);
		
		for (int icontent = 0; icontent < rowCount; icontent++) {
			string content = master.GetContentAt (this, icontent);
			if (icontent == SelectedRow) {
				GUI.backgroundColor = selectionColor;
			} else {
				GUI.backgroundColor = normalColor;
			}
			
			if (GUILayout.Button (content)) {
				int oldSelection = selection;
				selection = icontent;
				master.SelectionChanged (this, oldSelection, selection);
			}
			
			if (isRepaintEvent && icontent == SelectedRow) {
				selectionRect = GUILayoutUtility.GetLastRect ();
			}
		}
		
		GUILayout.EndScrollView ();
		
		if (isRepaintEvent) {
			viewRect = GUILayoutUtility.GetLastRect ();
			contentRect.Set (scrollPosition.x, scrollPosition.y, viewRect.width, viewRect.height);	
			
			if (focusToSelection) {
				focusToSelection = false;
			
				if (contentRect.x > selectionRect.x) {
					scrollPosition.x = selectionRect.x;
				} else if (contentRect.xMax < selectionRect.xMax) {
					scrollPosition.x = selectionRect.xMax - contentRect.width;
				}
			
				if (contentRect.y > selectionRect.y) {
					scrollPosition.y = selectionRect.y;
				} else if (contentRect.yMax < selectionRect.yMax) {
					scrollPosition.y = selectionRect.yMax - contentRect.height;
				}
			}
		}
		
		GUI.backgroundColor = normalColor;
		GUILayout.EndVertical();
	}
}
