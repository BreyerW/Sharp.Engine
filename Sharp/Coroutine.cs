using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp
{
	public static class Coroutine
	{
		internal static List<IEnumerator> customInstructions = new List<IEnumerator>();
		internal static List<WaitForEndOfFrame> endOfFrameInstructions = new List<WaitForEndOfFrame>();
		internal static List<WaitForStartOfFrame> startOfFrameInstructions = new List<WaitForStartOfFrame>();
		internal static List<WaitForSeconds> timeInstructions = new List<WaitForSeconds>();

		internal static void AdvanceInstructions<T>(ref List<T> instructions) where T : IEnumerator
		{
			var runNextFrame = new List<T>();
			foreach (var coroutine in instructions)
			{
				var prev = coroutine.Current.GetType();
				if (coroutine.MoveNext() is false)
					continue;
				else if (coroutine.Current.GetType() == prev)
					runNextFrame.Add(coroutine);
				else if (coroutine.Current is WaitForStartOfFrame waitStart)
					startOfFrameInstructions.Add(waitStart);
				else if (coroutine.Current is WaitForEndOfFrame waitEnd)
					endOfFrameInstructions.Add(waitEnd);
				else if (coroutine.Current is WaitForSeconds waitSeconds)
					timeInstructions.Add(waitSeconds);
				else
					customInstructions.Add(coroutine);
			}
			instructions = runNextFrame;
		}

		public static void Start(IEnumerator instruction)
		{
			customInstructions.Add(instruction);
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
