using System;
using System.Runtime.InteropServices;

namespace Embree
{
    /// <summary>
    /// Specifies the available scene flags.
    /// </summary>
    [Flags]
    public enum SceneFlags
    {
        /// <summary>
        /// The scene supports dynamic modification.
        /// </summary>
        Dynamic = 1 << 0,

        /// <summary>
        /// Use compact acceleration structures.
        /// </summary>
        Compact = 1 << 8,

        /// <summary>
        /// Optimize traversal for coherent rays.
        /// </summary>
        Coherent = 1 << 9,

        /// <summary>
        /// Optimize traversal for incoherent rays.
        /// </summary>
        Incoherent = 1 << 10,

        /// <summary>
        /// Use high-quality acceleration structures.
        /// </summary>
        HighQuality = 1 << 11,

        /// <summary>
        /// Avoid optimizations reducing arithmetical accuracy.
        /// </summary>
        Robust = 1 << 16,
    }

    /// <summary>
    /// Specifies the available algorithm flags.
    /// </summary>
    [Flags]
    public enum AlgorithmFlags
    {
        /// <summary>
        /// Enable single-ray traversal.
        /// </summary>
        Intersect1 = 1 << 0,
        /// <summary>
        /// Enable 4-ray packet traversal.
        /// </summary>
        Intersect4 = 1 << 1,
        /// <summary>
        /// Enable 8-ray packet traversal (requires AVX).
        /// </summary>
        Intersect8 = 1 << 2,
        /// <summary>
        /// Enable 16-ray packet traversal (requires Xeon Phi).
        /// </summary>
        Intersect16 = 1 << 3,
    }

    /// <summary>
    /// Specifies the available types of matrix layout.
    /// </summary>
    public enum MatrixLayout
    {
        /// <summary>
        /// Row-major affine (3x4) matrix.
        /// </summary>
        RowMajor = 0,

        /// <summary>
        /// Column-major affine (3x4) matrix.
        /// </summary>
        ColumnMajor = 1,

        /// <summary>
        /// Column-major homogenous (4x4) matrix.
        /// </summary>
        ColumnMajorAligned16 = 2,
    }

    /// <summary>
    /// Specifies the available geometry buffer types.
    /// </summary>
    public enum BufferType
    {
        /// <summary>
        /// The index buffer.
        /// </summary>
        IndexBuffer = 0x01000000,

        /// <summary>
        /// The vertex buffer.
        /// </summary>
        VertexBuffer = 0x02000000,

        /// <summary>
        /// The T = 0 vertex buffer.
        /// </summary>
        VertexBuffer0 = 0x02000000,

        /// <summary>
        /// The T = 1 vertex buffer.
        /// </summary>
        VertexBuffer1 = 0x02000001,

        /// <summary>
        /// The face buffer.
        /// </summary>
        FaceBuffer = 0x03000000,

        /// <summary>
        /// The level buffer.
        /// </summary>
        LevelBuffer = 0x04000001,

        /// <summary>
        /// The edge crease index buffer.
        /// </summary>
        EdgeCreaseIndexBuffer = 0x05000000,

        /// <summary>
        /// The edge crease weight buffer.
        /// </summary>
        EdgeCreaseWeightBuffer = 0x06000000,

        /// <summary>
        /// The vertex crease index buffer.
        /// </summary>
        VertexCreaseIndexBuffer = 0x07000000,

        /// <summary>
        /// The vertex crease weight buffer.
        /// </summary>
        VertexCreaseWeightBuffer = 0x08000000,

        /// <summary>
        /// The hole buffer.
        /// </summary>
        HoleBuffer = 0x09000001,
    }

    /// <summary>
    /// Specifies the available geometry flags.
    /// </summary>
    [Flags]
    public enum GeometryFlags
    {
        /// <summary>
        /// Geometry that will change rarely.
        /// </summary>
        Static = 0,

        /// <summary>
        /// Geometry with deformable (fixed connectivity) motion.
        /// </summary>
        Deformable = 1,

        /// <summary>
        /// Geometry with arbitrary motion.
        /// </summary>
        Dynamic = 2,
    }

    /// <summary>
    /// Specifies the method signature for Embree error callbacks.
    /// </summary>
    public delegate void ErrorCallback(NativeMethods.Error code,
                                       [MarshalAs(UnmanagedType.LPStr)] String str);

    /// <summary>
    /// Specifies the method signature for Embree memory monitor callbacks.
    /// </summary>
    public delegate bool MemoryMonitorCallback(IntPtr bytes,
                                               bool post);

    /// <summary>
    /// Specifies the method signature for Embree progress monitor callbacks.
    /// </summary>
    public delegate bool ProgressMonitorCallback(IntPtr ptr,
                                                 double n);

    /// <summary>
    /// Provides access to the raw Embree API calls.
    /// </summary>
    public static unsafe class NativeMethods
    {
        public const String DLLName = "embree";

        /// <summary>
        /// Specifies the error codes returned by the Embree library.
        /// </summary>
        public enum Error
        {
            /// <summary>
            /// No error has been recorded.
            /// </summary>
            NoError = 0,

            /// <summary>
            /// An unknown error has occurred.
            /// </summary>
            UnknownError = 1,

            /// <summary>
            /// An invalid argument is specified.
            /// </summary>
            InvalidArgument = 2,

            /// <summary>
            /// The operation is not allowed for the specified object.
            /// </summary>
            InvalidOperation = 3,

            /// <summary>
            /// There is not enough memory left to execute the command.
            /// </summary>
            OutOfMemory = 4,

            /// <summary>
            /// The CPU is not supported as it does not support SSE2.
            /// </summary>
            UnsupportedCPU = 5,

            /// <summary>
            /// The user has cancelled the operation through the RTCProgressMonitorFunc callback.
            /// </summary>
            Cancelled = 6,
        }

        [DllImport(DLLName)]
        public static extern Error rtcGetError();

        [DllImport(DLLName, CharSet=CharSet.Ansi, BestFitMapping=false, ThrowOnUnmappableChar=true)]
        public static extern void rtcInit([MarshalAs(UnmanagedType.LPStr)] String cfg);

        [DllImport(DLLName)]
        public static extern void rtcSetErrorFunction(ErrorCallback func);

        [DllImport(DLLName)]
        public static extern void rtcSetMemoryMonitorFunction(MemoryMonitorCallback func);

        [DllImport(DLLName)]
        public static extern void rtcExit();

        [DllImport(DLLName)]
        public static extern IntPtr rtcNewScene(SceneFlags flags,
                                                AlgorithmFlags aFlags);

        [DllImport(DLLName)]
        public static extern void rtcSetProgressMonitorFunction(IntPtr scene,
                                                                ProgressMonitorCallback func,
                                                                IntPtr ptr);

        [DllImport(DLLName)]
        public static extern void rtcCommit(IntPtr scene);

        [DllImport(DLLName)]
        public static extern void rtcCommitThread(IntPtr scene,
                                                  int threadID,
                                                  int numThreads);

        [DllImport(DLLName)]
        public static extern void rtcDeleteScene(IntPtr scene);

        [DllImport(DLLName)]
        public static extern Int32 rtcNewInstance(IntPtr target,
                                                  IntPtr source);

        [DllImport(DLLName)]
        public static extern void rtcSetTransform(IntPtr scene,
                                                  Int32 geomID,
                                                  MatrixLayout layout,
                                                  float* transform);

        [DllImport(DLLName)]
        public static extern Int32 rtcNewTriangleMesh(IntPtr scene,
                                                      GeometryFlags flags,
                                                      UIntPtr numTriangles,
                                                      UIntPtr numVertices,
                                                      UIntPtr numTimeSteps);

        [DllImport(DLLName)]
        public static extern Int32 rtcNewSubdivisionMesh(IntPtr scene,
                                                         GeometryFlags flags,
                                                         UIntPtr numFaces,
                                                         UIntPtr numEdges,
                                                         UIntPtr numVertices,
                                                         UIntPtr numEdgeCreases,
                                                         UIntPtr numVertexCreases,
                                                         UIntPtr numHoles,
                                                         UIntPtr numTimeSteps);

        [DllImport(DLLName)]
        public static extern Int32 rtcNewHairGeometry(IntPtr scene,
                                                      GeometryFlags flags,
                                                      UIntPtr numCurves,
                                                      UIntPtr numVertices,
                                                      UIntPtr numTimeSteps);

        [DllImport(DLLName)]
        public static extern void rtcEnable(IntPtr scene,
                                            Int32 geomID);

        [DllImport(DLLName)]
        public static extern void rtcDisable(IntPtr scene,
                                             Int32 geomID);

        [DllImport(DLLName)]
        public static extern void rtcSetMask(IntPtr scene,
                                             Int32 geomID,
                                             uint mask);

        [DllImport(DLLName)]
        public static extern void rtcDeleteGeometry(IntPtr scene,
                                                    Int32 geomID);

        [DllImport(DLLName)]
        public static extern IntPtr rtcMapBuffer(IntPtr scene,
                                                 Int32 geomID,
                                                 BufferType type);

        [DllImport(DLLName)]
        public static extern void rtcUpdate(IntPtr scene,
                                            Int32 geomID);

        [DllImport(DLLName)]
        public static extern void rtcUpdateBuffer(IntPtr scene,
                                                  Int32 geomID,
                                                  BufferType type);

        [DllImport(DLLName)]
        public static extern void rtcUnmapBuffer(IntPtr scene,
                                                 Int32 geomID,
                                                 BufferType type);

        [DllImport(DLLName)]
        public static extern void rtcSetBuffer(IntPtr scene,
                                               Int32 geomID,
                                               BufferType type,
                                               IntPtr ptr,
                                               UIntPtr offset,
                                               UIntPtr stride);

        [DllImport(DLLName)]
        public static extern void rtcIntersect(IntPtr scene,
                                               RayStruct1* ray);

        [DllImport(DLLName)]
        public static extern void rtcIntersect4(uint* valid,
                                                IntPtr scene,
                                                RayStruct4* ray);

        [DllImport(DLLName)]
        public static extern void rtcIntersect8(uint* valid,
                                                IntPtr scene,
                                                RayStruct8* ray);

        [DllImport(DLLName)]
        public static extern void rtcIntersect16(uint* valid,
                                                 IntPtr scene,
                                                 RayStruct16* ray);

        [DllImport(DLLName)]
        public static extern void rtcOccluded(IntPtr scene,
                                              RayStruct1* ray);

        [DllImport(DLLName)]
        public static extern void rtcOccluded4(uint* valid,
                                               IntPtr scene,
                                               RayStruct4* ray);

        [DllImport(DLLName)]
        public static extern void rtcOccluded8(uint* valid,
                                               IntPtr scene,
                                               RayStruct8* ray);

        [DllImport(DLLName)]
        public static extern void rtcOccluded16(uint* valid,
                                                IntPtr scene,
                                                RayStruct16* ray);
    }



    /// <summary>
    /// Provides access to the exception-wrapped Embree API.
    /// </summary>
    /// <remarks>
    /// The intersection/occlusion functions are checked in debug mode only.
    /// </remarks>
    public static unsafe class RTC
    {
        private static void CheckLastError()
        {
            switch (NativeMethods.rtcGetError())
            {
                case NativeMethods.Error.UnknownError:
                    throw new InvalidOperationException("An unknown error occurred in the Embree library.");
                case NativeMethods.Error.InvalidArgument:
                    throw new ArgumentException("An argument to an Embree function was invalid.");
                case NativeMethods.Error.InvalidOperation:
                    throw new InvalidOperationException("An invalid operation was attempted on an Embree object.");
                case NativeMethods.Error.OutOfMemory:
                    throw new OutOfMemoryException("The Embree library encountered an out-of-memory condition.");
                case NativeMethods.Error.UnsupportedCPU:
                    throw new InvalidOperationException("This operation is not valid due to an unsupported processor.");
                case NativeMethods.Error.Cancelled:
                    throw new OperationCanceledException("This operation was interrupted by a progress monitor callback.");
            }
        }

        public static void Initialize(String cfg = null)
        {
            NativeMethods.rtcInit(cfg);
            CheckLastError();
        }

        public static void SetErrorCallback(ErrorCallback callback)
        {
            NativeMethods.rtcSetErrorFunction(callback);
            CheckLastError();
        }

        public static void SetMemoryMonitorCallback(MemoryMonitorCallback callback)
        {
            NativeMethods.rtcSetMemoryMonitorFunction(callback);
            CheckLastError();
        }

        #pragma warning disable 465
        public static void Finalize()
        {
            NativeMethods.rtcExit();
        }
        #pragma warning restore 465

        public static IntPtr NewScene(SceneFlags flags,
                                      AlgorithmFlags aFlags)
        {
            var retval = NativeMethods.rtcNewScene(flags,
                                                   aFlags);
            CheckLastError();
            return retval;
        }

        public static void SetProgressMonitorCallback(IntPtr scene,
                                                      ProgressMonitorCallback callback,
                                                      IntPtr ptr = default(IntPtr))
        {
            NativeMethods.rtcSetProgressMonitorFunction(scene,
                                                        callback,
                                                        ptr);
            CheckLastError();
        }

        public static void Commit(IntPtr scene)
        {
            NativeMethods.rtcCommit(scene);
            CheckLastError();
        }

        public static void CommitThread(IntPtr scene,
                                        int threadID,
                                        int numThreads)
        {
            NativeMethods.rtcCommitThread(scene,
                                          threadID,
                                          numThreads);
            CheckLastError();
        }

        public static void DeleteScene(IntPtr scene)
        {
            NativeMethods.rtcDeleteScene(scene);
            CheckLastError();
        }

        public static Int32 NewInstance(IntPtr target,
                                        IntPtr source)
        {
            var retval = NativeMethods.rtcNewInstance(target,
                                                      source);
            CheckLastError();
            return retval;
        }

        public static void SetTransform(IntPtr scene,
                                        Int32 geomID,
                                        MatrixLayout layout,
                                        float* transform)
        {
            NativeMethods.rtcSetTransform(scene,
                                          geomID,
                                          layout,
                                          transform);
            CheckLastError();
        }

        public static Int32 NewTriangleMesh(IntPtr scene,
                                            GeometryFlags flags,
                                            int numTriangles,
                                            int numVertices,
                                            int numTimeSteps)
        {
            var retval = NativeMethods.rtcNewTriangleMesh(scene,
                                                          flags,
                                                          (UIntPtr)numTriangles,
                                                          (UIntPtr)numVertices,
                                                          (UIntPtr)numTimeSteps);
            CheckLastError();
            return retval;
        }

        public static Int32 NewSubdivisionMesh(IntPtr scene,
                                               GeometryFlags flags,
                                               UIntPtr numFaces,
                                               UIntPtr numEdges,
                                               UIntPtr numVertices,
                                               UIntPtr numEdgeCreases,
                                               UIntPtr numVertexCreases,
                                               UIntPtr numHoles,
                                               UIntPtr numTimeSteps)
        {
            var retval = NativeMethods.rtcNewSubdivisionMesh(scene,
                                                             flags,
                                                             numFaces,
                                                             numEdges,
                                                             numVertices,
                                                             numEdgeCreases,
                                                             numVertexCreases,
                                                             numHoles,
                                                             numTimeSteps);
            CheckLastError();
            return retval;
        }

        public static Int32 NewHairGeometry(IntPtr scene,
                                            GeometryFlags flags,
                                            UIntPtr numCurves,
                                            UIntPtr numVertices,
                                            UIntPtr numTimeSteps)
        {
            var retval = NativeMethods.rtcNewHairGeometry(scene,
                                                          flags,
                                                          numCurves,
                                                          numVertices,
                                                          numTimeSteps);
            CheckLastError();
            return retval;
        }

        public static void Enable(IntPtr scene,
                                  Int32 geomID)
        {
            NativeMethods.rtcEnable(scene,
                                    geomID);
            CheckLastError();
        }

        public static void Disable(IntPtr scene,
                                   Int32 geomID)
        {
            NativeMethods.rtcDisable(scene,
                                     geomID);
            CheckLastError();
        }

        public static void SetMask(IntPtr scene,
                                   Int32 geomID,
                                   uint mask)
        {
            NativeMethods.rtcSetMask(scene,
                                     geomID,
                                     mask);
            CheckLastError();
        }

        public static void DeleteGeometry(IntPtr scene,
                                          Int32 geomID)
        {
            NativeMethods.rtcDeleteGeometry(scene,
                                            geomID);
            CheckLastError();
        }

        public static IntPtr MapBuffer(IntPtr scene,
                                       Int32 geomID,
                                       BufferType type)
        {
            var retval = NativeMethods.rtcMapBuffer(scene,
                                                    geomID,
                                                    type);
            CheckLastError();
            return retval;
        }

        public static void Update(IntPtr scene,
                                  Int32 geomID)
        {
            NativeMethods.rtcUpdate(scene,
                                    geomID);
            CheckLastError();
        }

        public static void UpdateBuffer(IntPtr scene,
                                        Int32 geomID,
                                        BufferType type)
        {
            NativeMethods.rtcUpdateBuffer(scene,
                                          geomID,
                                          type);
            CheckLastError();
        }

        public static void UnmapBuffer(IntPtr scene,
                                       Int32 geomID,
                                       BufferType type)
        {
            NativeMethods.rtcUnmapBuffer(scene,
                                         geomID,
                                         type);
            CheckLastError();
        }

        public static void SetBuffer(IntPtr scene,
                                     Int32 geomID,
                                     BufferType type,
                                     IntPtr ptr,
                                     UIntPtr offset,
                                     UIntPtr stride)
        {
            NativeMethods.rtcSetBuffer(scene,
                                       geomID,
                                       type,
                                       ptr,
                                       offset,
                                       stride);
            CheckLastError();
        }

        public static void Intersect(IntPtr scene,
                                     RayStruct1* ray)
        {
            NativeMethods.rtcIntersect(scene,
                                       ray);
            #if DEBUG
            CheckLastError();
            #endif
        }

        public static void Intersect4(uint* valid,
                                      IntPtr scene,
                                      RayStruct4* ray)
        {
            NativeMethods.rtcIntersect4(valid,
                                        scene,
                                        ray);
            #if DEBUG
            CheckLastError();
            #endif
        }

        public static void Intersect8(uint* valid,
                                      IntPtr scene,
                                      RayStruct8* ray)
        {
            NativeMethods.rtcIntersect8(valid,
                                        scene,
                                        ray);
            #if DEBUG
            CheckLastError();
            #endif
        }

        public static void Intersect16(uint* valid,
                                       IntPtr scene,
                                       RayStruct16* ray)
        {
            NativeMethods.rtcIntersect16(valid,
                                         scene,
                                         ray);
            #if DEBUG
            CheckLastError();
            #endif
        }

        public static void Occluded(IntPtr scene,
                                    RayStruct1* ray)
        {
            NativeMethods.rtcOccluded(scene,
                                      ray);
            #if DEBUG
            CheckLastError();
            #endif
        }

        public static void Occluded4(uint* valid,
                                     IntPtr scene,
                                     RayStruct4* ray)
        {
            NativeMethods.rtcOccluded4(valid,
                                       scene,
                                       ray);
            #if DEBUG
            CheckLastError();
            #endif
        }

        public static void Occluded8(uint* valid,
                                     IntPtr scene,
                                     RayStruct8* ray)
        {
            NativeMethods.rtcOccluded8(valid,
                                       scene,
                                       ray);
            #if DEBUG
            CheckLastError();
            #endif
        }

        public static void Occluded16(uint* valid,
                                      IntPtr scene,
                                      RayStruct16* ray)
        {
            NativeMethods.rtcOccluded16(valid,
                                        scene,
                                        ray);
            #if DEBUG
            CheckLastError();
            #endif
        }

        /// <summary>
        /// The default unassigned/invalid value for the various IDs.
        /// </summary>
        public const Int32 InvalidID = -1;

        /// <summary>
        /// If you use the RTC interop wrapper, Embree is initialized for you.
        /// </summary>
        static RTC()
        {
            RTC.Initialize();
        }
    }

    /// <summary>
    /// The native Embree ray structure (single ray).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 96)]
    public unsafe struct RayStruct1
    {
        [FieldOffset( 0)] public float orgX;
        [FieldOffset( 4)] public float orgY;
        [FieldOffset( 8)] public float orgZ;

        [FieldOffset(16)] public float dirX;
        [FieldOffset(20)] public float dirY;
        [FieldOffset(24)] public float dirZ;

        [FieldOffset(32)] public float tnear;
        [FieldOffset(36)] public float tfar;
        [FieldOffset(40)] public float time;
        [FieldOffset(44)] public uint mask;

        [FieldOffset(48)] public float NgX;
        [FieldOffset(52)] public float NgY;
        [FieldOffset(56)] public float NgZ;

        [FieldOffset(64)] public float u;
        [FieldOffset(68)] public float v;

        [FieldOffset(72)] public Int32 geomID;
        [FieldOffset(76)] public Int32 primID;
        [FieldOffset(80)] public Int32 instID;

        /// <summary>
        /// The required alignment of this structure in bytes.
        /// </summary>
        public const int Alignment = 16;
    }

    /// <summary>
    /// The native Embree ray structure (4-ray packet).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 288)]
    public unsafe struct RayStruct4
    {
        [FieldOffset(  0)] public fixed float orgX[4];
        [FieldOffset( 16)] public fixed float orgY[4];
        [FieldOffset( 32)] public fixed float orgZ[4];
        [FieldOffset( 48)] public fixed float dirX[4];
        [FieldOffset( 64)] public fixed float dirY[4];
        [FieldOffset( 80)] public fixed float dirZ[4];
        [FieldOffset( 96)] public fixed float tnear[4];
        [FieldOffset(112)] public fixed float tfar[4];
        [FieldOffset(128)] public fixed float time[4];
        [FieldOffset(144)] public fixed uint mask[4];

        [FieldOffset(160)] public fixed float NgX[4];
        [FieldOffset(176)] public fixed float NgY[4];
        [FieldOffset(192)] public fixed float NgZ[4];

        [FieldOffset(208)] public fixed float u[4];
        [FieldOffset(224)] public fixed float v[4];

        [FieldOffset(240)] public fixed Int32 geomID[4];
        [FieldOffset(256)] public fixed Int32 primID[4];
        [FieldOffset(272)] public fixed Int32 instID[4];

        /// <summary>
        /// The required alignment of this structure in bytes.
        /// </summary>
        public const int Alignment = 16;
    }

    /// <summary>
    /// The native Embree ray structure (8-ray packet).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 576)]
    public unsafe struct RayStruct8
    {
        [FieldOffset(  0)] public fixed float orgX[8];
        [FieldOffset( 32)] public fixed float orgY[8];
        [FieldOffset( 64)] public fixed float orgZ[8];
        [FieldOffset( 96)] public fixed float dirX[8];
        [FieldOffset(128)] public fixed float dirY[8];
        [FieldOffset(160)] public fixed float dirZ[8];
        [FieldOffset(192)] public fixed float tnear[8];
        [FieldOffset(224)] public fixed float tfar[8];
        [FieldOffset(256)] public fixed float time[8];
        [FieldOffset(288)] public fixed uint mask[8];

        [FieldOffset(320)] public fixed float NgX[8];
        [FieldOffset(352)] public fixed float NgY[8];
        [FieldOffset(384)] public fixed float NgZ[8];

        [FieldOffset(416)] public fixed float u[8];
        [FieldOffset(448)] public fixed float v[8];

        [FieldOffset(480)] public fixed Int32 geomID[8];
        [FieldOffset(512)] public fixed Int32 primID[8];
        [FieldOffset(544)] public fixed Int32 instID[8];

        /// <summary>
        /// The required alignment of this structure in bytes.
        /// </summary>
        public const int Alignment = 32;
    }

    /// <summary>
    /// The native Embree ray structure (16-ray packet).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 1152)]
    public unsafe struct RayStruct16
    {
        [FieldOffset(   0)] public fixed float orgX[16];
        [FieldOffset(  64)] public fixed float orgY[16];
        [FieldOffset( 128)] public fixed float orgZ[16];
        [FieldOffset( 192)] public fixed float dirX[16];
        [FieldOffset( 256)] public fixed float dirY[16];
        [FieldOffset( 320)] public fixed float dirZ[16];
        [FieldOffset( 384)] public fixed float tnear[16];
        [FieldOffset( 448)] public fixed float tfar[16];
        [FieldOffset( 512)] public fixed float time[16];
        [FieldOffset( 576)] public fixed uint mask[16];

        [FieldOffset( 640)] public fixed float NgX[16];
        [FieldOffset( 704)] public fixed float NgY[16];
        [FieldOffset( 768)] public fixed float NgZ[16];

        [FieldOffset( 832)] public fixed float u[16];
        [FieldOffset( 896)] public fixed float v[16];

        [FieldOffset( 960)] public fixed Int32 geomID[16];
        [FieldOffset(1024)] public fixed Int32 primID[16];
        [FieldOffset(1088)] public fixed Int32 instID[16];

        /// <summary>
        /// The required alignment of this structure in bytes.
        /// </summary>
        public const int Alignment = 64;
    }
}