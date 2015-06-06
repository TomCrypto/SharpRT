using System;
using System.Runtime.InteropServices;

namespace Embree
{
    /// <summary>
    /// Represents a managed array wrapper around an unmanaged buffer.
    /// </summary>
    internal class Buffer<T> where T : struct
    {
        /// <summary>
        /// The stride of this buffer, reinterpreted as a T[].
        /// </summary>
        private int stride = Marshal.SizeOf(typeof(T));

        /// <summary>
        /// Gets or sets the buffer's contents at a specific index.
        /// </summary>
        public T this[int index] {
            get { return (T)Marshal.PtrToStructure(pointer + index * stride, typeof(T)); }
            set { Marshal.StructureToPtr(value, pointer + index * stride, false); }
        }

        /// <summary>
        /// Gets the pointer to the underlying buffer.
        /// </summary>
        public IntPtr Pointer { get { return pointer; } }
        private readonly IntPtr pointer;

        public Buffer(IntPtr pointer)
        {
            this.pointer = pointer;
        }
    }
}