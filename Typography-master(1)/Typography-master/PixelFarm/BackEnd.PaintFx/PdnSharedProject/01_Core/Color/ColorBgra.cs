/////////////////////////////////////////////////////////////////////////////////
// Paint.NET (MIT,from version 3.36.7, see=> https://github.com/rivy/OpenPDN   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

//MIT, 2017-present, WinterDev
using System;
using System.Runtime.InteropServices;
using PixelFarm.Drawing;
namespace PaintFx
{
    /// <summary>
    /// This is our pixel format that we will work with. It is always 32-bits / 4-bytes and is
    /// always laid out in BGRA order.
    /// Generally used with the Surface class.
    /// </summary>

    [StructLayout(LayoutKind.Explicit)]
    public struct ColorBgra
    {
        [FieldOffset(0)]
        public byte B;

        [FieldOffset(1)]
        public byte G;

        [FieldOffset(2)]
        public byte R;

        [FieldOffset(3)]
        public byte A;

        /// <summary>
        /// Lets you change B, G, R, and A at the same time.
        /// </summary>
       
        [FieldOffset(0)]
        public uint Bgra;

        public const int BlueChannel = 0;
        public const int GreenChannel = 1;
        public const int RedChannel = 2;
        public const int AlphaChannel = 3;

        public const int SizeOf = 4;

        public static ColorBgra ParseHexString(string hexString)
        {
            uint value = Convert.ToUInt32(hexString, 16);
            return ColorBgra.FromUInt32(value);
        }

        public string ToHexString()
        {
            int rgbNumber = (this.R << 16) | (this.G << 8) | this.B;
            string colorString = Convert.ToString(rgbNumber, 16);

            while (colorString.Length < 6)
            {
                colorString = "0" + colorString;
            }

            string alphaString = System.Convert.ToString(this.A, 16);

            while (alphaString.Length < 2)
            {
                alphaString = "0" + alphaString;
            }

            colorString = alphaString + colorString;

            return colorString.ToUpper();
        }

        /// <summary>
        /// Gets or sets the byte value of the specified color channel.
        /// </summary>
        public unsafe byte this[int channel]
        {
            get
            {
                if (channel < 0 || channel > 3)
                {
                    throw new ArgumentOutOfRangeException("channel", channel, "valid range is [0,3]");
                }

                fixed (byte* p = &B)
                {
                    return p[channel];
                }
            }

            set
            {
                if (channel < 0 || channel > 3)
                {
                    throw new ArgumentOutOfRangeException("channel", channel, "valid range is [0,3]");
                }

                fixed (byte* p = &B)
                {
                    p[channel] = value;
                }
            }
        }

        /// <summary>
        /// Gets the luminance intensity of the pixel based on the values of the red, green, and blue components. Alpha is ignored.
        /// </summary>
        /// <returns>A value in the range 0 to 1 inclusive.</returns>
        public double GetIntensity()
        {
            return ((0.114 * (double)B) + (0.587 * (double)G) + (0.299 * (double)R)) / 255.0;
        }

        /// <summary>
        /// Gets the luminance intensity of the pixel based on the values of the red, green, and blue components. Alpha is ignored.
        /// </summary>
        /// <returns>A value in the range 0 to 255 inclusive.</returns>
        public byte GetIntensityByte()
        {
            return (byte)((7471 * B + 38470 * G + 19595 * R) >> 16);
        }

        /// <summary>
        /// Returns the maximum value out of the B, G, and R values. Alpha is ignored.
        /// </summary>
        /// <returns></returns>
        public byte GetMaxColorChannelValue()
        {
            return Math.Max(this.B, Math.Max(this.G, this.R));
        }

        /// <summary>
        /// Returns the average of the B, G, and R values. Alpha is ignored.
        /// </summary>
        /// <returns></returns>
        public byte GetAverageColorChannelValue()
        {
            return (byte)((this.B + this.G + this.R) / 3);
        }

        /// <summary>
        /// Compares two ColorBgra instance to determine if they are equal.
        /// </summary>
        public static bool operator ==(ColorBgra lhs, ColorBgra rhs)
        {
            return lhs.Bgra == rhs.Bgra;
        }

        /// <summary>
        /// Compares two ColorBgra instance to determine if they are not equal.
        /// </summary>
        public static bool operator !=(ColorBgra lhs, ColorBgra rhs)
        {
            return lhs.Bgra != rhs.Bgra;
        }

        /// <summary>
        /// Compares two ColorBgra instance to determine if they are equal.
        /// </summary>
        public override bool Equals(object obj)
        {

            if (obj != null && obj is ColorBgra && ((ColorBgra)obj).Bgra == this.Bgra)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a hash code for this color value.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (int)Bgra;
            }
        }

        /// <summary>
        /// Returns a new ColorBgra with the same color values but with a new alpha component value.
        /// </summary>
        public ColorBgra NewAlpha(byte newA)
        {
            return ColorBgra.FromBgra(B, G, R, newA);
        }

        /// <summary>
        /// Creates a new ColorBgra instance with the given color and alpha values.
        /// </summary>
        public static ColorBgra FromBgra(byte b, byte g, byte r, byte a)
        {
            ColorBgra color = new ColorBgra();
            color.Bgra = BgraToUInt32(b, g, r, a);
            return color;
        }

        /// <summary>
        /// Creates a new ColorBgra instance with the given color and alpha values.
        /// </summary>
        public static ColorBgra FromBgraClamped(int b, int g, int r, int a)
        {
            return FromBgra(
                PixelUtils.ClampToByte(b),
                PixelUtils.ClampToByte(g),
                PixelUtils.ClampToByte(r),
                PixelUtils.ClampToByte(a));
        }

        /// <summary>
        /// Creates a new ColorBgra instance with the given color and alpha values.
        /// </summary>
        public static ColorBgra FromBgraClamped(float b, float g, float r, float a)
        {
            return FromBgra(
                PixelUtils.ClampToByte(b),
                PixelUtils.ClampToByte(g),
                PixelUtils.ClampToByte(r),
                PixelUtils.ClampToByte(a));
        }

        /// <summary>
        /// Packs color and alpha values into a 32-bit integer.
        /// </summary>
        public static UInt32 BgraToUInt32(byte b, byte g, byte r, byte a)
        {
            return (uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)a << 24);
        }

        /// <summary>
        /// Packs color and alpha values into a 32-bit integer.
        /// </summary>
        public static UInt32 BgraToUInt32(int b, int g, int r, int a)
        {
            return (uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)a << 24);
        }

        /// <summary>
        /// Creates a new ColorBgra instance with the given color values, and 255 for alpha.
        /// </summary>
        public static ColorBgra FromBgr(byte b, byte g, byte r)
        {
            return FromBgra(b, g, r, 255);
        }

        /// <summary>
        /// Constructs a new ColorBgra instance with the given 32-bit value.
        /// </summary>
        public static ColorBgra FromUInt32(UInt32 bgra)
        {
            ColorBgra color = new ColorBgra();
            color.Bgra = bgra;
            return color;
        }

        /// <summary>
        /// Constructs a new ColorBgra instance given a 32-bit signed integer that represents an R,G,B triple.
        /// Alpha will be initialized to 255.
        /// </summary>
        public static ColorBgra FromOpaqueInt32(Int32 bgr)
        {
            if (bgr < 0 || bgr > 0xffffff)
            {
                throw new ArgumentOutOfRangeException("bgr", "must be in the range [0, 0xffffff]");
            }

            ColorBgra color = new ColorBgra();
            color.Bgra = (uint)bgr;
            color.A = 255;

            return color;
        }

        public static int ToOpaqueInt32(ColorBgra color)
        {
            if (color.A != 255)
            {
                throw new InvalidOperationException("Alpha value must be 255 for this to work");
            }

            return (int)(color.Bgra & 0xffffff);
        }

        /// <summary>
        /// Constructs a new ColorBgra instance from the values in the given Color instance.
        /// </summary>
        public static ColorBgra FromColor(Color c)
        {
            return FromBgra(c.B, c.G, c.R, c.A);
        }

        /// <summary>
        /// Converts this ColorBgra instance to a Color instance.
        /// </summary>
        public Color ToColor()
        {
            return Color.FromArgb(A, R, G, B);
        }

        /// <summary>
        /// Smoothly blends between two colors.
        /// </summary>
        public static ColorBgra Blend(ColorBgra ca, ColorBgra cb, byte cbAlpha)
        {
            uint caA = (uint)PixelUtils.FastScaleByteByByte((byte)(255 - cbAlpha), ca.A);
            uint cbA = (uint)PixelUtils.FastScaleByteByByte(cbAlpha, cb.A);
            uint cbAT = caA + cbA;

            uint r;
            uint g;
            uint b;

            if (cbAT == 0)
            {
                r = 0;
                g = 0;
                b = 0;
            }
            else
            {
                r = ((ca.R * caA) + (cb.R * cbA)) / cbAT;
                g = ((ca.G * caA) + (cb.G * cbA)) / cbAT;
                b = ((ca.B * caA) + (cb.B * cbA)) / cbAT;
            }

            return ColorBgra.FromBgra((byte)b, (byte)g, (byte)r, (byte)cbAT);
        }

        /// <summary>
        /// Linearly interpolates between two color values.
        /// </summary>
        /// <param name="from">The color value that represents 0 on the lerp number line.</param>
        /// <param name="to">The color value that represents 1 on the lerp number line.</param>
        /// <param name="frac">A value in the range [0, 1].</param>
        /// <remarks>
        /// This method does a simple lerp on each color value and on the alpha channel. It does
        /// not properly take into account the alpha channel's effect on color blending.
        /// </remarks>
        public static ColorBgra Lerp(ColorBgra from, ColorBgra to, float frac)
        {
            ColorBgra ret = new ColorBgra();

            ret.B = (byte)PixelUtils.ClampToByte(PixelUtils.Lerp(from.B, to.B, frac));
            ret.G = (byte)PixelUtils.ClampToByte(PixelUtils.Lerp(from.G, to.G, frac));
            ret.R = (byte)PixelUtils.ClampToByte(PixelUtils.Lerp(from.R, to.R, frac));
            ret.A = (byte)PixelUtils.ClampToByte(PixelUtils.Lerp(from.A, to.A, frac));

            return ret;
        }

        /// <summary>
        /// Linearly interpolates between two color values.
        /// </summary>
        /// <param name="from">The color value that represents 0 on the lerp number line.</param>
        /// <param name="to">The color value that represents 1 on the lerp number line.</param>
        /// <param name="frac">A value in the range [0, 1].</param>
        /// <remarks>
        /// This method does a simple lerp on each color value and on the alpha channel. It does
        /// not properly take into account the alpha channel's effect on color blending.
        /// </remarks>
        public static ColorBgra Lerp(ColorBgra from, ColorBgra to, double frac)
        {
            ColorBgra ret = new ColorBgra();

            ret.B = (byte)PixelUtils.ClampToByte(PixelUtils.Lerp(from.B, to.B, frac));
            ret.G = (byte)PixelUtils.ClampToByte(PixelUtils.Lerp(from.G, to.G, frac));
            ret.R = (byte)PixelUtils.ClampToByte(PixelUtils.Lerp(from.R, to.R, frac));
            ret.A = (byte)PixelUtils.ClampToByte(PixelUtils.Lerp(from.A, to.A, frac));

            return ret;
        }

        /// <summary>
        /// Blends four colors together based on the given weight values.
        /// </summary>
        /// <returns>The blended color.</returns>
        /// <remarks>
        /// The weights should be 16-bit fixed point numbers that add up to 65536 ("1.0").
        /// 4W16IP means "4 colors, weights, 16-bit integer precision"
        /// </remarks>
        public static ColorBgra BlendColors4W16IP(ColorBgra c1, uint w1, ColorBgra c2, uint w2, ColorBgra c3, uint w3, ColorBgra c4, uint w4)
        {
#if DEBUG
            /*
            if ((w1 + w2 + w3 + w4) != 65536)
            {
                throw new ArgumentOutOfRangeException("w1 + w2 + w3 + w4 must equal 65536!");
            }
             * */
#endif

            const uint ww = 32768;
            uint af = (c1.A * w1) + (c2.A * w2) + (c3.A * w3) + (c4.A * w4);
            uint a = (af + ww) >> 16;

            uint b;
            uint g;
            uint r;

            if (a == 0)
            {
                b = 0;
                g = 0;
                r = 0;
            }
            else
            {
                b = (uint)((((long)c1.A * c1.B * w1) + ((long)c2.A * c2.B * w2) + ((long)c3.A * c3.B * w3) + ((long)c4.A * c4.B * w4)) / af);
                g = (uint)((((long)c1.A * c1.G * w1) + ((long)c2.A * c2.G * w2) + ((long)c3.A * c3.G * w3) + ((long)c4.A * c4.G * w4)) / af);
                r = (uint)((((long)c1.A * c1.R * w1) + ((long)c2.A * c2.R * w2) + ((long)c3.A * c3.R * w3) + ((long)c4.A * c4.R * w4)) / af);
            }

            return ColorBgra.FromBgra((byte)b, (byte)g, (byte)r, (byte)a);
        }

        /// <summary>
        /// Blends the colors based on the given weight values.
        /// </summary>
        /// <param name="c">The array of color values.</param>
        /// <param name="w">The array of weight values.</param>
        /// <returns>
        /// The weights should be fixed point numbers. 
        /// The total summation of the weight values will be treated as "1.0".
        /// Each color will be blended in proportionally to its weight value respective to 
        /// the total summation of the weight values.
        /// </returns>
        /// <remarks>
        /// "WAIP" stands for "weights, arbitrary integer precision"</remarks>
        public static ColorBgra BlendColorsWAIP(ColorBgra[] c, uint[] w)
        {
            if (c.Length != w.Length)
            {
                throw new ArgumentException("c.Length != w.Length");
            }

            if (c.Length == 0)
            {
                return ColorBgra.FromUInt32(0);
            }

            long wsum = 0;
            long asum = 0;

            for (int i = 0; i < w.Length; ++i)
            {
                wsum += w[i];
                asum += c[i].A * w[i];
            }

            uint a = (uint)((asum + (wsum >> 1)) / wsum);

            long b;
            long g;
            long r;

            if (a == 0)
            {
                b = 0;
                g = 0;
                r = 0;
            }
            else
            {
                b = 0;
                g = 0;
                r = 0;

                for (int i = 0; i < c.Length; ++i)
                {
                    b += (long)c[i].A * c[i].B * w[i];
                    g += (long)c[i].A * c[i].G * w[i];
                    r += (long)c[i].A * c[i].R * w[i];
                }

                b /= asum;
                g /= asum;
                r /= asum;
            }

            return ColorBgra.FromUInt32((uint)b + ((uint)g << 8) + ((uint)r << 16) + ((uint)a << 24));
        }

        /// <summary>
        /// Blends the colors based on the given weight values.
        /// </summary>
        /// <param name="c">The array of color values.</param>
        /// <param name="w">The array of weight values.</param>
        /// <returns>
        /// Each color will be blended in proportionally to its weight value respective to 
        /// the total summation of the weight values.
        /// </returns>
        /// <remarks>
        /// "WAIP" stands for "weights, floating-point"</remarks>
        public static ColorBgra BlendColorsWFP(ColorBgra[] c, double[] w)
        {
            if (c.Length != w.Length)
            {
                throw new ArgumentException("c.Length != w.Length");
            }

            if (c.Length == 0)
            {
                return ColorBgra.Transparent;
            }

            double wsum = 0;
            double asum = 0;

            for (int i = 0; i < w.Length; ++i)
            {
                wsum += w[i];
                asum += (double)c[i].A * w[i];
            }

            double a = asum / wsum;
            double aMultWsum = a * wsum;

            double b;
            double g;
            double r;

            if (asum == 0)
            {
                b = 0;
                g = 0;
                r = 0;
            }
            else
            {
                b = 0;
                g = 0;
                r = 0;

                for (int i = 0; i < c.Length; ++i)
                {
                    b += (double)c[i].A * c[i].B * w[i];
                    g += (double)c[i].A * c[i].G * w[i];
                    r += (double)c[i].A * c[i].R * w[i];
                }

                b /= aMultWsum;
                g /= aMultWsum;
                r /= aMultWsum;
            }

            return ColorBgra.FromBgra((byte)b, (byte)g, (byte)r, (byte)a);
        }

        public static ColorBgra Blend(ColorBgra[] colors)
        {
            unsafe
            {
                fixed (ColorBgra* pColors = colors)
                {
                    return Blend(pColors, colors.Length);
                }
            }
        }

        /// <summary>
        /// Smoothly blends the given colors together, assuming equal weighting for each one.
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="colorCount"></param>
        /// <returns></returns>
        public unsafe static ColorBgra Blend(ColorBgra* colors, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count must be 0 or greater");
            }

            if (count == 0)
            {
                return ColorBgra.Transparent;
            }

            ulong aSum = 0;

            for (int i = 0; i < count; ++i)
            {
                aSum += (ulong)colors[i].A;
            }

            byte b = 0;
            byte g = 0;
            byte r = 0;
            byte a = (byte)(aSum / (ulong)count);

            if (aSum != 0)
            {
                ulong bSum = 0;
                ulong gSum = 0;
                ulong rSum = 0;

                for (int i = 0; i < count; ++i)
                {
                    bSum += (ulong)(colors[i].A * colors[i].B);
                    gSum += (ulong)(colors[i].A * colors[i].G);
                    rSum += (ulong)(colors[i].A * colors[i].R);
                }

                b = (byte)(bSum / aSum);
                g = (byte)(gSum / aSum);
                r = (byte)(rSum / aSum);
            }

            return ColorBgra.FromBgra(b, g, r, a);
        }

        public override string ToString()
        {
            return "B: " + B + ", G: " + G + ", R: " + R + ", A: " + A;
        }

        /// <summary>
        /// Casts a ColorBgra to a UInt32.
        /// </summary>
        public static explicit operator UInt32(ColorBgra color)
        {
            return color.Bgra;
        }

        /// <summary>
        /// Casts a UInt32 to a ColorBgra.
        /// </summary>
        public static explicit operator ColorBgra(UInt32 uint32)
        {
            return ColorBgra.FromUInt32(uint32);
        }

        //// Colors: copied from System.Drawing.Color's list (don't worry I didn't type it in 
        //// manually, I used a code generator w/ reflection ...)

        public static ColorBgra Transparent
        {
            get
            {
                return ColorBgra.FromBgra(255, 255, 255, 0);
            }
        }
        public static ColorBgra Black
        {
            get
            {
                return ColorBgra.FromBgra(0, 0, 0, 255);
            }
        }
        public static ColorBgra White
        {
            get
            {
                return ColorBgra.FromBgra(255, 255, 255, 255);
            }
        }
        public static ColorBgra Red
        {
            get
            {
                return ColorBgra.FromBgra(0, 0, 255, 255);
            }
        }
        public static ColorBgra Green
        {
            get
            {
                return ColorBgra.FromBgra(0, 255, 0, 255);
            }
        }
        public static ColorBgra Blue
        {
            get
            {
                return ColorBgra.FromBgra(255, 0, 0, 255);
            }
        }

    }
}
