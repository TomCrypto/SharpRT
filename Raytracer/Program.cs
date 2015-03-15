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

    public struct Material
    {
        private Vector albedo;

        public Material(Vector albedo)
        {
            this.albedo = albedo; // 0 <= albedo <= 1 (for each channel)
        }

        public Vector BRDF(float cosI, float cosO)
        {
            return this.albedo / (float)Math.PI;
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

        private static IList<Material> materials = new List<Material>() {
            new Material(new Vector(1, 0, 0)),
            new Material(new Vector(0, 0, 1)),
            new Material(new Vector(0, 1, 0)),
        };

        private static IList<Light> lights = new List<Light>() {
            new Light(new Point(0, 6, 0), 160),
            new Light(new Point(-2, 4, 1), 45),
        };

        public static bool Intersect(Ray ray, out int sphereIndex, out float distance,
                                     float minDistance = 0, float maxDistance = float.MaxValue)
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

        public static Vector RandomDirectionInHemisphere(Vector normal)
        {
            while (true) {
                float x = (float)(random.NextDouble() - 0.5);
                float y = (float)(random.NextDouble() - 0.5);
                float z = (float)(random.NextDouble() - 0.5);

                if (x*x + y*y + z*z > 0.5f*0.5f) {
                    continue;
                }

                Vector dir = Vector.Normalize(new Vector(x, y, z));

                if (Vector.Dot(dir, normal) < 0) {
                    dir = -dir;
                }

                return dir;
            }
        }

        public static Vector Radiance(Ray ray, bool recurse = true)
        {
            Vector total = Vector.Zero;

            float distance;
            int hitSphere;

            if (Intersect(ray, out hitSphere, out distance)) {
                Point hitPoint = ray.PointAt(distance);

                Vector normal = geometry[hitSphere].Normal(hitPoint);

                // compute cosine of angle of outgoing direction with surface normal
                float cosO = -Vector.Dot(ray.Direction, normal);

                // evaluate radiance in the direction of the point light source(s)

                foreach (var light in lights) {
                    Vector hitPointToLight = light.position - hitPoint;
                    float distanceToLight = Vector.Length(hitPointToLight);

                    Ray lightRay = new Ray(hitPoint, hitPointToLight);

                    float distanceToObstacle;
                    int unused;

                    if (!Intersect(lightRay, out unused, out distanceToObstacle, 1e-4f, distanceToLight)) {
                        float cosI = Vector.Dot(lightRay.Direction, normal);

                        total += materials[hitSphere].BRDF(cosI, cosO) * cosI * (light.intensity / (float)Math.Pow(distanceToLight, 2));
                    }
                }

                if (recurse) {
                    // now try and approximately evaluate light contribution over the entire hemisphere

                    const int NUM_DIRECTIONS = 20;

                    for (int i = 0; i < NUM_DIRECTIONS; ++i) {
                        Vector dir = RandomDirectionInHemisphere(normal);

                        Vector radianceInThatDir = Radiance(new Ray(hitPoint, dir), false); // don't recurse further

                        float cosI = Vector.Dot(dir, normal);

                        total += materials[hitSphere].BRDF(cosI, cosO) * cosI * radianceInThatDir / NUM_DIRECTIONS;
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

            var img = new Bitmap(600, 400);

            float uScale = 1;
            float vScale = 1;

            if (img.Width > img.Height) {
                uScale = (float)img.Width / img.Height;
            } else if (img.Height > img.Width) {
                vScale = (float)img.Height / img.Width;
            }

            for (int y = 0; y < img.Height; ++y) {
                for (int x = 0; x < img.Width; ++x) {
                    // compute the resolution-independent camera uv coordinates
                    float u = 2 * ((float)x / (img.Width - 1)) - 1;
                    float v = 1 - 2 * ((float)y / (img.Height - 1));

                    u *= uScale;
                    v *= vScale;

                    // get the corresponding camera ray for this pixel
                    var ray = camera.TraceRay(u, v);

                    Vector radiance = Radiance(ray);

                    img.SetPixel(x, y, Color.FromArgb(floatToInt(radiance.X),
                                                      floatToInt(radiance.Y),
                                                      floatToInt(radiance.Z)));
                }
            }

            // and save the resulting bitmap as a PNG file

            img.Save("output.png", ImageFormat.Png);

            Console.WriteLine("Done!");
        }
    }
}
