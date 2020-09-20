using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace SkImageResizer
{
    public class SKImageProcess
    {
        /// <summary>
        /// 進行圖片的縮放作業
        /// </summary>
        /// <param name="sourcePath">圖片來源目錄路徑</param>
        /// <param name="destPath">產生圖片目的目錄路徑</param>
        /// <param name="scale">縮放比例</param>
        public void ResizeImages(string sourcePath, string destPath, double scale)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            var allFiles = FindImages(sourcePath);
            foreach (var filePath in allFiles)
            {
                var bitmap = SKBitmap.Decode(filePath);
                var imgPhoto = SKImage.FromBitmap(bitmap);
                var imgName = Path.GetFileNameWithoutExtension(filePath);

                var sourceWidth = imgPhoto.Width;
                var sourceHeight = imgPhoto.Height;

                var destinationWidth = (int)(sourceWidth * scale);
                var destinationHeight = (int)(sourceHeight * scale);

                using var scaledBitmap = bitmap.Resize(
                    new SKImageInfo(destinationWidth, destinationHeight),
                    SKFilterQuality.High);
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                using var data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 100);
                using var s = File.OpenWrite(Path.Combine(destPath, imgName + ".jpg"));
                data.SaveTo(s);
            }
        }

        public Task ResizeImagesAsync(string sourcePath, string destPath, double scale)
        {
            return ResizeImagesAsync(sourcePath, destPath, scale, CancellationToken.None);
        }

        public async Task ResizeImagesAsync(string sourcePath, string destPath, double scale, CancellationToken token)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            await Task.Yield();

            var taskList = new List<Task>();
            var allFiles = FindImages(sourcePath);
            foreach (var filePath in allFiles)
            {
                taskList.Add(ResizePicture(destPath, scale, filePath, token));
            }

            try
            {
                await Task.WhenAll(taskList);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"捕捉到例外異常的物件內容為 : {exception.Message}");

                //foreach (Task cancel in taskList.Where(it => it.IsCanceled))
                //{
                //    Console.WriteLine($"[{cancel.Id}] is being cancelled.");
                //}

                //foreach (Task faulted in taskList.Where(t => t.IsFaulted))
                //{
                //    Console.WriteLine(faulted.Exception.InnerException.Message);
                //}

                // 保哥的做法：
                foreach (var task in taskList)
                {
                    switch (task.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            Console.WriteLine($"[{task.Id}] is completed.");
                            break;
                        case TaskStatus.Canceled:
                            Console.WriteLine($"[{task.Id}] is cancelled.");
                            break;
                        case TaskStatus.Faulted:
                            Console.WriteLine($"[{task.Id}] exception!!!");
                            break;
                    }
                }
            }
        }

        private static Task ResizePicture(string destPath, double scale, string filePath, CancellationToken token)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"[{filePath.Substring(59, 6)}] Task Id: {Task.CurrentId} | Thread Id: {Thread.CurrentThread.ManagedThreadId:D2}");
                //Stopwatch watch = Stopwatch.StartNew();

                var bitmap = SKBitmap.Decode(filePath);
                var imgPhoto = SKImage.FromBitmap(bitmap);
                var imgName = Path.GetFileNameWithoutExtension(filePath);

                var sourceWidth = imgPhoto.Width;
                var sourceHeight = imgPhoto.Height;

                var destinationWidth = (int)(sourceWidth * scale);
                var destinationHeight = (int)(sourceHeight * scale);

                using var scaledBitmap = bitmap.Resize(
                    new SKImageInfo(destinationWidth, destinationHeight),
                    SKFilterQuality.High);
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                using var data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 100);
                using var s = File.OpenWrite(Path.Combine(destPath, imgName + ".jpg"));
                data.SaveTo(s);

                //Console.WriteLine($"[{filePath.Substring(10, 6)}]Thread Id: {Thread.CurrentThread.ManagedThreadId:D2}");

                //watch.Stop();
                //Console.WriteLine($"[{filePath}]: {watch.ElapsedMilliseconds}");
            }, token);
        }

        /// <summary>
        /// 清空目的目錄下的所有檔案與目錄
        /// </summary>
        /// <param name="destPath">目錄路徑</param>
        public void Clean(string destPath)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }
            else
            {
                var allImageFiles = Directory.GetFiles(destPath, "*", SearchOption.AllDirectories);

                foreach (var item in allImageFiles)
                {
                    File.Delete(item);
                }
            }
        }

        /// <summary>
        /// 找出指定目錄下的圖片
        /// </summary>
        /// <param name="srcPath">圖片來源目錄路徑</param>
        /// <returns></returns>
        public List<string> FindImages(string srcPath)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(srcPath, "*.png", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(srcPath, "*.jpg", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(srcPath, "*.jpeg", SearchOption.AllDirectories));
            return files;
        }
    }
}