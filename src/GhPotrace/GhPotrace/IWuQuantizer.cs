using System.Collections.Generic;
using System.Drawing;

namespace nQuant
{
    public interface IWuQuantizer
    {
        Image QuantizeImage(Bitmap image, int alphaThreshold, int alphaFader);
    }

    public struct Box
    {
        public byte AlphaMinimum;
        public byte AlphaMaximum;
        public byte RedMinimum;
        public byte RedMaximum;
        public byte GreenMinimum;
        public byte GreenMaximum;
        public byte BlueMinimum;
        public byte BlueMaximum;
        public int Size;
    }

    public class ColorData
    {
        public ColorData(int dataGranularity, int bitmapWidth, int bitmapHeight)
        {
            dataGranularity++;
            Weights = new long[dataGranularity, dataGranularity, dataGranularity, dataGranularity];
            MomentsAlpha = new long[dataGranularity, dataGranularity, dataGranularity, dataGranularity];
            MomentsRed = new long[dataGranularity, dataGranularity, dataGranularity, dataGranularity];
            MomentsGreen = new long[dataGranularity, dataGranularity, dataGranularity, dataGranularity];
            MomentsBlue = new long[dataGranularity, dataGranularity, dataGranularity, dataGranularity];
            Moments = new float[dataGranularity, dataGranularity, dataGranularity, dataGranularity];

            pixelsCount = bitmapWidth * bitmapHeight;
            pixels = new Pixel[pixelsCount];
            quantizedPixels = new int[pixelsCount];
        }

        public long[,,,] Weights { get; private set; }
        public long[,,,] MomentsAlpha { get; private set; }
        public long[,,,] MomentsRed { get; private set; }
        public long[,,,] MomentsGreen { get; private set; }
        public long[,,,] MomentsBlue { get; private set; }
        public float[,,,] Moments { get; private set; }

        public IList<int> QuantizedPixels { get { return quantizedPixels; } }
        public IList<Pixel> Pixels { get { return pixels; } }

        public int PixelsCount { get { return pixels.Length; } }
        public void AddPixel(Pixel pixel, int quantizedPixel)
        {
            pixels[pixelFillingCounter] = pixel;
            quantizedPixels[pixelFillingCounter++] = quantizedPixel;
        }

        private Pixel[] pixels;
        private int[] quantizedPixels;
        private int pixelsCount;
        private int pixelFillingCounter;
    }

    public class Lookup
    {
        public int Alpha;
        public int Red;
        public int Green;
        public int Blue;
    }

    public class LookupData
    {
        public LookupData(int granularity)
        {
            Lookups = new List<nQuant.Lookup>();
            Tags = new int[granularity, granularity, granularity, granularity];
        }

        public IList<nQuant.Lookup> Lookups { get; private set; }
        public int[,,,] Tags { get; private set; }
    }

    internal struct CubeCut
    {
        public readonly byte? Position;
        public readonly float Value;

        public CubeCut(byte? cutPoint, float result)
        {
            Position = cutPoint;
            Value = result;
        }
    }
}