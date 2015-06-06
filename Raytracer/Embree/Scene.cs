using System;
using System.Collections;
using System.Collections.Generic;

namespace Embree
{
    /// <summary>
    /// Represents a read-only, lightweight key-value mapping.
    /// </summary>
    public struct IdentifierMapping<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        /// <summary>
        /// Gets the value corresponding to a given key.
        /// </summary>
        /// <param name="key">The key to map to a value.</param>
        public V this[K key] { get { return mapping[key]; } }
        private readonly IDictionary<K, V> mapping;

        internal IdentifierMapping(IDictionary<K, V> mapping)
        {
            this.mapping = mapping;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return mapping.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    /// Represents a collection of geometries and instances.
    /// </summary>
    public sealed class Scene : IDisposable
    {
        /// <summary>
        /// Gets this scene's underlying native pointer.
        /// </summary>
        internal IntPtr NativePtr { get { CheckDisposed(); return nativePtr; } }
        private readonly IntPtr nativePtr;

        /// <summary>
        /// Gets this scene's scene flags.
        /// </summary>
        public SceneFlags SceneFlags { get { CheckDisposed(); return sceneFlags; } }
        private readonly SceneFlags sceneFlags;

        /// <summary>
        /// Gets this scene's algorithm flags.
        /// </summary>
        public AlgorithmFlags AlgorithmFlags { get { CheckDisposed(); return algorithmFlags; } }
        private readonly AlgorithmFlags algorithmFlags;

        /// <summary>
        /// Gets the mapping from instance ID's to instance objects.
        /// </summary>
        public IdentifierMapping<Int32, Instance> Instances {
            get { CheckDisposed(); return new IdentifierMapping<Int32, Instance>(instanceMapping); }
        }

        /// <summary>
        /// Gets the mapping from geometry ID's to geometry objects.
        /// </summary>
        public IdentifierMapping<Int32, Geometry> Geometries {
            get { CheckDisposed(); return new IdentifierMapping<Int32, Geometry>(geometryMapping); }
        }

        private readonly IDictionary<Int32, Instance> instanceMapping = new Dictionary<Int32, Instance>();
        private readonly IDictionary<Instance, Int32> instanceInverse = new Dictionary<Instance, Int32>();
        private readonly IDictionary<Int32, Geometry> geometryMapping = new Dictionary<Int32, Geometry>();
        private readonly IDictionary<Geometry, Int32> geometryInverse = new Dictionary<Geometry, Int32>();

        /// <summary>
        /// Instantiates a new scene with the given scene and algorithm flags.
        /// </summary>
        /// <param name="sceneFlags">The scene flags to use.</param>
        /// <param name="algorithmFlags">The algorithm flags to use.</param>
        public Scene(SceneFlags sceneFlags = SceneFlags.Dynamic
                                           | SceneFlags.Coherent,
                     AlgorithmFlags algorithmFlags = AlgorithmFlags.Intersect1
                                                   | AlgorithmFlags.Intersect4)
        {
            if (!sceneFlags.HasFlag(SceneFlags.Dynamic)) {
                sceneFlags |= SceneFlags.Dynamic;
            }

            this.sceneFlags = sceneFlags;
            this.algorithmFlags = algorithmFlags;
            this.nativePtr = RTC.NewScene(SceneFlags,
                                          AlgorithmFlags);
        }

        /// <summary>
        /// Commits all geometry within this scene.
        /// </summary>
        public void Commit()
        {
            CheckDisposed();

            RTC.Commit(NativePtr);
        }

        /// <summary>
        /// Creates a new instance of a scene in this scene.
        /// </summary>
        /// <returns>The newly created instance.</returns>
        /// <param name="source">The scene to instance.</param>
        public Instance NewInstance(Scene source)
        {
            CheckDisposed();

            if (source == null) {
                throw new ArgumentNullException("source");
            }

            var instance = new Instance(this, source);
            instanceMapping[instance.ID] = instance;
            instanceInverse[instance] = instance.ID;
            return instance;
        }

        /// <summary>
        /// Creates a new triangle mesh in this scene.
        /// </summary>
        /// <returns>The newly created geometry.</returns>
        /// <param name="desc">The geometry's description.</param>
        public TriangleMesh NewTriangleMesh(TriangleMeshDescription desc)
        {
            CheckDisposed();

            var geometry = new TriangleMesh(this, desc);
            geometryMapping[geometry.ID] = geometry;
            geometryInverse[geometry] = geometry.ID;
            return geometry;
        }

        /// <summary>
        /// Removes an instance from this scene.
        /// </summary>
        public Boolean Remove(Instance instance)
        {
            CheckDisposed();

            if (instance == null) {
                throw new ArgumentNullException("instance");
            }

            if (instanceInverse.ContainsKey(instance)) {
                RTC.DeleteGeometry(NativePtr, instance.ID);
                instanceMapping.Remove(instance.ID);
                instanceInverse.Remove(instance);
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Removes a geometry from this scene.
        /// </summary>
        public Boolean Remove(Geometry geometry)
        {
            CheckDisposed();

            if (geometry == null) {
                throw new ArgumentNullException("geometry");
            }

            if (geometryInverse.ContainsKey(geometry)) {
                RTC.DeleteGeometry(NativePtr, geometry.ID);
                geometryMapping.Remove(geometry.ID);
                geometryInverse.Remove(geometry);
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Performs an intersection query on this scene.
        /// </summary>
        /// <param name="ray">The ray structure to use.</param>
        public unsafe void Intersection(RayStruct1* ray)
        {
            #if DEBUG
            CheckDisposed();

            if (!AlgorithmFlags.HasFlag(AlgorithmFlags.Intersect1)) {
                throw new InvalidOperationException("AlgorithmFlags.Intersect1 not set.");
            }
            #endif

            RTC.Intersect(NativePtr, ray);
        }

        /// <summary>
        /// Performs an intersection query on this scene.
        /// </summary>
        /// <param name="ray">The ray structure to use.</param>
        /// <param name="activityMask">The ray activity mask.</param>
        public unsafe void Intersection4(RayStruct4* ray, uint* activityMask)
        {
            #if DEBUG
            CheckDisposed();

            if (!AlgorithmFlags.HasFlag(AlgorithmFlags.Intersect4)) {
                throw new InvalidOperationException("AlgorithmFlags.Intersect4 not set.");
            }
            #endif

            RTC.Intersect4(activityMask, NativePtr, ray);
        }

        /// <summary>
        /// Performs an intersection query on this scene.
        /// </summary>
        /// <param name="ray">The ray structure to use.</param>
        /// <param name="activityMask">The ray activity mask.</param>
        public unsafe void Intersection8(RayStruct8* ray, uint* activityMask)
        {
            #if DEBUG
            CheckDisposed();

            if (!AlgorithmFlags.HasFlag(AlgorithmFlags.Intersect8)) {
                throw new InvalidOperationException("AlgorithmFlags.Intersect8 not set.");
            }
            #endif

            RTC.Intersect8(activityMask, NativePtr, ray);
        }

        /// <summary>
        /// Performs an intersection query on this scene.
        /// </summary>
        /// <param name="ray">The ray structure to use.</param>
        /// <param name="activityMask">The ray activity mask.</param>
        public unsafe void Intersection16(RayStruct16* ray, uint* activityMask)
        {
            #if DEBUG
            CheckDisposed();

            if (!AlgorithmFlags.HasFlag(AlgorithmFlags.Intersect16)) {
                throw new InvalidOperationException("AlgorithmFlags.Intersect16 not set.");
            }
            #endif

            RTC.Intersect16(activityMask, NativePtr, ray);
        }

        /// <summary>
        /// Performs an occlusion query on this scene.
        /// </summary>
        /// <param name="ray">The ray structure to use.</param>
        public unsafe void Occlusion(RayStruct1* ray)
        {
            #if DEBUG
            CheckDisposed();

            if (!AlgorithmFlags.HasFlag(AlgorithmFlags.Intersect1)) {
                throw new InvalidOperationException("AlgorithmFlags.Intersect1 not set.");
            }
            #endif

            RTC.Occluded(NativePtr, ray);
        }

        /// <summary>
        /// Performs an occlusion query on this scene.
        /// </summary>
        /// <param name="ray">The ray structure to use.</param>
        /// <param name="activityMask">The ray activity mask.</param>
        public unsafe void Occlusion4<T>(RayStruct4* ray, uint* activityMask)
        {
            #if DEBUG
            CheckDisposed();

            if (!AlgorithmFlags.HasFlag(AlgorithmFlags.Intersect4)) {
                throw new InvalidOperationException("AlgorithmFlags.Intersect4 not set.");
            }
            #endif

            RTC.Occluded4(activityMask, NativePtr, ray);
        }

        /// <summary>
        /// Performs an occlusion query on this scene.
        /// </summary>
        /// <param name="ray">The ray structure to use.</param>
        /// <param name="activityMask">The ray activity mask.</param>
        public unsafe void Occlusion8(RayStruct8* ray, uint* activityMask)
        {
            #if DEBUG
            CheckDisposed();

            if (!AlgorithmFlags.HasFlag(AlgorithmFlags.Intersect8)) {
                throw new InvalidOperationException("AlgorithmFlags.Intersect8 not set.");
            }
            #endif

            RTC.Occluded8(activityMask, NativePtr, ray);
        }

        /// <summary>
        /// Performs an occlusion query on this scene.
        /// </summary>
        /// <param name="ray">The ray structure to use.</param>
        /// <param name="activityMask">The ray activity mask.</param>
        public unsafe void Occlusion16(RayStruct16* ray, uint* activityMask)
        {
            #if DEBUG
            CheckDisposed();

            if (!AlgorithmFlags.HasFlag(AlgorithmFlags.Intersect16)) {
                throw new InvalidOperationException("AlgorithmFlags.Intersect16 not set.");
            }
            #endif

            RTC.Occluded16(activityMask, NativePtr, ray);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed) {
                RTC.DeleteScene(NativePtr);

                disposed = true;
            }
        }

        #region IDisposable

        private bool disposed;

        ~Scene()
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

        private void CheckDisposed()
        {
            if (disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        #endregion
    }
}