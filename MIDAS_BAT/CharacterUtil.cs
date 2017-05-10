using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDAS_BAT
{
    public class CharacterUtil
    {
        private static string m_chosungTable = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        private static string m_joonsungTable = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        private static string m_jongsungTable = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";
        private static int[] m_chosungCntTable = { 1, 2, 1, 2, 4, 3, 3, 4, 8, 2, 4, 1, 2, 4, 3, 2, 3, 4, 3 };
        private static int[] m_joongsungCntTable = { 2, 3, 3, 4, 2, 3, 3, 4, 2, 4, 5, 3, 3, 2, 4, 5, 3, 3, 1, 2, 1};
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
            for( int i = 0; i < word.Length; ++i )
            {
                if (word.ElementAt(i) < m_unicodeHangulBase || word.ElementAt(i) > m_unicodeHangulLast)
                    return false;
            }
            return true;
        }

        public static int[] GetSplitStrokeNumber(string targetWord)
        {
            int[] numbers = new int[targetWord.Length * 3];
            for (int i = 0; i < targetWord.Length; ++i)
            {
                char ch = targetWord.ElementAt(i);

            }

            return numbers;
        }
        public static string[] GetSplitStrokeStr(string targetWord)
        {
            string[] ret = new string[targetWord.Length];
            for (int i = 0; i < targetWord.Length; ++i)
            {
                ret[i] = "";
                char ch = targetWord.ElementAt(i);
                char[] seq = GenerateSequence(ch);

                for (int j = 0; j < seq.Length; ++j)
                    ret[i] += seq[j];
            }

            return ret;
        }
        public static char[] GenerateSequence(char ch)
        {
            char[] ret = new char[3];
            int chosungIdx, joongsungIdx, jongsungIdx;
            ushort uTempCode = 0x0000; 
            uTempCode = Convert.ToUInt16(ch);
            if ((uTempCode < m_unicodeHangulBase) || (uTempCode > m_unicodeHangulLast))
            {
                ret[0] = ret[1] = ret[2] = ' ';
            }
            int iUniCode = uTempCode - m_unicodeHangulBase;
            chosungIdx = iUniCode / (21 * 28);
            iUniCode = iUniCode % (21 * 28);
            joongsungIdx = iUniCode / 28;
            iUniCode = iUniCode % 28;
            jongsungIdx = iUniCode;

            ret[0] = m_chosungTable[chosungIdx];
            ret[1] = Convert.ToChar(m_unicodeHangulBase + 21 * 28 * chosungIdx + 28 * joongsungIdx);
            ret[2] = Convert.ToChar(m_unicodeHangulBase + 21 * 28 * chosungIdx + 28 * joongsungIdx + jongsungIdx);

            return ret;
        }

        public static int[] GetSingleCharStrokeCnt(char ch)
        {
            int[] ret = new int[3];
            int chosungIdx, joongsungIdx, jongsungIdx;
            ushort uTempCode = 0x0000; 
            uTempCode = Convert.ToUInt16(ch);
            if ((uTempCode < m_unicodeHangulBase) || (uTempCode > m_unicodeHangulLast))
            {
                ret[0] = ret[1] = ret[2] = 0;
            }
            int iUniCode = uTempCode - m_unicodeHangulBase;
            chosungIdx = iUniCode / (21 * 28);
            iUniCode = iUniCode % (21 * 28);
            joongsungIdx = iUniCode / 28;
            iUniCode = iUniCode % 28;
            jongsungIdx = iUniCode;

            ret[0]= m_chosungCntTable[chosungIdx];
            ret[1]= m_joongsungCntTable[joongsungIdx];
            ret[2]= m_jongsungCntTable[jongsungIdx];

            return ret;
        }

    }
}
