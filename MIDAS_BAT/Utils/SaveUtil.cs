using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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


        public async Task<bool> saveRawData( int testOrder, string testName, List<double> times, List<DiffData> diffResults, InkCanvas inkCanvas )
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("euc-kr");

            // pressure & time diff 저장...?
            string file_name = String.Format("{0}_{1}_{2}_raw_time_{3}.txt", TestExec.TesterId.ToString(), testOrder, testName, TestSetItem.Number);
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(TestExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(times.Count.ToString());
            for (int i = 0; i < times.Count; ++i)
                builder.AppendLine(times[i].ToString("F3"));
            await FileIO.WriteTextAsync(file, builder.ToString());

            // 필기시간
            file_name = String.Format("{0}_{1}_{2}_raw_time_{3}.csv", TestExec.TesterId.ToString(), testOrder, testName, TestSetItem.Number);
            StorageFile time_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
            IReadOnlyList<InkStroke> strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            builder.Clear();
            builder.Append(TestSetItem.Word);
            builder.AppendLine("( 총 " + strokes.Count.ToString() + " 획)");

            builder.AppendLine("Index,Pressed(ms), Released(ms), Duration(ms), Transition(ms)");
            for( int i = 0; i < times.Count; i+=2 ) // 한 획당 2개씩 기록되어있어서... 
            {
                builder.Append(String.Format("{0},", i + 1));
                builder.Append((times[i] - times[0]).ToString("F3") + "," );
                builder.Append((times[i+1] - times[0]).ToString("F3")  + "," );
                builder.Append((times[i+1] - times[i]).ToString("F3")  + "," );
               
                if (i + 2 < times.Count )
                    builder.Append((times[i+2] - times[i+1]).ToString("F3")  + "," );
                builder.AppendLine("");
            }

            byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(time_file, fileBytes);

            // 필압
            file_name = String.Format("{0}_{1}_{2}_raw_pressure_{3}.csv", TestExec.TesterId.ToString(), testOrder, testName, TestSetItem.Number);
            StorageFile pressure_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
            builder.Clear();
            builder.Append(TestSetItem.Word);
            builder.AppendLine("( 총 " + strokes.Count.ToString() + " 획)");
            builder.AppendLine("Index,총 샘플링 수,평균 필압,Raw값");
            for (int i = 0; i < strokes.Count; ++i)
            {
                builder.Append(String.Format("{0},", i + 1));
                IReadOnlyList<InkStrokeRenderingSegment> segments = strokes[i].GetRenderingSegments();
                builder.Append(segments.Count.ToString() + ", ");

                float avgPressure = 0.0f;
                int pressure_cnt = 0;
                foreach (var seg in segments)
                {
                    avgPressure += seg.Pressure;
                    pressure_cnt++;
                }
                avgPressure = avgPressure / pressure_cnt;
                builder.Append(avgPressure.ToString("F6") + ", ");

                foreach (var seg in segments)
                {
                    builder.Append(seg.Pressure.ToString("F6") + ", ");
                }

                builder.AppendLine("");
            }

            fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(pressure_file, fileBytes);

            // min / max
            // template은 1획밖에 없어서 하나만 쓴다. 
            double templateMinX = 0.0;
            double templateMinY = 0.0;
            double templateMaxX = 0.0;
            double templateMaxY = 0.0;
            if ( diffResults.Count > 0)
            {
                templateMinX = diffResults.OrderByDescending(t => t.org.X).Last().org.X;
                templateMinY = diffResults.OrderByDescending(t => t.org.Y).Last().org.Y;
                templateMaxX = diffResults.OrderBy(t => t.org.X).Last().org.X;
                templateMaxY = diffResults.OrderBy(t => t.org.Y).Last().org.Y;
            }

            file_name = String.Format("{0}_{1}_{2}_minmax_{3}.csv", TestExec.TesterId.ToString(), testOrder, testName, TestSetItem.Number);
            StorageFile minmax_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
            builder.Clear();
            builder.Append(TestSetItem.Word);
            builder.AppendLine("( 총 " + strokes.Count.ToString() + " 획)");
            builder.AppendLine("Index, 템플릿 Min X, 템플릿 Min Y, 템플릿 Max X, 템플릿 Max Y, Min X,Min Y,Max X,Max Y");
            for (int i = 0; i < strokes.Count; ++i)
            {
                builder.Append(String.Format("{0},", i + 1));
                // 탬플릿 쪽
                builder.Append(String.Format("{0},{1},{2},{3},", templateMinX, templateMinY, templateMaxX, templateMaxY));

                // min x, min y, max x, max y
                builder.Append(String.Format("{0},{1},{2},{3},",
                    strokes[i].BoundingRect.X,
                    strokes[i].BoundingRect.Y,
                    strokes[i].BoundingRect.X + strokes[i].BoundingRect.Width,
                    strokes[i].BoundingRect.Y + strokes[i].BoundingRect.Height));

                builder.AppendLine("");
            }

            fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(minmax_file, fileBytes);

            // diff
            file_name = String.Format("{0}_{1}_{2}_diff_{3}.csv", TestExec.TesterId.ToString(), testOrder, testName, TestSetItem.Number);
            StorageFile diff_file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);
            builder.Clear();
            builder.Append(testName);
            builder.AppendLine("Index,위치,템플릿X,템플릿Y,그린점X,그린점Y,거리차이");
            for (int i = 0; i < diffResults.Count; ++i)
            {
                builder.Append(String.Format("{0},", i + 1));
                builder.Append(String.Format("{0},{1},{2},", diffResults[i].name, diffResults[i].org.X, diffResults[i].org.Y));
                if (diffResults[i].hasValue)
                    builder.Append(String.Format("{0},{1},{2}", diffResults[i].drawn.X, diffResults[i].drawn.Y, diffResults[i].getDistance()));

                builder.AppendLine("");
            }

            fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
            await FileIO.WriteBytesAsync(diff_file, fileBytes);

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
