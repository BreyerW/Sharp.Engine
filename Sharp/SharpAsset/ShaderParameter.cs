using Sharp;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpAsset

{
	internal interface IParameter
	{
		ref byte DataAddress { get; }

		void ConsumeData(int location);
	}

	[Serializable]
	internal class Matrix4Parameter : IParameter
	{
		public Matrix4x4 data;

		public ref byte DataAddress { get { return ref Unsafe.As<Matrix4x4, byte>(ref data); } }

		public void ConsumeData(int location)
		{
			SendToGPU(location, ref data.M11);
		}

		public static void SendToGPU(int location, ref float address)
		{
			MainWindow.backendRenderer.SendMatrix4(location, ref address/*, Slot*/);
		}
	}

	[Serializable]
	internal class Texture2DParameter : IParameter //zalatwiaj primitivy tutaj
	{
		//private int Slot;

		public int data;

		public ref byte DataAddress { get { return ref Unsafe.As<int, byte>(ref data); } }

		public void ConsumeData(int location)
		{
			SendToGPU(data, location);
		}

		public static void SendToGPU(int tbo, int location)
		{
			MainWindow.backendRenderer.SendTexture2D(location, tbo/*, Slot*/);
		}
	}
}

/*
 *
 using OpenTK;
using Sharp;
using SharpAsset.Pipeline;
using System.Runtime.CompilerServices;

namespace SharpAsset

{
    public delegate ref T GetData<T>();

    internal interface IParameter
    {
        void ConsumeData(int location);
    }

    internal struct Matrix4Parameter : IParameter
    {
        public Matrix4 data;

        public Matrix4Parameter(ref Matrix4 data)
        {
            this.data = data;
        }

        public void ConsumeData(int location)
        {
            MainWindow.backendRenderer.SendMatrix4(location, ref data.Row0.X);
        }
    }

    internal struct Texture2DParameter : IParameter //zalatwiaj primitivy tutaj
    {
        //private int Slot;

        public int data;

        public Texture2DParameter(ref Texture data/*, int slot*)
        {
            //Slot = slot;
            this.data = TexturePipeline.nameToKey.IndexOf(data.Name);
        }

public void ConsumeData(int location)
{
    MainWindow.backendRenderer.SendTexture2D(location, ref Pipeline.Pipeline.GetPipeline<TexturePipeline>().GetAsset(data).TBO/*, Slot*);
}
    }
}

    public override void ConsumeData()
        {
            for(int i=0; i<data.Length; i++)
            SceneView.backendRenderer.Send(ref location, ref data[i],i);
        }
 */