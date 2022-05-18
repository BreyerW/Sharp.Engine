using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Editor
{
	internal enum DirtyState { 
	None,
	OneOrMore,
	All
	}

	public class Editor
	{
		internal static HashSet<IEngineObject> dirtyObjects = new ();
		internal static DirtyState isObjectsDirty = DirtyState.None;
		public static void MakeObjectsDirty<T>(IEnumerable<T> objs) where T: IEngineObject
		{
			if (isObjectsDirty is not DirtyState.All)
			{
				isObjectsDirty = DirtyState.OneOrMore;
				foreach (var obj in objs)
				{
					//if(dirtyObjects.Contains(obj) is false)
					dirtyObjects.Add(obj);
				}
			}
		}
		public static void MakeObjectsDirty()
		{
			isObjectsDirty =DirtyState.All;
			dirtyObjects.Clear();
		}
		public static void MakeObjectDirty<T>(in T objs) where T : IEngineObject
		{
			if (isObjectsDirty is not DirtyState.All) {

				isObjectsDirty = DirtyState.OneOrMore;
				//if (dirtyObjects.Contains(objs) is false)
					dirtyObjects.Add(objs);
					}
		}
	}
}
