using System;

namespace SharpRT
{
    /// <summary>
    /// Describes a surface's underlying geometry (its mesh, etc).
    /// </summary>
    public interface IGeometryDescription
    {
        Embree.Scene EmbreeScene { get; }

        void At(Surface.Intersection intersection, ref Surface.Attributes attributes);
    }

    /// <summary>
    /// Describes a surface's material (its BRDF, texture, etc).
    /// </summary>
    public interface IMaterialDescription
    {
        void At(Surface.Intersection intersection, ref Surface.Attributes attributes);
    }

    /// <summary>
    /// Describes a surface's location within its parent scene.
    /// </summary>
    public interface ILocationDescription
    {
        Matrix Transform { get; }

        Boolean Visible { get; }
    }

    /// <summary>
    /// Represents an individual surface within a scene.
    /// </summary>
    public class Surface
    {
        /// <summary>
        /// Represents a ray-surface intersection.
        /// </summary>
        public struct Intersection
        {
            /// <summary>
            /// The ID of the intersected surface.
            /// </summary>
            public Int32 SurfaceID;

            /// <summary>
            /// The ID of the intersected geometry primitive.
            /// </summary>
            public Int32 PrimitiveID;
           
            /// <summary>
            /// The intersection's local barycentric u-coordinate.
            /// </summary>
            public float LocalU;

            /// <summary>
            /// The intersection's local barycentric v-coordinate.
            /// </summary>
            public float LocalV;

            /// <summary>
            /// The intersection's distance along the ray.
            /// </summary>
            public float Distance;
        }

        /// <summary>
        /// Represents the attributes of a surface at any given point.
        /// </summary>
        public struct Attributes
        {
            /// <summary>
            /// The local surface basis (normal/tangent/bitangent).
            /// </summary>
            public Basis Basis;

            /// <summary>
            /// The surface BRDF.
            /// </summary>
            public IMaterial Material;
        }

        /// <summary>
        /// Gets this surface's geometry description.
        /// </summary>
        public IGeometryDescription Geometry { get; private set; }

        /// <summary>
        /// Gets this surface's material description.
        /// </summary>
        public IMaterialDescription Material { get; private set; }

        /// <summary>
        /// Gets this surface's location description.
        /// </summary>
        public ILocationDescription Location { get; private set; }

        internal Surface(IGeometryDescription geometry,
                         IMaterialDescription material,
                         ILocationDescription location)
        {
            this.Geometry = geometry;
            this.Material = material;
            this.Location = location;
        }

        /// <summary>
        /// Gets the attributes of this surface for a given intersection.
        /// </summary>
        public Attributes At(Intersection intersection)
        {
            Attributes attributes = default(Attributes);

            Geometry.At(intersection, ref attributes);
            Material.At(intersection, ref attributes);

            return attributes;
        }
    }
}
