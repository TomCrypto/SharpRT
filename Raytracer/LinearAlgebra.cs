using System;

namespace SharpRT
{
    public struct Basis
    {
        // can support oriented basis later (when we load models with tangent/bitangent data)

        private Matrix transform;

        public Matrix Transform
        {
            get { return transform; }
        }

        public Vector Tangent
        {
            get { return transform.U; }
        }

        public Vector Normal
        {
            get { return transform.V; }
        }

        public Vector Bitangent
        {
            get { return transform.W; }
        }

        /// <summary>
        /// Creates an unoriented basis about the given normal axis.
        /// </summary>
        public Basis(Vector normal)
        {
            Vector tangent;

            if (normal.X == 0) {
                tangent = new Vector(1, 0, 0);
            } else {
                /* (z, 0, -x) = cross((0, 1, 0), (x, y, z)) */
                tangent = Vector.Normalize(new Vector(normal.Z, 0, -normal.X));
            }

            Vector bitangent = Vector.Cross(tangent, normal);

            transform = new Matrix(tangent, normal, bitangent);
        }
    };

    public struct Direction
    {
        // will support phi (orientation about the surface normal) when we need it

        private Vector vector;
        private float cosTheta;

        public float CosTheta
        {
            get { return cosTheta; }
        }

        public Vector Vector
        {
            get { return vector; }
        }

        public Direction(Vector vector, Basis basis)
        {
            this.vector = vector; // assumed to be normalized
            this.cosTheta = Math.Max(0, Vector.Dot(basis.Normal, vector));
        }
    }
}

