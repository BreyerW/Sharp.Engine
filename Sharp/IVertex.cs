using System;
using OpenTK.Graphics;

namespace  Sharp
{
	public interface IVertex
	{
		void RegisterAttributes<T>() where T:struct, IConvertible;
		//IntPtr CalculateIntPtr ();
	}
}

