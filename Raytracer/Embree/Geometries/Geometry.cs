using System;

namespace Embree
{
    /// <summary>
    /// Represents a geometry inside a scene.
    /// </summary>
    public abstract class Geometry
    {
        /// <summary>
        /// Gets the scene this geometry belongs to.
        /// </summary>
        public abstract Scene Parent { get; }

        /// <summary>
        /// Gets this geometry's ID in its parent scene.
        /// </summary>
        public abstract Int32 ID { get; }

        /// <summary>
        /// Gets the flags this geometry was created with.
        /// </summary>
        public abstract GeometryFlags Flags { get; }

        /// <summary>
        /// Gets or sets whether this geometry is enabled.
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
    }

    /// <summary>
    /// Specifies the available time steps for motion blur.
    /// </summary>
    public enum TimeStep
    {
        /// <summary>
        /// The T = 0 timestep (interpolated via ray time).
        /// </summary>
        T0,
        /// <summary>
        /// The T = 1 timestep (interpolated via ray time).
        /// </summary>
        T1,
    }

    public static class Extensions
    {
        public static BufferType AsVertexBuffer(this TimeStep timeStep)
        {
            switch (timeStep) {
                case TimeStep.T0:
                    return BufferType.VertexBuffer0;
                case TimeStep.T1:
                    return BufferType.VertexBuffer1;
                default:
                    throw new ArgumentException("Invalid timestep.");
            }
        }
    }
}