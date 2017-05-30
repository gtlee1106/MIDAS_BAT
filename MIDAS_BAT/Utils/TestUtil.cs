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

        // 켜져 있으면 필기인식, 꺼져있으면 획수로 카운트
        bool m_bInkRecognize = false;

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

        public void EnableRecognize()
        {
            m_bInkRecognize = true;
        }

        public void DisableRecognize()
        {
            m_bInkRecognize = true;
        }

        public async Task<bool> IsCorrectWriting( string targetWord, InkCanvas inkCanvas)
        {
            if (m_bInkRecognize)
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
            // 미리 계산해둘까...?
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
