﻿//MIT, 2014-2017, WinterDev

using System;
namespace OpenTK.Graphics.ES20
{
    public struct ShaderVtxAttrib2f
    {
        readonly int _location;
        public ShaderVtxAttrib2f(int location)
        {
            _location = location;
        }
        public unsafe void UnsafeLoadMixedV2f(float* vertexH, int totalFieldCount)
        {
            GL.VertexAttribPointer(_location, 2, VertexAttribPointerType.Float, false, totalFieldCount * sizeof(float), (IntPtr)vertexH);
            GL.EnableVertexAttribArray(_location);
        }
        /// <summary>
        /// load pure vector2f, from start array
        /// </summary>
        /// <param name="vertices"></param>
        public void LoadPureV2f(float[] vertices)
        {
            //bind 
            GL.VertexAttribPointer(_location,
                2, //float2
                VertexAttribPointerType.Float,
                false,
                2 * sizeof(float), //total size
                vertices);
            GL.EnableVertexAttribArray(_location);
        }
        /// <summary>
        /// load pure vector2f, from start array
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="totalFieldCount"></param>
        /// <param name="startOffset"></param>
        public unsafe void UnsafeLoadPureV2f(float* vertices)
        {
            //bind 
            GL.VertexAttribPointer(_location,
                2, //float2
                VertexAttribPointerType.Float,
                false,
                 2 * sizeof(float), //total size
                (IntPtr)(vertices));
            GL.EnableVertexAttribArray(_location);
        }
    }
    public struct ShaderVtxAttrib3f
    {
        readonly int _location;
        public ShaderVtxAttrib3f(int location)
        {
            _location = location;
        }
        public unsafe void UnsafeLoadMixedV3f(float* vertexH, int totalFieldCount)
        {
            GL.VertexAttribPointer(_location, 3, VertexAttribPointerType.Float, false, totalFieldCount * sizeof(float), (IntPtr)vertexH);
            GL.EnableVertexAttribArray(_location);
        }
        public void LoadPureV3f(float[] vertices)
        {
            //bind 
            GL.VertexAttribPointer(_location,
                3, //float3
                VertexAttribPointerType.Float,
                false,
                3 * sizeof(float), //total size
                vertices);
            GL.EnableVertexAttribArray(_location);
        }
    }
    public struct ShaderVtxAttrib4f
    {
        readonly int _location;
        public ShaderVtxAttrib4f(int location)
        {
            _location = location;
        }
        /// <summary>
        ///  load pure vector4f, from start array
        /// </summary>
        /// <param name="vertices"></param>
        public void LoadPureV4f(float[] vertices)
        {
            unsafe
            {
                fixed (float* h = &vertices[0])
                {
                    GL.VertexAttribPointer(_location,
                        4, //float4
                        VertexAttribPointerType.Float,
                        false,
                        4 * sizeof(float), //total size
                        (IntPtr)h);
                }
            }
            GL.EnableVertexAttribArray(_location);
        }
    }


    public struct ShaderUniformMatrix4
    {
        readonly int _location;
        public ShaderUniformMatrix4(int location)
        {
            _location = location;
        }
        public void SetData(int count, bool transpose, float[] mat)
        {
            GL.UniformMatrix4(_location, count, transpose, mat);
        }
        public void SetData(float[] mat)
        {
            GL.UniformMatrix4(_location, 1, false, mat);
        }
    }
    public struct ShaderUniformMatrix3
    {
        readonly int _location;
        public ShaderUniformMatrix3(int location)
        {
            _location = location;
        }
        public void SetData(int count, bool transpose, float[] mat)
        {
            GL.UniformMatrix3(_location, count, transpose, mat);
        }
        public void SetData(float[] mat)
        {
            GL.UniformMatrix3(_location, 1, false, mat);
        }
    }
    public struct ShaderUniformVar1
    {
        readonly int _location;
        public ShaderUniformVar1(int location)
        {
            _location = location;
        }
        public void SetValue(float value)
        {
            GL.Uniform1(_location, value);
        }
        public void SetValue(int value)
        {
            GL.Uniform1(_location, value);
        }
        public void SetValue(bool value)
        {
            GL.Uniform1(_location, value ? 1 : 0);
        }
    }
    public struct ShaderUniformVar2
    {
        readonly int _location;
        public ShaderUniformVar2(int location)
        {
            _location = location;
        }
        public void SetValue(int a, int b)
        {
            GL.Uniform2(_location, a, b);
        }
        public void SetValue(float a, float b)
        {
            GL.Uniform2(_location, a, b);
        }
        public void SetValue(byte a, byte b)
        {
            GL.Uniform2(_location, a, b);
        }
    }
    public struct ShaderUniformVar3
    {
        readonly int _location;
        public ShaderUniformVar3(int location)
        {
            _location = location;
        }
        public void SetValue(int a, int b, int c)
        {
            GL.Uniform3(_location, a, b, c);
        }
        public void SetValue(float a, float b, float c)
        {
            GL.Uniform3(_location, a, b, c);
        }
        public void SetValue(byte a, byte b, byte c)
        {
            GL.Uniform3(_location, a, b, c);
        }
    }
    public struct ShaderUniformVar4
    {
        readonly int _location;
        public ShaderUniformVar4(int location)
        {
            _location = location;
        }
        public void SetValue(int a, int b, int c, int d)
        {
            GL.Uniform4(_location, a, b, c, d);
        }
        public void SetValue(float a, float b, float c, float d)
        {
            GL.Uniform4(_location, a, b, c, d);
        }
        public void SetValue(byte a, byte b, byte c, byte d)
        {
            GL.Uniform4(_location, a, b, c, d);
        }
    }


    public class MiniShaderProgram
    {
        int _mProgram;
        string _vs;
        string _fs;
        public void LoadVertexShaderSource(string vs)
        {
            _vs = vs;
        }
        public void LoadFragmentShaderSource(string fs)
        {
            _fs = fs;
        }
        public void DeleteMe()
        {
            GL.DeleteProgram(_mProgram);
            _mProgram = 0;
        }
        public bool Build()
        {
            _mProgram = OpenTK.Graphics.ES20.ES2Utils.CompileProgram(_vs, _fs);
            return _mProgram != 0;
        }
        public bool Build(string vs, string fs)
        {
            LoadVertexShaderSource(vs);
            LoadFragmentShaderSource(fs);
            try
            {
                _mProgram = OpenTK.Graphics.ES20.ES2Utils.CompileProgram(vs, fs);
            }
            catch (Exception ex)
            {
            }
            if (_mProgram == 0)
            {
                return false;
            }
            return true;
        }
        public ShaderVtxAttrib2f GetAttrV2f(string attrName)
        {
            return new ShaderVtxAttrib2f(GL.GetAttribLocation(_mProgram, attrName));
        }
        public ShaderVtxAttrib3f GetAttrV3f(string attrName)
        {
            return new ShaderVtxAttrib3f(GL.GetAttribLocation(_mProgram, attrName));
        }
        public ShaderVtxAttrib4f GetAttrV4f(string attrName)
        {
            return new ShaderVtxAttrib4f(GL.GetAttribLocation(_mProgram, attrName));
        }
        public ShaderUniformVar1 GetUniform1(string uniformVarName)
        {
            return new ShaderUniformVar1(GL.GetUniformLocation(_mProgram, uniformVarName));
        }
        public ShaderUniformVar2 GetUniform2(string uniformVarName)
        {
            return new ShaderUniformVar2(GL.GetUniformLocation(_mProgram, uniformVarName));
        }
        public ShaderUniformVar3 GetUniform3(string uniformVarName)
        {
            return new ShaderUniformVar3(GL.GetUniformLocation(_mProgram, uniformVarName));
        }
        public ShaderUniformVar4 GetUniform4(string uniformVarName)
        {
            return new ShaderUniformVar4(GL.GetUniformLocation(_mProgram, uniformVarName));
        }
        public ShaderUniformMatrix4 GetUniformMat4(string uniformVarName)
        {
            return new ShaderUniformMatrix4(GL.GetUniformLocation(_mProgram, uniformVarName));
        }
        public ShaderUniformMatrix3 GetUniformMat3(string uniformVarName)
        {
            return new ShaderUniformMatrix3(GL.GetUniformLocation(_mProgram, uniformVarName));
        }
        public void UseProgram()
        {
            GL.UseProgram(_mProgram);
        }
        public int ProgramId => _mProgram;
    }
}