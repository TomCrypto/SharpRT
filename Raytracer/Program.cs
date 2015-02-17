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
    }

    class MainClass
    {
        private static IList<Sphere> geometry = new List<Sphere>() {
            new Sphere(new Point(-1, 1, 10), 2),
            new Sphere(new Point(1, -1, 4), 1),
        };

        private static IList<Color> materials = new List<Color>() { // using the word "material" loosely here
            Color.Red,
            Color.Blue,
        };

        public static bool Intersect(Ray ray, out int sphereIndex, out float distance)
        {
            distance = float.MaxValue;
            sphereIndex = -1;

            for (int t = 0; t < geometry.Count; ++t) {
                float distToSphere;

                if (geometry[t].Intersect(ray, out distToSphere) && (distToSphere < distance)) {
                    distance = distToSphere;
                    sphereIndex = t;
                }
            }

            return sphereIndex != -1;
        }

        public static void Main(string[] args)
        {
            // setup the camera as well

            var camera = new Camera(new Point(0, 0, 0), 0, 0, (float)(75 * Math.PI / 180));

            // we'll output the result to an image

            var img = new Bitmap(512, 512);

            for (int y = 0; y < img.Height; ++y) {
                for (int x = 0; x < img.Width; ++x) {
                    // compute the resolution-independent camera uv coordinates
                    float u = 2 * ((float)x / (img.Width - 1)) - 1;
                    float v = 1 - 2 * ((float)y / (img.Height - 1));

                    // get the corresponding camera ray for this pixel
                    var ray = camera.TraceRay(u, v);

                    float distance;
                    int hitSphere;

                    if (Intersect(ray, out hitSphere, out distance)) {
                        img.SetPixel(x, y, materials[hitSphere]);
                    } else {
                        img.SetPixel(x, y, Color.Black);
                    }
                }
            }

            // and save the resulting bitmap as a PNG file

            img.Save("output.png", ImageFormat.Png);

            Console.WriteLine("Done!");
        }
    }
}
