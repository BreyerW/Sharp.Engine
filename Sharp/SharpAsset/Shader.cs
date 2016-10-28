using System;
using System.IO;
using System.Collections.Generic;
using Sharp.Editor.Views;

namespace SharpAsset
{
	//This will hold our shader code in a nice clean class
	//this example only uses a shader with position and color
	//but didnt want to leave out the other bits for the shader
	//so you could practice writing a shader on your own :P
	public struct Shader: IAsset
	{
        internal static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

        public string Name{ get{return Path.GetFileNameWithoutExtension (FullPath);  } set{ }}
		public string Extension{ get{return Path.GetExtension (FullPath);  } set{ }}
		public string FullPath{ get; set;}

		public string VertexSource { get; set; }
		public string FragmentSource { get; set; }

		internal int VertexID;
		internal int FragmentID;

		internal int Program;

        internal Dictionary<UniformType, Dictionary<string, int>> uniformArray;//=new Dictionary<UniformType, Dictionary<string, int>> ();

        public bool allocated;
        public static Shader getAsset(string name) {
            var shader = shaders[name];
            if (!shader.allocated)
            {
                Console.WriteLine("shader allocation");
                SceneView.backendRenderer.GenerateBuffers(ref shader);
                SceneView.backendRenderer.Allocate(ref shader);
                shader.allocated=true;
                shaders[name] = shader;
            }
            return shader;
        }
		public override string ToString ()
		{
			return Name;
		}

		public void Dispose()
		{
			SceneView.backendRenderer.Delete(ref this);
		}
	}
	public enum UniformType{
		Int = 5124,
		UnsignedInt,
		Float,
		Double = 5130,
		FloatVec2 = 35664,
		FloatVec3,
		FloatVec4,
		IntVec2,
		IntVec3,
		IntVec4,
		Bool,
		BoolVec2,
		BoolVec3,
		BoolVec4,
		FloatMat2,
		FloatMat3,
		FloatMat4,
		Sampler1D,
		Sampler2D,
		Sampler3D,
		SamplerCube,
		Sampler1DShadow,
		Sampler2DShadow,
		Sampler2DRect,
		Sampler2DRectShadow,
		FloatMat2x3,
		FloatMat2x4,
		FloatMat3x2,
		FloatMat3x4,
		FloatMat4x2,
		FloatMat4x3,
		Sampler1DArray = 36288,
		Sampler2DArray,
		SamplerBuffer,
		Sampler1DArrayShadow,
		Sampler2DArrayShadow,
		SamplerCubeShadow,
		UnsignedIntVec2,
		UnsignedIntVec3,
		UnsignedIntVec4,
		IntSampler1D,
		IntSampler2D,
		IntSampler3D,
		IntSamplerCube,
		IntSampler2DRect,
		IntSampler1DArray,
		IntSampler2DArray,
		IntSamplerBuffer,
		UnsignedIntSampler1D,
		UnsignedIntSampler2D,
		UnsignedIntSampler3D,
		UnsignedIntSamplerCube,
		UnsignedIntSampler2DRect,
		UnsignedIntSampler1DArray,
		UnsignedIntSampler2DArray,
		UnsignedIntSamplerBuffer,
		DoubleVec2 = 36860,
		DoubleVec3,
		DoubleVec4,
		SamplerCubeMapArray = 36876,
		SamplerCubeMapArrayShadow,
		IntSamplerCubeMapArray,
		UnsignedIntSamplerCubeMapArray,
		Image1D = 36940,
		Image2D,
		Image3D,
		Image2DRect,
		ImageCube,
		ImageBuffer,
		Image1DArray,
		Image2DArray,
		ImageCubeMapArray,
		Image2DMultisample,
		Image2DMultisampleArray,
		IntImage1D,
		IntImage2D,
		IntImage3D,
		IntImage2DRect,
		IntImageCube,
		IntImageBuffer,
		IntImage1DArray,
		IntImage2DArray,
		IntImageCubeMapArray,
		IntImage2DMultisample,
		IntImage2DMultisampleArray,
		UnsignedIntImage1D,
		UnsignedIntImage2D,
		UnsignedIntImage3D,
		UnsignedIntImage2DRect,
		UnsignedIntImageCube,
		UnsignedIntImageBuffer,
		UnsignedIntImage1DArray,
		UnsignedIntImage2DArray,
		UnsignedIntImageCubeMapArray,
		UnsignedIntImage2DMultisample,
		UnsignedIntImage2DMultisampleArray,
		Sampler2DMultisample = 37128,
		IntSampler2DMultisample,
		UnsignedIntSampler2DMultisample,
		Sampler2DMultisampleArray,
		IntSampler2DMultisampleArray,
		UnsignedIntSampler2DMultisampleArray,
		UnsignedIntAtomicCounter = 37595
	}
}

