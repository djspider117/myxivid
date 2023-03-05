using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace MyxiVid.VideoEffects
{
    public sealed class ExampleAudioEffect : IBasicAudioEffect
    {
        private AudioEncodingProperties currentEncodingProperties;
        IPropertySet configuration;
        public bool UseInputFrameForOutput => true;

        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var supportedEncodingProperties = new List<AudioEncodingProperties>();
                AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 1, 32);
                encodingProps1.Subtype = MediaEncodingSubtypes.Float;
                AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 1, 32);
                encodingProps2.Subtype = MediaEncodingSubtypes.Float;

                supportedEncodingProperties.Add(encodingProps1);
                supportedEncodingProperties.Add(encodingProps2);

                return supportedEncodingProperties;

            }
        }

        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;
        }
        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            currentEncodingProperties = encodingProperties;
        }

        unsafe public void ProcessFrame(ProcessAudioFrameContext context)
        {
            AudioFrame inputFrame = context.InputFrame;

            using (AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference inputReference = inputBuffer.CreateReference())
            {
                (inputReference as IMemoryBufferByteAccess).GetBuffer(out byte* inputDataInBytes, out uint inputCapacity);

                float* inputDataInFloat = (float*)inputDataInBytes;
                int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                for (int i = 0; i < dataInFloatLength; i++)
                {
                    SharedData.Frames[SharedData.FrameIndex] = inputDataInFloat[i];
                    SharedData.FrameIndex++;
                    if (SharedData.FrameIndex >= SharedData.Frames.Length)
                        SharedData.FrameIndex = 0;
                }
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
        }

        public void DiscardQueuedFrames()
        {
        }

    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
