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
	public struct Point : IComparable<Point>
	{
		public Line line;
		public int segment;
		public float normalized;

		public Point(Line tline, int tsegment, float tnormalized)
		{
			line = tline;
			segment = tsegment;
			normalized = tnormalized;
		}

		public bool AtLastSegment { get { return segment >= line.VertexCount; } }
		public bool AtFirstSegment { get { return segment <= 0; } }
		public Vector3 Next { get { return line.VertexAt(segment); } }
		public Vector3 Prev { get { return line.VertexAt(segment - 1); } }
		public Vector3 Position { get { var prev = line.VertexAt(segment - 1); return prev + (line.VertexAt(segment) - prev) * normalized; } }
		public Vector3 Orientation { get { return line.VertexAt(segment) - line.VertexAt(segment - 1); } }
		public float SegmentLength { get { return line.SegmentLengthAt(segment); } }
		public float DistanceToPrev { get { return -line.SegmentLengthAt(segment) * normalized; } }
		public float DistanceToNext { get { return line.SegmentLengthAt(segment) * (1f - normalized); } }
		public float DistanceToStart { get { return -line.EndOfSegmentAt(segment - 1) + line.SegmentLengthAt(segment) * normalized; } }
		public float DistanceToEnd { get { return line.Length - line.EndOfSegmentAt(segment) + line.SegmentLengthAt(segment) * (1f - normalized); } }

		public int CompareTo(Point other)
		{
			if (other.line == line) {
				return segment != other.segment ? (segment < other.segment ? -1 : 1) : (normalized < other.normalized ? -1 : 1);
			} else {
				throw new ArgumentException();
			}
		}
		public static bool operator >(Point p1, Point p2)
		{
			if (p1.line == p2.line) {
				return p1.segment != p2.segment ? (p1.segment > p2.segment) : (p1.normalized > p2.normalized);
			} else {
				throw new ArgumentException();
			}
		}
		public static bool operator <(Point p1, Point p2)
		{
			return p2 > p1;
		}
		public static float operator -(Point point1, Point point2)
		{
			if (point1.line == point2.line) {
				return point1.DistanceToStart - point2.DistanceToStart;
			} else {
				throw new ArgumentException();
			}
		}
	}

}