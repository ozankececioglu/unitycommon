using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Extensions;

namespace AI
{
	[System.Serializable]
	public class Graph : System.Runtime.Serialization.IDeserializationCallback
	{
		public List<Line> lines;
		public List<Node> nodes;

		public Graph()
		{
			lines = new List<Line>();
			nodes = new List<Node>();
			OnDeserialization(null);
		}

		public void OnDeserialization(object sender)
		{
			
		}

		public Node CreateNode(Vector3 position, object meta = null)
		{
			var node = new Node();
			node.graph = this;
			node.meta = meta;
			node.position = position;
			return node;
		}

		public Line CreateLine(Node start, Node end, IEnumerable<Vector3> vertices, object meta = null)
		{
			var line = new Line();
			line.graph = this;
			line.meta = meta;
			line.start = start;
			line.end = end;
			start.outgoings.Add(line);
			end.incomings.Add(line);
			line.vertices.AddRange(vertices);
			line.OnDeserialization(null);
			return line;
		}
	}
}