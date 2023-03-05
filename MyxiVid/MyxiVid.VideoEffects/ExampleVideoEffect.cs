using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas;
using Windows.UI;
using Windows.Storage;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Media.Audio;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Media;
using Microsoft.Graphics.Canvas.Geometry;
using System.Runtime.ConstrainedExecution;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using MathNet.Numerics.Statistics;

namespace MyxiVid.VideoEffects
{
    public sealed class ExampleVideoEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties _encodingProps;
        private CanvasDevice _canvasDevice;
        private IPropertySet _configuration;


        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var encodingProperties = new VideoEncodingProperties
                {
                    Subtype = "ARGB32"
                };
                return new List<VideoEncodingProperties>() { encodingProperties };
            }
        }
        public MediaMemoryTypes SupportedMemoryTypes => MediaMemoryTypes.Gpu;
        public bool TimeIndependent => true;
        public bool IsReadOnly => false;

        public void SetProperties(IPropertySet configuration)
        {
            _configuration = configuration;
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            _encodingProps = encodingProperties;
            _canvasDevice = CanvasDevice.CreateFromDirect3D11Device(device);
        }

        internal static Complex[] ComputeFFT(float[] audioSamples)
        {
            // Compute the FFT of the audio samples using the MathNet.Numerics library
            Complex[] fft = new Complex[audioSamples.Length];
            for (int i = 0; i < audioSamples.Length; i++)
            {
                fft[i] = new Complex(audioSamples[i], 0);
            }

            Fourier.Forward(fft, FourierOptions.NoScaling);

            return fft;
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {

            using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
            using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            using (CanvasPathBuilder pathBuilder = new CanvasPathBuilder(_canvasDevice))

            {
                ds.Clear(Colors.Transparent);
                ds.DrawImage(inputBitmap);

                var fftResult = ComputeFFT(SharedData.Frames);

                float xScale = 1920 / (fftResult.Length / 2);

                pathBuilder.BeginFigure(new Vector2(0, 200));

                for (int i = 0; i < fftResult.Length / 2; i++)
                {
                    var mag = (2.0 / SharedData.Frames.Length) * fftResult[i].Magnitude;

                    var y = 200 + mag * 150;

                    pathBuilder.AddLine(new Vector2(i * xScale, (float)y));
                }

                pathBuilder.EndFigure(CanvasFigureLoop.Open);
                ds.DrawGeometry(CanvasGeometry.CreatePath(pathBuilder), Colors.White, 2);
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
        }

        public void DiscardQueuedFrames()
        {
        }
    }
}
