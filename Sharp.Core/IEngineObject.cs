using System;

namespace Sharp
{
	public interface IEngineObject/*:IDisposable*/
	{
		//TODO: Available when DIM ship or as method GetInstanceId()
		/*
		 public Guid Id{
		 get{
		 this.GetInstanceId();
			}
			}
			 */
		void Destroy();
	}
}