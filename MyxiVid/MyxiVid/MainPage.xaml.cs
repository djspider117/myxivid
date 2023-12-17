using GhostCore.UWP.Media;
using GhostCore.UWP.Utils;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Toolkit.Uwp.Helpers;
using MyxiVid.VideoEffects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MyxiVid
{
    public sealed partial class MainPage : Page
    {
        private MediaStreamSource _previewStream;
        private MediaEncodingProfile _encProfile;
        private Parser _parser;
        private CanvasTextFormat _artistTextFormat;
        private CanvasTextFormat _trackTextFormat;
        private CanvasTextFormat _episodeTextFormat;
        private MediaComposition _comp;
        private List<IDisposable> _toDisposeOnUpdate = new List<IDisposable>();


        private float _l1x;
        private float _l1y;
        private float _l2x;
        private float _l2y;
        private float _l3x;
        private float _l3y;

        public MainPage()
        {
            _encProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
            _parser = new Parser();

            _artistTextFormat = new CanvasTextFormat
            {
                FontFamily = "Futura LT",
                FontWeight = FontWeights.Normal,
                FontSize = 50,
                HorizontalAlignment = CanvasHorizontalAlignment.Right,
            };

            _trackTextFormat = new CanvasTextFormat
            {
                FontFamily = "Futura LT",
                FontWeight = FontWeights.Bold,
                FontSize = 50,
                HorizontalAlignment = CanvasHorizontalAlignment.Right,
            };

            _episodeTextFormat = new CanvasTextFormat
            {
                FontFamily = "Exo 2",
                FontStyle = FontStyle.Italic,
                FontSize = 88,
                HorizontalAlignment = CanvasHorizontalAlignment.Right,
            };


            _comp = new MediaComposition();

            InitializeComponent();
            Loaded += MainPage_Loaded;
            slBitrate.Value = _encProfile.Video.Bitrate / 1000000;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainPage_Loaded;

            tbAudioPath.Text = @"C:\_freq\FREQ104\Mixdown\FREQ104.wav";
            tbPlaylistPath.Text = @"C:\_freq\FREQ104\Mixdown\FREQ104.txt";

            //btnUpdateComp_Click(sender, e);
        }

        private async void btnUpdateComp_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbPlaylistPath.Text) ||
                string.IsNullOrWhiteSpace(tbAudioPath.Text) ||
                string.IsNullOrWhiteSpace(tbVJLoops.Text))
                return;

            previewPlayer.Visibility = Visibility.Collapsed;

            tbTopRight.Text = $"#{Path.GetFileNameWithoutExtension(tbAudioPath.Text)}".Replace("FREQ", "");

            _l1x = float.Parse(tbLine1x.Text);
            _l1y = float.Parse(tbLine1y.Text);
            _l2x = float.Parse(tbLine2x.Text);
            _l2y = float.Parse(tbLine2y.Text);
            _l3x = float.Parse(tbLine3x.Text);
            _l3y = float.Parse(tbLine3y.Text);

            foreach (var overlayLayers in _comp.OverlayLayers)
            {
                overlayLayers.Overlays.Clear();
            }
            _comp.Clips.Clear();
            _comp.OverlayLayers.Clear();
            _comp.BackgroundAudioTracks.Clear();
            foreach (var disp in _toDisposeOnUpdate)
            {
                disp.Dispose();
            }

            var mixfolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(tbAudioPath.Text));
            var audio = await mixfolder.GetFileAsync(Path.GetFileName(tbAudioPath.Text));

            var bgAudio = await BackgroundAudioTrack.CreateFromFileAsync(audio);

            //AudioEffectDefinition echoEffectDefinition = new AudioEffectDefinition("MyxiVid.VideoEffects.ExampleAudioEffect");
            //bgAudio.AudioEffectDefinitions.Add(echoEffectDefinition);

            _comp.BackgroundAudioTracks.Add(bgAudio);

            var plsFile = await StorageFile.GetFileFromPathAsync(tbPlaylistPath.Text);
            var rdstream = await plsFile.OpenReadAsync();
            var text = await StreamHelper.ReadTextAsync(rdstream);

            var lines = text.Split('\n');
            var parseResult = _parser.Parse(lines);
            var layer = new MediaOverlayLayer();

            var totalDuration = bgAudio.OriginalDuration;
            for (int i = 0; i < parseResult.Count; i++)
            {
                var track = parseResult[i];
                var next = (i + 1 == parseResult.Count) ? null : parseResult[i + 1];

                var duration = (next?.Timestamp ?? totalDuration) - track.Timestamp;

                var rt = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), 1920, 1080, 96);
                _toDisposeOnUpdate.Add(rt);
                using (var ds = rt.CreateDrawingSession())
                {
                    ds.DrawText(track.Artist, _l1x, _l1y, Colors.White, _artistTextFormat);
                    if (!string.IsNullOrEmpty(track.Remix))
                        ds.DrawText($"{track.TrackName} ({track.Remix})", _l2x, _l2y, Colors.White, _trackTextFormat);
                    else
                        ds.DrawText(track.TrackName, _l2x, _l2y, Colors.White, _trackTextFormat);

                    if (!string.IsNullOrWhiteSpace(tbTopRight.Text))
                        ds.DrawText(tbTopRight.Text, _l3x, _l3y, Colors.White, _episodeTextFormat);
                }

                var tlr = MediaClip.CreateFromSurface(rt, duration);
                var overlay = new MediaOverlay(tlr, new Rect(0, 0, 1920, 1080), 1)
                {
                    Delay = track.Timestamp
                };
                layer.Overlays.Add(overlay);
            }

            if (!string.IsNullOrWhiteSpace(tbOverlayPath.Text))
            {
                var mainOverlayFile = await StorageFile.GetFileFromPathAsync(tbOverlayPath.Text);
                var mainOverlayStream = await mainOverlayFile.OpenReadAsync();

                var mainOverlayTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), 1920, 1080, 96);
                var cb = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), mainOverlayStream);

                using (var ds = mainOverlayTarget.CreateDrawingSession())
                {
                    ds.DrawImage(cb);
                }


                var mainOverlaySurface = MediaClip.CreateFromSurface(mainOverlayTarget, totalDuration);

                //var videoEffectDefinition = new VideoEffectDefinition("MyxiVid.VideoEffects.ExampleVideoEffect");
                //mainOverlaySurface.VideoEffectDefinitions.Add(videoEffectDefinition);

                var mainOverlay = new MediaOverlay(mainOverlaySurface, new Rect(0, 0, 1920, 1080), 1);
                layer.Overlays.Add(mainOverlay);

            }
            _comp.OverlayLayers.Add(layer);

            var sf = await StorageFolder.GetFolderFromPathAsync(tbVJLoops.Text);
            var files = await sf.GetFilesAsync();
            while (_comp.Duration < totalDuration)
            {
                foreach (var file in files)
                {
                    var clip = await MediaClip.CreateFromFileAsync(file);
                    clip.Volume = 0;
                    _comp.Clips.Add(clip);

                    if (_comp.Duration >= totalDuration)
                        break;
                }
            }
            _comp.Clips.Last().TrimTimeFromEnd = _comp.Duration - totalDuration;

            _previewStream = _comp.GeneratePreviewMediaStreamSource(1920, 1080);
            previewPlayer.Source = MediaSource.CreateFromMediaStreamSource(_previewStream);
            previewPlayer.MediaPlayer.Volume = 0;
            previewPlayer.Visibility = Visibility.Visible;
        }

        private async void btnRender_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbOutputPath.Text))
                return;

            _encProfile.Video.Bitrate = (uint)slBitrate.Value * 1000000;

            var file = await StorageFile.GetFileFromPathAsync(tbOutputPath.Text);
            var saveOperation = _comp.RenderToFileAsync(file, MediaTrimmingPreference.Precise, _encProfile);

            safeOverlay.Visibility = Visibility.Visible;
            progStatus.Visibility = Visibility.Visible;

            saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {
                    Debug.WriteLine(string.Format("Saving file... Progress: {0:F0}%", progress));
                    progStatus.Value = progress;
                }));
            });
            saveOperation.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>(async (info, status) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {
                    progStatus.Visibility = Visibility.Collapsed;
                    safeOverlay.Visibility = Visibility.Collapsed;
                }));
            });
        }

        private async void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var tb = (sender as Button).Tag as TextBox;

            var pickerResult = await PickerUtils.PickFileAsync();

            if (pickerResult == null)
                return;

            tb.Text = pickerResult.Path;
        }

        private async void btnSaveBrowse_Click(object sender, RoutedEventArgs e)
        {
            var tb = (sender as Button).Tag as TextBox;

            var pickerResult = await new FileSavePicker
            {
                SuggestedFileName = Path.GetFileNameWithoutExtension(tbAudioPath.Text),
                FileTypeChoices = {
                {
                    "MP4 File",
                    new List<string> { ".mp4" }
                } }
            }.PickSaveFileAsync();

            if (pickerResult == null)
                return;

            tb.Text = pickerResult.Path;
        }

        private async void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var tb = (sender as Button).Tag as TextBox;

            var pickerResult = await PickerUtils.PickFolderAsync();

            if (pickerResult == null)
                return;

            tb.Text = pickerResult.Path;
        }

        private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            ds.Clear(Colors.Gray);
            ds.DrawText("Sample Artist feat. Vocalist", _l1x, _l1y, Colors.White, _artistTextFormat);
            ds.DrawText($"Song Name that's a lil bit longer (Extended Mix)", _l2x, _l2y, Colors.White, _trackTextFormat);
        }

        private void btnForcePreview_Click(object sender, RoutedEventArgs e)
        {
            canvas.Invalidate();
        }
    }


}
