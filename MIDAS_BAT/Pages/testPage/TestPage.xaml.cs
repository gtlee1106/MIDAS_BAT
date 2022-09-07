using Microsoft.Graphics.Canvas;
using MIDAS_BAT.Data;
using MIDAS_BAT.Pages;
using MIDAS_BAT.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace MIDAS_BAT
{
    public sealed partial class TestPage : Page
    {
        List<TestSetItem> m_wordList;
        TestExec m_testExec;
        string m_targetWord = "";
        int m_curIdx = 0;

        // 획 시작 - 끝 시간 기록
        List<double> m_Times = new List<double>();
        List<List<BATPoint>> m_drawLines = new List<List<BATPoint>>();

        SaveUtil m_saveUtil = SaveUtil.Instance;

        public static string TEST_NAME = "characterTest";
        public static string TEST_NAME_KR = "글자 쓰기";
        public static int TEST_ORDER = 6;

        public TestPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse
                                                    | CoreInputDeviceTypes.Pen;

            inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            CoreInkIndependentInputSource core = CoreInkIndependentInputSource.Create(inkCanvas.InkPresenter);
            core.PointerPressing += Core_PointerPressing;
            core.PointerMoving += Core_PointerMoving;
            core.PointerReleasing += Core_PointerReleasing;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Window.Current.SizeChanged += Current_SizeChanged;

            if (e.Parameter is TestExec)
            {
                DatabaseManager dbManager = DatabaseManager.Instance;
                TestExec exec = e.Parameter as TestExec;

                m_testExec = exec;
                m_saveUtil.TestExec = m_testExec;
                m_wordList = dbManager.GetTestSetItems(exec.TestSetId);
                if (m_wordList.Count == 0)
                    return;

                UpdateCurrnetStatus();

                ResizeCanvas();
            }
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            ResizeCanvas();
        }

        /////// events ////////
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

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            //RecognizeCurrentCanvas();
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
            }
        }

        private async void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            bool exit = await Util.ShowStopExamAlertDlg();
            if (!exit)
                return;

            this.Frame.Navigate(typeof(MainPage));
        }

        private async void cleanBtn_Click(object sender, RoutedEventArgs e)
        {
            bool erase= await Util.ShowEraseAlertDlg();
            if (!erase)
                return;

            ClearInkData();
        }

        private async void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            bool goBack = await Util.ShowGoBackAlertDlg();
            if (!goBack)
                return;

            if( m_curIdx == 0 )
            {
                Type prevTest = Util.getPrevTest(DatabaseManager.Instance.GetActiveTestSet(), TEST_ORDER);
                if (prevTest == null)
                    await Util.ShowCannotGoBackAlertDlg();
                else
                {
                    this.Frame.Navigate(prevTest, m_testExec, new SuppressNavigationTransitionInfo());
                }
                return;
            }

            m_curIdx--;

            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(m_testExec.TesterId);

            string testerName = tester.GetTesterName(m_testExec.Datetime);
            
            string prefix = String.Format("{0}_{1}_{2}", testerName, TEST_ORDER, TEST_NAME_KR);

            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string[] file_names = {
                String.Format("{0}_{1}_{2}_{3}.gif", testerName, TEST_ORDER, TEST_NAME_KR, m_wordList[m_curIdx].Number),
                String.Format("{0}_{1}_{2}_{3}_{4}_최종.png", testerName, TEST_ORDER, TEST_NAME_KR, m_wordList[m_curIdx].Number, m_wordList[m_curIdx].Word),
                String.Format("{0}_{1}_{2}_{3}_{4}_잉크.png", testerName, TEST_ORDER, TEST_NAME_KR, m_wordList[m_curIdx].Number, m_wordList[m_curIdx].Word),
                String.Format("{0}_{1}_{2}_{3}_{4}_time.png", testerName, TEST_ORDER, TEST_NAME_KR, m_wordList[m_curIdx].Number, m_wordList[m_curIdx].Word),
                String.Format("{0}_{1}_{2}_{3}_{4}_pressure.png", testerName, TEST_ORDER, TEST_NAME_KR, m_wordList[m_curIdx].Number, m_wordList[m_curIdx].Word),
                String.Format("{0}_{1}_{2}_{3}_{4}_MinMax.png", testerName, TEST_ORDER, TEST_NAME_KR, m_wordList[m_curIdx].Number, m_wordList[m_curIdx].Word),
            };

            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            foreach( var file_name in file_names )
            {
                if (await storageFolder.TryGetItemAsync(file_name) != null)
                {
                    StorageFile targetFile = await storageFolder.GetFileAsync(file_name);
                    if (targetFile != null)
                        await targetFile.DeleteAsync();
                }
            }

            m_saveUtil.deleteResultFromDB(m_testExec, m_wordList[m_curIdx]);

            // UI 업데이트 
            UpdateCurrnetStatus();
            ResizeCanvas();
            ClearInkData();
        }
        /////// end of events ////////

        private void ResizeCanvas()
        {
            // ui setup
            DisplayInformation di = DisplayInformation.GetForCurrentView();
            int len = m_targetWord.Length;
            int width = (int)(di.RawDpiX * (m_testExec.ScreenWidth / 25.4f) / (float)di.RawPixelsPerViewPixel);
            int height = (int)(di.RawDpiY * (m_testExec.ScreenHeight / 25.4f) / (float)di.RawPixelsPerViewPixel);

            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            inkCanvas.Width = bounds.Width;
            inkCanvas.Height = bounds.Height;

            ClearInkData();

            if (m_testExec.ShowBorder)
            {
                borderCanvas.BorderThickness = new Thickness(1.0);
                borderCanvas.Width = width * len;
                borderCanvas.Height = height;

                guideLineCanvas.Width = width * len;
                guideLineCanvas.Height = height;

                guideLineCanvas.Children.Clear();
                for (int i = 0; i < len - 1; ++i)
                {
                    var line = new Line();
                    line.Stroke = new SolidColorBrush(Colors.Black);
                    line.StrokeThickness = 2;

                    var dashed = new DoubleCollection();
                    dashed.Add(1);
                    line.StrokeDashArray = dashed;

                    line.X1 = (i + 1) * width;
                    line.X2 = (i + 1) * width;
                    line.Y2 = height;

                    guideLineCanvas.Children.Add(line);
                }
            }
            else
            {
                guideLineCanvas.Children.Clear();
                borderCanvas.BorderThickness = new Thickness(0.0);
                borderCanvas.Height = height; // 버튼 위치들 때문에 높이만 맞춰준다. 
            }
        }

        private void UpdateCurrnetStatus()
        {
            m_saveUtil.TestSetItem = m_wordList[m_curIdx];

            m_targetWord = m_wordList[m_curIdx].Word;

            if( AppConfig.Instance.ShowTargetWord == true )
                title.Text = m_targetWord;
            number.Text = (m_curIdx + 1).ToString();
        }

        private async Task nextHandling()
        {
            try
            {

                TestUtil testUtil = TestUtil.Instance;
                if (!await testUtil.IsCorrectWriting(m_targetWord, inkCanvas))
                {
                    await Util.ShowWrongWritingAlertDlg();
                    ClearInkData();

                    return;
                }

                string testName = String.Format("{0}_{1}", TEST_ORDER, TEST_NAME_KR);

                await Util.CaptureInkCanvasForStroke2(TEST_ORDER, testName, inkCanvas, borderCanvas, null, m_drawLines, m_testExec, m_wordList[m_curIdx]);
                await Util.CaptureInkCanvas(TEST_ORDER, testName, inkCanvas, borderCanvas, null, m_drawLines, new List<List<DiffData>>(), m_testExec, m_wordList[m_curIdx]);

                await m_saveUtil.saveStroke(TEST_ORDER, testName, inkCanvas);
                await m_saveUtil.saveRawData2(TEST_ORDER, testName, null, m_drawLines, new List<List<DiffData>>(), inkCanvas);
                m_saveUtil.saveResultIntoDB(m_Times, inkCanvas);

                // index 증가
                if (AvailableToGoToNext())
                {
                    m_curIdx++;

                    // 새로운 단어 지정 및 전체 초기화.
                    UpdateCurrnetStatus();
                    ResizeCanvas();
                    ClearInkData();
                }
                else
                {
                    var dialog = new MessageDialog("검사가 끝났습니다. 수고하셨습니다.");
                    await dialog.ShowAsync();
                    this.Frame.Navigate(typeof(MainPage));
                    return;
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

        private bool AvailableToGoToNext()
        {
            if (m_curIdx + 1 >= m_wordList.Count)
                return false;
            return true;
        }

        private void ClearInkData()
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            m_Times.Clear();
            m_drawLines.Clear();
        }

    }
}
