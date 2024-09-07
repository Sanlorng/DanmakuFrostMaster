using Atelier39;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace DanmakuFrostMasterDemo
{
    public sealed partial class BilibiliDemo1Page : Page
    {
        private DanmakuFrostMaster _danmakuController;
        private DispatcherTimer _danmakuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };

        public BilibiliDemo1Page()
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
                _danmakuController.SetLayerRenderState(DanmakuDefaultLayerDef.RollingLayerId, false);
                _danmakuController.SetLayerRenderState(DanmakuDefaultLayerDef.TopLayerId, false);
                _danmakuController.SetLayerRenderState(DanmakuDefaultLayerDef.BottomLayerId, false);

                _danmakuController.SetAutoControlDensity(false);
                _danmakuController.SetRollingAreaRatio(10);
                _danmakuController.SetRollingDensity(-1);
                _danmakuController.SetIsTextBold(true);
                _danmakuController.SetBorderColor(Colors.Blue);

                StorageFile danmakuFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///DemoFiles/demo_2_danmaku.xml")).AsTask().Result;
                using (Stream danmakuStream = danmakuFile.OpenStreamForReadAsync().Result)
                {
                    using (StreamReader danmakuReader = new StreamReader(danmakuStream))
                    {
                        string danmakuXml = danmakuReader.ReadToEnd();
                        List<DanmakuItem> danmakuList = BilibiliDanmakuXmlParser.GetDanmakuList(danmakuXml, null, false, out _, out _, out _);
                        _danmakuController.SetDanmakuList(danmakuList);
                    }
                }

                _cbRegularDanmaku.IsEnabled = true;
            }

            base.OnNavigatedTo(args);
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Pause();
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

        private void _btnStartPause_Click(object sender, RoutedEventArgs args)
        {
            Button button = (Button)sender;
            if (!_danmakuTimer.IsEnabled)
            {
                Resume();

                _btnBackward.IsEnabled = true;
                _btnForward.IsEnabled = true;

                button.Content = "Pause";
            }
            else
            {
                Pause();

                _btnBackward.IsEnabled = false;
                _btnForward.IsEnabled = false;

                button.Content = "Resume";
            }
        }

        private void _btnBackward_Click(object sender, RoutedEventArgs args)
        {
            TimeSpan currentPlayPosition = _mpe.MediaPlayer.PlaybackSession.Position;
            if (currentPlayPosition.TotalSeconds > 5)
            {
                Seek(TimeSpan.FromSeconds(currentPlayPosition.TotalSeconds - 5));
            }
        }

        private void _btnForward_Click(object sender, RoutedEventArgs args)
        {
            TimeSpan currentPlayPosition = _mpe.MediaPlayer.PlaybackSession.Position;
            if (currentPlayPosition.TotalSeconds < _mpe.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds - 5)
            {
                Seek(TimeSpan.FromSeconds(currentPlayPosition.TotalSeconds + 5));
            }
        }

        private void _cbRegularDanmaku_Click(object sender, RoutedEventArgs args)
        {
            CheckBox checkBox = (CheckBox)sender;
            bool isChecked = checkBox.IsChecked == true;
            _danmakuController.SetLayerRenderState(DanmakuDefaultLayerDef.RollingLayerId, isChecked);
            _danmakuController.SetLayerRenderState(DanmakuDefaultLayerDef.TopLayerId, isChecked);
            _danmakuController.SetLayerRenderState(DanmakuDefaultLayerDef.BottomLayerId, isChecked);
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

        private void Pause()
        {
            _mpe.MediaPlayer.Pause();

            _danmakuTimer.Stop();
            _danmakuController?.Pause();
        }

        private void Resume()
        {
            _mpe.MediaPlayer.Play();

            _danmakuController?.Resume();
            _danmakuTimer.Start();
        }

        private void Seek(TimeSpan newTime)
        {
            _mpe.MediaPlayer.PlaybackSession.Position = newTime;
            _danmakuController?.Seek((uint)newTime.TotalMilliseconds);
        }
    }
}
