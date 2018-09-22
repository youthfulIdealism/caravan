using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace caravan.Worldgen
{
    public class PerlinNoise
    {
        public const int octaves = 4;
        public string seed { get; private set; }

        public PerlinNoise(String seed)
        {
            this.seed = seed;
        }

        public float xxNoise(int x, int y, int octave)
        {
            return (float)Math.Log10(xxHash.CalculateHash(Encoding.UTF8.GetBytes(seed + (octave * 8) + "" + (x + x * y))) / 1000);
        }

        public float xxNoise(int k, int octave)
        {
            return (float)Math.Log10(xxHash.CalculateHash(Encoding.UTF8.GetBytes(seed + (octave * 8) + "" + (k))) / 1000);
        }

        public float ease(double val)
        {
            return (float)(val * val * val * (val * (val * 6 - 15) + 10));
        }

        public float perlin(float x, float y)
        {
            int xCoord = (int)Math.Floor(x);
            int yCoord = (int)Math.Floor(y);

            float xInterp = ease(x - xCoord);
            float yInterp = ease(y - yCoord);

            float a = 0;
            float b = 0;
            float c = 0;
            float d = 0;

            a += xxNoise(xCoord, yCoord, 0);
            b += xxNoise(xCoord + 1, yCoord, 0);

            c += xxNoise(xCoord, yCoord + 1, 0);
            d += xxNoise(xCoord + 1, yCoord + 1, 0);

            float q = MathHelper.Lerp(a, b, xInterp);

            float w = MathHelper.Lerp(c, d, xInterp);

            float e = MathHelper.Lerp(q, w, yInterp);
            if (float.IsNaN(e)) { return 0; }

            return (e / 56);
        }

        public float perlin1D(float x)
        {
            int xCoord = (int)Math.Floor(x);

            float xInterp = ease(x - xCoord);

            float a = 0;
            float b = 0;

            a += xxNoise(xCoord, 0);
            b += xxNoise(xCoord + 1, 0);
            float q = MathHelper.Lerp(a, b, xInterp);

            return (q / 56);
        }

        public float octavePerlin(float x, float y)
        {
            float persistence = 1.2f;
            float maxValue = 1;

            float total = 0;
            for (int i = 0; i < octaves; i++)
            {
                float frequency = (float)Math.Pow(2, i);
                float amplitude = (float)Math.Pow(persistence, i);
                float effectiveX = x * frequency;
                float effectiveY = y * frequency;

                total += perlin(effectiveX, effectiveY) * amplitude;

            }

            return total / maxValue;
        }

        public float octavePerlin1D(float x)
        {
            float persistence = .9f;
            float maxValue = 1;

            //add terrain details
            float total = 0;

            //float metaNoise = (float)Math.Pow(1.3, .9f + perlin1D(x * .2f));


            for (int i = 0; i < octaves; i++)
            {
                float frequency = (float)Math.Pow(2, i);
                float amplitude = (float)Math.Pow(persistence, i)/* * metaNoise*/;
                float effectiveX = x * frequency;

                total += perlin1D(effectiveX) * amplitude;

            }
            //float temp = total;

            //add giant, overarching terrain. TODO: find performant way to reimplement
            /*float giganticFrequency = (float).05f;
            float giganticAmplitude = (float)15 * metaNoise;
            float giganticEffectiveX = x * giganticFrequency;

            total += perlin1D(giganticEffectiveX) * giganticAmplitude;*/

            //Console.WriteLine(temp + " ... " + total);

            return total / maxValue + 30;
        }

    }
}
