using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
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
        List<long> m_Times;
        

        public TestPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse 
                                                    | Windows.UI.Core.CoreInputDeviceTypes.Pen
                                                    | Windows.UI.Core.CoreInputDeviceTypes.Touch;

            inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            CoreInkIndependentInputSource core = CoreInkIndependentInputSource.Create(inkCanvas.InkPresenter);
            core.PointerPressing += Core_PointerPressing;
            core.PointerReleasing += Core_PointerReleasing;

            m_curIdx = 0;
            m_targetWord = "";
            m_Times = new List<long>();
        }

        private void Core_PointerReleasing(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            m_Times.Add(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond );
            Debug.WriteLine("Added End");
        }

        private void Core_PointerPressing(CoreInkIndependentInputSource sender, PointerEventArgs args)
        {
            m_Times.Add(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond );
            Debug.WriteLine("Added Start");
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
            }
        }

        private void SetTargetWord(string targetWord)
        {
            m_targetWord = targetWord;
            title.Text = m_targetWord;
        }

        private async void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            var currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStrokes.Count == 0)
            {  
                // 다시 시작한다.
                inkCanvas.InkPresenter.StrokeContainer.Clear();
                return;
            }

            int totalCnt = 0;
            string[] charSeq  = CharacterUtil.GetSplitStrokeStr(m_targetWord);
            for (int i = 0; i < m_targetWord.Length; ++i)
            {
                int[] charCnt = CharacterUtil.GetSingleCharStrokeCnt(m_targetWord.ElementAt(i));
                for (int j = 0; j < charCnt.Length; ++j)
                    totalCnt += charCnt[j];
            }
            
            if( totalCnt != currentStrokes.Count )
            {
                var dialog = new MessageDialog("인식할 수 없습니다. 정자체로 다시 써주시기바랍니다.");
                var res = await dialog.ShowAsync();

                inkCanvas.InkPresenter.StrokeContainer.Clear();
                m_Times.Clear();
                return;
            }

 
            // 개수 카운트 필요하당

            DatabaseManager dbManager = DatabaseManager.Instance;

            await saveStroke(currentStrokes);
            await saveResultIntoDB();

            long[] timeDiff = new long[m_Times.Count - 1];
            for( int i = 0; i < timeDiff.Length; ++i )
                timeDiff[i] = m_Times[i+1] - m_Times[i];
            
            int timeIdx = 0;
            for (int i = 0; i < m_targetWord.Length; ++i)
            {
                int[] charCnt = CharacterUtil.GetSingleCharStrokeCnt(m_targetWord.ElementAt(i));

                long[] duration = new long[charCnt.Length];
                long[] idleTime = new long[charCnt.Length - 1];

                for (int j = 0; j < charCnt.Length;  ++j)
                {
                    duration[j] = 0;
                    for (int k = 0; k < charCnt[j]*2-1; ++k)
                        duration[j] += timeDiff[timeIdx++];

                    if( j < charCnt.Length - 1 )
                        idleTime[j] = timeDiff[timeIdx++];
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
                };

                dbManager.InsertTestExecResult(result);
            }
            
            // index 증가
            m_curIdx++;
            if (m_curIdx == m_wordList.Count)
            {
                // 종료함.
                this.Frame.Navigate(typeof(MainPage));
                return;
            }

            // 새로운 단어 지정 및 전체 초기화.
            SetTargetWord(m_wordList[m_curIdx].Word);
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            m_Times.Clear();
        }

        private async Task saveResultIntoDB()
        {
            DatabaseManager dbManager = DatabaseManager.Instance;

            return;
        }

        private async Task<int> saveStroke(IReadOnlyList<InkStroke> currentStroke)
        {
            string file_name = m_testExec.TesterId.ToString() + "_" + m_curIdx.ToString() + ".gif";
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            Debug.WriteLine("step1");
            if (file == null)
                return 1;

            Windows.Storage.CachedFileManager.DeferUpdates(file);
            IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

            Debug.WriteLine("step2");
            using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
            {
                await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                await outputStream.FlushAsync();
            }
            stream.Dispose();

            Debug.WriteLine("step3");
            Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

            if( status == Windows.Storage.Provider.FileUpdateStatus.Complete )
            {
            }
            Debug.WriteLine("step4");

            return 0;
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
