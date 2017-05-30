using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

namespace MIDAS_BAT
{
    public class CharacterUtil
    {
        private static string m_chosungTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        private static string m_joonsungTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        private static string m_jongsungTable = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";
        private static int[] m_chosungCntTable = { 1, 2, 1, 2, 4, 3, 3, 4, 8, 2, 4, 1, 2, 4, 3, 2, 3, 4, 3 };
        private static int[] m_joongsungCntTable = { 2, 3, 3, 4, 2, 3, 3, 4, 2, 4, 5, 3, 3, 2, 4, 5, 3, 3, 1, 2, 1 };
        private static int[] m_jongsungCntTable = {
            0, 1, 2, 3, // ( ) ㄱ ㄲ ㄳ
            1, 3, 4, 2, // ㄴ ㄵ ㄶ ㄷ
            3, 4, 6, 7, // ㄹ ㄺ ㄻ ㄼ
            5, 6, 7, 6, // ㄽ ㄾ ㄿ ㅀ
            3, 4, 6, 2, // ㅁ ㅂ ㅄ ㅅ
            4, 1, 2, 3, // ㅆ ㅇ ㅈ ㅊ 
            2, 3, 4, 3 }; // ㅋ ㅌ ㅍ ㅎ
        private static ushort m_unicodeHangulBase = 0xAC00;
        private static ushort m_unicodeHangulLast = 0xD79F;

        public static bool IsHangul(string word)
        {
            for (int i = 0; i < word.Length; ++i)
            {
                if (word.ElementAt(i) < m_unicodeHangulBase || word.ElementAt(i) > m_unicodeHangulLast)
                    return false;
            }
            return true;
        }

        public static List<string> GetSplitStrokeStr(string targetWord)
        {
            List<string> res = new List<string>();
            string lastWord = "";
            for (int i = 0; i < targetWord.Length; ++i)
            {
                char ch = targetWord.ElementAt(i);
                List<string> seq = GenerateSequence(ch);

                for (int j = 0; j < seq.Count; ++j)
                    res.Add(lastWord + seq[j]);
                lastWord += seq[seq.Count - 1];
            }

            return res;
        }
        public static List<string> GenerateSequence(char ch)
        {
            List<string> res = new List<string>();
            int chosungIdx, joongsungIdx, jongsungIdx;
            ushort uTempCode = 0x0000;
            uTempCode = Convert.ToUInt16(ch);
            if ((uTempCode < m_unicodeHangulBase) || (uTempCode > m_unicodeHangulLast))
            {
                return res;
            }

            int iUniCode = uTempCode - m_unicodeHangulBase;
            chosungIdx = iUniCode / (21 * 28);
            iUniCode = iUniCode % (21 * 28);
            joongsungIdx = iUniCode / 28;
            iUniCode = iUniCode % 28;
            jongsungIdx = iUniCode;

            char a = m_chosungTable[chosungIdx];
            char b = Convert.ToChar(m_unicodeHangulBase + 21 * 28 * chosungIdx + 28 * joongsungIdx);
            char c = Convert.ToChar(m_unicodeHangulBase + 21 * 28 * chosungIdx + 28 * joongsungIdx + jongsungIdx);

            res.Add(a.ToString());
            res.Add(b.ToString());
            if (!b.Equals(c))
                res.Add(c.ToString());

            return res;
        }

        public static int getChosungIdx(char ch)
        {
            for (int i = 0; i < m_chosungTable.Length; ++i)
            {
                if (ch.Equals(m_chosungTable.ElementAt(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int getJoongsungIdx(char ch)
        {
            for( int i = 0; i < m_joonsungTable.Length; ++i )
            {
                if (ch.Equals(m_joonsungTable.ElementAt(i)))
                    return i;
            }

            return -1;
        }

        public static List<int> GetSingleCharStrokeCnt(char ch)
        {
            List<int> retList = new List<int>();

            int chosungIdx, joongsungIdx, jongsungIdx;
            ushort uTempCode = 0x0000;
            uTempCode = Convert.ToUInt16(ch);
            if ((uTempCode < m_unicodeHangulBase) || (uTempCode > m_unicodeHangulLast))
            {
                retList.Add(0);
                retList.Add(0);
                retList.Add(0);
                return retList;
            }
            int iUniCode = uTempCode - m_unicodeHangulBase;
            chosungIdx = iUniCode / (21 * 28);
            iUniCode = iUniCode % (21 * 28);
            joongsungIdx = iUniCode / 28;
            iUniCode = iUniCode % 28;
            jongsungIdx = iUniCode;

            retList.Add(m_chosungCntTable[chosungIdx]);
            retList.Add(m_joongsungCntTable[joongsungIdx]);
            retList.Add(m_jongsungCntTable[jongsungIdx]);

            return retList;
        }

        public static async Task<bool> IsRecognizable( string targetWord, InkCanvas inkCanvas)
        {
            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStrokes.Count == 0)
                return false;

            int len = currentStrokes.Count;
            InkRecognizerContainer inkRecognizerContainer =
                new InkRecognizerContainer();

            if (!setHangulRecognizerDefault(inkRecognizerContainer))
                return false;

            IReadOnlyList<InkRecognitionResult> recognitionResults =
                    await inkRecognizerContainer.RecognizeAsync(
                        inkCanvas.InkPresenter.StrokeContainer,
                        InkRecognitionTarget.All);

            int startIdx = 0;
            foreach (var result in recognitionResults)
            {
                IReadOnlyList<string> candidates = result.GetTextCandidates();
                foreach (var candi in candidates)
                {
                    string str = MergingString(candi);
                    string subStr = targetWord.Substring(startIdx,
                        startIdx + str.Length > targetWord.Length ? targetWord.Length - startIdx : str.Length);
                    if (str.Equals(subStr))
                    {
                        startIdx += str.Length;
                        break;
                    }
                }
            }
            if (startIdx != targetWord.Length)
                return false;

            return true;
        }


        public static async Task< List<int> > recognizeTargetWord(string targetWord, InkCanvas inkCanvas)
        {
            // 1. 인식결과가 targetWord랑 맞는지 비교함
            //    -> 다른 경우, 다시 쓰라고 알림을 던져야...
            // 2. 같은 경우, splitstrokestr 생성
            //    -> 테스트 => ㅌ테테/ㅅ스스/ㅌ트트 
            //    -> 학생 => ㅎ하학/ㅅ새생

            List<int> result = new List<int>();

            if (!await IsRecognizable(targetWord, inkCanvas))
            {
                inkCanvas.InkPresenter.StrokeContainer.Clear();
                return result;
            }

            List<string> splits = GetSplitStrokeStr(targetWord);
            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            int startIdx = 0;
            for (int i = 0; i < currentStrokes.Count; ++i)
            {
                var stroke = currentStrokes.ElementAt(i);
                currentStrokes.ElementAt(i).Selected = true;
                for( int j = startIdx; j < splits.Count; ++i )
                {
                    if (await IsRecognizable(splits[j], inkCanvas, InkRecognitionTarget.Selected))
                    {
                        Debug.WriteLine(i);
                        startIdx = j + 1;
                        break;
                    }
                }
            }
            return result;
        }

        public static async Task<string> RecognizeBest(InkCanvas inkCanvas, InkRecognitionTarget selectionTarget)
        {
            string result = "";
            try
            {
                IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                if (currentStrokes.Count == 0)
                    return result;

                int len = currentStrokes.Count;
                InkRecognizerContainer inkRecognizerContainer =
                    new InkRecognizerContainer();

                if (!setHangulRecognizerDefault(inkRecognizerContainer))
                    return result;

                InkStrokeContainer container = inkCanvas.InkPresenter.StrokeContainer;
                IReadOnlyList<InkRecognitionResult> recognitionResults =
                        await inkRecognizerContainer.RecognizeAsync(
                            container,
                            selectionTarget);

                int ll = recognitionResults.Count;

                foreach (var recog in recognitionResults )
                {
                    // 한글자로 인식할 수도 있고, 두 글자 이상으로 인식할 수도 있음.
                    // .......... 어쩌지 -_-
                    IReadOnlyList<string> candidates = recog.GetTextCandidates();
                    foreach (var candi in candidates)
                    {
                        result += candi;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            return result;
        }

        public static async Task<List<string>> RecognizeAll(InkCanvas inkCanvas, InkRecognitionTarget selectionTarget)
        {
            List<string> result = new List<string>();
            try
            {
                IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                if (currentStrokes.Count == 0)
                    return result;

                int len = currentStrokes.Count;
                InkRecognizerContainer inkRecognizerContainer =
                    new InkRecognizerContainer();

                if (!setHangulRecognizerDefault(inkRecognizerContainer))
                {
                    return result;
                }

                InkStrokeContainer container = inkCanvas.InkPresenter.StrokeContainer;
                IReadOnlyList<InkRecognitionResult> recognitionResults =
                        await inkRecognizerContainer.RecognizeAsync(
                            container,
                            selectionTarget);

                int ll = recognitionResults.Count;

                foreach (var recog in recognitionResults)
                {
                    IReadOnlyList<string> candidates = recog.GetTextCandidates();
                    for( int i = 0; i < candidates.Count; ++i )
                    {
                        string str = MergingString(candidates[i]);
                        result.Add(str);
                    }
                    result.Add("===================================");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            return result;
        }

        private static string MergingString(string str)
        {
            StringBuilder builder = new StringBuilder();
            for( int i = 0; i < str.Length; ++i )
            {
                int chosungIdx = -1;
                if (i > 0)
                    chosungIdx = getChosungIdx(str.ElementAt(i - 1));
                int joongsungIdx = getJoongsungIdx(str.ElementAt(i));
                if( chosungIdx != -1 && joongsungIdx != -1 )
                {
                    char a = Convert.ToChar(m_unicodeHangulBase + 21 * 28 * chosungIdx + 28 * joongsungIdx);
                    builder.Remove(i - 1, 1).Append(a);
                }
                else
                {
                    builder.Append(str.ElementAt(i));
                }
            }

            return builder.ToString();
        }

        private static bool setHangulRecognizerDefault(InkRecognizerContainer inkRecognizerContainer)
        {
            IReadOnlyList<InkRecognizer> list = inkRecognizerContainer.GetRecognizers();
            foreach (var recognizer in list)
            {
                if (recognizer.Name.Contains("한글") == true)
                {
                    inkRecognizerContainer.SetDefaultRecognizer(recognizer);
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> IsRecognizable(string targetWord, InkCanvas inkCanvas, InkRecognitionTarget target = InkRecognitionTarget.All )
        {
            try
            {
                IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                if (currentStrokes.Count == 0)
                    return false;

                int len = currentStrokes.Count;
                InkRecognizerContainer inkRecognizerContainer =
                    new InkRecognizerContainer();

                if (!setHangulRecognizerDefault(inkRecognizerContainer))
                    return false;

                InkStrokeContainer container = inkCanvas.InkPresenter.StrokeContainer;
                IReadOnlyList<InkRecognitionResult> recognitionResults =
                        await inkRecognizerContainer.RecognizeAsync(
                            container,
                            target);

                int ll = recognitionResults.Count;

                int startIdx = 0;
                foreach (var result in recognitionResults)
                {
                    // 한글자로 인식할 수도 있고, 두 글자 이상으로 인식할 수도 있음.
                    // .......... 어쩌지 -_-
                    IReadOnlyList<string> candidates = result.GetTextCandidates();
                    foreach (var candi in candidates)
                    {
                        string str = MergingString(candi);
                        string subStr = targetWord.Substring(startIdx,
                            startIdx + str.Length > targetWord.Length ? targetWord.Length - startIdx : str.Length);
                        if (str.Equals(subStr))
                        {
                            startIdx += str.Length;
                            break;
                        }
                    }
                }
                if (startIdx != targetWord.Length)
                    return false;

            }catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return true;
        }


        public static async Task<List<string>> recognizeIt(InkRecognitionTarget target, InkCanvas inkCanvas)
        {
            List<string> res = new List<string>();
            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (currentStrokes.Count == 0)
                return res;

            InkRecognizerContainer inkRecognizerContainer =
                new InkRecognizerContainer();

            if (!setHangulRecognizerDefault(inkRecognizerContainer))
                return res;

            InkStrokeContainer container = inkCanvas.InkPresenter.StrokeContainer;
            IReadOnlyList<InkRecognitionResult> recognitionResults =
                    await inkRecognizerContainer.RecognizeAsync(
                        container,
                        target);

            foreach (var result in recognitionResults)
            {
                // 한글자로 인식할 수도 있고, 두 글자 이상으로 인식할 수도 있음.
                // .......... 어쩌지 -_-
                int cnt = recognitionResults.Count;
                IReadOnlyList<string> candidates = result.GetTextCandidates();
                foreach (var candi in candidates)
                {
                    int candi_len = candi.Length;
                }

                res.AddRange(candidates);
            }
            return res;
        }

    }
}
