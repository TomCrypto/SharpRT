using System;

namespace Embree
{
    /// <summary>
    /// Represents a managed array wrapper around a mapped Embree buffer.
    /// </summary>
    /// <remarks>
    /// This class is intended to be a short-lived map/unmap helper.
    /// </remarks>
    internal sealed class MappedBuffer<T> : Buffer<T>, IDisposable where T : struct
    {
        private readonly Scene scene;
        private readonly Int32 geometryID;
        private readonly BufferType bufferType;

        public MappedBuffer(Scene scene, Int32 geometryID, BufferType bufferType)
        : base(RTC.MapBuffer(scene.NativePtr, geometryID, bufferType))
        {
            this.scene = scene;
            this.geometryID = geometryID;
            this.bufferType = bufferType;
        }

        private void Dispose(bool disposing)
        {
            if (!disposed) {
                RTC.UnmapBuffer(scene.NativePtr,
                                geometryID,
                                bufferType);

                disposed = true;
            }
        }

        #region IDisposable

        private bool disposed;

        ~MappedBuffer()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes of this instance, releasing all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}