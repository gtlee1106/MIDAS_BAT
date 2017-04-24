using System;
using System.Collections.Generic;
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

        public TestPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse 
                                                    | Windows.UI.Core.CoreInputDeviceTypes.Pen
                                                    | Windows.UI.Core.CoreInputDeviceTypes.Touch;

            inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            m_curIdx = 0;
            m_targetWord = "";
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
//            Recognize_Stroke();
        }

        private async void Recognize_Stroke()
        {
            char[] split_results = Util.GetSplitStrokeStr(m_targetWord);

            IReadOnlyList<InkStroke> curStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (curStrokes.Count < 1)
                return;

            InkRecognizerContainer inkRecognizerContainer = new InkRecognizerContainer();
            if (inkRecognizerContainer == null)
                return;

            IReadOnlyList<InkRecognitionResult> recognitionResults = await inkRecognizerContainer.RecognizeAsync(inkCanvas.InkPresenter.StrokeContainer, InkRecognitionTarget.All);
            if (recognitionResults.Count < 1)
                return;

            string str = "";
            foreach( var result in recognitionResults)
            {
                IReadOnlyList<string> candidates = result.GetTextCandidates();
                foreach( string candidate in candidates)
                {
                    str += candidate + " ";
                }
            }

            int a = 0;
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
            IReadOnlyList<InkStroke> currentStroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStroke.Count == 0)
            {  
                // 다시 시작한다.
                inkCanvas.InkPresenter.StrokeContainer.Clear();
                return;
            }

            await saveStroke(currentStroke);
            await saveResultIntoDB();
            
            // targetWord 의 초성/중성/종성 획수 계산
            //       Recognize_Stroke();

            // stroke에서 전체 시간 저장 & 초성중성종성 획수 계산

            // stroke 저장
            // 

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
        }

        private async Task saveResultIntoDB()
        {
            DatabaseManager dbManager = DatabaseManager.Instance;
            TestExecResult result = new TestExecResult()
            {
                TestExecId = m_testExec.Id,
                TestSetItemId = m_wordList.ElementAt(m_curIdx).Id,
                ChosungTime = 0.1,
                FirstIdleTIme = 0.1,
                JoongsungTime = 0.1,
                SecondIdelTime = 0.1,
                JongSungTime = 0.1
            };
            dbManager.InserTestExecResult(result);

            return;
        }

        private async Task<int> saveStroke(IReadOnlyList<InkStroke> currentStroke)
        {
            string file_name = m_testExec.TesterId.ToString() + "_" + m_curIdx.ToString() + ".gif";
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            if (file == null)
                return 1;

            Windows.Storage.CachedFileManager.DeferUpdates(file);
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
    }
}
