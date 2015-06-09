using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpRT
{
    public class Scene : IEnumerable<Surface>, IDisposable
    {
        private IList<Surface> surfaces = new List<Surface>();

        public Surface this[Int32 instID] {
            get { return surfaces[instID]; }
        }

        public IDictionary<Surface, Embree.Instance> instances = new Dictionary<Surface, Embree.Instance>();

        private readonly Embree.Scene scene;

        public Scene()
        {
            scene = new Embree.Scene(Embree.SceneFlags.Coherent
                                   | Embree.SceneFlags.Incoherent
                                   | Embree.SceneFlags.Robust);
        }

        public void Add(Surface surface)
        {
            var instance = scene.NewInstance(surface.Geometry.EmbreeScene);

            if (instance.ID < surfaces.Count) {
                surfaces[instance.ID] = surface;
            } else {
                surfaces.Add(surface);
            }

            instances[surface] = instance;
        }

        private void Delete(Surface surface, Int32 id)
        {
            scene.Remove(instances[surface]);
            instances.Remove(surface);
            surfaces[id] = null;
        }

        public void Remove(Int32 surfaceID)
        {
            if ((surfaceID < 0) || (surfaceID >= surfaces.Count)) {
                throw new ArgumentOutOfRangeException("surfaceID");
            }

            Delete(surfaces[surfaceID], surfaceID);
        }

        public void Remove(Surface surface)
        {
            if (surface == null) {
                throw new ArgumentNullException("surface");
            } else if (!instances.ContainsKey(surface)) {
                throw new ArgumentOutOfRangeException("surface");
            }

            Delete(surface, instances[surface].ID);
        }

        public IEnumerator<Surface> GetEnumerator()
        {
            return surfaces.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Commit()
        {
            foreach (var kv in instances) {
                var surface = kv.Key;
                var instance = kv.Value;

                instance.SetTransform(surface.Location.Transform.ToArray(),
                                      Embree.MatrixLayout.ColumnMajor);
                instance.Enabled = surface.Location.Visible;
            }

            scene.Commit();
        }

        private static unsafe byte* Align(byte* ptr, ulong boundary)
        {
            return ptr + (boundary - (ulong)ptr) % boundary;
        }

        public unsafe Boolean Intersect(Ray ray, float near, float far, out Surface.Intersection intersection)
        {
            var ptr = stackalloc byte[Marshal.SizeOf(typeof(Embree.RayStruct1))
                                    + Embree.RayStruct1.Alignment];
            var rs = (Embree.RayStruct1*)(Align(ptr, Embree.RayStruct1.Alignment));

            rs->orgX = ray.Origin.X;
            rs->orgY = ray.Origin.Y;
            rs->orgZ = ray.Origin.Z;

            rs->dirX = ray.Direction.X;
            rs->dirY = ray.Direction.Y;
            rs->dirZ = ray.Direction.Z;

            rs->tnear = near;
            rs->tfar  = far;
            rs->time  = 0.0f;

            rs->geomID = Embree.RTC.InvalidID;
            rs->primID = Embree.RTC.InvalidID;
            rs->instID = Embree.RTC.InvalidID;
            rs->mask   = 0xFFFFFFFF;

            scene.Intersection(rs);

            if (rs->geomID != Embree.RTC.InvalidID) {
                intersection = new Surface.Intersection() {
                    Distance = rs->tfar,
                    SurfaceID = rs->instID,
                    PrimitiveID = rs->primID,
                    LocalU = rs->u,
                    LocalV = rs->v
                };
                return true;
            } else {
                intersection = default(Surface.Intersection);
                return false;
            }
        }

        public unsafe Boolean Occlusion(Ray ray, float near, float far)
        {
            var ptr = stackalloc byte[Marshal.SizeOf(typeof(Embree.RayStruct1))
                                    + Embree.RayStruct1.Alignment];
            var rs = (Embree.RayStruct1*)(Align(ptr, Embree.RayStruct1.Alignment));

            rs->orgX = ray.Origin.X;
            rs->orgY = ray.Origin.Y;
            rs->orgZ = ray.Origin.Z;

            rs->dirX = ray.Direction.X;
            rs->dirY = ray.Direction.Y;
            rs->dirZ = ray.Direction.Z;

            rs->tnear = near;
            rs->tfar  = far;
            rs->time  = 0.0f;

            rs->geomID = Embree.RTC.InvalidID;
            rs->primID = Embree.RTC.InvalidID;
            rs->instID = Embree.RTC.InvalidID;
            rs->mask   = 0xFFFFFFFF;

            scene.Occlusion(rs);

            return rs->geomID == 0;
        }

        public void Dispose()
        {
            scene.Dispose();
        }
    }
}