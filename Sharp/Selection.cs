using System;
using Sharp;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Antmicro.Migrant;

namespace Sharp
{
	public static class Selection
	{
		private static Stack<object> assets = new Stack<object>();
		private static Serializer serializer = new Serializer();
		private static MD5 md5 = MD5.Create();//maybe sha instead
		internal static string lastHash;

		public static object Asset
		{
			set
			{
				assets.Push(value);
				OnSelectionChange?.Invoke(value, EventArgs.Empty);
			}
			get
			{
				if (assets.Count == 0)
					return null;
				return assets.Peek();
			}
		}
		public static EventHandler OnSelectionChange;
		public static EventHandler OnSelectionDirty;
		public static bool isDragging = false;
		public static bool IsDirty = false;

		public static void IsSelectionDirty()
		{

			if (Asset == null) return;

			var memStream = new MemoryStream();
			serializer.Serialize(Asset, memStream);
			var byteHash = md5.ComputeHash(memStream.ToArray());

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < byteHash.Length; i++)
			{
				sb.Append(byteHash[i].ToString("X2"));
			}
			var currentHash = sb.ToString();
			if (currentHash != lastHash)
			{
				lastHash = currentHash;
				OnSelectionDirty?.Invoke(Asset, EventArgs.Empty);
				//Console.WriteLine("GUI dirty");//powoduje problemy
			}
			//else IsDirty = false;

		}
	}
}

