using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MIDAS_BAT.Utils
{
    class TestUtil
    {
        private static readonly TestUtil instance = new TestUtil();

        private TestUtil()
        {
        }

        public static TestUtil Instance
        {
            get
            {
                return instance;
            }
        }

        public async Task<bool> IsCorrectWriting( string targetWord, InkCanvas inkCanvas)
        {
            int strokeCount = inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count;
            if (strokeCount < 1 )
                return false;


            if (AppConfig.Instance.UseHandWritingRecognition == true)
                return await IsCorrectWriting_InkRecognize(targetWord, inkCanvas);
            else
                return IsCorrectWriting_LineCounting(targetWord, inkCanvas);
        }

        private async Task<bool> IsCorrectWriting_InkRecognize(string targetWord, InkCanvas inkCanvas)
        {
            return await CharacterUtil.IsRecognizable(targetWord, inkCanvas);
        }

        private bool IsCorrectWriting_LineCounting(string targetWord, InkCanvas inkCanvas)
        {
            //gtlee. 당장은 사용하지 않는 방향으로...
            return true;

            int totalCnt = 0;
            List<string> charSeq = CharacterUtil.GetSplitStrokeStr(targetWord);
            for (int i = 0; i < targetWord.Length; ++i)
            {
                List<int> charCnt = CharacterUtil.GetSingleCharStrokeCnt(targetWord.ElementAt(i));
                foreach (var cnt in charCnt)
                    totalCnt += cnt;
            }

            var currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (totalCnt != currentStrokes.Count)
                return false;

            return true;
        }
    }
}
