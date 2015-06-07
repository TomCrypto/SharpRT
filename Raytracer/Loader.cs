using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using YamlDotNet.Serialization;

namespace SharpRT
{
    namespace Mappings
    {
        public class Position
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }

            public Position()
            {
                this.x = 0;
                this.y = 0;
                this.z = 0;
            }

            public Position(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public static implicit operator SharpRT.Point(Position position)
            {
                return new SharpRT.Point(position.x,
                                         position.y,
                                         position.z);
            }
        }

        public class Color
        {
            public float r { get; set; }
            public float g { get; set; }
            public float b { get; set; }

            public Color()
            {
                this.r = 0;
                this.g = 0;
                this.b = 0;
            }

            public Color(float r, float g, float b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }

            public static implicit operator SharpRT.Vector(Color color)
            {
                return new SharpRT.Vector(color.r,
                                          color.g,
                                          color.b);
            }
        }

        public class Rotation
        {
            public float pitch { get; set; }
            public float yaw { get; set; }
            public float roll { get; set; }

            public Rotation()
            {
                this.pitch = 0;
                this.yaw = 0;
                this.roll = 0;
            }
        }

        public class Camera
        {
            public Position pos { get; set; }
            public Rotation rot { get; set; }
            public float fov { get; set; }

            public SharpRT.Camera ToCamera()
            {
                return new SharpRT.Camera(pos,
                                          rot.pitch,
                                          rot.yaw,
                                          fov * (float)Math.PI / 180);
            }
        }

        public class Geometry
        {
            public String path { get; set; }
            public bool smoothNormals { get; set; }
        }

        public interface IMaterial
        {
            SharpRT.IMaterial ToMaterial();
        }

        public class LambertianMaterial : IMaterial
        {
            public Color albedo { get; set; }
            public Color emittance { get; set; }

            public LambertianMaterial()
            {
                this.albedo = new Color(0, 0, 0);
                this.emittance = new Color(0, 0, 0);
            }

            public SharpRT.IMaterial ToMaterial()
            {
                return new SharpRT.Lambertian(albedo, emittance);
            }
        }

        public class MirrorMaterial : IMaterial
        {
            public Color reflection { get; set; }
            public Color emittance { get; set; }

            public MirrorMaterial()
            {
                this.reflection = new Color(0, 0, 0);
                this.emittance = new Color(0, 0, 0);
            }

            public SharpRT.IMaterial ToMaterial()
            {
                return new SharpRT.Mirror(reflection, emittance);
            }
        }

        public class Location
        {
            public Position translation { get; set; }
            public Rotation rotation { get; set; }
            public float scale { get; set; }
            public bool visible { get; set; }

            public Location()
            {
                translation = new Position();
                rotation = new Rotation();
                scale = 1;
                visible = true;
            }
        }

        public class Surface
        {
            public String geometry { get; set; }
            public String material { get; set; }
            public String location { get; set; }
        }

        public class Scene
        {
            public Camera camera { get; set; }

            public Dictionary<String, Geometry> geometries { get; set; }
            public Dictionary<String, IMaterial> materials { get; set; }
            public Dictionary<String, Location> locations { get; set; }
            public Dictionary<String, Surface> surfaces { get; set; }
        }
    }

    public static class Loader
    {
        public static Scene LoadScene(String path, out Camera camera)
        {
            var input = new StreamReader(path);

            var deserializer = new Deserializer();

            deserializer.RegisterTagMapping("tag:yaml.org,2002:lambertian", typeof(Mappings.LambertianMaterial));
            deserializer.RegisterTagMapping("tag:yaml.org,2002:mirror", typeof(Mappings.MirrorMaterial));

            var parsed = deserializer.Deserialize<Mappings.Scene>(input);

            var scene = new Scene();

            var geometry = new Dictionary<String, SimpleMesh>();
            var material = new Dictionary<String, SimpleMaterial>();
            var location = new Dictionary<String, AbsoluteLocation>();

            foreach (var parsedGeometry in parsed.geometries) {
                geometry[parsedGeometry.Key] = new SimpleMesh(parsedGeometry.Value.path,
                                                              parsedGeometry.Value.smoothNormals);
            }

            foreach (var parsedMaterial in parsed.materials) {
                material[parsedMaterial.Key] = new SimpleMaterial(parsedMaterial.Value.ToMaterial());
            }

            foreach (var parsedLocation in parsed.locations) {
                location[parsedLocation.Key] = new AbsoluteLocation(Matrix.Translation(parsedLocation.Value.translation)
                                                                  * Matrix.Rotation(parsedLocation.Value.rotation.pitch,
                                                                                    parsedLocation.Value.rotation.yaw,
                                                                                    parsedLocation.Value.rotation.roll)
                                                                  * Matrix.Scaling(parsedLocation.Value.scale),
                                                                    parsedLocation.Value.visible);
            }

            foreach (var parsedSurface in parsed.surfaces.Values) {
                scene.Add(new Surface(geometry[parsedSurface.geometry],
                                      material[parsedSurface.material],
                                      location[parsedSurface.location]));
            }

            scene.Commit();

            camera = parsed.camera.ToCamera();
            return scene;
        }
    }
}
