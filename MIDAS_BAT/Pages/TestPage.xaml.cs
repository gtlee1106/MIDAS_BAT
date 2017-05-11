using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 http://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace MIDAS_BAT
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class TestPage : Page
    {
        List<TestSetItem> m_wordList;
        TestExec m_testExec;
        string m_targetWord;
        int m_curIdx;
        List<double> m_Times;

        public TestPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse 
                                                    | CoreInputDeviceTypes.Pen
                                                    | CoreInputDeviceTypes.Touch;

            inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            CoreInkIndependentInputSource core = CoreInkIndependentInputSource.Create(inkCanvas.InkPresenter);
            core.PointerPressing += Core_PointerPressing;
            core.PointerReleasing += Core_PointerReleasing;

            m_curIdx = 0;
            m_targetWord = "";
            m_Times = new List<double>();
        }

        private void Core_PointerReleasing(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            m_Times.Add((double)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond );
        }

        private void Core_PointerPressing(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            m_Times.Add((double)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond );
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
        }

        private async Task<int[]> Recognize_Stroke( string chSeq, int startIdx )
        {
            int[] ret = { startIdx, startIdx, startIdx };
            string[] split_results = CharacterUtil.GetSplitStrokeStr(m_targetWord);

            IReadOnlyList<InkStroke> curStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (curStrokes.Count < 1)
                return ret;

            int strokeCnt = curStrokes.Count;

            // 다른데 만들어둘까... 
            InkRecognizerContainer inkRecognizerContainer = new InkRecognizerContainer();
            if (inkRecognizerContainer == null)
                return ret;


            foreach (var item in split_results)
            {
                IReadOnlyList<InkRecognitionResult> recognitionResults = await inkRecognizerContainer.RecognizeAsync(inkCanvas.InkPresenter.StrokeContainer, InkRecognitionTarget.Selected);
                if (recognitionResults.Count < 1)
                    return ret;

                bool found = false;
                foreach (var result in recognitionResults)
                {
                    IReadOnlyList<string> candidates = result.GetTextCandidates();
                    foreach (string candidate in candidates)
                    {
                        if( candidate.Equals(item) )
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }
            }

            return ret;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is TestExec)
            {
                DatabaseManager dbManager = DatabaseManager.Instance;
                TestExec exec = e.Parameter as TestExec;

                m_testExec = exec;

                m_wordList = dbManager.GetTestSetItems(exec.TestSetId);

                if (m_wordList.Count > 0)
                    m_targetWord = m_wordList.ElementAt(0).Word;

                SetTargetWord(m_targetWord);


                // ui setup
                if (exec.ShowBorder)
                {
                    borderInkCanvas.BorderThickness = new Thickness(1.0);
                    DisplayInformation di = DisplayInformation.GetForCurrentView();
                    borderInkCanvas.Width = (int)(di.RawDpiX * (exec.ScreenWidth / 25.4f) / (float)di.RawPixelsPerViewPixel);
                    borderInkCanvas.Height = (int)(di.RawDpiY * (exec.ScreenHeight / 25.4f) / (float)di.RawPixelsPerViewPixel);
                }
                else
                {
                    borderInkCanvas.BorderThickness = new Thickness(0.0);
                }

                
            }
        }

        private void SetTargetWord(string targetWord)
        {
            m_targetWord = targetWord;
            title.Text = m_targetWord;
        }

        private async Task<bool> CheckTotalStrokeWithAnswer()
        {
            // 미리 계산해둘까...?
            int totalCnt = 0;
            string[] charSeq = CharacterUtil.GetSplitStrokeStr(m_targetWord);
            for (int i = 0; i < m_targetWord.Length; ++i)
            {
                int[] charCnt = CharacterUtil.GetSingleCharStrokeCnt(m_targetWord.ElementAt(i));
                for (int j = 0; j < charCnt.Length; ++j)
                    totalCnt += charCnt[j];
            }

            var currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (totalCnt != currentStrokes.Count)
            {
                var dialog = new MessageDialog("인식할 수 없습니다. 정자체로 다시 써주시기바랍니다.");
                var res = await dialog.ShowAsync();

                inkCanvas.InkPresenter.StrokeContainer.Clear();
                m_Times.Clear();
                return false;
            }

            return true;
        }

        private async Task nextHandling()
        {
            bool check =  await CheckTotalStrokeWithAnswer();
            if (!check)
                return;

            await Util.CaptureInkCanvasForStroke(inkCanvas, m_testExec.TesterId.ToString(), m_curIdx);
            
            await saveStroke();
            
            await saveRawData();
            saveResultIntoDB_other();


            // index 증가
            if( AvailableToGoToNext() )
            {
                m_curIdx++;

                // 새로운 단어 지정 및 전체 초기화.
                SetTargetWord(m_wordList[m_curIdx].Word);

                ClearInkData();
            }
            else
            {

                var dialog = new MessageDialog("검사가 끝났습니다. 수고하셨습니다.");
                dialog.ShowAsync();
                this.Frame.Navigate(typeof(MainPage));
                return;
            }
        }

        private async Task<bool> saveRawData()
        {
            // pressure & time diff 저장...?
            string file_name = m_testExec.TesterId.ToString() + "_raw_" + m_curIdx.ToString() + ".txt";
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(m_Times.Count.ToString());
            for( int i = 0; i < m_Times.Count; ++i )
                builder.AppendLine(m_Times[i].ToString("F3"));

            IReadOnlyList<InkStroke> strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            builder.AppendLine(strokes.Count.ToString());
            for( int i = 0; i < strokes.Count; ++i )
            {
                IReadOnlyList<InkStrokeRenderingSegment> segments = strokes[i].GetRenderingSegments();
                builder.AppendLine(segments.Count.ToString());
                foreach( var seg in segments )
                {
                    builder.AppendLine(seg.Pressure.ToString("F6"));
                }
            }

            await FileIO.WriteTextAsync(file, builder.ToString());

            return true;
        }

        private async Task<bool> saveInkCanvas( InkCanvas inkCanvas )
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = m_testExec.TesterId.ToString() + "_char_" + m_curIdx.ToString() + ".gif";
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            var displayInformation = DisplayInformation.GetForCurrentView();
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.GifEncoderId, stream);

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget rtb = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96); // 96 쓰는게 맞나? or dpi 받아서 써야되나?
            IReadOnlyList<InkStroke> strokeList = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            List<InkStroke> newStrokeList = new List<InkStroke>();

            foreach (var stroke in strokeList)
            {
                IReadOnlyList<InkPoint> pointList = stroke.GetInkPoints();

                List<InkPoint> newPointList = new List<InkPoint>();
                foreach (var point in pointList)
                {
                    newPointList.Add(point);
                    InkStrokeBuilder strokeBuilder = new InkStrokeBuilder();
                    var newStroke = strokeBuilder.CreateStrokeFromInkPoints(newPointList, stroke.PointTransform);

                    newStrokeList.Add(newStroke);

                    // 한프레임...?
                    using (var ds = rtb.CreateDrawingSession())
                    {
                        ds.Clear(Colors.White);
                        ds.DrawInk(newStrokeList);
                    }

                    var pixelBuffer = rtb.GetPixelBytes();
                    var pixels = pixelBuffer.ToArray();

                    encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                         BitmapAlphaMode.Premultiplied,
                                             (uint)inkCanvas.ActualWidth,
                                             (uint)inkCanvas.ActualWidth,
                                             displayInformation.RawDpiX,
                                             displayInformation.RawDpiY,
                                             pixels);

                    await encoder.GoToNextFrameAsync();

                }
            }

            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(newStrokeList);
            }

            var lastPixelBuffer = rtb.GetPixelBytes();
            var lastPixels = lastPixelBuffer.ToArray();

            for (int i = 0; i < 10; ++i)
            {
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Premultiplied,
                                    (uint)inkCanvas.ActualWidth,
                                    (uint)inkCanvas.ActualWidth,
                                    displayInformation.RawDpiX,
                                    displayInformation.RawDpiY,
                                    lastPixels);
                if (i < 10 - 1)
                    await encoder.GoToNextFrameAsync();
            }

            await encoder.FlushAsync();
            stream.Dispose();

            return true;
        }

        private bool AvailableToGoToNext()
        {
            if (m_curIdx + 1 >= m_wordList.Count)
                return false;
            return true;
        }

        private static bool nextLock = false;
        private async void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (nextLock == false) 
            {
                nextLock = true;
                await nextHandling();
                nextLock = false;
            }
        }

        private void saveResultIntoDB_other()
        {
            DatabaseManager dbManager = DatabaseManager.Instance;
            var currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            int strokeIdx = 0;
            int baseIdx = 0;
            for (int i = 0; i < m_targetWord.Length; ++i)
            {
                int[] charCnt = CharacterUtil.GetSingleCharStrokeCnt(m_targetWord.ElementAt(i));
                double[] duration = new double[charCnt.Length];     // 초성/중성/종성 시간 측정
                double[] idleTime = new double[charCnt.Length]; // idle time 측정

                // duration 계산
                for( int j = 0; j < charCnt.Length; ++j )
                {
                    if( charCnt[j] == 0 ) // 종성이 없는 경우 들어옴. 
                    {
                        duration[j] = 0.0;
                        idleTime[j] = 0.0;
                        continue;
                    }

                    int offset = baseIdx + charCnt[j] * 2 - 1;
                    duration[j] = m_Times[offset] - m_Times[baseIdx];

                    if (m_Times.Count > offset + 1)
                        idleTime[j] = m_Times[offset + 1] - m_Times[offset];
                    else
                        idleTime[j] = 0.0;

                    baseIdx = offset + 1;
                }

                // pressure 계산
                double[] avgPressure = new double[charCnt.Length];  // 초성/중성/종성 평균 압력 측정
                for (int j = 0; j < charCnt.Length; ++j)
                {
                    avgPressure[j] = 0;
                    int segCnt = 0;
                    for (int k = 0; k < charCnt[j]; ++k)
                    {
                        IReadOnlyList<InkStrokeRenderingSegment> segList = currentStrokes[strokeIdx].GetRenderingSegments();
                        foreach (var seg in segList)
                        {
                            avgPressure[j] += seg.Pressure;
                        }
                        segCnt += segList.Count;

                        strokeIdx++;
                    }
                    if (segCnt != 0)
                        avgPressure[j] /= (double)segCnt;
                }

                TestExecResult result = new TestExecResult()
                {
                    TestExecId = m_testExec.Id,
                    TestSetItemId = m_wordList[m_curIdx].Id,
                    TestSetItemCharIdx = i,
                    ChosungTime = duration[0],
                    JoongsungTime = duration[1],
                    JongsungTime = duration[2],
                    FirstIdleTIme = idleTime[0],
                    SecondIdelTime = idleTime[1],
                    ThirdIdleTime = idleTime[2],
                    ChosungAvgPressure = avgPressure[0],
                    JoongsungAvgPressure = avgPressure[1],
                    JongsungAvgPressure = avgPressure[2]
                };

                dbManager.InsertTestExecResult(result);


            }
        }

        private void saveResultIntoDB()
        {
            DatabaseManager dbManager = DatabaseManager.Instance;

            double[] timeDiff = new double[m_Times.Count - 1];
            for (int i = 0; i < timeDiff.Length; ++i)
                timeDiff[i] = m_Times[i + 1] - m_Times[i];

            var currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            int timeIdx = 0;
            int strokeIdx = 0;
            for (int i = 0; i < m_targetWord.Length; ++i)
            {
                int[] charCnt = CharacterUtil.GetSingleCharStrokeCnt(m_targetWord.ElementAt(i));

                double[] duration = new double[charCnt.Length];     // 초성/중성/종성 시간 측정
                double[] idleTime = new double[charCnt.Length]; // idle time 측정
                double[] avgPressure = new double[charCnt.Length];  // 초성/중성/종성 평균 압력 측정

                for (int j = 0; j < charCnt.Length; ++j)
                {
                    duration[j] = 0;
                    for (int k = 0; k < charCnt[j] * 2 - 1; ++k)
                        duration[j] += timeDiff[timeIdx++];

                    if (timeIdx < timeDiff.Count())
                        idleTime[j] = timeDiff[timeIdx++];
                    else
                        idleTime[j] = 0.0;


                    avgPressure[j] = 0;
                    int segCnt = 0;
                    for(int k = 0; k < charCnt[j]; ++k )
                    {
                        IReadOnlyList<InkStrokeRenderingSegment> segList = currentStrokes[strokeIdx].GetRenderingSegments();
                        foreach( var seg in segList)
                        {
                            avgPressure[j] += seg.Pressure;
                        }
                        segCnt += segList.Count;

                        strokeIdx++;
                    }
                    if( segCnt != 0 )
                        avgPressure[j] /= (double)segCnt;
                }

                TestExecResult result = new TestExecResult()
                {
                    TestExecId = m_testExec.Id,
                    TestSetItemId = m_wordList[m_curIdx].Id,
                    TestSetItemCharIdx = i,
                    ChosungTime = duration[0],
                    JoongsungTime = duration[1],
                    JongsungTime = duration[2],
                    FirstIdleTIme = idleTime[0],
                    SecondIdelTime = idleTime[1],
                    ThirdIdleTime = idleTime[2],
                    ChosungAvgPressure = avgPressure[0],
                    JoongsungAvgPressure = avgPressure[1],
                    JongsungAvgPressure = avgPressure[2]
                };

                dbManager.InsertTestExecResult(result);
            }

            return;
        }

        private async Task<int> saveStroke()
        {
            string file_name = m_testExec.TesterId.ToString() + "_" + m_curIdx.ToString() + ".gif";
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            if (file == null)
                return 1;

            CachedFileManager.DeferUpdates(file);
            IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

            using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
            {
                await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                await outputStream.FlushAsync();
            }
            stream.Dispose();

            Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

            if( status == Windows.Storage.Provider.FileUpdateStatus.Complete )
            {
            }

            return 0;
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void cleanBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearInkData();
        }

        private void ClearInkData()
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            m_Times.Clear();
        }
    }
}
