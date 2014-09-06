#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Linq;
using System.Diagnostics;
using Extensions;

struct CheckTimer
{
	float nextCheck;
	float interval;

	public CheckTimer(float ainterval)
	{
		interval = ainterval;
		nextCheck = UnityEngine.Time.time + interval;
	}

	public void SetInterval(float ainterval)
	{
		interval = ainterval;
		nextCheck = UnityEngine.Time.time + interval;
	}

	public bool Check()
	{
		if (UnityEngine.Time.time > nextCheck) {
			nextCheck = UnityEngine.Time.time + interval;
			return true;
		}
		return false;
	}
}

public class UnitySingleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T instance = null;
	public static T Instance
	{
		get
		{
			if (instance == null) {
				var className = typeof(T).Name;
				var go = GameObject.Find("/" + className);
				if (go == null) {
					go = new GameObject(className);
				}

				instance = go.GetComponent<T>();
				if (instance == null) {
					instance = go.AddComponent<T>();
				}
			}
			return instance;
		}
	}
}

public static class Common
{
#if UNITY_EDITOR
	[MenuItem("SimBT/Apply All Prefabs", priority = 500)]
	public static void ApplyChangesToPrefabs()
	{
		var gos = Selection.gameObjects;
		foreach (var go in gos) {
			Selection.activeGameObject = go;
			EditorApplication.ExecuteMenuItem("GameObject/Apply Changes To Prefab");
		}
		Selection.objects = gos;
	}
	public static void ApplyChangeToPrefab(GameObject go)
	{
		var selection = Selection.objects;
		Selection.activeObject = go;
		EditorApplication.ExecuteMenuItem("GameObject/Apply Changes To Prefab");
		Selection.objects = selection;
	}
	public static bool IsPrefab(UnityEngine.Object obj)
	{
		return PrefabUtility.GetPrefabParent(obj) == null && PrefabUtility.GetPrefabObject(obj) != null;
	}
	public static bool IsGameObject(UnityEngine.Object obj)
	{
		return obj is GameObject ? !IsPrefab(obj) : false;
	}
#endif

	static Common()
	{
#if !UNITY_EDITOR
		File.AppendAllText("log.txt", "\n=============================" + DateTime.Now.ToString() + "=============================\n");
#endif
	}

	public static void Log(object obj, bool forceToFile = false)
	{
		string log = PrintObject(obj, 0);

		UnityEngine.Debug.Log(log);
		if (Application.isEditor || forceToFile) {
			File.AppendAllText("log.txt", "info: " + log.Replace("\n", "\n\t") + "\n");
		}
	}

	public static void LogWarning(object obj, bool forceToFile = false)
	{
		string log = PrintObject(obj, 0);

		UnityEngine.Debug.LogWarning(log);
		if (Application.isEditor || forceToFile) {
			File.AppendAllText("log.txt", "*warning*: " + log.Replace("\n", "\n\t") + "\n");
		}
	}

	public static void LogError(object obj, bool forceToFile = false)
	{
		string log = PrintObject(obj, 0);

		UnityEngine.Debug.LogError(log);
		if (Application.isEditor || forceToFile) {
			File.AppendAllText("log.txt", "**error**: " + log.Replace("\n", "\n\t") + "\n");
		}
	}

	static Texture2D dummyTexture = null;
	public static Texture2D DummyTexture()
	{
		if (dummyTexture == null) {
			dummyTexture = new Texture2D(1, 1);
			dummyTexture.SetPixel(0, 0, Color.white);
		}
		return dummyTexture;
	}

	public static Transform[] Roots()
	{
		return GameObject.FindSceneObjectsOfType(typeof(Transform)).Cast<Transform>().Where(x => x.parent == null).ToArray();
	}

	public static Rect DrawTextureAligned(Rect bounds, Texture2D texture)
	{
		Rect target = new Rect();
		float boundsAspectRatio = bounds.width / bounds.height;
		float textureAspectRatio = ((float)texture.width) / texture.height;
		if (boundsAspectRatio > textureAspectRatio) { // fit height
			float heightRatio = texture.height / bounds.height;
			float width = texture.width / heightRatio;
			target.Set(bounds.x + (bounds.width - width) * 0.5f, bounds.y, width, bounds.height);
		} else { // fit width
			float widthRatio = texture.width / bounds.width;
			float height = texture.height / widthRatio;
			target.Set(bounds.x, bounds.y + (bounds.height - height) * 0.5f, bounds.width, height);
		}

		GUI.DrawTexture(target, texture);
		return target;
	}

	public static string DayTimeToString(float timeValue)
	{
		int time = (int)(1439.0f * timeValue);
		int hour = time / 60;
		int min = time % 60;
		return "" + (hour < 10 ? "0" + hour : "" + hour) + ":" + (min < 10 ? "0" + min : "" + min);
	}

	public static Bounds GetLocalBounds(GameObject go)
	{
		Collider collider = go.GetComponent<Collider>();

		if (collider == null) {
			throw new System.Exception(go.name + ": GameObject has no collider attached");
		} else if (collider is BoxCollider) {
			BoxCollider box = collider as BoxCollider;
			return new Bounds(box.center, box.size);
		} else if (collider is MeshCollider) {
			MeshCollider mesh = collider as MeshCollider;
			return mesh.sharedMesh.bounds;
		} else if (collider is CharacterController) {
			CharacterController character = collider as CharacterController;
			return new Bounds(character.center, new Vector3(character.radius, character.height, character.radius));
		} else if (collider is CapsuleCollider) {
			CapsuleCollider capsule = collider as CapsuleCollider;
			return new Bounds(capsule.center, new Vector3(capsule.radius, capsule.height, capsule.radius));
		} else if (collider is SphereCollider) {
			SphereCollider sphere = collider as SphereCollider;
			return new Bounds(sphere.center, new Vector3(sphere.radius, sphere.radius, sphere.radius));
		} else {
			throw new NotImplementedException();
		}
	}

	public static void DrawLine(IEnumerable<Vector3> line)
	{
		DrawLine(line, Color.white);
	}
	public static void DrawLine(IEnumerable<Vector3> line, Color color)
	{
		CoupleEnumerator<Vector3> enumer = new CoupleEnumerator<Vector3>(line);
		while (enumer.MoveNext()) {
			UnityEngine.Debug.DrawLine(enumer.Previous, enumer.Current, color);
		}
	}
	private const float dim2 = 0.7071068f;
	private const float dim3 = 0.5773503f;
	public static void DrawStar(Vector3 position, float size = 1.0f)
	{
		DrawStar(position, Color.white, size);
	}
	public static void DrawStar(Vector3 position, Color color, float size = 1.0f)
	{
		float size2 = dim2 * size;
		float size3 = dim3 * size;

		UnityEngine.Debug.DrawLine(position + new Vector3(size, 0f, 0f), position + new Vector3(-size, 0f, 0f), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(0f, size, 0f), position + new Vector3(0f, -size, 0f), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(0f, 0f, size), position + new Vector3(0f, 0f, -size), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(size2, size2, 0f), position + new Vector3(-size2, -size2, 0f), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(size2, 0f, size2), position + new Vector3(-size2, 0f, -size2), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(0f, size2, size2), position + new Vector3(0f, -size2, -size2), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(size2, -size2, 0f), position + new Vector3(-size2, size2, 0f), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(size2, 0f, -size2), position + new Vector3(-size2, 0f, size2), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(0f, size2, -size2), position + new Vector3(0f, -size2, size2), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(size3, size3, size3), position + new Vector3(-size3, -size3, -size3), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(size3, size3, -size3), position + new Vector3(-size3, -size3, size3), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(size3, -size3, size3), position + new Vector3(-size3, size3, -size3), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(-size3, size3, size3), position + new Vector3(size3, -size3, -size3), color);
	}
	public static void DrawBox(Vector3 position, Vector3 size)
	{
		DrawBox(position, size, Color.white);
	}
	public static void DrawBox(Vector3 position, Vector3 size, Color color)
	{
		size = size * 0.5f;
		float xmax = +size.x;
		float xmin = -size.x;
		float ymax = +size.y;
		float ymin = -size.y;
		float zmax = +size.z;
		float zmin = -size.z;

		UnityEngine.Debug.DrawLine(position + new Vector3(xmin, ymin, zmin), position + new Vector3(xmin, ymin, zmax), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmin, ymin, zmin), position + new Vector3(xmin, ymax, zmin), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmin, ymin, zmin), position + new Vector3(xmax, ymin, zmin), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmin, ymax, zmax), position + new Vector3(xmin, ymax, zmin), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmin, ymax, zmax), position + new Vector3(xmin, ymin, zmax), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmin, ymax, zmax), position + new Vector3(xmax, ymax, zmax), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmax, ymax, zmin), position + new Vector3(xmax, ymax, zmax), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmax, ymax, zmin), position + new Vector3(xmax, ymin, zmin), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmax, ymax, zmin), position + new Vector3(xmin, ymax, zmin), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmax, ymin, zmax), position + new Vector3(xmax, ymin, zmin), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmax, ymin, zmax), position + new Vector3(xmax, ymax, zmax), color);
		UnityEngine.Debug.DrawLine(position + new Vector3(xmax, ymin, zmax), position + new Vector3(xmin, ymin, zmax), color);
	}

	public static Vector2 MouseDeltaNormalized()
	{
		return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
	}
	public static Vector2 MouseDeltaInPixels()
	{
		var result = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		return result;
	}

	public static float[] Discriminant(float a, float b, float c)
	{
		float discriminant = b * b - 4f * a * c;
		if (discriminant.IsZero()) {
			return new float[1] { -b / 2f * a };
		} else if (discriminant > 0f) {
			discriminant = Mathf.Sqrt(discriminant);
			a = 2f * a;
			return new float[2] {
				(discriminant - b) / a,
				- (discriminant + b) / a
			};
		} else {
			return new float[0];
		}
	}
	public static float TimeDiscriminant(float distance, float velocity, float accel)
	{
		if (!accel.IsZero()) {
			float[] roots = Discriminant(0.5f * accel, velocity, -distance);
			float time = float.MaxValue;
			foreach (float root in roots)
				if (root > 0f && root < time)
					time = root;
			return time;
		} else if (!velocity.IsZero()) {
			float time = distance / velocity;
			return time > 0f ? time : float.MaxValue;
		} else {
			return float.MaxValue;
		}
	}

	public static float RandomAverage(float average)
	{
		float rand = UnityEngine.Random.Range(0f, 1f);
		float diff = rand - average;
		return average + diff * diff * Mathf.Sign(diff);
	}
	public static float RandomAverage(float average, float min, float max)
	{
		float range = max - min;
		float clippedAverage = (average - min) / range;
		return min + RandomAverage(clippedAverage) * range;
	}

	public static Type[] GetExtendedTypes(Type fromType)
	{
		var types = Assembly.GetExecutingAssembly().GetTypes();
		List<Type> result = new List<Type>();
		foreach (var type in types) {
			if (fromType.IsAssignableFrom(type) && !type.IsAbstract) {
				result.Add(type);
			}
		}
		return result.ToArray();
	}

	public static object CreateInstance(Type type)
	{
		if (type == typeof(string))
			return "";

		var constructor = type.GetConstructor(Type.EmptyTypes);
		return constructor != null ? constructor.Invoke(new object[] { }) : FormatterServices.GetUninitializedObject(type);
	}
	public static Array CreateInstanceArray(Type type, int length)
	{
		var result = Array.CreateInstance(type, length);
		for (int index = 0; index < length; index++) {
			result.SetValue(CreateInstance(type), index);
		}
		return result;
	}

	private static string PrintObject(object obj, int depth)
	{
		if (obj == null) {
			return "'NULL'";
		} else {
			var objType = obj.GetType();
			var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

			if (objType.GetMethod("ToString", bindingFlags, null, Type.EmptyTypes, new ParameterModifier[0]) != null) {
				return obj.ToString();
			} else {
				var result = "";
				var tabs = "".PadRight(depth, '\t');

				if (obj is IEnumerable) {
					result += "[\n";
					var enumer = obj as IEnumerable;
					int ielement = 0;
					foreach (var element in enumer) {
						result += tabs + '\t' + ielement + ": " + PrintObject(element, depth + 1) + "\n";
						ielement++;
					}

					result += tabs + "]";
				} else {
					var fields = objType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					result += "{\n";

					foreach (var field in fields) {
						result += tabs + '\t' + field.Name + ": " + PrintObject(field.GetValue(obj), depth + 1) + "\n";
					}

					result += tabs + "}";
				}

				return result;
			}
		}
	}

	public static GameObject CreateGameObject(string name, Transform parent)
	{
		var result = new GameObject(name);
		result.transform.parent = parent;
		result.transform.localPosition = Vector3.zero;
		result.transform.localRotation = Quaternion.identity;
		return result;
	}

	public static GameObject Instantiate(GameObject go, Transform parent = null)
	{
		return Instantiate(go, parent, Vector3.zero, Quaternion.identity, go.transform.localScale);
	}
	public static GameObject Instantiate(GameObject go, Transform parent, Vector3 localPosition)
	{
		return Instantiate(go, parent, localPosition, Quaternion.identity, go.transform.localScale);
	}
	public static GameObject Instantiate(GameObject go, Transform parent, Vector3 localPosition, Quaternion localRotation)
	{
		return Instantiate(go, parent, localPosition, localRotation, go.transform.localScale);
	}
	public static GameObject Instantiate(GameObject go, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var result = (GameObject)GameObject.Instantiate(go);
		result.name = go.name;
		result.transform.parent = parent;
		result.transform.localPosition = localPosition;
		result.transform.localRotation = localRotation;
		result.transform.localScale = localScale;
		return result;
	}

	public static GameObject CreatePrimitive(PrimitiveType type, Transform parent = null)
	{
		return CreatePrimitive(type, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}
	public static GameObject CreatePrimitive(PrimitiveType type, Transform parent, Vector3 localPosition)
	{
		return CreatePrimitive(type, parent, localPosition, Quaternion.identity, Vector3.one);
	}
	public static GameObject CreatePrimitive(PrimitiveType type, Transform parent, Vector3 localPosition, Quaternion localRotation)
	{
		return CreatePrimitive(type, parent, localPosition, localRotation, Vector3.one);
	}
	public static GameObject CreatePrimitive(PrimitiveType type, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var result = GameObject.CreatePrimitive(type);
		result.transform.parent = parent;
		result.transform.localPosition = localPosition;
		result.transform.localRotation = localRotation;
		result.transform.localScale = localScale;
		return result;
	}

	public static bool Serialize(object obj, Stream stream, bool silent = false)
	{
		try {
			MemoryStream memStream = new MemoryStream();
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(memStream, obj);
			BinaryWriter writer = new BinaryWriter(stream);
			var length = (int)memStream.Length;
			writer.Write(length);
			writer.Write(memStream.GetBuffer(), 0, length);
			return true;
		} catch (Exception e) {
			if (!silent) {
				Log("Serialization error: " + e.ToString() + "\nType: " + obj.GetType().ToString());
			}
			return false;
		}
	}
	public static byte[] Serialize(object obj, bool silent = false)
	{
		try {
			MemoryStream memStream = new MemoryStream();
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(memStream, obj);
			return memStream.ToArray();
		} catch (Exception e) {
			if (!silent) {
				Log("Serialization error: " + e.ToString() + "\nType: " + obj.GetType().ToString());
			}
			return null;
		}

	}

	public static object Deserialize(Stream stream, bool silent = false)
	{
		try {
			BinaryReader reader = new BinaryReader(stream);
			int size = reader.ReadInt32();
			var bytes = reader.ReadBytes(size);
			return Deserialize(bytes, silent);
		} catch (Exception e) {
			if (!silent) {
				Log("Deserialization error: " + e.ToString());
			}
			return null;
		}
	}
	public static object Deserialize(byte[] bytes, bool silent = false)
	{
		try {
			MemoryStream memStream = new MemoryStream(bytes);
			var formatter = new BinaryFormatter();
			return formatter.Deserialize(memStream);
		} catch (Exception e) {
			if (!silent) {
				Log("Deserialization error: " + e.ToString());
			}
			return null;
		}
	}
	public static T Deserialize<T>(Stream stream, bool silent = false)
	{
		return (T)Deserialize(stream, silent);
	}
	public static T Deserialize<T>(byte[] bytes, bool silent = false)
	{
		return (T)Deserialize(bytes, silent);
	}

	public static float RangeSearch<T, K>(T subject, IList<K> list, IComparer<T, K> comparer = null)
	{
		if (comparer == null) {
			comparer = ComparerFactory<T, K>.Default;
		}

		int min = -1;
		int max = list.Count;
		while (true) {
			int range = max - min;
			if (range != 1) {
				int mean = min + (range >> 1);
				switch (comparer.Compare(subject, list[mean])) {
					case 0:
						return mean;
					case -1:
						max = mean;
						break;
					case 1:
						min = mean;
						break;
				}
			} else {
				return min + 0.5f;
			}
		}
	}

	public static void CloneDirectory(string sourceDirName, string destDirName)
	{
		// Get the subdirectories for the specified directory.
		DirectoryInfo dir = new DirectoryInfo(sourceDirName);
		DirectoryInfo[] dirs = dir.GetDirectories();

		if (!dir.Exists) {
			throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
		}

		// If the destination directory already exists, delete it. 
		if (Directory.Exists(destDirName)) {
			Directory.Delete(destDirName, true);
		}

		Directory.CreateDirectory(destDirName);

		// Get the files in the directory and copy them to the new location.
		FileInfo[] files = dir.GetFiles();
		foreach (FileInfo file in files) {
			string temppath = Path.Combine(destDirName, file.Name);
			file.CopyTo(temppath, false);
		}

		foreach (DirectoryInfo subdir in dirs) {
			string temppath = Path.Combine(destDirName, subdir.Name);
			CloneDirectory(subdir.FullName, temppath);
		}
	}

	public static void ExecuteCommand(string command)
	{
		System.Diagnostics.ProcessStartInfo processInfo;

		processInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + command);
		processInfo.CreateNoWindow = true;
		processInfo.UseShellExecute = false;

		System.Diagnostics.Process.Start(processInfo);
	}
	public static void ExecuteCommandAndWait(string command, out string outputStream, out string errorStream, out int exitCode)
	{
		System.Diagnostics.ProcessStartInfo processInfo;
		System.Diagnostics.Process process;

		processInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + command);
		processInfo.CreateNoWindow = true;
		processInfo.UseShellExecute = false;
		processInfo.RedirectStandardError = true;
		processInfo.RedirectStandardOutput = true;
		processInfo.StandardOutputEncoding = new System.Text.UnicodeEncoding();

		outputStream = null;
		errorStream = null;
		exitCode = -1;
		process = System.Diagnostics.Process.Start(processInfo);
		process.WaitForExit();

		outputStream = process.StandardOutput.ReadToEnd();
		errorStream = process.StandardError.ReadToEnd();
		exitCode = process.ExitCode;

		process.Close();
	}

	public static void SetLayerForAllChild(GameObject go, int layer)
	{
		foreach (Transform trans in go.GetComponentsInChildren<Transform>(true)) {
			trans.gameObject.layer = layer;
		}
	}

	#region
	public static Color[] COLORS = new Color[60] {
		new Color (0x1f / 255.0f, 0x77 / 255.0f, 0xb4 / 255.0f),
		new Color (0xae / 255.0f, 0xc7 / 255.0f, 0xe8 / 255.0f),
		new Color (0xff / 255.0f, 0x7f / 255.0f, 0x0e / 255.0f),
		new Color (0xff / 255.0f, 0xbb / 255.0f, 0x78 / 255.0f),
		new Color (0x2c / 255.0f, 0xa0 / 255.0f, 0x2c / 255.0f),
		new Color (0x98 / 255.0f, 0xdf / 255.0f, 0x8a / 255.0f),
		new Color (0xd6 / 255.0f, 0x27 / 255.0f, 0x28 / 255.0f),
		new Color (0xff / 255.0f, 0x98 / 255.0f, 0x96 / 255.0f),
		new Color (0x94 / 255.0f, 0x67 / 255.0f, 0xbd / 255.0f),
		new Color (0xc5 / 255.0f, 0xb0 / 255.0f, 0xd5 / 255.0f),
		new Color (0x8c / 255.0f, 0x56 / 255.0f, 0x4b / 255.0f),
		new Color (0xc4 / 255.0f, 0x9c / 255.0f, 0x94 / 255.0f),
		new Color (0xe3 / 255.0f, 0x77 / 255.0f, 0xc2 / 255.0f),
		new Color (0xf7 / 255.0f, 0xb6 / 255.0f, 0xd2 / 255.0f),
		new Color (0x7f / 255.0f, 0x7f / 255.0f, 0x7f / 255.0f),
		new Color (0xc7 / 255.0f, 0xc7 / 255.0f, 0xc7 / 255.0f),
		new Color (0xbc / 255.0f, 0xbd / 255.0f, 0x22 / 255.0f),
		new Color (0xdb / 255.0f, 0xdb / 255.0f, 0x8d / 255.0f),
		new Color (0x17 / 255.0f, 0xbe / 255.0f, 0xcf / 255.0f),
		new Color (0x9e / 255.0f, 0xda / 255.0f, 0xe5 / 255.0f),
		new Color (0x31 / 255.0f, 0x82 / 255.0f, 0xbd / 255.0f),
		new Color (0x6b / 255.0f, 0xae / 255.0f, 0xd6 / 255.0f),
		new Color (0x9e / 255.0f, 0xca / 255.0f, 0xe1 / 255.0f),
		new Color (0xc6 / 255.0f, 0xdb / 255.0f, 0xef / 255.0f),
		new Color (0xe6 / 255.0f, 0x55 / 255.0f, 0x0d / 255.0f),
		new Color (0xfd / 255.0f, 0x8d / 255.0f, 0x3c / 255.0f),
		new Color (0xfd / 255.0f, 0xae / 255.0f, 0x6b / 255.0f),
		new Color (0xfd / 255.0f, 0xd0 / 255.0f, 0xa2 / 255.0f),
		new Color (0x31 / 255.0f, 0xa3 / 255.0f, 0x54 / 255.0f),
		new Color (0x74 / 255.0f, 0xc4 / 255.0f, 0x76 / 255.0f),
		new Color (0xa1 / 255.0f, 0xd9 / 255.0f, 0x9b / 255.0f),
		new Color (0xc7 / 255.0f, 0xe9 / 255.0f, 0xc0 / 255.0f),
		new Color (0x75 / 255.0f, 0x6b / 255.0f, 0xb1 / 255.0f),
		new Color (0x9e / 255.0f, 0x9a / 255.0f, 0xc8 / 255.0f),
		new Color (0xbc / 255.0f, 0xbd / 255.0f, 0xdc / 255.0f),
		new Color (0xda / 255.0f, 0xda / 255.0f, 0xeb / 255.0f),
		new Color (0x63 / 255.0f, 0x63 / 255.0f, 0x63 / 255.0f),
		new Color (0x96 / 255.0f, 0x96 / 255.0f, 0x96 / 255.0f),
		new Color (0xbd / 255.0f, 0xbd / 255.0f, 0xbd / 255.0f),
		new Color (0xd9 / 255.0f, 0xd9 / 255.0f, 0xd9 / 255.0f),
		new Color (0x39 / 255.0f, 0x3b / 255.0f, 0x79 / 255.0f),
		new Color (0x52 / 255.0f, 0x54 / 255.0f, 0xa3 / 255.0f),
		new Color (0x6b / 255.0f, 0x6e / 255.0f, 0xcf / 255.0f),
		new Color (0x9c / 255.0f, 0x9e / 255.0f, 0xde / 255.0f),
		new Color (0x63 / 255.0f, 0x79 / 255.0f, 0x39 / 255.0f),
		new Color (0x8c / 255.0f, 0xa2 / 255.0f, 0x52 / 255.0f),
		new Color (0xb5 / 255.0f, 0xcf / 255.0f, 0x6b / 255.0f),
		new Color (0xce / 255.0f, 0xdb / 255.0f, 0x9c / 255.0f),
		new Color (0x8c / 255.0f, 0x6d / 255.0f, 0x31 / 255.0f),
		new Color (0xbd / 255.0f, 0x9e / 255.0f, 0x39 / 255.0f),
		new Color (0xe7 / 255.0f, 0xba / 255.0f, 0x52 / 255.0f),
		new Color (0xe7 / 255.0f, 0xcb / 255.0f, 0x94 / 255.0f),
		new Color (0x84 / 255.0f, 0x3c / 255.0f, 0x39 / 255.0f),
		new Color (0xad / 255.0f, 0x49 / 255.0f, 0x4a / 255.0f),
		new Color (0xd6 / 255.0f, 0x61 / 255.0f, 0x6b / 255.0f),
		new Color (0xe7 / 255.0f, 0x96 / 255.0f, 0x9c / 255.0f),
		new Color (0x7b / 255.0f, 0x41 / 255.0f, 0x73 / 255.0f),
		new Color (0xa5 / 255.0f, 0x51 / 255.0f, 0x94 / 255.0f),
		new Color (0xce / 255.0f, 0x6d / 255.0f, 0xbd / 255.0f),
		new Color (0xde / 255.0f, 0x9e / 255.0f, 0xd6 / 255.0f)
	};

	public static Color RandomColor()
	{
		return COLORS[UnityEngine.Random.Range(0, COLORS.Length)];
	}
	#endregion
}
