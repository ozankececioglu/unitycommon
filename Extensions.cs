using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using System.Text;

namespace Extensions
{
	public static class ListExtensions
	{
		public static T RemoveLast<T>(this List<T> me)
		{
			if (me.Count == 0)
				return default(T);
			T result = me[me.Count - 1];
			me.RemoveAt(me.Count - 1);
			return result;
		}
		public static T RandomUnit<T>(this List<T> me)
		{
			return me.Count > 0 ? me[UnityEngine.Random.Range(0, me.Count)] : default(T);
		}

		public static void SetCount<T>(this List<T> me, int tcount, bool filled = false)
		{
			if (tcount < 0) {
				throw new ArgumentOutOfRangeException();
			}

			if (me.Count > tcount) {
				me.RemoveRange(tcount, me.Count - tcount);
			} else if (me.Count < tcount) {
				if (filled) {
					me.AddRange(Common.CreateInstanceArray(typeof(T), tcount - me.Count) as T[]);
				} else {
					me.AddRange(new T[tcount - me.Count]);
				}
			}
		}

		public static void Randomize<T>(this List<T> me)
		{
			int count = me.Count;
			for (int iobj = 0; iobj < count; iobj++) {
				int target = UnityEngine.Random.Range(0, count - 1);
				if (target != iobj) {
					T obj = me[iobj];
					me[iobj] = me[target];
					me[target] = obj;
				}
			}
		}

		public static void Merge<T>(this List<T> me, IList<T> first, IList<T> second)
		{
			int capacity = first.Count + second.Count;
			me.Capacity = capacity;
			var firstEnumer = first.GetEnumerator();
			var secondEnumer = second.GetEnumerator();
			bool firstValid = firstEnumer.MoveNext();
			bool secondValid = secondEnumer.MoveNext();
			IComparer<T> comparer = Comparer<T>.Default;

			while (firstValid && secondValid) {
				if (!secondValid || firstValid && comparer.Compare(firstEnumer.Current, secondEnumer.Current) <= 0) {
					me.Add(firstEnumer.Current);
					firstValid = firstEnumer.MoveNext();
				} else {
					me.Add(secondEnumer.Current);
					secondValid = secondEnumer.MoveNext();
				}
			}
		}
		public static List<T> Merge<T>(IList<T> first, IList<T> second)
		{
			List<T> list = new List<T>();
			list.Merge(first, second);
			return list;
		}
	}

	public static class StringExtensions
	{
		public static bool IsNullOrEmpty(this string me)
		{
			return me == null || me.Length == 0;
		}
	}

	public static class FloatExtensions
	{
		public static bool IsZero(this float me)
		{
			return Mathf.Approximately(0.0f, me);
		}

		public static bool Is01(this float me)
		{
			return me >= 0f && me <= 1f;
		}
	}

	public static class Vector3Extensions
	{
		public static bool IsZero(this Vector3 me)
		{
			return Mathf.Approximately(0.0f, me.x) && Mathf.Approximately(0.0f, me.y) && Mathf.Approximately(0.0f, me.z);
		}

		public static float Max(this Vector3 me)
		{
			return Mathf.Max(me.x, me.y, me.z);
		}
		public static Vector3 Abs(this Vector3 me)
		{
			return new Vector3(Mathf.Abs(me.x), Mathf.Abs(me.y), Mathf.Abs(me.z));
		}

		public static Vector3 InverseScaled(this Vector3 me)
		{
			return new Vector3(1f / me.x, 1f / me.y, 1f / me.z);
		}

		public static float BirdsEyeMagnitude(this Vector3 me)
		{
			return Mathf.Sqrt(me.x * me.x + me.z * me.z);
		}
	}

	public static class RectExtensions
	{
		public static Vector2 TopLeft(this Rect me)
		{
			return new Vector2(me.xMin, me.yMax);
		}
		public static Vector2 TopRight(this Rect me)
		{
			return new Vector2(me.xMax, me.yMax);
		}
		public static Vector2 BottomLeft(this Rect me)
		{
			return new Vector2(me.xMin, me.yMin);
		}
		public static Vector2 BottomRight(this Rect me)
		{
			return new Vector2(me.xMax, me.yMin);
		}
	}

	public static class BoundsExtensions
	{
		public static IEnumerable<Vector3> Corners(this Bounds me)
		{
			yield return me.center + new Vector3(-me.extents.x, me.extents.y, me.extents.z);
			yield return me.center + new Vector3(me.extents.x, -me.extents.y, me.extents.z);
			yield return me.center + new Vector3(me.extents.x, me.extents.y, -me.extents.z);
			yield return me.center + new Vector3(-me.extents.x, -me.extents.y, me.extents.z);
			yield return me.center + new Vector3(me.extents.x, -me.extents.y, -me.extents.z);
			yield return me.center + new Vector3(-me.extents.x, me.extents.y, -me.extents.z);
			yield return me.center + new Vector3(-me.extents.x, -me.extents.y, -me.extents.z);
			yield return me.center + new Vector3(me.extents.x, me.extents.y, me.extents.z);
		}

		public static bool Contains(this Bounds me, Bounds other) { return me.Contains(other.max) && me.Contains(other.min); }

		public static Vector3 Bottom(this Bounds me) { return me.center - new Vector3(0f, me.extents.y, 0f); }
		public static Vector3 Top(this Bounds me)
		{
			return me.center + new Vector3(0f, me.extents.y, 0f);
		}
		public static Vector3 Far(this Bounds me)
		{
			return me.center - new Vector3(0f, 0f, me.extents.z);
		}
		public static Vector3 Near(this Bounds me)
		{
			return me.center + new Vector3(0f, 0f, me.extents.z);
		}
		public static Vector3 Left(this Bounds me)
		{
			return me.center - new Vector3(me.extents.x, 0f, 0f);
		}
		public static Vector3 Right(this Bounds me)
		{
			return me.center + new Vector3(me.extents.x, 0f, 0f);
		}

		public static Sphere ToSphere(this Bounds me)
		{
			return new Sphere() {
				center = me.center,
				radius = me.extents.magnitude
			};
		}

		public static void DrawBounds(this Bounds me)
		{
			DrawBounds(me, Color.white);
		}
		public static void DrawBounds(this Bounds me, Color color)
		{
			Vector3 center = me.center;
			Vector3 x = new Vector3(me.extents.x, 0.0f, 0.0f);
			Vector3 y = new Vector3(0.0f, me.extents.y, 0.0f);
			Vector3 z = new Vector3(0.0f, 0.0f, me.extents.z);
#if UNITY_EDITOR
			UnityEditor.Handles.color = color;
			UnityEditor.Handles.DrawLine(center + x + y + z, center + x + y - z);
			UnityEditor.Handles.DrawLine(center + x + y + z, center + x - y + z);
			UnityEditor.Handles.DrawLine(center + x + y + z, center - x + y + z);

			UnityEditor.Handles.DrawLine(center + x - y - z, center - x - y - z);
			UnityEditor.Handles.DrawLine(center + x - y - z, center + x + y - z);
			UnityEditor.Handles.DrawLine(center + x - y - z, center + x - y + z);

			UnityEditor.Handles.DrawLine(center - x + y - z, center + x + y - z);
			UnityEditor.Handles.DrawLine(center - x + y - z, center - x - y - z);
			UnityEditor.Handles.DrawLine(center - x + y - z, center - x + y + z);

			UnityEditor.Handles.DrawLine(center - x - y + z, center + x - y + z);
			UnityEditor.Handles.DrawLine(center - x - y + z, center - x + y + z);
			UnityEditor.Handles.DrawLine(center - x - y + z, center - x - y - z);
#else
			Gizmos.color = color;
			Gizmos.DrawLine (center + x + y + z, center + x + y - z);
			Gizmos.DrawLine (center + x + y + z, center + x - y + z);
			Gizmos.DrawLine (center + x + y + z, center - x + y + z);
		
			Gizmos.DrawLine (center + x - y - z, center - x - y - z);
			Gizmos.DrawLine (center + x - y - z, center + x + y - z);
			Gizmos.DrawLine (center + x - y - z, center + x - y + z);
		
			Gizmos.DrawLine (center - x + y - z, center + x + y - z);
			Gizmos.DrawLine (center - x + y - z, center - x - y - z);
			Gizmos.DrawLine (center - x + y - z, center - x + y + z);
		
			Gizmos.DrawLine (center - x - y + z, center + x - y + z);
			Gizmos.DrawLine (center - x - y + z, center - x + y + z);
			Gizmos.DrawLine (center - x - y + z, center - x - y - z);
#endif
		}
	}

	public static class TransformExtensions
	{
		public static IEnumerable<Transform> Children(this Transform me)
		{
			yield return me;
			foreach (Transform child in me) {
				foreach (var subchild in child.Children()) {
					yield return subchild;
				}
			}
		}
		public static IEnumerable<Transform> Children(this Transform me, Predicate<Transform> predicate)
		{
			foreach (var child in me.Children()) {
				if (predicate(child)) {
					yield return child;
				}
			}
		}
		public static IEnumerable<Transform> Children(this Transform me, string name)
		{
			return me.Children(child => {
				return name.Equals(child.name);
			});
		}

		public static IEnumerable<Transform> Parents(this Transform me)
		{
			Transform result = me.parent;
			while (result != null) {
				yield return result;
				result = result.parent;
			}
		}
		public static IEnumerable<Transform> Parents(this Transform me, Predicate<Transform> predicate)
		{
			foreach (var parent in me.Parents()) {
				if (predicate(parent))
					yield return parent;
			}
		}

		public static void RemoveChildren(this Transform me, Predicate<Transform> predicate)
		{
			foreach (Transform child in me) {
				if (predicate(child)) {
#if UNITY_EDITOR
					GameObject.DestroyImmediate(child.gameObject);
#else
					GameObject.Destroy (child.gameObject);
#endif
				}
			}
		}

		public static string ScenePath(this Transform me)
		{
			string result = "/" + me.name;
			Transform parent = me.parent;
			while (parent != null) {
				result = "/" + parent.name + result;
				parent = parent.parent;
			}
			return result;
		}

		public static void RotateAroundLocal(this Transform me, Vector3 local, Vector3 axis, float angle)
		{
			var point = me.TransformPoint(local);
			me.RotateAround(point, axis, angle);
		}
		public static void Reset(this Transform me)
		{
			me.localPosition = Vector3.zero;
			me.localRotation = Quaternion.identity;
		}
	}

	public static class GameObjectExtensions
	{
		public static GameObject Instantiate(this GameObject go, Transform parent = null)
		{
			return Common.Instantiate(go, parent, Vector3.zero, Quaternion.identity, go.transform.localScale);
		}
		public static GameObject Instantiate(this GameObject go, Transform parent, Vector3 localPosition)
		{
			return Common.Instantiate(go, parent, localPosition, Quaternion.identity, go.transform.localScale);
		}
		public static GameObject Instantiate(this GameObject go, Transform parent, Vector3 localPosition, Quaternion localRotation)
		{
			return Common.Instantiate(go, parent, localPosition, localRotation, go.transform.localScale);
		}
		public static GameObject Instantiate(this GameObject go, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var result = (GameObject)GameObject.Instantiate(go);
			result.name = go.name;
			result.transform.parent = parent;
			result.transform.localPosition = localPosition;
			result.transform.localRotation = localRotation;
			result.transform.localScale = localScale;
			return result;
		}

		public static Mesh GetCombinedMesh(this GameObject me)
		{
			var meshFilters = me.GetComponentsInChildren<MeshFilter>();
			var worldToLocalMatrix = me.transform.worldToLocalMatrix;
			List<Vector3> vertices = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<int> triangles = new List<int>();

			foreach (var meshFilter in meshFilters) {
				int indexOffset = vertices.Count;
				var tmatrix = worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
				foreach (var vertex in meshFilter.sharedMesh.vertices) {
					vertices.Add(tmatrix.MultiplyPoint(vertex));
				}
				foreach (var normal in meshFilter.sharedMesh.normals) {
					normals.Add(tmatrix.MultiplyVector(normal));
				}
				foreach (var index in meshFilter.sharedMesh.triangles) {
					triangles.Add(indexOffset + index);
				}
			}

			Mesh result = new Mesh();
			result.vertices = vertices.ToArray();
			result.normals = normals.ToArray();
			result.triangles = triangles.ToArray();

			return result;
		}
		public static Bounds GetCombinedMeshBounds(this GameObject me)
		{
			var meshFilters = me.GetComponentsInChildren<MeshFilter>();
			Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			var worldToLocalMatrix = me.transform.worldToLocalMatrix;

			foreach (var meshFilter in meshFilters) {
				if (meshFilter.gameObject.activeInHierarchy) {
					var tmatrix = worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
					foreach (var vertex in meshFilter.sharedMesh.vertices) {
						var transformedVertex = tmatrix.MultiplyPoint(vertex);
						min = Geometry.Min(min, transformedVertex);
						max = Geometry.Max(max, transformedVertex);
					}
				}
			}

			return new Bounds((max + min) * 0.5f, max - min);
		}
	}

	public static class CameraExtensions
	{
		public static Vector2 OrthograhicSize(this Camera me)
		{
			return new Vector2(me.orthographicSize * me.aspect, me.orthographicSize);
		}
		public static Vector3 ScreenToViewPort(this Camera me, Vector3 pos)
		{
			return new Vector3(2.0f * pos.x / me.pixelWidth - 1.0f, 2.0f * pos.y / me.pixelHeight - 1.0f, pos.z);
		}
		public static Vector3 ScreenToWorld(this Camera me, Vector3 pos)
		{
			var result = me.projectionMatrix.inverse.MultiplyPoint(me.ScreenToViewPort(pos));
			result.z *= -1f;
			return me.transform.localToWorldMatrix.MultiplyPoint(result);
		}
		public static Ray ScreenToRay(this Camera me, Vector2 pos)
		{
			var near = me.ScreenToWorld(new Vector3(pos.x, pos.y, -1.0f));
			var far = me.ScreenToWorld(new Vector3(pos.x, pos.y, 1.0f));
			return new Ray(near, far - near);
		}
	}
	
	public static class Texture2DExtensions
	{
		public static Texture2D GetResized(this Texture2D me, int width, int height)
		{
			Texture2D result = new Texture2D(width, height);
			float widthScale = me.width / (float)width;
			float heightScale = me.height / (float)height;
			for (int i = 0; i < width; i++) {
				for (int j = 0; j < height; j++) {
					int w = (int)((widthScale / width) * i);
					int h = (int)((heightScale / height) * j);
					var color = me.GetPixel(w, h);
					result.SetPixel(i, j, color);
				}
			}
			return result;
		}
	}
}