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
            builder.Clear();
            builder.Append(TestSetItem.Word);
            builder.AppendLine("( 총 " + drawLines.Count.ToString() + " 획)");

            builder.AppendLine("Index,Pressed(ms),Released(ms),Duration(ms),Transition(ms),획 길이,");
            for (int i = 0; i < drawLines.Count; i++)
            {
                if (drawLines[i].Count() == 0)
                    continue;

                builder.Append(String.Format("{0},", i + 1));
                builder.Append(BATPoint.getTimeDiffMs(drawLines[i][0], drawLines[0][0]).ToString("F3") + ",");
                builder.Append(BATPoint.getTimeDiffMs(drawLines[i].Last(), drawLines[0][0]).ToString("F3") + ",");
                builder.Append(BATPoint.getTimeDiffMs(drawLines[i].Last(), drawLines[i][0]).ToString("F3") + ",");
                if (i < drawLines.Count - 1 && drawLines[i + 1].Count != 0 )
                {
                    builder.Append(BATPoint.getTimeDiffMs(drawLines[i + 1][0], drawLines[i].Last()).ToString("F3") + ",");
                }
                else
                {
                    builder.Append(",");
                }

                builder.Append(Util.getLength(drawLines[i]).ToString("F6") + ",");

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
            for (int i = 0; i < drawLines.Count; ++i)
            {
                builder.Append(String.Format("{0},", i + 1));
                builder.Append(drawLines[i].Count.ToString() + ", ");

                List<double> pressures = new List<double>();
                foreach (var pt in drawLines[i])
                {
                    if (Math.Abs(pt.pressure) < 0.000001)
                        continue;

                    pressures.Add(pt.pressure);
                }
                if( pressures.Count > 0 )
                {
                    builder.Append(pressures.Average().ToString("F6") + ",");
                    builder.Append(Util.calculateStdev(pressures).ToString("F6") + ",");
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
            builder.AppendLine("Index, 템플릿 Min X, 템플릿 Min Y, 템플릿 Max X, 템플릿 Max Y, Min X,Min Y,Max X,Max Y,획 길이");
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
                builder.Append(Util.getLength(drawLines[i]).ToString("F6") + ",");
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
            foreach(var diffResult in diffResults)
            {
                foreach(var diffItem in diffResult)
                    flattenDiff.Add(diffItem);
            }

            string file_name = String.Format("{0}_{1}_차이.csv", tester.GetTesterName(TestExec.Datetime), testName);

            StorageFile diff_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
            StringBuilder builder = new StringBuilder();
            builder.Clear();
            builder.Append(testName);
            builder.AppendLine("Index,위치,템플릿X,템플릿Y,그린점X,그린점Y,거리차이");
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
                    builder.Append(String.Format("{0},", flattenDiff[i].getDistance()));


                builder.AppendLine("");
            }

            byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(diff_file, fileBytes);
        }

        public async Task<bool> saveRawData2(int testOrder, string testName, List<List<Point>> orgLines, List<List<BATPoint>> drawLines, List<List<DiffData>> diffResults, InkCanvas inkCanvas)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("euc-kr");

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
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
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
