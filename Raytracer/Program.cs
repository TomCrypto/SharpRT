using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SharpRT
{
    /// <summary>
    /// Represents a rendering context for a worker thread.
    /// </summary>
    public struct RenderContext
    {
        public Random Random;
        public Scene Scene;

        public RenderContext(Scene scene)
        {
            this.Scene = scene;
            this.Random = new Random();
        }
    }

    class MainClass
    {
        private static IList<Light> lights = new List<Light>() {
            new Light(new Point(-2, 1, 0), 20),
        };

        // used when a ray escapes from the scene (assumes constant sky radiance)
        // later on you could implement a sky/environment class that takes the
        // escaped direction and works out the incoming radiance using e.g. an
        // environment map or atmospheric simulation
        private static Vector skyRadiance = new Vector(0.5f, 0.5f, 0.5f);

        public static Vector Radiance(RenderContext ctx, Ray ray)
        {
            const int MAX_BOUNCES = 10;

            Vector weights = new Vector(1.0f, 1.0f, 1.0f);
            Vector radiance = Vector.Zero;

            for (int bounce = 0; bounce < MAX_BOUNCES; ++bounce) {
                Surface.Intersection intersection;

                if (!ctx.Scene.Intersect(ray, 1e-4f, float.PositiveInfinity, out intersection)) {
                    radiance += weights * skyRadiance;
                    break;
                }

                var surface = ctx.Scene[intersection.SurfaceID];
                var surf = surface.At(intersection);

                var outgoing = new Direction(-ray.Direction, surf.Basis);
                var hitPoint = Ray.PointAt(ray, intersection.Distance);

                /* Include object emittance as light path */

                radiance += weights * surf.Material.Emittance(outgoing, surf.Basis);

                /* Include point light source light paths */

                foreach (var light in lights) {
                    var pointToLight = light.position - hitPoint;
                    var lightDistance = pointToLight.Length();

                    if (!ctx.Scene.Occlusion(new Ray(hitPoint, pointToLight), 1e-4f, lightDistance)) {
                        Direction incomimg = new Direction(pointToLight / lightDistance, surf.Basis);

                        radiance += weights * surf.Material.BRDF(incomimg, outgoing, surf.Basis) * incomimg.CosTheta
                                  * light.intensity / (lightDistance * lightDistance);
                    }
                }

                /* Russian roulette */

                Vector weight = surf.Material.WeightPDF(outgoing, surf.Basis);
                float p = Math.Max(weight.X, Math.Max(weight.Y, weight.Z));

                if (bounce > 2) {
                    if (ctx.Random.NextDouble() <= p) {
                        weight /= p;
                    } else {
                        break;
                    }
                }

                /* Update light path weights and prepare for next bounce */

                weights *= weight;

                var newDir = surf.Material.SamplePDF(outgoing, surf.Basis, ctx.Random);

                ray = new Ray(hitPoint, newDir);
            }

            return radiance;
        }

        public static int FloatToInt(float rgb)
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

            var camera = new Camera(new Point(0, 1, -2), -0.2f, 0, (float)(75 * Math.PI / 180));

            // load some generic geometric meshes to be rendered

            var floorGeometry = new SimpleMesh("Models/floor.obj", false);
            var bunnyGeometry = new SimpleMesh("Models/bunny.obj", true);
            var sphereGeometry = new SimpleMesh("Models/sphere.obj", true);
            var teapotGeometry = new SimpleMesh("Models/teapot.obj", true);

            // then set up the scene

            using (var scene = new Scene()) {
                // instantiate the meshes into concrete surfaces, with their own materials and locations

                // place a grayish floor on the ground

                scene.Add(new Surface(floorGeometry,
                    new SimpleMaterial(new Lambertian(new Vector(0.8f, 0.8f, 0.8f))),
                    AbsoluteLocation.Origin)
                );

                // place a large rabbit with a blue material

                scene.Add(new Surface(bunnyGeometry,
                    new SimpleMaterial(new Lambertian(new Vector(0.25f, 0.25f, 0.75f))),
                    new AbsoluteLocation(Matrix.Scaling(5)))
                );

                // place another smaller rabbit somewhere else with a yellow material

                scene.Add(new Surface(bunnyGeometry,
                    new SimpleMaterial(new Lambertian(new Vector(0.75f, 0.75f, 0.25f))),
                    new AbsoluteLocation(Matrix.Translation(new Vector(-0.5f, 0.0f, -0.5f))
                                       * Matrix.Scaling(3.5f)))
                );

                // place yet another smaller rabbit somewhere else with a red material

                scene.Add(new Surface(bunnyGeometry,
                    new SimpleMaterial(new Lambertian(new Vector(0.75f, 0.25f, 0.25f))),
                    new AbsoluteLocation(Matrix.Translation(new Vector(0.5f, 0.0f, -1.0f))
                                       * Matrix.Scaling(2.5f)))
                );

                // place a copy of the sphere at the back, towards the right, with a mirror material slightly tinted green

                scene.Add(new Surface(sphereGeometry,
                    new SimpleMaterial(new Mirror(new Vector(0.85f, 0.95f, 0.85f))),
                    new AbsoluteLocation(Matrix.Translation(new Vector(1.5f, 1.0f, 0.5f))))
                );

                // place a teapot somewhere

                scene.Add(new Surface(teapotGeometry,
                    new SimpleMaterial(new Lambertian(new Vector(0.25f, 0.75f, 0.25f))),
                    new AbsoluteLocation(Matrix.Translation(new Vector(0, 0, -0.5f))
                                       * Matrix.Scaling(0.1f)))
                );

                // commit when done messing with the geometry

                scene.Commit();

                // scene is now ready for rendering

                int width = 800, height = 600;
                var img = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                float uScale = 1;
                float vScale = 1;

                if (img.Width > img.Height) {
                    uScale = (float)img.Width / img.Height;
                } else if (img.Height > img.Width) {
                    vScale = (float)img.Height / img.Width;
                }

                byte[] pixelData = new byte[img.Width * img.Height * 3];

                Parallel.For(0, height, () => new RenderContext(scene), (y, pls, ctx) => {
                    for (int x = 0; x < width; ++x) {
                        float u = 2 * ((float)x / (width - 1)) - 1;
                        float v = 1 - 2 * ((float)y / (height - 1));

                        u *= uScale;
                        v *= vScale;

                        var ray = camera.TraceRay(u, v);

                        Vector radiance = Vector.Zero;

                        const int SAMPLES = 500; // more = crisper image

                        for (int s = 0; s < SAMPLES; ++s) {
                            radiance += Radiance(ctx, ray);
                        }

                        pixelData[3 * (y * width + x) + 2] = (byte)FloatToInt(radiance.X / SAMPLES); /* BGR legacy */
                        pixelData[3 * (y * width + x) + 1] = (byte)FloatToInt(radiance.Y / SAMPLES);
                        pixelData[3 * (y * width + x) + 0] = (byte)FloatToInt(radiance.Z / SAMPLES);
                    }

                    return ctx;
                }, (ctx) => {});

                var bitmapData = img.LockBits(new Rectangle(0, 0, width, height),
                                              ImageLockMode.WriteOnly,
                                              PixelFormat.Format24bppRgb);
                Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
                img.UnlockBits(bitmapData);

                img.Save("output.png", ImageFormat.Png);
            }

            Console.WriteLine("Done!");
        }
    }
}