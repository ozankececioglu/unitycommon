using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Extensions;

public interface IComparer<T, K>
{
	int Compare (T t, K k);
}

public class ComparerFactory<T, K>
{
	static IComparer<T, K> defaultComparer;
	
	public static IComparer<T, K> Default {
		get { return defaultComparer; }
	}
	
	static ComparerFactory ()
	{
		if (typeof(IComparable<K>).IsAssignableFrom (typeof(T))) {
			defaultComparer = (IComparer<T, K>)Activator.CreateInstance (typeof(ForwardComparer<,>).MakeGenericType (new Type[] {typeof(T), typeof(K)}));
		} else if (typeof(IComparable<T>).IsAssignableFrom (typeof(K))) {
			defaultComparer = (IComparer<T, K>)Activator.CreateInstance (typeof(InverseComparer<,>).MakeGenericType (new Type[] {typeof(T), typeof(K)}));
		} else {
			defaultComparer = new ComparerFactory<T, K>.DefaultComparer ();
		}
	}

	private class ForwardComparer<T1, K1> : IComparer<T1, K1> where T1 : IComparable<K1>
	{
		public int Compare (T1 t, K1 k)
		{
			return t.CompareTo (k);
		}
	}

	private class InverseComparer<T1, K1> : IComparer<T1, K1> where K1 : IComparable<T1>
	{
		public int Compare (T1 t, K1 k)
		{
			var result = k.CompareTo (t);
			return result < 0 ? 1 : (result > 0 ? -1 : 0);
		}
	}

	private class DefaultComparer : IComparer<T, K>
	{
		public int Compare (T x, K y)
		{
			if (x == null) {
				return (y != null) ? -1 : 0;
			} else if (y == null) {
				return 1;
			} else if (x is IComparable) {
				return ((IComparable)((object)x)).CompareTo (y);
			} else if (y is IComparable) {
				var result = ((IComparable)((object)y)).CompareTo (x);
				return result < 0 ? 1 : (result > 0 ? -1 : 0);
			} else {
				throw new ArgumentException ("does not implement right interface");
			}
		}
	}
}
