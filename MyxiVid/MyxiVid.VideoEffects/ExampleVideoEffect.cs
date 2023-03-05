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

        public void ProcessFrame(ProcessVideoFrameContext context)
        {

            using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
            using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            {
                ds.Clear(Colors.Transparent);
                //var gaussianBlurEffect = new GaussianBlurEffect
                //{
                //    Source = inputBitmap,
                //    BlurAmount = (float)BlurAmount,
                //    Optimization = EffectOptimization.Speed
                //};

                ds.DrawImage(inputBitmap);

                ds.DrawText($"RelativeTime: {context.InputFrame.RelativeTime}", 20, 20, Colors.White);
                ds.DrawText($"SystemRelativeTime: {context.InputFrame.SystemRelativeTime}", 20, 40, Colors.White);
                ds.DrawText($"Type: {context.InputFrame.Type}", 20, 60, Colors.White);
                ds.DrawText($"IsDiscontinuous: {context.InputFrame.IsDiscontinuous}", 20, 80, Colors.White);
                ds.DrawText($"Duration: {context.InputFrame.Duration}", 20, 100, Colors.White);


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
