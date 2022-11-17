using MIDAS_BAT.Data;
using MIDAS_BAT.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
    public sealed partial class CounterClockWiseFreeSpiralTestPage : Page
    {
        TestExec m_testExec = null;

        // 획 시작 - 끝 시간 기록
        List<double> m_Times = new List<double>();

        // original line
        List<List<Point>> m_orgLines = new List<List<Point>>();
        List<List<BATPoint>> m_drawLines = new List<List<BATPoint>>();

        SaveUtil m_saveUtil = SaveUtil.Instance;

        public static string TEST_NAME = "couterClockWiseFreeSpiralTest";
        public static string TEST_NAME_KR = "자유 나선그리기(반시계방향)";
        public static int TEST_ORDER = 6;

        public CounterClockWiseFreeSpiralTestPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse
                                                    | CoreInputDeviceTypes.Pen;

            CoreInkIndependentInputSource core = CoreInkIndependentInputSource.Create(inkCanvas.InkPresenter);
            core.PointerPressing += Core_PointerPressing;
            core.PointerMoving += Core_PointerMoving;
            core.PointerReleasing += Core_PointerReleasing;
        }
        
        private void Core_PointerReleasing(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            if (nextLock)
                return;

            m_Times.Add((double)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            if (m_drawLines.Count() > 0)
            {
                BATPoint point = new BATPoint(args.CurrentPoint.Position, args.CurrentPoint.Properties.Pressure, args.CurrentPoint.Timestamp);
                point.isEnd = true;
                m_drawLines.Last().Add(point);
            }
        }
        private void Core_PointerMoving(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            if (nextLock)
                return;

            m_drawLines.Last().Add(new BATPoint(args.CurrentPoint.Position, args.CurrentPoint.Properties.Pressure, args.CurrentPoint.Timestamp));
        }

        private void Core_PointerPressing(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            if (nextLock)
                return;

            m_Times.Add((double)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);

            // 최초의 point 
            List<BATPoint> list = new List<BATPoint>();
            list.Add(new BATPoint(args.CurrentPoint.Position, args.CurrentPoint.Properties.Pressure, args.CurrentPoint.Timestamp));
            m_drawLines.Add(list);
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

                await Util.deleteFiles(m_testExec.TesterId, TEST_ORDER, TEST_NAME_KR);
            }

        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            ResizeCanvas();
        }

        private void enableInkCanvas(bool enable)
        {
            if (enable)
            {
                inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse
                                                        | CoreInputDeviceTypes.Pen;
            }
            else
            {
                inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
            }
        }

        private static bool nextLock = false;
        private async void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (nextLock == false)
            {
                nextLock = true;
                enableInkCanvas(false);
                await nextHandling();
                enableInkCanvas(true);
                nextLock = false;

                Type nextTest = Util.getNextTest(DatabaseManager.Instance.GetActiveTestSet(), TEST_ORDER);
                if (nextTest == null)
                {
                    await Util.ShowEndOfTestDlg();
                    this.Frame.Navigate(typeof(MainPage));
                }
                else
                {
                    this.Frame.Navigate(nextTest, m_testExec, new SuppressNavigationTransitionInfo());
                }
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
            Type prevTest = Util.getPrevTest(DatabaseManager.Instance.GetActiveTestSet(), TEST_ORDER);
            if (prevTest == null)
                await Util.ShowCannotGoBackAlertDlg();
            else
            {
                bool goBack = await Util.ShowGoBackAlertDlg();
                if (!goBack)
                    return;

                this.Frame.Navigate(prevTest, m_testExec, new SuppressNavigationTransitionInfo());
            }
        }
        /////// end of events ////////

        private void ResizeCanvas()
        {
            title.Text = TEST_NAME_KR;

            // ui setup
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            
            point.Width = Util.mmToPixels(3.0);
            point.Height = Util.mmToPixels(3.0);

            inkCanvas.Width = bounds.Width;
            inkCanvas.Height = bounds.Height;

            content.Width = bounds.Width;
            content.Height = bounds.Height;

            ClearInkData();
        }

        private List<List<BATPoint>> getSplitDrawing(bool cutSmallFirst)
        {
            Rect? bbox = Util.getBoundingBox2(m_drawLines);
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            Point orgCenter = new Point(bounds.Width / 2, bounds.Height / 2);
            List<List<BATPoint>> drawSplits = Util.splitDrawing(orgCenter, m_drawLines, cutSmallFirst, true);
            return drawSplits;
        }

        private List<List<DiffData>> calculateDifference()
        {
            List<List<DiffData>> results = new List<List<DiffData>>();

            Rect? bbox = Util.getBoundingBox2(m_drawLines);
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            List<List<BATPoint>> drawSplits = getSplitDrawing(true);
            if(drawSplits.Count > 4)
            {
                bbox = Util.getBoundingBox2(drawSplits.GetRange(0, 4)); // 4바퀴까지만 사용한다
            }
            //double radiusStep = bbox.Value.Width / (2 * drawSplits.Count);
            double radiusStep = bbox.Value.Width / (2 * 4); // 4바퀴로 고정하기로 함

            var ttv = point.TransformToVisual(Window.Current.Content);
            Point centerPt = ttv.TransformPoint(new Point(0, 0));
            // point 자체 사이즈가 있어서 아래 작업 해줘야 함 
            centerPt.X += point.ActualWidth / 2;
            centerPt.Y += point.ActualHeight / 2;

            // stroke 전체의 사이즈를 구함 
            //m_orgLines = Util.generateClockWiseSpiralPoints(centerPt, bbox.Value.Width, drawSplits.Count, true);
            m_orgLines = Util.generateClockWiseSpiralPoints(centerPt, Math.Max(bbox.Value.Width, bbox.Value.Height), 4, true);

            Point orgCenter = new Point(bounds.Width / 2, bounds.Height / 2);
            for (int i = 0; i < Math.Max(m_orgLines.Count, drawSplits.Count); i++)
            {
                List<DiffData> result = new List<DiffData>();
                int orgIdx = 0;
                int drawIdx = 0;
                for (int angle = 360; angle > 0; angle -= 1)
                {
                    Point center = orgCenter;
                    if (angle >= 180)
                        center.X = orgCenter.X - radiusStep / 2;
                    double radian = Math.PI * angle / 180;
                    Point targetPt = new Point(center.X + bounds.Width * Math.Cos(radian), center.Y + bounds.Width * Math.Sin(radian));

                    Point? orgIntersected = null;
                    if (i < m_orgLines.Count)
                    {
                        for (int j = orgIdx; j < m_orgLines[i].Count - 1; j++)
                        {
                            Util.FindIntersection(center, targetPt, m_orgLines[i][j], m_orgLines[i][j + 1], out bool isIntersected, out Point intersectedPt);

                            if (isIntersected)
                            {
                                // 최초 한 점은 뺄 것 
                                if (Util.getDistance(intersectedPt, center) < 0.000001)
                                    continue;

                                orgIntersected = intersectedPt;
                                orgIdx = j; // 다음 각도는 j 부터 시작
                                break;
                            }
                        }
                    }

                    Point? drawIntersected = null;
                    if (i < drawSplits.Count)
                    {
                        int prevDrawIdx = drawIdx;
                        bool found = false;
                        for (int j = drawIdx; j < drawSplits[i].Count - 1; j++) 
                        {
                            if (drawSplits[i][j].isEnd)
                                continue;

                            Util.FindIntersection(center, targetPt, drawSplits[i][j].point, drawSplits[i][j + 1].point, out bool isIntersected, out Point intersectedPt);
                            if (isIntersected && !(angle == 360 && j == drawSplits[i].Count - 1 - 1)) // 나선 한 바퀴의 가장 마지막에 제일 첫 각도가 걸릴 수 있어서 이를 배제하기 위한 조건
                            {
                                drawIntersected = intersectedPt;
                                drawIdx = j; // 다음 각도는 j 부터 시작
                                found = true;
                                break;
                            }
                        }

                        // 첫번째 바퀴에서만 못찾는 경우, 가장 가까운 점을 선택하도록 한다.
                        if (!found && i == 0 && orgIntersected.HasValue)
                        {
                            drawIdx = prevDrawIdx;
                            double minDist = 10000000;

                            for (int j = drawIdx; j < drawSplits[i].Count - 1; j++)
                            {
                                if (drawSplits[i][j].isEnd)
                                    continue;

                                double dist = Util.getDistance(orgIntersected.Value, drawSplits[i][j].point);
                                if (minDist > dist)
                                {
                                    drawIntersected = drawSplits[i][j].point;
                                    drawIdx = j;
                                    minDist = dist;
                                    found = true;
                                }
                            }
                        }

                        if (!found)
                            drawIdx = prevDrawIdx;
                    }

                    // 최초 매칭만 최소거리로 찾는다.
                    // 그 이후는 동일하게 매핑 된다고 가정
                    if (drawIntersected != null && orgIntersected != null)
                    {
                        result.Add(new DiffData(String.Format("Cycle: {0} / Angle: {1}", i + 1, 360 - angle), orgIntersected.Value, drawIntersected.Value));
                    }
                    else if (orgIntersected != null)
                    {
                        result.Add(new DiffData(String.Format("Cycle: {0} / Angle: {1}", i + 1, 360 - angle), orgIntersected.Value));
                    }
                    else if (drawIntersected != null)
                    {
                        result.Add(new DiffData(String.Format("Cycle: {0} / Angle: {1}", i + 1, 360 - angle), null, drawIntersected.Value));
                    }
                }

                if (i == m_orgLines.Count - 1 && drawSplits.Count < m_orgLines.Count) // 마지막 바퀴인 경우 제일 끝에 360 도를 한 번 더 체크
                {
                    int angle = 360;

                    Point center = orgCenter;
                    if (angle >= 180)
                        center.X = orgCenter.X - radiusStep / 2;
                    double radian = Math.PI * angle / 180;
                    Point targetPt = new Point(center.X + bounds.Width * Math.Cos(radian), center.Y + bounds.Width * Math.Sin(radian));

                    Point? orgIntersected = null;
                    for (int j = orgIdx; j < m_orgLines[i].Count - 1; j++)
                    {
                        Util.FindIntersection(center, targetPt, m_orgLines[i][j], m_orgLines[i][j + 1], out bool isIntersected, out Point intersectedPt);

                        if (isIntersected)
                        {
                            // 최초 한 점은 뺄 것 
                            if (Util.getDistance(intersectedPt, center) < 0.000001)
                                continue;

                            orgIntersected = intersectedPt;
                            orgIdx = j + 1; // 다음 각도는 j + 1 부터 시작
                            break;
                        }
                    }

                    Point? drawIntersected = null;
                    if (i < drawSplits.Count)
                    {
                        for (int j = drawIdx; j < drawSplits[i].Count - 1; j++)
                        {
                            if (drawSplits[i][j].isEnd)
                                continue;

                            Util.FindIntersection(center, targetPt, drawSplits[i][j].point, drawSplits[i][j + 1].point, out bool isIntersected, out Point intersectedPt);
                            if (isIntersected)
                            {
                                drawIntersected = intersectedPt;
                                drawIdx = j + 1; // 다음 각도는 j + 1 부터 시작
                                break;
                            }
                        }
                    }

                    // 최초 매칭만 최소거리로 찾는다.
                    // 그 이후는 동일하게 매핑 된다고 가정
                    if (drawIntersected != null && orgIntersected != null)
                    {
                        result.Add(new DiffData(String.Format("Cycle: {0} / Angle: {1}", i + 1, 360 - angle), orgIntersected.Value, drawIntersected.Value));
                    }
                    else if (orgIntersected != null)
                    {
                        result.Add(new DiffData(String.Format("Cycle: {0} / Angle: {1}", i + 1, 360 - angle), orgIntersected.Value));
                    }
                    else if (drawIntersected != null)
                    {
                        result.Add(new DiffData(String.Format("Cycle: {0} / Angle: {1}", i + 1, 360 - angle), null, drawIntersected.Value));
                    }
                }

                results.Add(result);
            }

            return results;
        }

        private async Task nextHandling()
        {
            try
            {
                TestUtil testUtil = TestUtil.Instance;
                TestSetItem testSetItem = new TestSetItem()
                {
                    Id = 0,
                    Number = 0,
                    TestSetId = 0,
                    Word = "반시계방향 자유 나선 그리기"
                };

                m_saveUtil.TestSetItem = testSetItem;

                List<List<DiffData>> diffResults = new List<List<DiffData>>();

                // stroke 가 없는 경우 무시하도록 하고... 
                if (m_drawLines.Count > 0 && m_drawLines[0].Count > 2) // 점 하나만 찍히는 케이스 
                {

                    diffResults = calculateDifference();

                    string testName = String.Format("{0}_{1}", TEST_ORDER, TEST_NAME_KR);

                    await Util.CaptureInkCanvasForStroke2(TEST_ORDER, testName, inkCanvas, null, m_orgLines, m_drawLines, m_testExec, testSetItem);
                    await Util.CaptureInkCanvasForStroke3(TEST_ORDER, testName, inkCanvas, null, m_orgLines, m_drawLines, m_testExec, testSetItem);
                    await Util.CaptureInkCanvasForSpiral(TEST_ORDER, testName, inkCanvas, null, m_orgLines, m_drawLines, diffResults, m_testExec, testSetItem, true);

                    await m_saveUtil.saveStroke(TEST_ORDER, testName, inkCanvas);
                    await m_saveUtil.saveRawData2(TEST_ORDER, testName, m_orgLines, getSplitDrawing(false), diffResults, inkCanvas);

                    m_saveUtil.saveResultIntoDB(m_Times, inkCanvas);
                }
            }
            catch (Exception e)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(e.ToString());
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding encoding = Encoding.GetEncoding("euc-kr");

                string logFileName = String.Format("log_{0}_{1}.txt", TEST_NAME, DateTime.Now.ToString("yyyyMMddHHmmss"));

                byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
                StorageFolder orgFolder = ApplicationData.Current.LocalFolder;
                StorageFile resultFile = await orgFolder.CreateFileAsync(logFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(resultFile, fileBytes);
            }
        }

        private void ClearInkData()
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            m_drawLines.Clear();
            m_Times.Clear();
        }
    }
}
