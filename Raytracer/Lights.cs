using System;

namespace SharpRT
{
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
}

