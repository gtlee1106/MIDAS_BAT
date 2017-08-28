using MIDAS_BAT.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace MIDAS_BAT
{
    public sealed partial class PreTestPage : Page
    {
        TestExec m_testExec = null;

        // 획 시작 - 끝 시간 기록
        List<double> m_Times = new List<double>();

        SaveUtil m_saveUtil = SaveUtil.Instance;

        public PreTestPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse
                                                    | CoreInputDeviceTypes.Pen;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is TestExec)
                m_testExec = e.Parameter as TestExec;

            ResizeCanvas();
        }

        private static bool nextLock = false;
        private async void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (nextLock == false)
            {
                nextLock = true;
                await nextHandling();
                nextLock = false;

                this.Frame.Navigate(typeof(TestPage), m_testExec);
            }
        }

        private async void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            bool exit = await Util.ShowStopExamAlertDlg();
            if (!exit)
                return;

            this.Frame.Navigate(typeof(MainPage));
        }

        private void cleanBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearInkData();
        }
        /////// end of events ////////

        private void ResizeCanvas()
        {
            // ui setup
            DisplayInformation di = DisplayInformation.GetForCurrentView();
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            // 5cm 고정이긴한데.... 어쩔까 ㅋㅋㅋ
            int lineWidth = (int)(di.RawDpiX * (50.0f / 25.4f) / (float)di.RawPixelsPerViewPixel);
            int lineHeight = (int)(di.RawDpiY * (50.0f / 25.4f) / (float)di.RawPixelsPerViewPixel);

            float targetHeight = 50.0f;
            if (m_testExec != null)
                targetHeight = (float)m_testExec.ScreenWidth;

            int canvasHeight = (int)(di.RawDpiY * (targetHeight / 25.4f) / (float)di.RawPixelsPerViewPixel);

            inkCanvas.Width = bounds.Width;
            inkCanvas.Height = bounds.Height;

            content.Width = bounds.Width;
            content.Height = canvasHeight;

            horizontalLine.X2 = lineWidth;
            verticalLine.Y2 = lineHeight;
        }

        private async Task nextHandling()
        {
            /*
            TestUtil testUtil = TestUtil.Instance;

            await Util.CaptureInkCanvasForStroke(inkCanvas, borderCanvas, m_testExec, m_wordList[m_curIdx]);
            await Util.CaptureInkCanvas(inkCanvas, borderCanvas, m_testExec, m_wordList[m_curIdx]);
            
            await m_saveUtil.saveStroke( inkCanvas);
            await m_saveUtil.saveRawData( m_Times, inkCanvas );
            m_saveUtil.saveResultIntoDB( m_Times, inkCanvas );

            ResizeCanvas();
            ClearInkData();
            */
        }

        private void ClearInkData()
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            m_Times.Clear();
        }
    }
}
