using Sharp.Editor.Views;
using OpenTK;

namespace SharpAsset
{
    public delegate ref T GetData<T>();
    internal interface IParameter
    {
        void ConsumeData(int location);
    }
    internal struct Matrix4Parameter : IParameter//attribute ShaderParameter generic bind can find all parameters 
    {
        public GetData<Matrix4> data;

        public Matrix4Parameter(GetData<Matrix4> getData)
        {
            data = getData;
        }
        public void ConsumeData(int location)
        {
            SceneView.backendRenderer.Send(ref location, ref data());
        }
    }
    internal struct Texture2DParameter : IParameter
    {
        //zlypomysl bo zuzyje wyzsze sloty dla kazdego nastepnego obiektu
        private int Slot;

        public GetData<Texture> data;
        public Texture2DParameter(GetData<Texture> getData, int slot)
        {
            Slot = slot;
            data = getData;
        }
        public void ConsumeData(int location)
        {
            SceneView.backendRenderer.Send(ref location, ref data().TBO, Slot);
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
