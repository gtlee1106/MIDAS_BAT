using MIDAS_BAT.Data;
using MIDAS_BAT.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace MIDAS_BAT.Pages
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class VerticalLineTestPage : Page
    {
        TestExec m_testExec = null;

        // 획 시작 - 끝 시간 기록
        List<double> m_Times = new List<double>();

        // original line
        List<Point> m_orgLines = new List<Point>();

        SaveUtil m_saveUtil = SaveUtil.Instance;

        public static string TEST_NAME = "verticalLineTest";
        public static string TEST_NAME_KR = "직선 따라그리기(세로선)";
        public static int TEST_ORDER = 1;
        
        public VerticalLineTestPage()
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

            Window.Current.SizeChanged += Current_SizeChanged;

            if (e.Parameter is TestExec)
            {
                m_testExec = e.Parameter as TestExec;
                m_saveUtil.TestExec = m_testExec;

                await Util.deleteFiles(m_testExec.TesterId, TEST_ORDER, TEST_NAME);
            }

        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
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

                this.Frame.Navigate(typeof(CounterClockWiseSpiralTestPage), m_testExec, new SuppressNavigationTransitionInfo());
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
            bool goBack = await Util.ShowGoBackAlertDlg();
            if (!goBack)
                return;

            this.Frame.Navigate(typeof(HorizontalLineTestPage), m_testExec, new SuppressNavigationTransitionInfo());

        }
        /////// end of events ////////

        private void ResizeCanvas()
        {
            title.Text = TEST_NAME_KR;

            // ui setup
            DisplayInformation di = DisplayInformation.GetForCurrentView();
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            // 12cm 
            int lineHeight = (int)(di.RawDpiY * (120.0f / 25.4f) / (float)di.RawPixelsPerViewPixel);

            inkCanvas.Width = bounds.Width;
            inkCanvas.Height = bounds.Height;

            content.Width = bounds.Width;
            content.Height = bounds.Height;

            verticalLine.Y2 = lineHeight;
            verticalLine.HorizontalAlignment = HorizontalAlignment.Center;
            verticalLine.VerticalAlignment = VerticalAlignment.Center;

            ClearInkData();
        }
        private List<DiffData> calculateDifference()
        {
            List<DiffData> results = new List<DiffData>();

            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var ttv = verticalLine.TransformToVisual(Window.Current.Content);
            Point l_s = ttv.TransformPoint(new Point(-bounds.Width, verticalLine.Y1));
            Point l_e = ttv.TransformPoint(new Point(bounds.Width, verticalLine.Y1));

            Point orgPoint = ttv.TransformPoint(new Point(verticalLine.X1, verticalLine.Y1));

            DisplayInformation di = DisplayInformation.GetForCurrentView();
            double unit = (int)(di.RawDpiX * (10.0f / 25.4f) / (float)di.RawPixelsPerViewPixel);

            // 12 + 1 개의 포인트를 구해서 거리를 본다?
            IReadOnlyList<InkStroke> strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            for (int i = 0; i <= 12; i++)
            {
                Point p1 = l_s;
                Point p2 = l_e;
                p1.Y += i * unit;
                p2.Y += i * unit;

                bool found = false;
                Point intersectedPoint;
                for (int j = 0; j < strokes.Count; j++)
                {
                    IReadOnlyList<InkStrokeRenderingSegment> segments = strokes[j].GetRenderingSegments();
                    for (int k = 0; k < segments.Count - 1; k++)
                    {
                        Util.FindIntersection(p1, p2, segments[k].Position, segments[k + 1].Position, out bool isIntersected, out intersectedPoint);
                        if (isIntersected)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }

                if (found)
                    results.Add(new DiffData(String.Format("Num: {0}", i), new Point(orgPoint.X, p1.Y), intersectedPoint));
                else
                    results.Add(new DiffData(String.Format("Num: {0}", i), new Point(orgPoint.X, orgPoint.Y)));
            }

            return results;
        }

        private async Task nextHandling()
        {
            TestUtil testUtil = TestUtil.Instance;

            TestSetItem testSetItem = new TestSetItem()
            {
                Id = 0,
                Number = 0,
                TestSetId = 0,
                Word = "세로선 따라그리기"
            };

            m_saveUtil.TestSetItem = testSetItem;

            // original line을 계산해둔다
            var ttv = verticalLine.TransformToVisual(Window.Current.Content);
            m_orgLines.Clear();
            m_orgLines.Add(ttv.TransformPoint(new Point(verticalLine.X1, verticalLine.Y1)));
            m_orgLines.Add(ttv.TransformPoint(new Point(verticalLine.X2, verticalLine.Y2)));

            List<DiffData> diffResults = calculateDifference();

            await Util.CaptureInkCanvasForStroke(TEST_ORDER, TEST_NAME, inkCanvas, null, m_orgLines, m_testExec, testSetItem);
            await Util.CaptureInkCanvas(TEST_ORDER, TEST_NAME, inkCanvas, null, m_orgLines, diffResults, m_testExec, testSetItem);

            await m_saveUtil.saveStroke(TEST_ORDER, TEST_NAME, inkCanvas);
            await m_saveUtil.saveRawData(TEST_ORDER, TEST_NAME, m_Times, diffResults, inkCanvas);
            m_saveUtil.saveResultIntoDB(m_Times, inkCanvas);

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
