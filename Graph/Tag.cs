using UnityEngine;
using System.Collections;

namespace ArSim
{
	[System.Serializable]
	public struct Tag
	{
		public uint id;
	
		public Tag (uint aid)
		{
			id = aid;
		}
	
		public static implicit operator uint (Tag atag)
		{
			return atag.id;
		}
	
		public static implicit operator Tag (uint aid)
		{
			return new Tag (aid);
		}
	}

	[System.Serializable]
	public struct TagSet
	{
		uint ids;
	
		public TagSet (uint aids = 0)
		{
			ids = aids;
		}
	
		public bool Contains (Tag atag)
		{
			return (ids & atag.id) != 0u;
		}
	
		public void Add (Tag atag)
		{
			ids = ids | atag.id;
		}
	
		public void Remove (Tag atag)
		{
			ids = ids & ~atag.id;
		}
	}

}