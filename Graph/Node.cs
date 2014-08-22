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
	public class Node
	{
		public Graph graph;
		public object meta;
		public Vector3 position;
		public List<Line> incomings;
		public List<Line> outgoings;
	}
}