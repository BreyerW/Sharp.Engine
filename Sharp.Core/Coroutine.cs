using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sharp
{
	/// </summary>
	// TODO: maybe AdvanceInstructions shouldnt rely on generic instead rely on type queue allowing insterting custom instructions without dedicated slot for them. it would require calling AdvanceInstruction per new entry in queue
	public static class Coroutine
	{
		//TODO: maybe add Ienumertor slot where unknown ienumerators go and make AdvanceInstructions<Ienumerator> call somewhere
		private static Dictionary<Type, Queue<IEnumerator>> typeToInstructionsMapping = new();

		[ModuleInitializer]
		internal static void RegisterWaitFors()
		{
			Coroutine.RegisterEnumerator<WaitForStartOfFrame>();
			Coroutine.RegisterEnumerator<WaitForSeconds>();
			Coroutine.RegisterEnumerator<WaitForSecondsScaled>();
			Coroutine.RegisterEnumerator<WaitForEndOfFrame>();
			Coroutine.RegisterEnumerator<WaitForMakeCurrent>();
			Coroutine.RegisterEnumerator<WaitForUndoRedo>();
		}

		public static void RegisterEnumerator<T>() where T : IEnumerator
		{
			typeToInstructionsMapping.Add(typeof(T), new Queue<IEnumerator>());
		}
		internal static void AdvanceInstructions<T>() where T : IEnumerator
		{
			Queue<IEnumerator> instructions = typeToInstructionsMapping[typeof(T)];
			var len = instructions.Count;
			var i = 0;
			while (i < len)
			{
				i++;
				var coroutine = instructions.Dequeue();

				if (coroutine.MoveNext() is false) continue;

				typeToInstructionsMapping[coroutine.Current.GetType()].Enqueue(coroutine);
			}
		}

		public static void Start<T>(T instruction) where T : IEnumerator
		{
			if (instruction.MoveNext() is false)
				return;
			typeToInstructionsMapping[instruction.Current.GetType()].Enqueue(instruction);
		}

	}
	/*public interface IWaitFor : IEnumerator
	{

	}*/
	//TODO: maybe add WaitForMeshLoading, WaitForShaderLoading etc.
	public class WaitForEndOfFrame : IEnumerator
	{
		public object Current => null;

		public virtual bool MoveNext()
		{
			return true;
		}

		public void Reset()
		{
		}
	}
	public class WaitForMakeCurrent : IEnumerator
	{
		public object Current => null;

		public virtual bool MoveNext()
		{
			return true;
		}

		public void Reset()
		{
		}
	}
	public class WaitForUndoRedo : IEnumerator
	{
		public object Current => null;

		public bool MoveNext()
		{
			return true;
		}

		public void Reset()
		{
		}
	}

	public class WaitForStartOfFrame : IEnumerator
	{
		public object Current => null;

		public virtual bool MoveNext()
		{
			return true;
		}

		public void Reset()
		{
		}
	}
	public class WaitForSeconds : IEnumerator
	{
		protected double seconds;
		public object Current => null;

		public WaitForSeconds(double seconds)
		{
			this.seconds = seconds;
		}
		public virtual bool MoveNext()
		{
			seconds -= Time.deltaTime;
			return !(seconds <= 0);
		}

		public void Reset()
		{
		}
	}
	public class WaitForSecondsScaled : WaitForSeconds
	{
		public WaitForSecondsScaled(double seconds) : base(seconds)
		{

		}
		public override bool MoveNext()
		{
			seconds -= Time.deltaTime;//add scale
			return !(seconds <= 0);
		}
	}
}
