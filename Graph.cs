using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Extensions;

[System.Serializable]
public class Graph
{
	public struct Segment
	{
		public Vertex start;
		public Vertex end;
	}

	public struct Vertex
	{
		public Vector3 position;
		public float roll;
	}

	[System.Serializable]
	public struct Point : IComparable<Point>
	{
		public Line line;
		public int isegment;
		public float normalized;

		public Point(Line aline, int asegment, float anormalized)
		{
			line = aline;
			isegment = asegment;
			normalized = anormalized;
		}

		public Graph Graph { get { return line.Graph; } }
		public bool IsValid { get { return line != null && line.SegmentCount > isegment && isegment >= 0 && normalized >= 0f && normalized <= 1f; } }
		public bool AtLastSegment { get { return isegment == line.SegmentCount - 1; } }
		public bool AtFirstSegment { get { return isegment == 0; } }
		public Vertex Next { get { return line.VertexAt(isegment); } }
		public Vertex Prev { get { return line.VertexAt(isegment - 1); } }
		public Vector3 Position { get { return Vector3.Lerp(line.VertexAt(isegment - 1).position, line.VertexAt(isegment).position, normalized); } }
		public float Roll { get { return Mathf.Lerp(line.VertexAt(isegment - 1).roll, line.VertexAt(isegment).roll, normalized); } }
		public Quaternion Orientation { get { return Quaternion.Euler(0f, 0f, Roll) * Quaternion.LookRotation(line.VertexAt(isegment).position - line.VertexAt(isegment - 1).position); } }
		public Segment Segment { get { return line.SegmentAt(isegment); } }
		public float SegmentLength { get { return line.GetSegmentLength(isegment); } }
		public float DistanceToPrev { get { return -line.GetSegmentLength(isegment) * normalized; } }
		public float DistanceToNext { get { return line.GetSegmentLength(isegment) * (1f - normalized); } }
		public float DistanceToStart { get { return -line.GetSegmentDistance(isegment - 1) + line.GetSegmentLength(isegment) * normalized; } }
		public float DistanceToEnd { get { return line.Length - line.GetSegmentDistance(isegment) + line.GetSegmentLength(isegment) * (1f - normalized); } }

		public int CompareTo(Point other)
		{
			if (other.line == line) {
				return isegment != other.isegment ? (isegment < other.isegment ? -1 : 1) : (normalized < other.normalized ? -1 : 1);
			} else {
				throw new ArgumentException();
			}
		}
		public static bool operator >(Point p1, Point p2)
		{
			if (p1.line == p2.line) {
				return p1.isegment != p2.isegment ? (p1.isegment > p2.isegment) : (p1.normalized > p2.normalized);
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

	[System.Serializable]
	public class Line : System.Runtime.Serialization.IDeserializationCallback
	{
		protected Graph graph;
		protected object meta;
		protected Node start;
		protected Node end;
		protected List<Vertex> vertices;
		protected bool directed;
		[System.NonSerialized]
		protected List<float> lengths;

		protected Line(Graph agraph, object ameta, Node node1, Node node2, IEnumerable<Vertex> avertices, bool adirected = false)
		{
			// TODO: generateRoll
			graph = agraph;
			meta = ameta;
			directed = adirected;

			// Consistency Check
			bool consistent = true;
			if (node1 == null || node2 == null || node1 == node2 || (node1.Vertex.position - node2.Vertex.position).sqrMagnitude.IsZero()) {
				consistent = false;
			} else if (avertices.Count() > 0) {
				var dist1 = (node1.Vertex.position - avertices.First().position).magnitude;
				var dist2 = (node1.Vertex.position - avertices.Last().position).magnitude;
				var dist3 = (node2.Vertex.position - avertices.First().position).magnitude;
				var dist4 = (node2.Vertex.position - avertices.Last().position).magnitude;

				if (dist1 < dist2 && dist3 > dist4) { // Regular
					start = node1;
					end = node2;
					dist2 = dist4;

				} else if (dist1 > dist2 && dist3 < dist4) { // Reverse
					start = node2;
					end = node1;
					dist1 = dist3;

				} else { // Undefined
					consistent = false;
					start = node1;
					end = node2;
					dist2 = dist4;
				}

				if (dist1.IsZero()) {
					avertices = avertices.Skip(1);
				}
				vertices = avertices.ToList();
				if (dist2.IsZero()) {
					if (vertices.Count == 0) {
						consistent = false;
					} else {
						vertices.RemoveLast();
					}
				}
			} else {
				vertices = avertices.ToList();
				start = node1;
				end = node2;
			}

			if (!consistent) {
				Common.LogWarning("Line consistency check failed: {0} ({1}, {2})",
					ameta.ToString(), node1.Vertex.position, node2.Vertex.position);
			} else {
				OnDeserialization(null);
			}
		}

		public virtual void OnDeserialization(object sender)
		{
			lengths = new List<float>(vertices.Count + 1);
			for (int ilength = 0; ilength < lengths.Count; ilength++) {
				lengths.Add(lengths[ilength - 1] + Vector3.Distance(VertexAt(ilength).position, VertexAt(ilength - 1).position));
			}
		}

		public Graph Graph { get { return graph; } }
		public object Meta { get { return meta; } }
		public bool IsDirected { get { return directed; } }
		public Node Start { get { return start; } }
		public Node End { get { return end; } }
		public float Length { get { return lengths.Last(); } }
		public int SegmentCount { get { return lengths.Count; } }
		public ReadOnlyList<Vertex> Vertices { get { return new ReadOnlyList<Vertex>(vertices); } }

		public Segment SegmentAt(int isegment)
		{
			return new Segment { start = VertexAt(isegment - 1), end = VertexAt(isegment) };
		}
		public Vertex VertexAt(int ivertex)
		{
			if (ivertex < 0) {
				return start.Vertex;
			} else if (ivertex >= vertices.Count) {
				return end.Vertex;
			} else {
				return vertices[ivertex];
			}
		}
		public float GetSegmentLength(int isegment)
		{
			return lengths[isegment] - ((isegment) <= 0 ? 0f : lengths[isegment - 1]);
		}
		public float GetSegmentDistance(int isegment)
		{
			return isegment < 0 ? 0f : lengths[isegment];
		}
	}

	[System.Serializable]
	public class Node
	{
		protected Graph graph;
		protected object meta;
		protected List<Line> incomings = new List<Line>();
		protected List<Line> outgoings = new List<Line>();
		protected Vertex vertex;

		protected Node(Graph agraph, object ameta, Vertex avertex)
		{
			graph = agraph;
			meta = ameta;
			vertex = avertex;
		}

		public Graph Graph { get { return graph; } }
		public object Meta { get { return meta; } }
		public Vertex Vertex { get { return vertex; } }
		public ReadOnlyList<Line> Incomings { get { return new ReadOnlyList<Line>(incomings); } }
		public ReadOnlyList<Line> Outgoings { get { return new ReadOnlyList<Line>(outgoings); } }
		public IEnumerable<Line> All { get { return outgoings.Concat(incomings.Where(line => !line.IsDirected)); } }
	}

	public interface IWalkerGuide
	{
		Line GetNextLine(Walker walker, Node node);
	}

	public class Walker
	{
		protected bool isValid = true;
		protected IWalkerGuide guide;
		protected Point point;
		protected float distance;
		protected float direction;

		public Walker(IWalkerGuide aguide, Point apoint)
		{
			guide = aguide;
			point = apoint;
			distance = 0f;
		}

		public bool Valid { get { return isValid; } }
		public Point Point { get { return point; } }
		public Quaternion Orientation { get { return direction > 0f ? point.Orientation : Quaternion.Inverse(point.Orientation); } }
		public Line Line { get { return point.line; } }
		public Graph Graph { get { return point.line.Graph; } }
		public float Distance { get { return distance; } }
		public bool Direction { get { return direction > 0f; } set { direction = value ? 1f : 0f; } }
		//
		public Vertex Next { get { return direction > 0f ? point.Next : point.Prev; } }
		public Vertex Prev { get { return direction > 0f ? point.Prev : point.Next; } }
		public Node Start { get { return direction > 0f ? point.line.Start : point.line.End; } }
		public Node End { get { return direction > 0f ? point.line.End : point.line.Start; } }
		public IEnumerable<Line> Junction { get { return End.All.Where(line => line != point.line); } }
		public IEnumerable<Line> JunctionBehind { get { return Start.All.Where(line => line != point.line); } }
		public float DistanceToNext { get { return direction > 0f ? point.DistanceToNext : -point.DistanceToPrev; } }
		public float DistanceToPrev { get { return direction > 0f ? point.DistanceToPrev : -point.DistanceToNext; } }
		public float DistanceToStart { get { return direction > 0f ? point.DistanceToStart : -point.DistanceToEnd; } }
		public float DistanceToEnd { get { return direction > 0f ? point.DistanceToEnd : -point.DistanceToStart; } }

		public float ResetDistance()
		{
			var result = distance;
			distance = 0f;
			return result;
		}
		public virtual void Move(float adistance)
		{
			var distanceResult = distance + adistance;

			if (adistance > 0f) {
				float nextDistance = DistanceToEnd;
				while (isValid && nextDistance < adistance) {
					adistance -= nextDistance;
					MoveToEnd();
					nextDistance = DistanceToEnd;
				}

				nextDistance = DistanceToNext;
				while (nextDistance < adistance) {
					adistance -= nextDistance;
					MoveToNext();
					nextDistance = DistanceToNext;
				}

			} else {
				float prevDistance = DistanceToStart;
				while (isValid && prevDistance > adistance) {
					adistance += prevDistance;
					MoveToStart();
					prevDistance = DistanceToStart;
				}

				prevDistance = DistanceToPrev;
				while (prevDistance > adistance) {
					adistance += prevDistance;
					MoveToPrev();
					prevDistance = DistanceToPrev;
				}
			}

			distance = distanceResult;
			adistance *= direction;
			point.normalized = adistance > 0f ? adistance / point.SegmentLength : (1f + adistance) / point.SegmentLength;
		}
		public virtual void Jump(float adistance)
		{
			Vector3 center = point.Position;
			if (adistance > 0f) {
				while (isValid) {
					float result = Geometry.CircleIntersectionOnLine(center, adistance, Prev.position, Next.position);
					if (result >= 0f && result <= 1f) {
						point.normalized = result;
						return;
					}
					MoveToNext();
				}
			} else {
				adistance = -adistance;
				while (isValid) {
					float result = Geometry.CircleIntersectionOnLine(center, adistance, Prev.position, Next.position);
					if (result >= 0f && result <= 1f) {
						point.normalized = result;
						return;
					}
					MoveToPrev();
				}
			}
		}
		public virtual void MoveToNext()
		{
			if (direction > 0f) {
				MoveToNextBase();
			} else {
				MoveToPrevBase();
			}
		}
		public virtual void MoveToPrev()
		{
			if (direction > 0f) {
				MoveToPrevBase();
			} else {
				MoveToNextBase();
			}
		}
		public virtual void MoveToEnd()
		{
			if (direction > 0f) {
				MoveToEndBase();
			} else {
				MoveToStartBase();
			}
		}
		public virtual void MoveToStart()
		{
			if (direction > 0f) {
				MoveToStartBase();
			} else {
				MoveToEndBase();
			}
		}
		//
		protected void MoveToNextBase()
		{
			if (!isValid) {
				return;
			}

			if (point.AtLastSegment) {
				MoveToEndBase();
			} else {
				distance += direction * point.DistanceToNext;
				point.isegment++;
				point.normalized = 0f;
			}
		}
		protected void MoveToPrevBase()
		{
			if (!isValid) {
				return;
			}

			if (point.AtFirstSegment) {
				MoveToStartBase();
			} else {
				distance += direction * point.DistanceToPrev;
				point.isegment--;
				point.normalized = 1f;
			}
		}
		protected void MoveToEndBase()
		{
			if (!isValid) {
				return;
			}

			distance += direction * point.DistanceToEnd;
			var nextLine = guide.GetNextLine(this, Line.End);
			if (nextLine == null) {
				isValid = false;
				throw new Exception("Next line should not be null");
			} else if (point.line.Start == nextLine.Start) {
				point.line = nextLine;
				point.isegment = 0;
				point.normalized = 0f;
			} else if (point.line.End == nextLine.End) {
				point.line = nextLine;
				point.isegment = nextLine.SegmentCount;
				point.normalized = 1f;
				direction = -direction;
			} else {
				isValid = false;
				throw new Exception("Next line is not valid");
			}
		}
		protected void MoveToStartBase()
		{
			if (!isValid) {
				return;
			}

			distance += direction * point.DistanceToStart;
			var prevLine = guide.GetNextLine(this, Line.Start);
			if (prevLine == null) {
				isValid = false;
				throw new Exception("Previous line should not be null");
			} else if (point.line.Start == prevLine.End) {
				point.line = prevLine;
				point.isegment = prevLine.SegmentCount;
				point.normalized = 1f;
			} else if (point.line.Start == prevLine.Start) {
				point.line = prevLine;
				point.isegment = 0;
				point.normalized = 0f;
				direction = -direction;
			} else {
				isValid = false;
				throw new Exception("Previous line is not valid");
			}
		}
	}

	protected Dictionary<object, NodeEdit> nodes;
	protected Dictionary<object, LineEdit> lines;

	public Graph()
	{
		nodes = new Dictionary<object, NodeEdit>();
		lines = new Dictionary<object, LineEdit>();

		OnDeserialization(null);
	}

	public void OnDeserialization(object sender)
	{

	}

	public IEnumerable<Node> Nodes { get { return nodes.Values.Cast<Node>(); } }
	public IEnumerable<Line> Lines { get { return lines.Values.Cast<Line>(); } }

	public Node GetNode(object meta)
	{
		NodeEdit result = null;
		nodes.TryGetValue(meta, out result);
		return result as Node;
	}
	public Line GetLine(object meta)
	{
		LineEdit result = null;
		lines.TryGetValue(meta, out result);
		return result as Line;
	}

	public Node AddNode(object meta, Vertex vertex)
	{
		if (nodes.ContainsKey(meta)) {
			Common.LogError("Graph already contains a node with meta:{0}", meta.ToString());
			return null;

		} else {
			var result = new NodeEdit(this, meta, vertex);
			nodes.Add(meta, result);
			return result;
		}
	}
	public Line AddLine(object meta, object node1, object node2, IEnumerable<Vertex> vertices, bool directed = false)
	{
		if (GetLine(meta) != null) {
			Common.LogError("Graph already contains a line with meta:{0}", meta.ToString());
			return null;
		}

		var vertexCount = vertices.Count();
		var startNode = GetNode(node1);
		if (startNode == null) {
			if (vertexCount > 0) {
				startNode = AddNode(node1, vertices.First());
			} else {
				Common.LogError("Node {0} cant be defined", node1.ToString());
				return null;
			}
		}

		var endNode = GetNode(node2);
		if (endNode == null) {
			if (vertexCount > 0) {
				endNode = AddNode(node2, vertices.Last());
			} else {
				Common.LogError("Node {0} cant be defined", node2.ToString());
				return null;
			}
		}

		var result = new LineEdit(this, meta, startNode, endNode, vertices, directed);
		lines.Add(meta, result);
		((NodeEdit)result.Start).AddOutgoing(result);
		((NodeEdit)result.End).AddIncoming(result);

		return result;
	}

	public Walker GetWalker(IWalkerGuide guide)
	{
		return new Walker(guide, new Point(lines.ElementAt(UnityEngine.Random.Range(0, lines.Count - 1)).Value, 0, 0f));
	}
	public Walker GetWalker(IWalkerGuide guide, object key, float distance = 0f)
	{
		LineEdit line;
		if (lines.TryGetValue(key, out line)) {
			distance = Mathf.Clamp(distance, 0f, line.Length);
			var result = new Walker(guide, new Point(line, 0, 0f));
			result.Move(distance);
			return result;
		} else {
			return null;
		}
	}

	protected class LineEdit : Line
	{
		public LineEdit(Graph agraph, object ameta, Node node1, Node node2, IEnumerable<Vertex> avertices, bool adirected = false)
			: base(agraph, ameta, node1, node2, avertices, adirected)
		{ }
	}

	protected class NodeEdit : Node
	{
		public NodeEdit(Graph agraph, object ameta, Vertex avertex)
			: base(agraph, ameta, avertex)
		{ }

		public void AddIncoming(Line line)
		{
			incomings.Add(line);
		}
		public void AddOutgoing(Line line)
		{
			outgoings.Add(line);
		}
	}
}
