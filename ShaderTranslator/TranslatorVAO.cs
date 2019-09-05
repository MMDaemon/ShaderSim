using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using ShaderRenderer;
using ShaderUtils;
using ShaderUtils.Mathematics;
using Vector2 = ShaderUtils.Mathematics.Vector2;
using Vector3 = ShaderUtils.Mathematics.Vector3;
using Vector4 = ShaderUtils.Mathematics.Vector4;

namespace ShaderTranslator
{
    public class TranslatorVAO : Disposable, IVertexArrayObject
    {
        private Dictionary<int, BufferObject> boundBuffers = new Dictionary<int, BufferObject>();
        /// <summary>The identifier vao</summary>
        private int idVAO;
        /// <summary>The identifier buffer binding</summary>
        private const int idBufferBinding = 2147483647;
        private int lastAttributeLength;

        public int IDLength { get; private set; }

        private RenderTranslator _wrapper;

        public TranslatorVAO(RenderTranslator wrapper)
        {
            _wrapper = wrapper;

            idVAO = GL.GenVertexArray();
        }

        public void SetIndex(IEnumerable<uint> data)
        {
            if (data == null || data.Count() == 0)
                return;
            this.Activate();
            BufferObject bufferObject = this.RequestBuffer(int.MaxValue, BufferTarget.ElementArrayBuffer);
            bufferObject.Set<uint>(data.ToArray(), BufferUsageHint.StaticDraw);
            bufferObject.Activate();
            this.Deactivate();
            bufferObject.Deactivate();
            this.IDLength = data.Count();
        }

        public void SetAttribute<T>(string name, Tuple<VertexShader, FragmentShader> shader, IList<T> data, bool perInstance = false) where T : struct
        {
            int bindingID = _wrapper.GetAttributeBindingID(shader, name);

            if (typeof(T) == typeof(float))
            {
                float[] dataArray = data.ToArray() as float[];
                SetAttribute(bindingID, dataArray, VertexAttribPointerType.Float, 1, perInstance);
            }
            if (typeof(T) == typeof(Vector2))
            {
                OpenTK.Vector2[] dataArray = new OpenTK.Vector2[data.Count()];
                for (int i = 0; i < data.Count(); i++)
                {
                    Vector2[] vectors = data.ToArray() as Vector2[];
                    if (vectors != null)
                    {
                        dataArray[i] = vectors[i].ToOpenTK();
                    }
                }
                SetAttribute(bindingID, dataArray, VertexAttribPointerType.Float, 2, perInstance);
            }
            if (typeof(T) == typeof(Vector3))
            {
                OpenTK.Vector3[] dataArray = new OpenTK.Vector3[data.Count()];
                for (int i = 0; i < data.Count(); i++)
                {
                    Vector3[] vectors = data.ToArray() as Vector3[];
                    if (vectors != null)
                    {
                        dataArray[i] = vectors[i].ToOpenTK();
                    }
                }
                SetAttribute(bindingID, dataArray, VertexAttribPointerType.Float, 3, perInstance);
            }
            if (typeof(T) == typeof(Vector4))
            {
                OpenTK.Vector4[] dataArray = new OpenTK.Vector4[data.Count()];
                for (int i = 0; i < data.Count(); i++)
                {
                    Vector4[] vectors = data.ToArray() as Vector4[];
                    if (vectors != null)
                    {
                        dataArray[i] = vectors[i].ToOpenTK();
                    }
                }
                SetAttribute(bindingID, dataArray, VertexAttribPointerType.Float, 4, perInstance);
            }
            if (typeof(T) == typeof(Matrix4x4))
            {
                Matrix4[] dataArray = new Matrix4[data.Count()];
                for (int i = 0; i < data.Count(); i++)
                {
                    Matrix4x4[] dataList = data.ToArray() as Matrix4x4[];
                    if (dataList != null)
                    {
                        dataArray[i] = dataList[i].ToOpenTK();
                    }
                }
                SetAttribute(bindingID, dataArray, true);
            }
        }

        /// <summary>
        /// Sets a vertex attribute array for the given <paramref name="bindingID" />.
        /// </summary>
        /// <typeparam name="DataElement">The data element type.</typeparam>
        /// <param name="bindingID">The binding ID.</param>
        /// <param name="data">The attribute array data.</param>
        /// <param name="type">The array elements base type.</param>
        /// <param name="elementSize">Element count for each array element.</param>
        /// <param name="perInstance">
        /// if set to <c>true</c> attribute array contains one entry for each instance
        /// if set to <c>false</c> all attribute array elements are for one instance
        /// </param>
        private void SetAttribute<DataElement>(
          int bindingID,
          DataElement[] data,
          VertexAttribPointerType type,
          int elementSize,
          bool perInstance = false)
          where DataElement : struct
        {
            if (-1 == bindingID)
                return;
            this.Activate();
            BufferObject bufferObject = this.RequestBuffer(bindingID, BufferTarget.ArrayBuffer);
            bufferObject.Set<DataElement>(data, BufferUsageHint.StaticDraw);
            bufferObject.Activate();
            int stride = Marshal.SizeOf(typeof(DataElement));
            GL.VertexAttribPointer(bindingID, elementSize, type, false, stride, 0);
            GL.EnableVertexAttribArray(bindingID);
            if (perInstance)
                GL.VertexAttribDivisor(bindingID, 1);
            else
                this.lastAttributeLength = data.Length;
            this.Deactivate();
            bufferObject.Deactivate();
            GL.DisableVertexAttribArray(bindingID);
        }

        /// <summary>
        /// sets or updates a vertex attribute of type Matrix4
        /// Matrix4 is stored row-major, but OpenGL expects data to be column-major, so the Matrix4 inputs become transposed in the shader
        /// </summary>
        /// <param name="bindingID">shader binding location</param>
        /// <param name="data">array of Matrix4 inputs</param>
        /// <param name="perInstance">if set to <c>true</c> [per instance].</param>
        private void SetAttribute(int bindingID, Matrix4[] data, bool perInstance = false)
        {
            if (-1 == bindingID)
                return;
            this.Activate();
            BufferObject bufferObject = this.RequestBuffer(bindingID, BufferTarget.ArrayBuffer);
            bufferObject.Set<Matrix4>(data, BufferUsageHint.StaticDraw);
            bufferObject.Activate();
            int num = Marshal.SizeOf(typeof(Vector4));
            int stride = Marshal.SizeOf(typeof(Matrix4));
            for (int index = 0; index < 4; ++index)
            {
                GL.VertexAttribPointer(bindingID + index, 4, VertexAttribPointerType.Float, false, stride, num * index);
                GL.EnableVertexAttribArray(bindingID + index);
                if (perInstance)
                    GL.VertexAttribDivisor(bindingID + index, 1);
            }
            this.Deactivate();
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            for (int index = 0; index < 4; ++index)
                GL.DisableVertexAttribArray(bindingID + index);
        }

        protected override void DisposeResources()
        {
            foreach (Disposable disposable in this.boundBuffers.Values)
                disposable.Dispose();
            this.boundBuffers.Clear();
            GL.DeleteVertexArray(this.idVAO);
            this.idVAO = 0;
        }

        private void Activate()
        {
            GL.BindVertexArray(this.idVAO);
        }

        private void Deactivate()
        {
            GL.BindVertexArray(0);
        }

        public void Draw(int instanceCount = 1)
        {
            this.Activate();
            if (this.IDLength == 0)
                GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, this.lastAttributeLength, instanceCount);
            else
                GL.DrawElementsInstanced(PrimitiveType.Triangles, this.IDLength, DrawElementsType.UnsignedInt, (IntPtr)0, instanceCount);
            this.Deactivate();
        }

        private BufferObject RequestBuffer(int bindingID, BufferTarget bufferTarget)
        {
            if (!this.boundBuffers.TryGetValue(bindingID, out var bufferObject))
            {
                bufferObject = new BufferObject(bufferTarget);
                this.boundBuffers[bindingID] = bufferObject;
            }
            return bufferObject;
        }
    }
}
