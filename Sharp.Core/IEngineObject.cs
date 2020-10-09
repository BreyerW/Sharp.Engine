using Newtonsoft.Json;
using System;

namespace Sharp
{
	public interface IEngineObject : IDisposable, IEquatable<IEngineObject>
	{
		/*[JsonProperty]
		public Guid Id
		{
			//internal set => Extension.objectToIdMapping.Add(this, value);
			get
			{

				if (Extension.objectToIdMapping.TryGetValue(this, out var id))
					return id;
				else
				{
					id = Guid.NewGuid();
					Extension.objectToIdMapping[this] = id;
					return id;
				}
			}
		}*/
		void IDisposable.Dispose() => Extension.objectToIdMapping.Remove(this);
		bool IEquatable<IEngineObject>.Equals(IEngineObject other){
			return this == other;
}
	}
}