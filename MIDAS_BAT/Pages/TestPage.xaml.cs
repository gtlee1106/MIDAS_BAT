using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
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
        string m_targetWord;
        int m_curIdx;
        public TestPage()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse 
                                                    | Windows.UI.Core.CoreInputDeviceTypes.Pen;

            m_curIdx = 0;
            m_targetWord = "";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is TestExec)
            {
                DatabaseManager dbManager = DatabaseManager.Instance;
                
                TestExec exec = e.Parameter as TestExec;

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

        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            m_curIdx++;

            if( m_curIdx == m_wordList.Count)
            {
                // 종료한다
            }
            else
            {
                // targetWord 의 초성/중성/종성 획수 계산
                
                // stroke에서 전체 시간 저장 & 초성중성종성 획수 계산
                
                // stroke 저장
                // 



                SetTargetWord(m_wordList[m_curIdx].Word);

                IReadOnlyList<InkStroke> currentStroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                if (currentStroke.Count > 0)
                {
                    //saveStroke( currentStroke );
                }

                inkCanvas.InkPresenter.StrokeContainer.Clear();
            }
        }

        private async void saveStroke(IReadOnlyList<InkStroke> currentStroke)
        {
            Windows.Storage.Pickers.FileSavePicker savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("GIT with embedded ISF", new List<String>() { ".gif" });
            savePicker.DefaultFileExtension = ".gif";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();

            if (file != null )
            {
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
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
