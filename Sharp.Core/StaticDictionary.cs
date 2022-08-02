
namespace Sharp;
public static class StaticDictionary<TTypeKey>
{
	private static class TypeStore<TKey, TTypeValue>
	{
		internal static TTypeValue Value = default!;
	}
	public static ref TTypeValue Get<TTypeValue>() => ref TypeStore<TTypeKey, TTypeValue>.Value;

	//public static void Set<TTypeValue>(in TTypeValue value) => TypeStore<TTypeKey, TTypeValue>.Value = value;
}