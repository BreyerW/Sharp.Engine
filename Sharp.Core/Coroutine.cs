using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp
{
	//use linkedlist for appropiate order of coroutines
	/// </summary>
	// TODO: maybe AdvanceInstructions shouldnt rely on generic instead rely on type queue allowing insterting custom instructions without dedicated slot for them. it would require calling AdvanceInstruction per new entry in queue
	public static class Coroutine
	{
		internal static Queue<IEnumerator> customInstructions = new Queue<IEnumerator>();
		internal static Queue<IEnumerator> endOfFrameInstructions = new Queue<IEnumerator>();
		internal static Queue<IEnumerator> startOfFrameInstructions = new Queue<IEnumerator>();
		internal static Queue<IEnumerator> timeInstructions = new Queue<IEnumerator>();
		internal static Queue<IEnumerator> makeCurrentInstructions = new Queue<IEnumerator>();
		private static LinkedList<Queue<IEnumerable>> instructionList;

		internal static void InitializeCoroutineQueue(params Type[] types)
		{

		}
		//TODO: optimize this
		internal static void AdvanceInstructions<T>() where T : IEnumerator
		{
			Queue<IEnumerator> instructions;
			if (typeof(T) == typeof(WaitForEndOfFrame))
			{
				instructions = endOfFrameInstructions;
				//endOfFrameInstructions.Clear();
			}
			else if (typeof(T) == typeof(WaitForStartOfFrame))
			{
				instructions = startOfFrameInstructions;
				//startOfFrameInstructions.Clear();
			}
			else if (typeof(T) == typeof(WaitForSeconds) || typeof(T) == typeof(WaitForSecondsScaled))
			{
				instructions = timeInstructions;
				//timeInstructions.Clear();
			}
			else if (typeof(T) == typeof(WaitForMakeCurrent))
			{
				instructions = makeCurrentInstructions;
				//makeCurrentInstructions.Clear();
			}
			else
			{
				instructions = customInstructions;
				//customInstructions.Clear();
			}
			var len = instructions.Count;
			var i = 0;
			while (i < len)
			{
				var coroutine = instructions.Dequeue();
				if (coroutine.MoveNext() is false)
				{
					i++;
					continue;
				}
				if (coroutine.Current is WaitForStartOfFrame)
					startOfFrameInstructions.Enqueue(coroutine);
				else if (coroutine.Current is WaitForEndOfFrame)
					endOfFrameInstructions.Enqueue(coroutine);
				else if (coroutine.Current is WaitForSeconds)
					timeInstructions.Enqueue(coroutine);
				else if (coroutine.Current is WaitForMakeCurrent)
					makeCurrentInstructions.Enqueue(coroutine);
				else
					customInstructions.Enqueue(coroutine);
				i++;
			}
		}

		public static void Start(IEnumerator instruction)
		{
			customInstructions.Enqueue(instruction);
		}

	}
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
