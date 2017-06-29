using OpenTK;
using Sharp;
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
        public GetData<Matrix4> data;

        public Matrix4Parameter(GetData<Matrix4> getData)
        {
            data = getData;
        }

        public void ConsumeData(int location)
        {
            MainWindow.backendRenderer.SendMatrix4(location, ref data().Row0.X);
        }
    }

    internal struct Texture2DParameter : IParameter //zalatwiaj primitivy tutaj
    {
        //private int Slot;

        public GetData<Texture> data;

        public Texture2DParameter(GetData<Texture> getData/*, int slot*/)
        {
            //Slot = slot;
            data = getData;
        }

        public void ConsumeData(int location)
        {
            MainWindow.backendRenderer.SendTexture2D(location, ref data().TBO/*, Slot*/);
        }
    }
}

/*
 * public override void ConsumeData()
        {
            for(int i=0; i<data.Length; i++)
            SceneView.backendRenderer.Send(ref location, ref data[i],i);
        }
 */