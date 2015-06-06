using System;

namespace SharpRT
{
    public class SimpleMaterial : IMaterialDescription
    {
        public IMaterial Material;

        public void At(Surface.Intersection intersection, ref Surface.Attributes attributes)
        {
            attributes.Material = Material;
        }

        public SimpleMaterial(IMaterial material)
        {
            this.Material = material;
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
}

