using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SharpRT
{
    public struct Sphere
    {
        Point center;
        float radius;

        public Sphere(Point center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        /// <summary>
        /// Tests whether the ray intersects the sphere, and if
        /// it does, returns the distance to intersection.
        /// </summary>
        public bool Intersect(Ray ray, out float distance, float minDistance = 1e-4f, float maxDistance = float.MaxValue)
        {
            Vector s = center - ray.Origin;
            float sd = Vector.Dot(s, ray.Direction);
            float ss = Vector.Dot(s, s);

            float disc = sd * sd - ss + radius * radius;

            // this means the line the ray is on does not intersect the sphere
            if (disc < 0) {
                distance = 0;
                return false;
            }

            // so here we have at most two intersection points
            // .. but hang on, the ray's origin might be inside
            // the sphere, in which case one solution is negative
            // while the other is positive. so we can't just take
            // the closest one naively

            float q = (float)Math.Sqrt(disc);
            float p1 = sd - q;
            float p2 = sd + q;

            // note that since q is nonnegative, p1 <= p2

            if ((minDistance <= p1) && (p1 < maxDistance)) {
                distance = p1;
                return true;
            } else if ((minDistance <= p2) && (p2 < maxDistance)) {
                distance = p2;
                return true;
            } else {
                distance = 0;
                return false;
            }
        }

        /// <summary>
        /// Returns the surface normal at any point on the sphere.
        /// </summary>
        /// <param name="pos">Point on the sphere's surface.</param>
        public Vector Normal(Point pos)
        {
            return Vector.Normalize(pos - center);
        }
    }

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

    public interface IMaterial
    {
        /// <summary>
        /// Returns the weight to account for when importance-sampling this material.
        /// </summary>
        /// <remarks>
        /// Formally, this is the (per-component) weight associated with
        /// an importance-sampled direction obtained using SamplePDF().
        /// </remarks>
        Vector WeightPDF(Direction outgoing, Basis basis);

        /// <summary>
        /// Importance-samples this material, returning a (possibly randomly selected)
        /// incoming direction according to this material's probability density
        /// function, for the outgoing direction provided.
        /// </summary>
        /// <remarks>
        /// The method signature may be improved later on by replacing the
        /// "random" parameter with something more appropriate.
        /// </remarks>
        Vector SamplePDF(Direction outgoing, Basis basis, Random random);

        /// <summary>
        /// Evaluates this material's BRDF for an incoming and outgoing direction.
        /// </summary>
        Vector BRDF(Direction incoming, Direction outgoing, Basis basis);

        /// <summary>
        /// Evaluates this material's emittance along some outgoing direction.
        /// </summary>
        Vector Emittance(Direction outgoing, Basis basis);
    }

    public struct Lambertian : IMaterial
    {
        private Vector albedo;
        private Vector emittance;

        public Lambertian(Vector albedo, Vector emittance = default(Vector))
        {
            this.albedo = albedo; // 0 <= albedo <= 1 (for each channel)
            this.emittance = emittance;
        }

        public Vector WeightPDF(Direction outgoing, Basis basis)
        {
            return this.albedo;
        }

        public Vector SamplePDF(Direction outgoing, Basis basis, Random random)
        {
            double u1 = random.NextDouble();
            double u2 = random.NextDouble();

            double sin_theta = Math.Sqrt(u1);
            double cos_theta = Math.Sqrt(1 - u1);

            double phi = 2 * Math.PI * u2;

            Vector dir = new Vector((float)(sin_theta * Math.Cos(phi)),
                                    (float)(cos_theta),
                                    (float)(sin_theta * Math.Sin(phi)));

            return Vector.Normalize(basis.Transform * dir);
        }

        public Vector BRDF(Direction incoming, Direction outgoing, Basis basis)
        {
            return this.albedo / (float)Math.PI;
        }

        public Vector Emittance(Direction outgoing, Basis basis)
        {
            return emittance;
        }
    }

    public struct Mirror : IMaterial
    {
        private Vector reflection;
        private Vector emittance;

        public Mirror(Vector reflection, Vector emittance = default(Vector))
        {
            this.reflection = reflection;
            this.emittance = emittance;
        }

        public Vector WeightPDF(Direction outgoing, Basis basis)
        {
            return this.reflection;
        }

        public Vector SamplePDF(Direction outgoing, Basis basis, Random random)
        {
            return 2 * basis.Normal * outgoing.CosTheta - outgoing.Vector;
        }

        public Vector BRDF(Direction incoming, Direction outgoing, Basis basis)
        {
            /* Correct whenever incoming and outgoing are not reflections of each other
             * about the basis normal. Since the BRDF is currently only used for direct
             * point light source sampling, that has probability zero of occurring.
             * 
             * Besides, delta functions are tricky to implement in code because of the
             * inaccuracies inherent to floating-point. If we were using fixed point
             * arithmetic, then it might actually make sense to implement the real BRDF.
            */

            return Vector.Zero;
        }

        public Vector Emittance(Direction outgoing, Basis basis)
        {
            return emittance;
        }
    }

    public struct Light
    {
        public Point position;
        public float intensity;

        public Light(Point position, float intensity)
        {
            this.position = position;
            this.intensity = intensity;
        }
    }

    class MainClass
    {
        private static IList<Sphere> geometry = new List<Sphere>() {
            new Sphere(new Point(0, -255, 0), 250),
            new Sphere(new Point(0, +255, 0), 250),
            new Sphere(new Point(-255, 0, 0), 250),
            new Sphere(new Point(+255, 0, 0), 250),
            new Sphere(new Point(0, 0, -255), 250),
            new Sphere(new Point(0, 0, +255), 250),
            new Sphere(new Point(2, 0, 2), 1),
            /* new Sphere(new Point(0, 55 - 0.05f, 1), 50), */ // use for area light
        };

        private static IList<IMaterial> materials = new List<IMaterial>() {
            new Lambertian(new Vector(0.75f, 0.75f, 0.75f)),
            new Lambertian(new Vector(0.75f, 0.75f, 0.75f)),
            new Lambertian(new Vector(0.75f, 0.25f, 0.25f)),
            new Lambertian(new Vector(0.25f, 0.25f, 0.75f)),
            new Lambertian(new Vector(0.75f, 0.75f, 0.75f)),
            new Lambertian(new Vector(0.75f, 0.75f, 0.75f)),
            new Mirror(new Vector(0.8f, 0.8f, 0.8f)),
            /* new Lambertian(new Vector(0.75f, 0.75f, 0.75f), new Vector(6.0f, 6.0f, 6.0f)), */ // use for area light
        };

        private static IList<Light> lights = new List<Light>() {
            new Light(new Point(0, 1, 1), 35),
        };

        public static bool Intersect(Ray ray, out int sphereIndex, out float distance,
                                     float minDistance = 1e-4f, float maxDistance = float.MaxValue)
        {
            distance = maxDistance;
            sphereIndex = -1;

            for (int t = 0; t < geometry.Count; ++t) {
                float distToSphere;

                if (geometry[t].Intersect(ray, out distToSphere, minDistance, distance)) {
                    distance = distToSphere;
                    sphereIndex = t;
                }
            }

            return sphereIndex != -1;
        }

        public static bool Occlude(Ray ray, float minDistance = 1e-4f, float maxDistance = float.MaxValue)
        {
            for (int t = 0; t < geometry.Count; ++t) {
                float distToSphere;

                if (geometry[t].Intersect(ray, out distToSphere, minDistance, maxDistance)) {
                    return true;
                }
            }

            return false;
        }

        [ThreadStatic] private static Random random;

        public static Vector Radiance(Ray ray)
        {
            const int MAX_BOUNCES = 10;

            Vector weights = new Vector(1.0f, 1.0f, 1.0f);
            Vector radiance = Vector.Zero;

            for (int bounce = 0; bounce < MAX_BOUNCES; ++bounce) {
                float distance;
                int hitSphere;

                if (!Intersect(ray, out hitSphere, out distance)) {
                    break;
                }

                Sphere sphere = geometry[hitSphere];
                IMaterial material = materials[hitSphere];
                var hitPoint = Ray.PointAt(ray, distance);

                var basis = new Basis(sphere.Normal(hitPoint));
                var outgoing = new Direction(-ray.Direction, basis);

                /* Include object emittance as light path */

                radiance += weights * material.Emittance(outgoing, basis);

                /* Include point light source light paths */

                foreach (var light in lights) {
                    var pointToLight = light.position - hitPoint;
                    var lightDistance = pointToLight.Length();

                    if (!Occlude(new Ray(hitPoint, pointToLight), 1e-4f, lightDistance)) {
                        Direction incomimg = new Direction(pointToLight / lightDistance, basis);

                        radiance += weights * material.BRDF(incomimg, outgoing, basis) * incomimg.CosTheta
                                  * light.intensity / (lightDistance * lightDistance);
                    }
                }

                /* Russian roulette */

                Vector weight = material.WeightPDF(outgoing, basis);
                float p = Math.Max(weight.X, Math.Max(weight.Y, weight.Z));

                if (bounce > 2) {
                    if (random.NextDouble() <= p) {
                        weight /= p;
                    } else {
                        break;
                    }
                }

                /* Update light path weights and prepare for next bounce */

                weights *= weight;

                var newDir = material.SamplePDF(outgoing, basis, random);

                ray = new Ray(hitPoint, newDir);
            }

            return radiance;
        }

        public static int floatToInt(float rgb)
        {
            if (rgb < 0) {
                return 0;
            } else if (rgb > 1) {
                return 255;
            } else {
                return (int)(rgb * 255);
            }
        }

        public static void Main(string[] args)
        {
            // first setup the camera

            var camera = new Camera(new Point(0, 0, -4), 0, 0, (float)(75 * Math.PI / 180));

            // we'll output the result to an image

            var img = new Bitmap(640, 480, PixelFormat.Format24bppRgb);

            float uScale = 1;
            float vScale = 1;

            if (img.Width > img.Height) {
                uScale = (float)img.Width / img.Height;
            } else if (img.Height > img.Width) {
                vScale = (float)img.Height / img.Width;
            }

            byte[] pixelData = new byte[img.Width * img.Height * 3];

            // render in parallel (easy since it's an embarrassingly parallel problem)

            Parallel.For(0, img.Height, (y) => {
                random = new Random(); // create random generator for this thread

                for (int x = 0; x < img.Width; ++x) {
                    // compute the resolution-independent camera uv coordinates
                    float u = 2 * ((float)x / (img.Width - 1)) - 1;
                    float v = 1 - 2 * ((float)y / (img.Height - 1));

                    u *= uScale;
                    v *= vScale;

                    // get the corresponding camera ray for this pixel
                    var ray = camera.TraceRay(u, v);

                    Vector radiance = Vector.Zero;

                    const int SAMPLES = 50; // more = crisper image

                    for (int s = 0; s < SAMPLES; ++s) {
                        radiance += Radiance(ray);
                    }

                    pixelData[3 * (y * img.Width + x) + 2] = (byte)floatToInt(radiance.X / SAMPLES); /* BGR legacy */
                    pixelData[3 * (y * img.Width + x) + 1] = (byte)floatToInt(radiance.Y / SAMPLES);
                    pixelData[3 * (y * img.Width + x) + 0] = (byte)floatToInt(radiance.Z / SAMPLES);
                }
            });

            var bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height),
                                          ImageLockMode.WriteOnly,
                                          PixelFormat.Format24bppRgb);


            Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
            img.UnlockBits(bitmapData);

            // and save the resulting bitmap as a PNG file

            img.Save("output.png", ImageFormat.Png);

            Console.WriteLine("Done!");
        }
    }
}
