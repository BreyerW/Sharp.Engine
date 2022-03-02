using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Core.Editor
{
	internal enum DirtyState { 
	None,
	OneOrMore,
	All
	}

	public class Editor
	{
		internal static List<IEngineObject> dirtyObjects = new ();
		internal static DirtyState isObjectsDirty = DirtyState.None;
		public static void MakeObjectsDirty<T>(IEnumerable<T> objs) where T: IEngineObject
		{
			isObjectsDirty = DirtyState.OneOrMore;
		}
		public static void MakeObjectsDirty()
		{
			isObjectsDirty =DirtyState.All;

		}
		public static void MakeObjectDirty<T>(in T objs) where T : IEngineObject
		{
			isObjectsDirty = DirtyState.OneOrMore;
		}
	}
}
