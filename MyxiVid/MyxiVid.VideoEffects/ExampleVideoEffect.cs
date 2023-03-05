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
using AudioVisualizer;

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

        private TimeSpan _prevTime = TimeSpan.Zero;
        private SpectrumData _prevSpectrum = null;
        private SpectrumData _curSpectrum;
        private TimeSpan _rmsRiseTime = TimeSpan.FromMilliseconds(100);
        private TimeSpan _rmsFallTime = TimeSpan.FromMilliseconds(200);
        private TimeSpan _animTime = TimeSpan.FromMilliseconds(16.6);

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
            using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            using (CanvasPathBuilder pathBuilder = new CanvasPathBuilder(_canvasDevice))
            {
                ds.Clear(Colors.Transparent);
                ds.DrawImage(inputBitmap);

                if (SharedData.AudioFrames.TryDequeue(out var frame))
                {
                    var fftResult = ComputeFFT(frame).Select(x => (float)x.Magnitude).ToArray();
                    _curSpectrum = SpectrumData.Create(fftResult, 1, ScaleType.Linear, ScaleType.Linear, 20, 20000);
                }
                else
                {
                    _curSpectrum = _prevSpectrum;
                }

                if (_prevSpectrum != null)
                {
                    var delta = context.InputFrame.RelativeTime.Value - _prevTime;
                    _prevTime = context.InputFrame.RelativeTime.Value;

                    if (_prevSpectrum.FrequencyCount == _curSpectrum.FrequencyCount)
                        _curSpectrum = _curSpectrum.ApplyRiseAndFall(_prevSpectrum, _rmsRiseTime, _rmsFallTime, _animTime);
                }

                _prevSpectrum = _curSpectrum;
                var s = _curSpectrum;
                DrawSpectrumSpline(s[0], ds, new Vector2(0, 0), 1920, 100f, Colors.White);
            }
        }

        private void DrawSpectrumSpline(IReadOnlyList<float> data, CanvasDrawingSession session, Vector2 offset, float width, float maxHeight, Color color)
        {
            int segmentCount = data.Count - 1;
            if (segmentCount <= 1 || width <= 0f)
                return;

            CanvasPathBuilder path = new CanvasPathBuilder(session);

            float segmentWidth = width / (float)segmentCount;

            Vector2 prevPosition = new Vector2(offset.X, data[0] * maxHeight + offset.Y);

            path.BeginFigure(offset.X, offset.Y);
            path.AddLine(prevPosition);

            for (int i = 1; i < data.Count; i++)
            {
                var x = data[i];
                var hsample = x * maxHeight;

                Vector2 position = new Vector2((float)i * segmentWidth + offset.X, hsample + offset.Y);

                Vector2 c1 = new Vector2(position.X - segmentWidth / 2.0f, prevPosition.Y);
                Vector2 c2 = new Vector2(prevPosition.X + segmentWidth / 2.0f, position.Y);
                path.AddCubicBezier(c1, c2, position);

                prevPosition = position;
            }

            path.AddLine(width + offset.X, offset.Y);

            path.EndFigure(CanvasFigureLoop.Open);

            CanvasGeometry geometry = CanvasGeometry.CreatePath(path);
            session.DrawGeometry(geometry, color, 2);
        }

        public void Close(MediaEffectClosedReason reason)
        {
        }

        public void DiscardQueuedFrames()
        {
        }
    }
}
