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
    public sealed partial class CounterClockWiseSpiralTestPage : Page
    {
        TestExec m_testExec = null;

        // 획 시작 - 끝 시간 기록
        List<double> m_Times = new List<double>();

        SaveUtil m_saveUtil = SaveUtil.Instance;

        // original line
        List<Point> m_orgLines = new List<Point>();

        public static string TEST_NAME = "counterClockwiseSpiralTest";
        public static string TEST_NAME_KR = "나선 따라그리기(반시계방향)";
        public static int TEST_ORDER = 2;
        public CounterClockWiseSpiralTestPage()
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

                this.Frame.Navigate(typeof(ClockWiseSpiralTestPage), m_testExec, new SuppressNavigationTransitionInfo());
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

            this.Frame.Navigate(typeof(VerticalLineTestPage), m_testExec, new SuppressNavigationTransitionInfo());

        }
        /////// end of events ////////

        private void ResizeCanvas()
        {
            title.Text = TEST_NAME_KR;

            // ui setup
            DisplayInformation di = DisplayInformation.GetForCurrentView();
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            
            inkCanvas.Width = bounds.Width;
            inkCanvas.Height = bounds.Height;

            content.Width = bounds.Width;
            content.Height = bounds.Height;

            polyline.Width = bounds.Width;
            polyline.Height = bounds.Height;
            polyline.StrokeThickness = 2;

            // original line
            int radiusStep = (int)(di.RawDpiX * (15.0f / 25.4f) / (float)di.RawPixelsPerViewPixel);
            m_orgLines = Util.generateClockWiseSpiralPoints(new Point(bounds.Width / 2, bounds.Height / 2), radiusStep * 8, true);
            foreach (var pt in m_orgLines)
                this.polyline.Points.Add(pt);

            ClearInkData();
        }

        private List<DiffData> calculateDifference()
        {
            List<DiffData> results = new List<DiffData>();

            DisplayInformation di = DisplayInformation.GetForCurrentView();
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            int radiusStep = (int)(di.RawDpiX * (15.0f / 25.4f) / (float)di.RawPixelsPerViewPixel);

            Point orgCenter = new Point(bounds.Width / 2, bounds.Height / 2);
            for (int angle = 0; angle < 360; angle += 10)
            {
                Point center = orgCenter;
                if (angle >= 180)
                    center.X = orgCenter.X - radiusStep / 2;
                double radian = Math.PI * angle / 180;
                Point targetPt = new Point(center.X + bounds.Width * Math.Cos(radian), center.Y + bounds.Width * Math.Sin(radian));

                List<Point> orgIntersected = new List<Point>();
                for (int i = 0; i < m_orgLines.Count - 1; i++)
                {
                    Util.FindIntersection(center, targetPt, m_orgLines[i], m_orgLines[i + 1], out bool isIntersected, out Point intersectedPt);
                    if (isIntersected)
                    {
                        if (orgIntersected.Count == 0)
                            orgIntersected.Add(intersectedPt);
                        else if (orgIntersected.Last() != intersectedPt)
                            orgIntersected.Add(intersectedPt);
                    }
                }

                List<Point> drawIntersected = new List<Point>();
                IReadOnlyList<InkStroke> strokeList = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                foreach (var stroke in strokeList)
                {
                    IReadOnlyList<InkStrokeRenderingSegment> segments = stroke.GetRenderingSegments();
                    for (int i = 0; i < segments.Count - 1; i++)
                    {
                        Util.FindIntersection(center, targetPt, segments[i].Position, segments[i + 1].Position, out bool isIntersected, out Point intersectedPt);
                        if (isIntersected)
                            drawIntersected.Add(intersectedPt);
                    }
                }

                // 최초 매칭만 최소거리로 찾는다.
                // 그 이후는 동일하게 매핑 된다고 가정
                if (drawIntersected.Count > 0)
                {
                    int idx = -1;
                    double minDist = bounds.Width * 100;
                    for (int i = 0; i < orgIntersected.Count; i++)
                    {
                        double dist = Util.getDistance(drawIntersected[0], orgIntersected[i]);
                        if (minDist > dist)
                        {
                            idx = i;
                            minDist = dist;
                        }
                    }

                    if (idx >= 0)
                    {
                        for (int i = 0; i < Math.Min(orgIntersected.Count - idx, drawIntersected.Count); i++)
                        {
                            if (Util.getDistance(orgIntersected[i + idx], drawIntersected[i]) <= radiusStep)
                            {
                                results.Add(new DiffData(String.Format("Angle: {0} / Num: {1}", angle, i), orgIntersected[i + idx], drawIntersected[i]));
                            }
                            else
                            {
                                results.Add(new DiffData(String.Format("Angle: {0} / Num: {1}", angle, i), orgIntersected[i + idx]));
                            }
                        }
                    }
                }
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
                Word = "반시계방향 나선 따라그리기"
            };

            m_saveUtil.TestSetItem = testSetItem;

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
