using MIDAS_BAT.Data;
using MIDAS_BAT.Pages;
using MIDAS_BAT.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking.Core;
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

        const string TEST_NAME = "preTest";
        const int TEST_ORDER = 0;
        public PreTestPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse
                                                    | CoreInputDeviceTypes.Pen;

            CoreInkIndependentInputSource core = CoreInkIndependentInputSource.Create(inkCanvas.InkPresenter);
            core.PointerPressing += Core_PointerPressing;
            core.PointerReleasing += Core_PointerReleasing;
        }

        private void Core_PointerReleasing(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            m_Times.Add((double)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
        }

        private void Core_PointerPressing(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            m_Times.Add((double)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ResizeCanvas();

            if (e.Parameter is TestExec)
            {
                m_testExec = e.Parameter as TestExec;
                m_saveUtil.TestExec = m_testExec;

                string[] file_names = {
                    m_testExec.TesterId + "_char_0.gif",
                    m_testExec.TesterId + "_char_0_last.png",
                    m_testExec.TesterId + "_0.gif",
                    m_testExec.TesterId + "_raw_time_0.txt",
                    m_testExec.TesterId + "_raw_time_0.csv",
                    m_testExec.TesterId + "_raw_pressure_0.csv"
                };

                StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(m_testExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
                foreach (var file_name in file_names)
                {
                    var targetFile = await storageFolder.TryGetItemAsync(file_name);
                    if (targetFile != null)
                        await targetFile.DeleteAsync();
                }
            }

        }

        private static bool nextLock = false;
        private async void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (nextLock == false)
            {
                nextLock = true;
                await nextHandling();
                nextLock = false;

                this.Frame.Navigate(typeof(ClockWiseSpiralTestPage), m_testExec);
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
        private async void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            await Util.ShowCannotGoBackAlertDlg();

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

            inkCanvas.Width = bounds.Width;
            inkCanvas.Height = bounds.Height;

            content.Width = bounds.Width;
            content.Height = lineHeight;

            horizontalLine.X2 = lineWidth;
            verticalLine.Y2 = lineHeight;
        }

        private async Task nextHandling()
        {
            TestUtil testUtil = TestUtil.Instance;

             TestSetItem testSetItem= new TestSetItem()
             {
                 Id = 0,
                 Number = 0,
                 TestSetId = 0,
                 Word = "사전테스트"
             };

            m_saveUtil.TestSetItem = testSetItem;

            //await Util.CaptureInkCanvasForStroke(TEST_ORDER, TEST_NAME, inkCanvas, null, null, m_testExec, testSetItem);
            //await Util.CaptureInkCanvas(TEST_ORDER, TEST_NAME, inkCanvas, null, null, null, m_testExec, testSetItem);

            //await m_saveUtil.saveStroke(TEST_ORDER, TEST_NAME, inkCanvas);
            //await m_saveUtil.saveRawData(TEST_ORDER, TEST_NAME, m_Times, new List<DiffData>(), inkCanvas); ;
            //m_saveUtil.saveResultIntoDB(m_Times, inkCanvas);

            //ResizeCanvas();
            //ClearInkData();
        }

        private void ClearInkData()
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            m_Times.Clear();
        }
    }
}
