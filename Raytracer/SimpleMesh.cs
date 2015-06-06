using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;

namespace SharpRT
{
    public class SimpleMesh : IGeometryDescription
    {
        private struct VertexAttributes
        {
            public Int32 VertexID;
            public Vector Normal;
        }

        private struct Triangle
        {
            public VertexAttributes V0;
            public VertexAttributes V1;
            public VertexAttributes V2;
            public Vector FaceNormal;
        }

        private Triangle[] triangles;

        private bool smoothNormals;

        public Embree.Scene EmbreeScene {
            get { return scene; }
        }

        private Embree.Scene scene;

        public void At(Surface.Intersection intersection, ref Surface.Attributes attributes)
        {
            Triangle tri = triangles[intersection.PrimitiveID];

            if (smoothNormals) {
                // barycentric interpolation
                Vector smoothed = tri.V1.Normal * intersection.LocalU
                                + tri.V2.Normal * intersection.LocalV
                                + tri.V0.Normal * (1 - intersection.LocalU - intersection.LocalV);

                attributes.Basis = new Basis(smoothed.Normalize());
            } else {
                attributes.Basis = new Basis(tri.FaceNormal);
            }
        }

        private static Embree.TriangleMesh LoadFromFileIntoScene(String path, Embree.Scene scene, out IList<Embree.Vertex> vertexData, out IList<Embree.Triangle> triData)
        {
            vertexData = new List<Embree.Vertex>();
            triData = new List<Embree.Triangle>();

            // load the geometry from the .obj file (basic OBJ loader)

            foreach (var line in File.ReadLines(path)) {
                var tokens = line.Split();

                switch (tokens[0]) {
                    case "v":
                        vertexData.Add(new Embree.Vertex {
                            X = float.Parse(tokens[1], CultureInfo.InvariantCulture),
                            Y = float.Parse(tokens[2], CultureInfo.InvariantCulture),
                            Z = float.Parse(tokens[3], CultureInfo.InvariantCulture),
                        });
                        break;
                    case "f":
                        triData.Add(new Embree.Triangle {
                            V0 = int.Parse(tokens[1].Split('/')[0]) - 1,
                            V1 = int.Parse(tokens[2].Split('/')[0]) - 1,
                            V2 = int.Parse(tokens[3].Split('/')[0]) - 1,
                        });
                        break;
                }
            }

            var mesh = scene.NewTriangleMesh(new Embree.TriangleMeshDescription() {
                NumVertices = vertexData.Count,
                NumTriangles = triData.Count,
                Flags = Embree.GeometryFlags.Static,
                LinearMotion = false,
            });

            mesh.SetVertices(vertexData.ToArray());
            mesh.SetTriangles(triData.ToArray());

            return mesh;
        }

        public SimpleMesh(String filePath, Boolean smoothNormals)
        {
            scene = new Embree.Scene(Embree.SceneFlags.Coherent
                                   | Embree.SceneFlags.Incoherent
                                   | Embree.SceneFlags.Robust);

            IList<Embree.Vertex> vertexData;
            IList<Embree.Triangle> triData;

            LoadFromFileIntoScene(filePath, scene, out vertexData, out triData);
            scene.Commit();

            // convert geometric vertex/triangle data into our own internal triangle format

            this.triangles = new Triangle[triData.Count];
            
            for (int t = 0; t < triData.Count; ++t) {
                triangles[t].V0.VertexID = triData[t].V0;
                triangles[t].V1.VertexID = triData[t].V1;
                triangles[t].V2.VertexID = triData[t].V2;
                triangles[t].FaceNormal = Vector.Normalize(Vector.Cross(
                    (Point)vertexData[triData[t].V1] - (Point)vertexData[triData[t].V0],
                    (Point)vertexData[triData[t].V2] - (Point)vertexData[triData[t].V0]
                ));
            }

            // if we want smooth vertex normals, generate them here
            // (note: we could load them from the .obj file if available, but never mind that for now)

            this.smoothNormals = smoothNormals;

            if (smoothNormals) {
                GenerateVertexNormals(vertexData);
            }
        }

        /// <summary>
        /// Computes reasonable vertex normals for a triangle mesh.
        /// </summary>
        private void GenerateVertexNormals(IList<Embree.Vertex> vertices)
        {
            var adjacent = new Dictionary<Int32, List<Triangle>>();
            var vertexNormals = new Vector[vertices.Count];

            for (int vertID = 0; vertID < vertices.Count; ++vertID) {
                adjacent[vertID] = new List<Triangle>();
            }

            // find all triangles adjacent to each vertex

            for (int triID = 0; triID < triangles.Length; ++triID) {
                var tri = triangles[triID];

                adjacent[tri.V0.VertexID].Add(triangles[triID]);
                adjacent[tri.V1.VertexID].Add(triangles[triID]);
                adjacent[tri.V2.VertexID].Add(triangles[triID]);
            }

            // for each vertex, average the face normals of its neighbouring triangles

            for (int vertID = 0; vertID < vertices.Count; ++vertID) {
                Vector avgNormal = Vector.Zero;

                foreach (var tri in adjacent[vertID]) {
                    avgNormal += tri.FaceNormal;
                }

                vertexNormals[vertID] = avgNormal.Normalize();
            }

            // finally, write the resulting vertex normals in each triangle's vertex attributes

            for (int t = 0; t < triangles.Length; ++t) {
                triangles[t].V0.Normal = vertexNormals[triangles[t].V0.VertexID];
                triangles[t].V1.Normal = vertexNormals[triangles[t].V1.VertexID];
                triangles[t].V2.Normal = vertexNormals[triangles[t].V2.VertexID];
            }
        }
    }
}