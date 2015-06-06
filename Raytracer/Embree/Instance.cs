using System;

namespace Embree
{
    /// <summary>
    /// Represents a scene instanced inside another.
    /// </summary>
    public sealed class Instance
    {
        /// <summary>
        /// Gets the scene in which the subject scene is instanced.
        /// </summary>
        public Scene Parent { get { return parent; } }
        private readonly Scene parent;

        /// <summary>
        /// Gets the source scene which is being instanced.
        /// </summary>
        public Scene Source { get { return source; } }
        private readonly Scene source;

        /// <summary>
        /// Gets this instance's ID in its parent scene.
        /// </summary>
        public Int32 ID { get { return id; } }
        private readonly Int32 id;

        /// <summary>
        /// Gets or sets whether this instance is enabled.
        /// </summary>
        public Boolean Enabled {
            get { return enabled; }
            set {
                if (!value) {
                    RTC.Disable(Parent.NativePtr, ID);
                    enabled = false;
                } else {
                    RTC.Enable(Parent.NativePtr, ID);
                    enabled = true;
                }
            }
        }

        private Boolean enabled = true;

        internal Instance(Scene parent, Scene source)
        {
            this.parent = parent;
            this.source = source;
            this.id = RTC.NewInstance(Parent.NativePtr,
                                      Source.NativePtr);
        }

        /// <summary>
        /// Sets the transformation matrix for this instance.
        /// </summary>
        /// <param name="transform">The transformation matrix entries.</param>
        /// <param name="layout">The layout of the matrix entries.</param>
        /// <remarks>
        /// Only the first 12 or 16 entries of the transform array are used.
        /// </remarks>
        /// <remarks>
        /// The transform should be uniform, i.e. no non-uniform scaling.
        /// </remarks>
        public unsafe void SetTransform(float[] transform, MatrixLayout layout)
        {
            if (transform == null) {
                throw new ArgumentNullException("transform");
            }

            switch (layout) {
                case MatrixLayout.ColumnMajorAligned16:
                    if (transform.Length < 16) {
                        throw new ArgumentException("transform");
                    } else {
                        break;
                    }
                case MatrixLayout.ColumnMajor:
                case MatrixLayout.RowMajor:
                    if (transform.Length < 12) {
                        throw new ArgumentException("transform");
                    } else {
                        break;
                    }
                default:
                    throw new ArgumentException("layout");
            }

            fixed (float* xfm = transform) {
                RTC.SetTransform(Parent.NativePtr, ID, layout, xfm);
            }
        }
    }
}