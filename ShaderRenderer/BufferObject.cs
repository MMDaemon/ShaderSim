using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace ShaderRenderer
{
    public class BufferObject : Disposable
    {
        private int bufferID;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Zenseless.OpenGL.BufferObject" /> class.
        /// </summary>
        /// <param name="bufferTarget">The buffer target.</param>
        public BufferObject(BufferTarget bufferTarget)
        {
            this.BufferTarget = bufferTarget;
            switch (bufferTarget)
            {
                case BufferTarget.ArrayBuffer:
                    this.Type = ShaderResourceType.Attribute;
                    break;
                case BufferTarget.UniformBuffer:
                    this.Type = ShaderResourceType.UniformBuffer;
                    break;
                case BufferTarget.ShaderStorageBuffer:
                    this.Type = ShaderResourceType.RWBuffer;
                    break;
            }
            GL.GenBuffers(1, out this.bufferID);
        }

        /// <summary>Gets the buffer target.</summary>
        /// <value>The buffer target.</value>
        public BufferTarget BufferTarget { get; private set; }

        /// <summary>Gets the type.</summary>
        /// <value>The type.</value>
        public ShaderResourceType Type { get; private set; }

        /// <summary>Activates this instance.</summary>
        public void Activate()
        {
            GL.BindBuffer(this.BufferTarget, this.bufferID);
        }

        /// <summary>Activates the bind.</summary>
        /// <param name="index">The index.</param>
        public void ActivateBind(int index)
        {
            this.Activate();
            GL.BindBufferBase((BufferRangeTarget)this.BufferTarget, index, this.bufferID);
        }

        /// <summary>Deactivates this instance.</summary>
        public void Deactivate()
        {
            GL.BindBuffer(this.BufferTarget, 0);
        }

        /// <summary>Sets the specified data.</summary>
        /// <typeparam name="DATA_ELEMENT_TYPE">The type of the ata element type.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="usageHint">The usage hint.</param>
        public void Set<DATA_ELEMENT_TYPE>(DATA_ELEMENT_TYPE[] data, BufferUsageHint usageHint) where DATA_ELEMENT_TYPE : struct
        {
            this.Activate();
            int num = Marshal.SizeOf(typeof(DATA_ELEMENT_TYPE));
            GL.BufferData<DATA_ELEMENT_TYPE>(this.BufferTarget, data.Length * num, data, usageHint);
            this.Deactivate();
        }

        /// <summary>Sets the specified data.</summary>
        /// <typeparam name="DATA_TYPE">The type of the ata type.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="usageHint">The usage hint.</param>
        public void Set<DATA_TYPE>(DATA_TYPE data, BufferUsageHint usageHint) where DATA_TYPE : struct
        {
            this.Activate();
            GL.BufferData<DATA_TYPE>(this.BufferTarget, Marshal.SizeOf(typeof(DATA_TYPE)), ref data, usageHint);
            this.Deactivate();
        }

        /// <summary>Will be called from the default Dispose method.</summary>
        protected override void DisposeResources()
        {
            if (-1 == this.bufferID)
                return;
            GL.DeleteBuffer(this.bufferID);
            this.bufferID = -1;
        }
    }
}
