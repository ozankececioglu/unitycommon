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

namespace AI
{
	[System.Serializable]
	public class Mark //: IComparable<Mark>//, System.Runtime.Serialization.IDeserializationCallback
	{
		protected Point point;
		protected int sortIndex = -1;
//
//		public Mark ()
//		{
//			//OnDeserialization (null);
//		}
//
//		public virtual void OnDeserialization (object sender)
//		{
//		
//		}
//		
		public Point Point { get { return point; } }
//		public Mark NextMark { 
//			get {
//				if (sortIndex == -1) {
//					throw new Exception ("sort index is not set");
//				} else {
//					return point.line.Marks [sortIndex + 1];
//				}
//			}
//		}
//		public Mark PrevMark {
//			get {
//				if (sortIndex == -1) {
//					throw new Exception ("sort index is not set");
//				} else {
//					return point.line.Marks [sortIndex - 1];
//				}
//			}
//		}
//	
//		public int CompareTo (Mark other)
//		{
//			return point.CompareTo (other.point);
//		}
//		public int CompareTo (Point tpoint)
//		{
//			return point.CompareTo (tpoint);
//		}
//		
//		[System.Serializable]
//		public class Container
//		{
//			protected List<Mark> defaultMarks;
//			[System.NonSerialized]
//			protected List<Mark> futureMarks = new List<Mark> ();
//			[System.NonSerialized]
//			protected List<Mark> marks = new List<Mark> ();
//
//			public Mark First { get { return marks.Count == 0 ? null : marks.First (); } }
//			public Mark Last { get { return marks.Count == 0 ? null : marks.Last (); } }
//			public Mark this [int index] { get { return index < 0 || index >= marks.Count ? null : marks [index]; } }
//			
//			public void Add (Mark mark)
//			{
//				futureMarks.Add (mark);
//			}
//			public void AddDefault (Mark mark)
//			{
//				defaultMarks.Add(mark);
//				defaultMarks.Sort();
//			}
//			public void RemoveDefault (Mark mark)
//			{
//				defaultMarks.Remove(mark);
//			}
//			public void Sort ()
//			{
//				marks.ForEach (mark => mark.sortIndex = -1);
//				marks.Clear ();
//				futureMarks.Sort ();
//				marks.Merge (defaultMarks, futureMarks);
//				for (int imark = 0; imark < marks.Count; imark++) {
//					marks [imark].sortIndex = imark;
//				}
//				futureMarks.Clear ();
//			}
//			public float RangeSearch (Point point)
//			{
//				return Common.RangeSearch (point, marks);
//			}
//		}
	}
}
