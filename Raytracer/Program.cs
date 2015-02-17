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

        private static IList<Vector> materials = new List<Vector>() { // using the word "material" loosely here
            new Vector(1, 0, 0),
            new Vector(0, 0, 1),
            new Vector(0, 1, 0),
        };

        private static IList<Light> lights = new List<Light>() {
            new Light(new Point(0, 6, 0), 60),
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

            var img = new Bitmap(512, 512);

            for (int y = 0; y < img.Height; ++y) {
                for (int x = 0; x < img.Width; ++x) {
                    // let's store the color in a 3D vector (as RGB components) for more flexibility
                    Vector color = Vector.Zero;

                    // compute the resolution-independent camera uv coordinates
                    float u = 2 * ((float)x / (img.Width - 1)) - 1;
                    float v = 1 - 2 * ((float)y / (img.Height - 1));

                    // get the corresponding camera ray for this pixel
                    var ray = camera.TraceRay(u, v);

                    float distance;
                    int hitSphere;

                    if (Intersect(ray, out hitSphere, out distance)) {

                        // if we get here it means that there is an object under this pixel
                        // so what we do is find the intersection point from the distance...

                        // the PointAt method amounts to ray.origin + distance * ray.direction
                        Point hitPoint = ray.PointAt(distance);

                        // now the question we want to ask is, does this sphere receive light
                        // from any light source? if so, we should calculate the amount of
                        // light falling onto the sphere at that position...

                        // we'll need the surface normal at the hit point for lighting
                        Vector normal = geometry[hitSphere].Normal(hitPoint);

                        // this is the same as asking whether there is any sphere in the way
                        // between the hit point and the light source, so we check that

                        foreach (var light in lights) {
                            Vector hitPointToLight = light.position - hitPoint;
                            float distanceToLight = Vector.Length(hitPointToLight);

                            Ray lightRay = new Ray(hitPoint, hitPointToLight);

                            float distanceToObstacle;
                            int unused;

                            if (!Intersect(lightRay, out unused, out distanceToObstacle, 1e-4f, distanceToLight)) {
                                // there is no obstacle, so this light is visible from the hit
                                // point. therefore, calculate the amount of light here...

                                // lighting term = sphere color * dot(light vector, normal) * intensity / distance^2
                                color += materials[hitSphere] * Math.Max(0, Vector.Dot(lightRay.Direction, normal)) * light.intensity / (float)Math.Pow(distanceToLight, 2);
                            }
                        }
                    }

                    img.SetPixel(x, y, Color.FromArgb(floatToInt(color.X),
                                                      floatToInt(color.Y),
                                                      floatToInt(color.Z)));
                }
            }

            // and save the resulting bitmap as a PNG file

            img.Save("output.png", ImageFormat.Png);

            Console.WriteLine("Done!");
        }
    }
}
