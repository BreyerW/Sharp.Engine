using System;
using System.Threading;
using Sharp.Editor.Views;

namespace Sharp
{
	public static class Extension
	{
		public static Guid GetInstanceID(this object obj)
		{
			if (!ThreadsafeReferenceResolver.objToGuidMapping.TryGetValue(obj, out var id))
				ThreadsafeReferenceResolver.objToGuidMapping.Add(obj, id = Guid.NewGuid().ToString());
			return new Guid(id);
		}

		public static T GetInstanceObject<T>(in this Guid id) where T : class
		{
			SceneView.entities.allEngineObjects.TryGetValue(id, out var obj);
			return obj as T;
		}
	}

	public static class Utils
	{
		public static void Swap<T>(ref T v1, ref T v2) where T : class
		{
			var prev = Interlocked.Exchange(ref v1, v2);
			Interlocked.Exchange(ref v2, prev);
		}
	}
}