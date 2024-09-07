using Atelier39;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT;
using Windows.UI.Core;

namespace DanmakuFrostMasterDemo
{
    public sealed partial class AssDemoPage : Page
    {
        private DanmakuFrostMaster _danmakuController;
        private DispatcherTimer _danmakuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };

        public AssDemoPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                _btnBack.Visibility = Visibility.Collapsed;
            }
            _danmakuTimer.Tick += _danmakuTimer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            if (_danmakuController == null)
            {
                _danmakuController = new DanmakuFrostMaster(_canvasDanmaku);
                _danmakuController.SetSubtitleLayer(DanmakuDefaultLayerDef.SubtitleLayerId);

                _danmakuController.SetAutoControlDensity(false);
                _danmakuController.SetRollingDensity(-1);
                _danmakuController.SetIsTextBold(true);
                _danmakuController.SetBorderColor(Colors.Blue);
            }

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            _mpe.MediaPlayer.Pause();

            _danmakuTimer.Stop();
            _danmakuController?.Stop();
            base.OnNavigatingFrom(e);
        }

        private void _btnBack_Click(object sender, RoutedEventArgs args)
        {
            Frame rootFrame = App.Window.NavigationFrame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

        private void _danmakuTimer_Tick(object sender, object arg)
        {
            if (_mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                TimeSpan playTime = _mpe.MediaPlayer.PlaybackSession.Position;
                _danmakuController?.UpdateTime((uint)playTime.TotalMilliseconds);
            }
        }

        private void _sliderPos_ValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            Slider slider = (Slider)sender;
            if (slider.IsEnabled)
            {
                if (_mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    TimeSpan videoDuration = _mpe.MediaPlayer.PlaybackSession.NaturalDuration;
                    TimeSpan newTime = TimeSpan.FromMilliseconds(videoDuration.TotalMilliseconds * slider.Value / slider.Maximum);
                    _mpe.MediaPlayer.PlaybackSession.Position = newTime;
                    _danmakuController?.Seek((uint)newTime.TotalMilliseconds);
                }
                else
                {
                    slider.Value = 0;
                }
            }
        }

        private async void _btnOpen_Click(object sender, RoutedEventArgs args)
        {
/*
    TODO 你应将 "App.WindowHandle" 替换为窗口的句柄(HWND) 
   请在此处阅读有关如何检索窗口句柄的详细信息: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/
            FileOpenPicker videoOpenPicker = InitializeWithWindow(new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.VideosLibrary,
            },App.WindowHandle);
            videoOpenPicker.FileTypeFilter.Add(".mp4");
            StorageFile videoFile = await videoOpenPicker.PickSingleFileAsync();
/*
    TODO 你应将 "App.WindowHandle" 替换为窗口的句柄(HWND) 
   请在此处阅读有关如何检索窗口句柄的详细信息: https://docs.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
*/

            FileOpenPicker assOpenPicker = InitializeWithWindow(new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.VideosLibrary,
            },App.WindowHandle);
            assOpenPicker.FileTypeFilter.Add(".ass");
            StorageFile assFile = await assOpenPicker.PickSingleFileAsync();

            if (videoFile != null && assFile != null)
            {
                using (Stream assStream = await assFile.OpenStreamForReadAsync())
                {
                    using (StreamReader assReader = new StreamReader(assStream))
                    {
                        string assStr = assReader.ReadToEnd();
                        List<DanmakuItem> danmakuList = AssParser.GetDanmakuList(assStr);
                        if (danmakuList != null && danmakuList.Count > 0)
                        {
                            _danmakuController?.SetDanmakuList(danmakuList);
                            //_danmakuController?.Restart();
                        }
                    }
                }

                Stream videoStream = await videoFile.OpenStreamForReadAsync();
                _mpe.MediaPlayer.Source = MediaSource.CreateFromStream(videoStream.AsRandomAccessStream(), string.Empty);
                _mpe.MediaPlayer.Play();

                _sliderPos.Value = 0;
                _sliderPos.IsEnabled = true;

                _danmakuTimer.Start();
            }
            else
            {
                MessageDialog md = new MessageDialog($"Failed to open {(videoFile == null ? "video" : "ass")} file");
                WinRT.Interop.InitializeWithWindow.Initialize(md, App.WindowHandle);
                await md.ShowAsync();
            }
        }

        private static FileOpenPicker InitializeWithWindow(FileOpenPicker obj, IntPtr windowHandle)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(obj, windowHandle);
            return obj;
        }

        private void _cbDebugMode_Checked(object sender, RoutedEventArgs args)
        {
            if (_danmakuController != null)
            {
                _danmakuController.DebugMode = true;
            }
        }

        private void _cbDebugMode_Unchecked(object sender, RoutedEventArgs args)
        {
            if (_danmakuController != null)
            {
                _danmakuController.DebugMode = false;
            }
        }
    }
}
