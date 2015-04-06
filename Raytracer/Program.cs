using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

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
        public bool Intersect(Ray ray, out float distance)
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

            if (p1 < 0) { // p1 is behind the ray, so p2 must be in front
                distance = p2;
            } else if (p2 < 0) {
                distance = p1;
            } else { // both are in front, just return the closest one
                distance = Math.Min(p1, p2);
            }

            return distance >= 0;
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
        private Matrix basisTransform;

        public Matrix Transform
        {
            get { return basisTransform; }
        }

        public Vector Tangent
        {
            get { return basisTransform.U; }
        }

        public Vector Normal
        {
            get { return basisTransform.V; }
        }

        public Vector Bitangent
        {
            get { return basisTransform.W; }
        }

        /// <summary>
        /// Creates an unoriented basis about the given normal axis.
        /// </summary>
        public Basis(Vector normal)
        {
            Vector dv;

            if (normal.X != 0) {
                dv = new Vector(0, 1, 1);
            } else if (normal.Y != 0) {
                dv = new Vector(1, 0, 1);
            } else if (normal.Z != 0) {
                dv = new Vector(1, 1, 0);
            } else {
                throw new ArgumentException("Normal vector was zero.");
            }

            var tmp = Vector.Cross(normal, dv);
            var tangent = Vector.Cross(normal, tmp);
            var bitangent = Vector.Cross(normal, tangent);

            basisTransform = new Matrix(tangent, normal, bitangent, Vector.Zero);
        }

        public Basis(Vector normal, Vector tangent, Vector bitangent)
        {
            basisTransform = new Matrix(tangent, normal, bitangent, Vector.Zero);
        }
    };

    public struct LocalDirection
    {
        private Vector direction;
        private float cosine;

        public float Cosine
        {
            get { return cosine; }
        }

        public Vector Direction
        {
            get { return direction; }
        }

        public LocalDirection(Vector direction, Basis basis)
        {
            this.direction = direction;
            this.cosine = Vector.Dot(basis.Normal, direction);
        }
    }

    public interface IMaterial
    {
        Vector Weight(LocalDirection outgoing, Basis relativeBasis, Random random /* sampling parameters ... */);
        Vector Sample(LocalDirection outgoing, Basis relativeBasis, Random random /* sampling parameters ... */);
        Vector BRDF(LocalDirection incident, LocalDirection outgoing, Basis relativeBasis);
    }

    public struct Lambertian : IMaterial
    {
        private Vector albedo;

        public Lambertian(Vector albedo)
        {
            this.albedo = albedo; // 0 <= albedo <= 1 (for each channel)
        }

        public Vector Weight(LocalDirection outgoing, Basis relativeBasis, Random random)
        {
            return this.albedo;
        }

        public Vector Sample(LocalDirection outgoing, Basis relativeBasis, Random random)
        {
            double Xi1 = random.NextDouble();
            double Xi2 = random.NextDouble();

            double theta = Math.Acos(Math.Sqrt(1 - Xi1));
            double phi = 2.0 * Math.PI * Xi2;

            float xs = (float)(Math.Sin(theta) * Math.Cos(phi));
            float ys = (float)(Math.Cos(theta));
            float zs = (float)(Math.Sin(theta) * Math.Sin(phi));

            return Vector.Normalize(relativeBasis.Transform * new Vector(xs, ys, zs));
        }

        public Vector BRDF(LocalDirection incident, LocalDirection outgoing, Basis relativeBasis)
        {
            return this.albedo / (float)Math.PI;
        }
    }

    public struct Mirror : IMaterial
    {
        private Vector reflection;

        public Mirror(Vector reflection)
        {
            this.reflection = reflection;
        }

        public Vector Weight(LocalDirection outgoing, Basis relativeBasis, Random random)
        {
            return this.reflection * outgoing.Cosine;
        }

        public Vector Sample(LocalDirection outgoing, Basis relativeBasis, Random random)
        {
            return outgoing.Direction + 2 * relativeBasis.Normal * outgoing.Cosine;
        }

        public Vector BRDF(LocalDirection incident, LocalDirection outgoing, Basis relativeBasis)
        {
            return Vector.Zero; /* actual probability of incident = reflect(outgoing, basis.normal) is zero */
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
            new Sphere(new Point(-1, 1, 10), 2),
            new Sphere(new Point(1, -1, 4), 1),
            new Sphere(new Point(0, -255, 0), 250),
        };

        private static IList<IMaterial> materials = new List<IMaterial>() {
            new Mirror(new Vector(1, 1, 1)),
            new Lambertian(new Vector(0, 0, 0.6f)),
            new Lambertian(new Vector(0, 1, 0)),
        };

        private static IList<Light> lights = new List<Light>() {
            new Light(new Point(0, 6, 0), 160),
            new Light(new Point(-2, 4, 1), 50),
        };

        public static bool Intersect(Ray ray, out int sphereIndex, out float distance,
                                     float minDistance = 1e-4f, float maxDistance = float.MaxValue)
        {
            distance = maxDistance;
            sphereIndex = -1;

            for (int t = 0; t < geometry.Count; ++t) {
                float distToSphere;

                if (geometry[t].Intersect(ray, out distToSphere)) {
                    if ((minDistance <= distToSphere) && (distToSphere < distance)) {
                        distance = distToSphere;
                        sphereIndex = t;
                    }
                }
            }

            return sphereIndex != -1;
        }

        private static Random random = new Random();

        public static Vector Radiance(Ray ray, bool recurse = true)
        {
            Vector total = Vector.Zero;

            float distance;
            int hitSphere;

            if (Intersect(ray, out hitSphere, out distance)) {
                Point hitPoint = ray.PointAt(distance);

                Vector normal = geometry[hitSphere].Normal(hitPoint);

                Basis basis = new Basis(normal);

                var outgoing = new LocalDirection(-ray.Direction, basis);

                // evaluate radiance in the direction of the point light source(s)

                foreach (var light in lights) {
                    Vector hitPointToLight = light.position - hitPoint;
                    float distanceToLight = Vector.Length(hitPointToLight);

                    Ray lightRay = new Ray(hitPoint, hitPointToLight);

                    float distanceToObstacle;
                    int unused;

                    if (!Intersect(lightRay, out unused, out distanceToObstacle, 1e-4f, distanceToLight)) {
                        var incident = new LocalDirection(lightRay.Direction, basis);

                        total += materials[hitSphere].BRDF(incident, outgoing, basis) * incident.Cosine * (light.intensity / (float)Math.Pow(distanceToLight, 2));
                    }
                }

                if (recurse) {
                    // now try and approximately evaluate light contribution over the entire hemisphere

                    const int NUM_DIRECTIONS = 10;

                    var weight = materials[hitSphere].Weight(outgoing, basis, random);

                    for (int i = 0; i < NUM_DIRECTIONS; ++i) {
                        Vector dir = materials[hitSphere].Sample(outgoing, basis, random);

                        Vector radianceInThatDir = Radiance(new Ray(hitPoint, dir), false); // don't recurse further

                        total += weight * radianceInThatDir / NUM_DIRECTIONS;
                    }
                }
            }

            return total;
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
            // setup the camera as well

            var camera = new Camera(new Point(0, 0, 0), 0, 0, (float)(75 * Math.PI / 180));

            // we'll output the result to an image

            var img = new Bitmap(600, 400, PixelFormat.Format24bppRgb);

            float uScale = 1;
            float vScale = 1;

            if (img.Width > img.Height) {
                uScale = (float)img.Width / img.Height;
            } else if (img.Height > img.Width) {
                vScale = (float)img.Height / img.Width;
            }

            byte[] pixelData = new byte[img.Width * img.Height * 3];

            for (int y = 0; y < img.Height; ++y) {
                for (int x = 0; x < img.Width; ++x) {
                    // compute the resolution-independent camera uv coordinates
                    float u = 2 * ((float)x / (img.Width - 1)) - 1;
                    float v = 1 - 2 * ((float)y / (img.Height - 1));

                    u *= uScale;
                    v *= vScale;

                    // get the corresponding camera ray for this pixel
                    var ray = camera.TraceRay(u, v);

                    Vector radiance = Radiance(ray); // pass false here to disable global illumination

                    pixelData[3 * (y * img.Width + x) + 2] = (byte)floatToInt(radiance.X); /* BGR legacy */
                    pixelData[3 * (y * img.Width + x) + 1] = (byte)floatToInt(radiance.Y);
                    pixelData[3 * (y * img.Width + x) + 0] = (byte)floatToInt(radiance.Z);
                }
            }

            var bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height),
                                          ImageLockMode.WriteOnly,
                                          PixelFormat.Format24bppRgb);


            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
            img.UnlockBits(bitmapData);

            // and save the resulting bitmap as a PNG file

            img.Save("output.png", ImageFormat.Png);

            Console.WriteLine("Done!");
        }
    }
}
