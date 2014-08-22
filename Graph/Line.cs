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
	public class Line : System.Runtime.Serialization.IDeserializationCallback
	{
		public Graph graph;
		public object meta;
		public Node start;
		public Node end;
		public List<Vector3> vertices;
//		protected Mark.Container markContainer;
		[System.NonSerialized]
		protected List<float> segmentLengths;
		
		public Line ()
		{
			vertices = new List<Vector3> ();
//			markContainer = new Mark.Container ();
			OnDeserialization (null);
		}
		
		public virtual void OnDeserialization (object sender)
		{
			segmentLengths = new List<float> (vertices.Count + 1);
			for (int ilength = 0; ilength < segmentLengths.Count; ilength++) {
				segmentLengths.Add (segmentLengths [ilength - 1] + Vector3.Distance (VertexAt (ilength), VertexAt (ilength - 1)));
			}
		}

//		public Graph Graph { get { return graph; } }
//		public object Meta { get { return meta; } }
//		public Mark.Container Marks { get { return markContainer; } }
//		public Node Start { get { return this.start; } }
//		public Node End { get { return this.end; } }
		public float Length { get { return segmentLengths.Last (); } }
		public int VertexCount { get { return vertices.Count; } }
//		public IEnumerable<Line> Nexts { get { return end.outgoings; } }
//		public IEnumerable<Line> Prevs { get { return start.incomings; } }

		public Vector3 VertexAt (int index)
		{
			if (index < 0) {
				return start.position;
			} else if (index >= vertices.Count) {
				return end.position;
			} else {
				return vertices [index];
			}
		}
		public float SegmentLengthAt (int index)
		{
			return segmentLengths [index] - ((index) <= 0 ? 0f : segmentLengths [index - 1]);
		}
		public float EndOfSegmentAt (int index)
		{
			return index < 0 ? 0f : segmentLengths [index];
		}
//		public Mark NextMarkOf (Point point)
//		{
//			return null;
//		}
//		public Mark PrevMarkOf (Point point)
//		{
//			return null;
//		}
	}
}

