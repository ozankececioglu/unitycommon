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

public class SafeSerializer : ISurrogateSelector, ISerializationSurrogate
{
	class SerializableRep
	{
		public Dictionary<string, FieldRep> names;
		public FieldRep[] fields;
		public MethodInfo didDeserialize;

		public SerializableRep(FieldRep[] afields)
		{
			names = new Dictionary<string, FieldRep>();
			fields = afields;
			foreach (var field in fields) {
				foreach (var name in field.names) {
					if (!names.ContainsKey(name)) {
						names.Add(name, field);
					} else {
						Common.LogWarning("SafeSerializer.SerializableRep has already name {0}, skipping", name);
					}
				}
			}
		}
	}

	class FieldRep
	{
		public string[] names;
		public FieldInfo field;
	}

	public class Serializable : Attribute { }

	public class Replace : Attribute
	{
		public string[] names;

		public Replace(params string[] anames) { names = anames; }
	}

	static Dictionary<Type, SerializableRep> types = new Dictionary<Type, SerializableRep>();
	static SafeSerializer instance;
	static BinaryFormatter formatter;

	static SafeSerializer()
	{
		instance = new SafeSerializer();
		formatter = new BinaryFormatter();
		formatter.SurrogateSelector = instance;
	}
	static SerializableRep Get(Type type, bool forced = false)
	{
		SerializableRep result;
		if (!types.TryGetValue(type, out result)) {
			if (!forced && type.GetCustomAttributes(typeof(Serializable), true).FirstOrDefault() == null) {
				return null;
			}

			result = new SerializableRep(type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Select(field => {
				if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).FirstOrDefault() != null) {
					return null;
				}
				var info = field.GetCustomAttributes(typeof(Replace), true).FirstOrDefault() as Replace;
				if (info != null && info.names != null && info.names.Length > 0) {
					return new FieldRep { names = new string[] { field.Name }.Concat(info.names).ToArray(), field = field };
				} else {
					return new FieldRep { names = new string[] { field.Name }, field = field };
				}
			}).Where(rep => rep != null).ToArray());
			var methodInfo = type.GetMethod("DidDeserialized", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (methodInfo != null && methodInfo.GetParameters().Length == 1 && methodInfo.GetParameters()[0].ParameterType == typeof(Dictionary<string, object>)) {
				result.didDeserialize = methodInfo;
			} else {
				result.didDeserialize = null;
			}
			types.Add(type, result);
		}
		return result;
	}
	public static void Recognize(Type type)
	{
		Get(type, true);
	}

	public static bool Serialize(object obj, Stream stream, bool silent = false)
	{
		var bytes = Serialize(obj, silent);
		if (bytes != null) {
			BinaryWriter writer = new BinaryWriter(stream);
			writer.Write(bytes.Length);
			writer.Write(bytes, 0, bytes.Length);
			return true;
		} else {
			return false;
		}
	}
	public static byte[] Serialize(object obj, bool silent = false)
	{
		try {
			MemoryStream memStream = new MemoryStream();
			formatter.Serialize(memStream, obj);
			return memStream.ToArray();
		} catch (Exception e) {
			UnityEngine.Debug.LogError(e.ToString());
			if (!silent) {
				UnityEngine.Debug.LogWarning("Serialization error: " + e.ToString() + "\nType: " + obj.GetType().ToString());
			}
			return null;
		}
	}
	public static object Deserialize(Stream stream, SerializationBinder binder = null, bool silent = false)
	{
		try {
			BinaryReader reader = new BinaryReader(stream);
			int size = reader.ReadInt32();
			var bytes = reader.ReadBytes(size);
			return Deserialize(bytes, binder, silent);
		} catch (Exception e) {
			if (!silent) {
				UnityEngine.Debug.LogWarning("Deserialization error: " + e.ToString());
			}
			return null;
		}
	}
	public static object Deserialize(byte[] bytes, SerializationBinder binder = null, bool silent = false)
	{
		try {
			MemoryStream memStream = new MemoryStream(bytes);
			formatter.Binder = binder;
			return formatter.Deserialize(memStream);
		} catch (Exception e) {
			if (!silent) {
				UnityEngine.Debug.LogWarning("Deserialization error: " + e.ToString());
			}
			return null;
		}
	}
	public static T Deserialize<T>(Stream stream, SerializationBinder binder = null, bool silent = false)
	{
		return (T)Deserialize(stream, binder, silent);
	}
	public static T Deserialize<T>(byte[] bytes, SerializationBinder binder = null, bool silent = false)
	{
		return (T)Deserialize(bytes, binder, silent);
	}

	public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
	{
		var rep = Get(obj.GetType());
		if (rep == null) {
			Common.LogWarning("SafeSerializer.GetObjectData is called with a obj of type {0} which isnt recognized", obj.GetType());
			return;
		}

		foreach (var field in rep.fields) {
			info.AddValue(field.names[0], field.field.GetValue(obj), field.field.FieldType);
		}
	}
	public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
	{
		var rep = Get(obj.GetType());
		if (rep == null) {
			Common.LogWarning("SafeSerializer.SetObjectData is called with a obj of type {0} which isnt recognized", obj.GetType());
			return null;
		}

		var missed = new Dictionary<string, object>();
		var enumer = info.GetEnumerator();
		while (enumer.MoveNext()) {
			var entry = enumer.Current;
			FieldRep field;
			if (rep.names.TryGetValue(entry.Name, out field)) {
				try {
					field.field.SetValue(obj, entry.Value);
				} catch (Exception e) {
					Common.LogWarning("SafeSerializer.SetObjectData cant assign field {0} of type {1}: {2}", field.field.Name, obj.GetType(), e.ToString());
				}
			} else {
				missed.Add(entry.Name, entry.Value);
			}
		}

		if (rep.didDeserialize != null) {
			rep.didDeserialize.Invoke(obj, new object[] { missed });
		}

		return obj;
	}

	ISurrogateSelector chain = null;
	public void ChainSelector(ISurrogateSelector selector) { chain = selector; }
	public ISurrogateSelector GetNextSelector() { return chain; }
	public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
	{
		if (Get(type) == null) {
			selector = null;
			return null;
		} else {
			selector = this;
			return this;
		}
	}
}