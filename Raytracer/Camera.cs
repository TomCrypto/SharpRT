using System;

namespace SharpRT
{
    public class Camera
    {
        private Point position;
        private float pitch, yaw;
        private float fovFactor;
        private float fov;

        private Matrix view;

        public Point Position
        {
            get { return position; }
            set
            {
                position = value;
                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// The pitch controls vertical orientation.
        /// </summary>
        public float Pitch
        {
            get { return pitch; }
            set
            {
                pitch = value;
                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// The yaw controls horizontal orientation.
        /// </summary>
        public float Yaw
        {
            get { return yaw; }
            set
            {
                yaw = value;
                UpdateViewMatrix();
            }
        }

        public float FieldOfView
        {
            get { return fov; }
            set
            {
                fov = value;
                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// Recreates the view matrix to respond to
        /// a change in position, pitch, or yaw.
        /// </summary>
        private void UpdateViewMatrix()
        {
            // no roll ("up" is always up)
            view = Matrix.Combine(Matrix.Rotation(pitch, yaw, 0), Matrix.Translation(position));
            this.fovFactor = 1.0f / (float)Math.Tan(fov / 2);
        }

        /// <summary>
        /// Creates a new camera with some initial settings.
        /// </summary>
        /// <param name="fov">Field of view in radians (0 to Pi).</param>
        public Camera(Point position, float pitch, float yaw, float fov)
        {
            this.position = position;
            this.pitch = pitch;
            this.yaw = yaw;
            this.fov = fov;
            UpdateViewMatrix();
        }

        /// <summary>
        /// Returns the camera ray corresponding to the resolution
        /// independent pixels (u, v) with -1 &lt;= u, v &lt;= 1.
        /// </summary>
        public Ray TraceRay(float u, float v)
        {
            return view * new Ray(Point.Zero, new Vector(u, v, fovFactor));
        }
    }
}

