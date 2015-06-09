using System;

namespace SharpRT
{
    public class AbsoluteLocation : ILocationDescription
    {
        public Matrix Transform { get; set; }
        public Boolean Visible { get; set; }

        public AbsoluteLocation(Matrix transform, Boolean visible = true)
        {
            Transform = transform;
            Visible = visible;
        }

        public static AbsoluteLocation Origin {
            get { return new AbsoluteLocation(Matrix.Identity, true); }
        }
    }
}
