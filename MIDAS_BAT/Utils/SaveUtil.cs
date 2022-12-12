using Microsoft.Graphics.Canvas;
using MIDAS_BAT.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MIDAS_BAT.Utils
{
    class SaveUtil
    {
        private static readonly SaveUtil instance = new SaveUtil();
       
        private SaveUtil()
        {
        }

        public static SaveUtil Instance
        {
            get
            {
                return instance;
            }
        }

        public TestExec TestExec { get; internal set; }
        public TestSetItem TestSetItem { get; internal set; }

        public void saveResultIntoDB( List<double> times, InkCanvas inkCanvas )
        {
            // 자모 구분을 하지 않으면 DB에 저장할 데이터 만들기가 어려움... 
            if ( TestExec.UseJamoSepartaion != true )
                return;

            DatabaseManager dbManager = DatabaseManager.Instance;
            var currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            int strokeIdx = 0;
            int baseIdx = 0;
            for (int i = 0; i < TestSetItem.Word.Length; ++i)
            {
                List<int> charCnt = CharacterUtil.GetSingleCharStrokeCnt(TestSetItem.Word.ElementAt(i));
                double[] duration = new double[charCnt.Count];     // 초성/중성/종성 시간 측정
                double[] idleTime = new double[charCnt.Count]; // idle time 측정

                // duration 계산
                for (int j = 0; j < charCnt.Count; ++j)
                {
                    if (charCnt[j] == 0) // 종성이 없는 경우 들어옴. 
                    {
                        duration[j] = 0.0;
                        idleTime[j] = 0.0;
                        continue;
                    }

                    int offset = baseIdx + charCnt[j] * 2 - 1;
                    duration[j] = times[offset] - times[baseIdx];

                    if (times.Count > offset + 1)
                        idleTime[j] = times[offset + 1] - times[offset];
                    else
                        idleTime[j] = 0.0;

                    baseIdx = offset + 1;
                }

                // pressure 계산
                double[] avgPressure = new double[charCnt.Count];  // 초성/중성/종성 평균 압력 측정
                for (int j = 0; j < charCnt.Count; ++j)
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
                    TestExecId = TestExec.Id,
                    TestSetItemId = TestSetItem.Id,
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

        public async Task<bool> saveStroke(int testOrder, string testName, InkCanvas inkCanvas)
        {
            string file_name = String.Format("{0}_{1}_{2}_{3}_canvas.gif", TestExec.TesterId.ToString(), testOrder, testName, TestSetItem.Number);
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(TestExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            if (file == null)
                return false; 

            CachedFileManager.DeferUpdates(file);
            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);

            using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
            {
                await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                await outputStream.FlushAsync();
            }
            stream.Dispose();

            Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

            if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                return true;
            else
                return false;
        }


        private async Task SaveTimeCsv(StorageFolder storageFolder, int testOrder, string testName, List<List<BATPoint>> drawLines)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(TestExec.TesterId);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("euc-kr");
            StringBuilder builder = new StringBuilder();

            string file_name = String.Format("{0}_{1}_time.csv", tester.GetTesterName(TestExec.Datetime), testName); 
            if(testOrder.Equals(TestPage.TEST_ORDER))
                file_name = String.Format("{0}_{1}_{2}_{3}_time.csv", tester.GetTesterName(TestExec.Datetime), testName, TestSetItem.Number, TestSetItem.Word);

            StorageFile time_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            //double total_duration = 0.0;
            //double total_length_mm = 0.0;

            List<double> durations = new List<double>();
            List<double> lineLength_mms = new List<double>();

            for (int i = 0; i < drawLines.Count; i++)
            {
                if (drawLines[i].Count() == 0)
                    continue;
                double duration = BATPoint.getTimeDiffMs(drawLines[i].Last(), drawLines[i][0]); // 아래에서 써야되서 별도로 저장
                double lineLength = Util.getLength(drawLines[i]);
                double lineLength_mm = Util.pixelsTomm(lineLength);

                durations.Add(duration);
                lineLength_mms.Add(lineLength_mm);
            }

            builder.Clear();
            builder.Append(TestSetItem.Word);
            builder.AppendLine("( 총 " + drawLines.Count.ToString() + " 획)");

            builder.Append("Index,Pressed(ms),Released(ms),Duration(ms),Transition(ms),획 길이(pixel),획 길이(mm),속도(pixel/ms),속도(mm/ms),전체 속도(mm/ms),");
            if (Util.isTestSpiral(testOrder) || Util.isTestFreeSpiral(testOrder))
            {
                builder.Append("1-4바퀴 속도(mm/ms),");
            }
            if(Util.isTestFreeSpiral(testOrder))
            {
                builder.Append("2-4바퀴 속도(mm/ms),");
            }

            builder.AppendLine("");
            for (int i = 0; i < drawLines.Count; i++)
            {
                if (drawLines[i].Count() == 0)
                    continue;

                builder.Append(String.Format("{0},", i + 1)); // index
                builder.Append(BATPoint.getTimeDiffMs(drawLines[i][0], drawLines[0][0]).ToString("F3") + ","); // pressed(ms)
                builder.Append(BATPoint.getTimeDiffMs(drawLines[i].Last(), drawLines[0][0]).ToString("F3") + ","); // released(ms)
                double duration = BATPoint.getTimeDiffMs(drawLines[i].Last(), drawLines[i][0]); // 아래에서 써야되서 별도로 저장
                builder.Append(duration.ToString("F3") + ","); // duration(ms)
                if (i < drawLines.Count - 1 && drawLines[i + 1].Count != 0 ) // transition(ms)
                {
                    builder.Append(BATPoint.getTimeDiffMs(drawLines[i + 1][0], drawLines[i].Last()).ToString("F3") + ",");
                }
                else
                {
                    builder.Append(",");
                }

                double lineLength = Util.getLength(drawLines[i]);
                double lineLength_mm = Util.pixelsTomm(lineLength);
                builder.Append(lineLength.ToString("F6") + ",");
                builder.Append(lineLength_mm.ToString("F6") + ",");
                builder.Append((lineLength / duration).ToString("F6") + ",");
                builder.Append((lineLength_mm / duration).ToString("F6") + ",");

                // 제일 첫 줄에 전체 속도 추가
                if(i == 0)
                {
                    double velocity = lineLength_mms.Sum() / durations.Sum();
                    builder.Append(velocity.ToString("F6") + ",");

                    if( Util.isTestFreeSpiral(testOrder) || Util.isTestSpiral(testOrder))
                    {
                        double l = 0.0;
                        double d = 0.0;
                        for ( int j = 0; j < Math.Min(4, lineLength_mms.Count); j++)
                        {
                            l += lineLength_mms[j];
                            d += durations[j];
                        }
                        velocity = l / d;
                        builder.Append(velocity.ToString("F6") + ",");
                    }
                    if (Util.isTestFreeSpiral(testOrder))
                    {
                        double l = 0.0;
                        double d = 0.0;
                        for (int j = 1; j < Math.Min(4, lineLength_mms.Count); j++)
                        {
                            l += lineLength_mms[j];
                            d += durations[j];
                        }
                        velocity = l / d;
                        builder.Append(velocity.ToString("F6") + ",");
                    }
                }
                builder.AppendLine("");
            }

            byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(time_file, fileBytes);
        }

        private async Task SavePressureCsv(StorageFolder storageFolder, int testOrder, string testName, List<List<BATPoint>> drawLines)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(TestExec.TesterId);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("euc-kr");

            StringBuilder builder = new StringBuilder();
            string file_name = String.Format("{0}_{1}_pressure.csv", tester.GetTesterName(TestExec.Datetime), testName);
            if (testOrder.Equals(TestPage.TEST_ORDER))
                file_name = String.Format("{0}_{1}_{2}_{3}_pressure.csv", tester.GetTesterName(TestExec.Datetime), testName, TestSetItem.Number, TestSetItem.Word);
            
            StorageFile pressure_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
            builder.Clear();
            builder.Append(TestSetItem.Word);
            builder.AppendLine("( 총 " + drawLines.Count.ToString() + " 획)");
            builder.AppendLine("Index,총 샘플링 수,필압 평균,필압 표준편차,raw pressure");

            List<double> pressureAll = new List<double>(); // "각 획의 필압 평균"의 평균을 계산하기 위한 리스트
            List<List<double>> pressureSpiral = new List<List<double>>();
            for (int i = 0; i < drawLines.Count; ++i)
            {
                builder.Append(String.Format("{0},", i + 1)); // index
                builder.Append(drawLines[i].Count.ToString() + ", "); // 총 샘플링 수

                List<double> pressures = new List<double>();
                foreach (var pt in drawLines[i])
                {
                    if (Math.Abs(pt.pressure) < 0.000001)
                        continue;

                    pressures.Add(pt.pressure);
                    pressureAll.Add(pt.pressure);
                }
                if( pressures.Count > 0 )
                {
                    builder.Append(Util.calculateAverage(pressures).ToString("F6") + ","); // 필압 평균
                    builder.Append(Util.calculateStdev(pressures).ToString("F6") + ","); // 필압 표준편차
                }
                else
                {
                    builder.Append(0.0.ToString("F6") + ",");
                    builder.Append(0.0.ToString("F6") + ",");
                }

                foreach(var p in pressures)
                {
                    builder.Append(p.ToString("F6") + ",");
                }
                builder.AppendLine("");

                pressureSpiral.Add(pressures);
            }
            // 가장 마지막 줄에 전체 획의 필압 평균의 평균 및 표준 편차의 평균을 제일 하단에 넣어줌
            double totalAvg = Util.calculateAverage(pressureAll);
            double totalStd = Util.calculateStdev(pressureAll);
            builder.AppendLine(",전체 평균 및 표준편차," + totalAvg.ToString("F6") + "," + totalStd.ToString("F6"));
            
            // 나선 과저에만 추가
            if(Util.isTestSpiral(testOrder) || Util.isTestFreeSpiral(testOrder))
            {
                List<double> pressures = new List<double>();
                for (int i = 0; i < Math.Min(pressureSpiral.Count, 4); i++)
                {
                    pressures.AddRange(pressureSpiral[i]);
                }

                totalAvg = Util.calculateAverage(pressures);
                totalStd = Util.calculateStdev(pressures);
                builder.AppendLine(",1-4바퀴 평균 및 표준편차," + totalAvg.ToString("F6") + "," + totalStd.ToString("F6"));
            }
            if (Util.isTestFreeSpiral(testOrder))
            {
                List<double> pressures = new List<double>();
                for (int i = 1; i < Math.Min(pressureSpiral.Count, 4); i++)
                {
                    pressures.AddRange(pressureSpiral[i]);
                }

                totalAvg = Util.calculateAverage(pressures);
                totalStd = Util.calculateStdev(pressures);
                builder.AppendLine(",2-4바퀴 평균 및 표준편차," + totalAvg.ToString("F6") + "," + totalStd.ToString("F6"));
            }

            byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(pressure_file, fileBytes);
        }

        private async Task SaveMinMaxCsv(StorageFolder storageFolder, int testOrder, string testName, List<List<Point>> orgLines, List<List<BATPoint>> drawLines)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(TestExec.TesterId);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("euc-kr");
            
            string file_name = String.Format("{0}_{1}_MinMax.csv", tester.GetTesterName(TestExec.Datetime), testName);
            if (testOrder.Equals(TestPage.TEST_ORDER))
                file_name = String.Format("{0}_{1}_{2}_{3}_MinMax.csv", tester.GetTesterName(TestExec.Datetime), testName, TestSetItem.Number, TestSetItem.Word);

            StorageFile minmax_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
            StringBuilder builder = new StringBuilder();
            builder.Clear();
            builder.Append(TestSetItem.Word);
            builder.AppendLine("( 총 " + drawLines.Count.ToString() + " 획)");
            builder.AppendLine("Index, 템플릿 Min X, 템플릿 Min Y, 템플릿 Max X, 템플릿 Max Y, Min X,Min Y,Max X,Max Y,획 길이(pixel),획 길이(mm),전체 바퀴 면적(mm^2),1-4바퀴 면적(mm^2),");


            // 
            Rect? totalBox = Util.getBoundingBox2(drawLines);
            double totalArea = 0.0;
            if (totalBox.HasValue)
                totalArea = Util.pixelsTomm(totalBox.Value.Width) * Util.pixelsTomm(totalBox.Value.Height);
            
            Rect? totalBox_14 = Util.getBoundingBox2(drawLines.GetRange(0, Math.Min(4, drawLines.Count)));
            double totalArea_14 = 0.0;
            if (totalBox_14.HasValue)
                totalArea_14 = Util.pixelsTomm(totalBox_14.Value.Width) * Util.pixelsTomm(totalBox_14.Value.Height);

            for (int i = 0; i < drawLines.Count; ++i)
            {
                builder.Append(String.Format("{0},", i + 1));
                // 탬플릿 쪽

                if( orgLines  != null && i < orgLines.Count )
                {
                    Rect? rect = Util.getBoundingBox(orgLines[i]);
                    if(rect.HasValue)
                    {
                        builder.Append(String.Format("{0},{1},{2},{3},", rect.Value.X, rect.Value.Y, rect.Value.X + rect.Value.Width, rect.Value.Y + rect.Value.Height));
                    }
                    else
                    {
                        builder.Append(",,,,");
                    }
                }
                else
                {
                    builder.Append(",,,,");
                }

                Rect? boundingBox = Util.getBoundingBox(drawLines[i]);

                if (boundingBox.HasValue)
                {
                    // min x, min y, max x, max y
                    builder.Append(String.Format("{0},{1},{2},{3},",
                        boundingBox.Value.X,
                        boundingBox.Value.Y,
                        boundingBox.Value.X + boundingBox.Value.Width,
                        boundingBox.Value.Y + boundingBox.Value.Height));
                }
                else
                {
                    builder.Append(",,,,");
                }
                double length = Util.getLength(drawLines[i]);
                double length_mm = Util.pixelsTomm(length);
                builder.Append(length.ToString("F6") + ",");
                builder.Append(length_mm.ToString("F6") + ",");

                if (i == 0 && Util.isTestFreeSpiral(testOrder))
                    builder.Append(String.Format("{0},{1},", totalArea, totalArea_14));

                builder.AppendLine("");
            }

            byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(minmax_file, fileBytes);
        }

        private async Task SaveDiffCsv(StorageFolder storageFolder, int testOrder, string testName, List<List<Point>> orgLines, List<List<BATPoint>> drawPoints, List<List<DiffData>> diffResults)
        {
            // 글자쓰기는 diff가 없다. 
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(TestExec.TesterId);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("euc-kr");

            List<DiffData> flattenDiff = new List<DiffData>();
            // csv 상에서 제일 위에 전체 결과에 대한 차이 평균을 넣어둬야 해서 미리 계산함
            
            List<Tuple<string, string, string>> summaryInfos = new List<Tuple<string, string, string>>();
            if (testOrder < 4) // 직선 그리기, 사선 그리기
            {
                List<double> diffAll = new List<double>();
                List<double> diffAvgs = new List<double>();
                List<double> diffStds = new List<double>();
                int idx = 0;
                // 총 121개가 들어있음. 직접 카운트 하면서 나눠서 처리함
                List<double> diffs = new List<double>();
                foreach (var diffResult in diffResults) 
                {
                    foreach (var diffItem in diffResult)
                    {
                        if (diffItem.hasValueOrg && diffItem.hasValueDrawn)
                        {
                            double diff_pixel = diffItem.getDistance();
                            double diff_mm = Util.pixelsTomm(diff_pixel);
                            diffs.Add(diff_mm);
                            diffAll.Add(diff_mm);
                        }
                        
                        if(idx != 0 && idx % 10 == 0) 
                        {
                            if(diffs.Count > 0)
                            {
                                double lastIem = diffs.Last();

                                diffAvgs.Add(Util.calculateAverage(diffs));
                                diffStds.Add(Util.calculateStdev(diffs));
                                diffs.Clear();
                                diffs.Add(lastIem); // 가장 마지막 점은 다음 측정들과 공유
                            }
                            else
                            {
                                diffAvgs.Add(0.0);
                                diffStds.Add(0.0);
                                diffs.Clear();
                            }
                        }
                        idx++;
                    }
                }

                // 전체
                double avg = Util.calculateAverage(diffAll);
                double std = Util.calculateStdev(diffAll);

                summaryInfos.Add(new Tuple<string, string, string>(
                   "1mm마다 측정",
                   avg.ToString("F3"),
                   std.ToString("F3"))
                );

                // 세부
                for(int i = 0; i < diffAvgs.Count; i++)
                {
                    summaryInfos.Add(new Tuple<string, string, string>(
                        string.Format("1cm마다 측정 - {0}", i+1),
                        diffAvgs[i].ToString("F3"),
                        diffStds[i].ToString("F3"))
                    );
                }
            }
            else if( Util.isTestSpiral(testOrder) || Util.isTestFreeSpiral(testOrder) )
            {
                List<double> diffAll = new List<double>();
                List<double> diffAll_14 = new List<double>();
                List<double> diffAll_24 = new List<double>();
                List<double> diffAvgs = new List<double>();
                List<double> diffStds = new List<double>();

                int idx = 0;
                foreach (var diffResult in diffResults)
                {
                    List<double> diffs = new List<double>();
                    foreach (var diffItem in diffResult)
                    {
                        if (diffItem.hasValueOrg && diffItem.hasValueDrawn)
                        {
                            double diff_pixel = diffItem.getDistance();
                            double diff_mm = Util.pixelsTomm(diff_pixel);
                            diffs.Add(diff_mm);
                            diffAll.Add(diff_mm);

                            if (idx < 4)
                                diffAll_14.Add(diff_mm);
                            if (idx > 0 && idx < 4)
                                diffAll_24.Add(diff_mm);
                        }
                    }
                    
                    if(diffs.Count > 0)
                    {
                        diffAvgs.Add(diffs.Average());
                        diffStds.Add(Util.calculateStdev(diffs));
                    }
                    idx++;
                }

                // 각 Cycle
                for (int i = 0; i < diffAvgs.Count; i++)
                {
                    summaryInfos.Add(new Tuple<string, string, string>(
                        String.Format("Cycle {0}", i + 1),
                        diffAvgs[i].ToString("F3"),
                        diffStds[i].ToString("F3")
                    ));
                }

                // 전체
                summaryInfos.Add(new Tuple<string, string, string>(
                   "전체 바퀴",
                   Util.calculateAverage(diffAll).ToString("F3"),
                   Util.calculateStdev(diffAll).ToString("F3")
                ));

                // 1-4바퀴
                summaryInfos.Add(new Tuple<string, string, string>(
                   "1-4바퀴",
                   Util.calculateAverage(diffAll_14).ToString("F3"),
                   Util.calculateStdev(diffAll_14).ToString("F3")
                ));

                if(Util.isTestFreeSpiral(testOrder))
                {
                    // 2-4바퀴
                    summaryInfos.Add(new Tuple<string, string, string>(
                       "2-4바퀴",
                       Util.calculateAverage(diffAll_24).ToString("F3"),
                       Util.calculateStdev(diffAll_24).ToString("F3")
                    ));
                }
            }

            double templateMinX = 1000000000.0;
            double templateMaxX = 0.0;
            foreach (var diffResult in diffResults)
            {
                List<double> diffs = new List<double>();
                foreach (var diffItem in diffResult)
                {
                    flattenDiff.Add(diffItem);
                    if( diffItem.org.HasValue)
                    {
                        if (templateMinX > diffItem.org.Value.X)
                            templateMinX = diffItem.org.Value.X;
                        if (templateMaxX < diffItem.org.Value.X)
                            templateMaxX = diffItem.org.Value.X;
                    }
                }
            }
            double freeSpiralRatio = 120 / Util.pixelsTomm(templateMaxX - templateMinX);


            string file_name = String.Format("{0}_{1}_차이.csv", tester.GetTesterName(TestExec.Datetime), testName);

            StorageFile diff_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
            StringBuilder builder = new StringBuilder();
            builder.Clear();
            builder.Append(testName);
            if(Util.isTestFreeSpiral(testOrder))
                builder.AppendLine("Index,위치,템플릿X,템플릿Y,그린점X,그린점Y,거리차이(pixel),거리차이(mm),보정된 거리차이(mm),,평균,표준편차,");
            else
                builder.AppendLine("Index,위치,템플릿X,템플릿Y,그린점X,그린점Y,거리차이(pixel),거리차이(mm),,평균,표준편차,");

            for (int i = 0; i < flattenDiff.Count; ++i)
            {
                builder.Append(String.Format("{0},{1},", i + 1, flattenDiff[i].name));
                if (flattenDiff[i].hasValueOrg)
                    builder.Append(String.Format("{0},{1},", flattenDiff[i].org.Value.X, flattenDiff[i].org.Value.Y));
                else
                    builder.Append(String.Format(",,"));

                if (flattenDiff[i].hasValueDrawn)
                    builder.Append(String.Format("{0},{1},", flattenDiff[i].drawn.Value.X, flattenDiff[i].drawn.Value.Y));
                else
                    builder.Append(String.Format(",,"));

                if (flattenDiff[i].hasValueOrg && flattenDiff[i].hasValueDrawn)
                {
                    double length = flattenDiff[i].getDistance();
                    double length_mm = Util.pixelsTomm(length);
                    builder.Append(String.Format("{0},{1},", length, length_mm));
                    if (Util.isTestFreeSpiral(testOrder))
                        builder.Append(String.Format("{0},", length_mm * freeSpiralRatio));
                }
                else
                {
                    builder.Append(",,");
                    if (Util.isTestFreeSpiral(testOrder))
                        builder.Append(",");
                }

                // 가장 첫 줄에 거리 차이의 평균 및 표준 편차를 기록해둠
                // 차이 값이 있는 경우에 한해서만 다룸 
                if (i < summaryInfos.Count) 
                {
                    builder.Append(String.Format("{0},{1},{2},", summaryInfos[i].Item1, summaryInfos[i].Item2, summaryInfos[i].Item3));
                }

                builder.AppendLine("");
            }

            byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(diff_file, fileBytes);
        }

        public async Task<bool> saveRawData2(int testOrder, string testName, List<List<Point>> orgLines, List<List<BATPoint>> drawLines, List<List<DiffData>> diffResults, InkCanvas inkCanvas)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(TestExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);

            await SaveTimeCsv(storageFolder, testOrder, testName, drawLines);
            await SavePressureCsv(storageFolder, testOrder, testName, drawLines);
            await SaveMinMaxCsv(storageFolder, testOrder, testName, orgLines, drawLines);
            await SaveDiffCsv(storageFolder, testOrder, testName, orgLines, drawLines, diffResults);

            return true;
        }

        internal void deleteResultFromDB( TestExec testExec, TestSetItem testSetItem )
        {
            // 자모 구분을 하지 않으면 DB에 저장할 데이터 만들기가 어려움... 
            if ( TestExec.UseJamoSepartaion != true )
                return;

            DatabaseManager dbManager = DatabaseManager.Instance;
            dbManager.DeleteTestExecResult(testExec, testSetItem);
        }

        public async Task<bool> saveInkCanvas(InkCanvas inkCanvas)
        {
            string file_name = TestExec.TesterId.ToString() + "_char_" + TestSetItem.Number.ToString() + ".gif";
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(TestExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

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
                        ds.Clear(Windows.UI.Colors.White);
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
                ds.Clear(Windows.UI.Colors.White);
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

    }
}
