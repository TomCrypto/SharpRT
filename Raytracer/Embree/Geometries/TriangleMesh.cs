using System;
using System.Runtime.InteropServices;

namespace Embree
{
    /// <summary>
    /// Represents a triangle mesh geometry vertex.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Vertex
    {
        [FieldOffset(0)] public float X;
        [FieldOffset(4)] public float Y;
        [FieldOffset(8)] public float Z;
    }

    /// <summary>
    /// Represents a triangle mesh geometry triangle.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct Triangle
    {
        [FieldOffset(0)] public int V0;
        [FieldOffset(4)] public int V1;
        [FieldOffset(8)] public int V2;
    }

    /// <summary>
    /// Represents the parameters for a triangle mesh geometry.
    /// </summary>
    public struct TriangleMeshDescription
    {
        /// <summary>
        /// The flags for this geometry.
        /// </summary>
        public GeometryFlags Flags;

        /// <summary>
        /// The number of triangles in the mesh.
        /// </summary>
        public int NumTriangles;

        /// <summary>
        /// The number of vertices in the mesh.
        /// </summary>
        public int NumVertices;

        /// <summary>
        /// Whether this mesh supports linear motion via timesteps.
        /// </summary>
        public bool LinearMotion;
    }

    /// <summary>
    /// Represents a geometry made out of triangles.
    /// </summary>
    public sealed class TriangleMesh : Geometry
    {
        public override Scene Parent { get { return parent; } }
        private readonly Scene parent;

        public override Int32 ID { get { return id; } }
        private readonly Int32 id;

        public override GeometryFlags Flags { get { return description.Flags; } }

        /// <summary>
        /// Gets the description this triangle mesh was created with.
        /// </summary>
        public TriangleMeshDescription Description { get { return description; } }
        private readonly TriangleMeshDescription description;

        internal TriangleMesh(Scene parent, TriangleMeshDescription desc)
        {
            this.parent = parent;
            this.description = desc;
            this.id = RTC.NewTriangleMesh(Parent.NativePtr,
                                          Description.Flags,
                                          Description.NumTriangles,
                                          Description.NumVertices,
                                          Description.LinearMotion ? 2 : 1);
        }

        /// <summary>
        /// Sets the triangles of this triangle mesh.
        /// </summary>
        public void SetTriangles(Triangle[] triangles, int srcOffset, int dstOffset, int length)
        {
            if (triangles == null) {
                throw new ArgumentNullException("triangles");
            } else if (srcOffset < 0) {
                throw new ArgumentOutOfRangeException("srcOffset");
            } else if (dstOffset < 0) {
                throw new ArgumentOutOfRangeException("dstOffset");
            } else if (length < 0) {
                throw new ArgumentOutOfRangeException("length");
            } else if (srcOffset + length > triangles.Length) {
                throw new ArgumentOutOfRangeException("srcOffset");
            } else if (dstOffset + length > Description.NumTriangles) {
                throw new ArgumentOutOfRangeException("dstOffset");
            } else if (length == 0) {
                return;
            }

            using (var triangleBuffer = new MappedBuffer<Triangle>(Parent, ID, BufferType.IndexBuffer)) {
                for (int t = 0; t < length; ++t) {
                    triangleBuffer[t + dstOffset] = triangles[t + srcOffset];
                }
            }

            RTC.UpdateBuffer(Parent.NativePtr, ID, BufferType.IndexBuffer);
        }

        /// <summary>
        /// Sets the triangles of this triangle mesh.
        /// </summary>
        public void SetTriangles(Triangle[] triangles)
        {
            SetTriangles(triangles, 0, 0, Description.NumTriangles);
        }

        /// <summary>
        /// Sets the vertices of this triangle mesh.
        /// </summary>
        public void SetVertices(Vertex[] vertices, TimeStep timeStep, int srcOffset, int dstOffset, int length)
        {
            if (vertices == null) {
                throw new ArgumentNullException("vertices");
            } else if (srcOffset < 0) {
                throw new ArgumentOutOfRangeException("srcOffset");
            } else if (dstOffset < 0) {
                throw new ArgumentOutOfRangeException("dstOffset");
            } else if (length < 0) {
                throw new ArgumentOutOfRangeException("length");
            } else if (srcOffset + length > vertices.Length) {
                throw new ArgumentOutOfRangeException("length");
            } else if (dstOffset + length > Description.NumVertices) {
                throw new ArgumentOutOfRangeException("length");
            } else if (!Description.LinearMotion && (timeStep != TimeStep.T0)) {
                throw new ArgumentOutOfRangeException("timeStep");
            } else if (length == 0) {
                return;
            }

            var vertexBufferType = timeStep.AsVertexBuffer();

            using (var vertexBuffer = new MappedBuffer<Vertex>(Parent, ID, vertexBufferType)) {
                for (int t = 0; t < length; ++t) {
                    vertexBuffer[t + dstOffset] = vertices[t + srcOffset];
                }
            }

            RTC.UpdateBuffer(Parent.NativePtr, ID, vertexBufferType);
        }

        /// <summary>
        /// Sets the vertices of this triangle mesh.
        /// </summary>
        public void SetVertices(Vertex[] vertices, int srcOffset, int dstOffset, int length)
        {
            SetVertices(vertices, TimeStep.T0, srcOffset, dstOffset, length);
        }

        /// <summary>
        /// Sets the vertices of this triangle mesh.
        /// </summary>
        public void SetVertices(Vertex[] vertices, TimeStep timeStep)
        {
            SetVertices(vertices, timeStep, 0, 0, Description.NumVertices);
        }

        /// <summary>
        /// Sets the vertices of this triangle mesh.
        /// </summary>
        public void SetVertices(Vertex[] vertices)
        {
            SetVertices(vertices, TimeStep.T0, 0, 0, Description.NumVertices);
        }
    }
}