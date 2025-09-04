using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Sledge.Formats.Texture.Vtf;
using Sledge.Formats.Texture.ImageSharp;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageSixLabor = SixLabors.ImageSharp.Image;
using System.Diagnostics;

namespace VTF_to_Image_converter
{
    public partial class MainWindow : Window
    {
        Task<System.Collections.Generic.IReadOnlyList<Avalonia.Platform.Storage.IStorageFile>> files;
        public MainWindow()
        {
            InitializeComponent();
        }
        private async void OpenFile(object sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);

            files = topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Image",
                AllowMultiple = true,
                FileTypeFilter = [FilePickerFileTypes.VTFAndImages]
            });
            var filePaths = await GetSelectedFilePathsAsync();
            foreach (var filePath in filePaths)
            {
                var stream = File.OpenRead(filePath);
                switch (filePath)
                {
                    case string s when IsFileImage(s):
                        ImageToVTF(stream, filePath);
                        break;

                    case string s when s.EndsWith(".vtf"):
                        VTFtoPNG(stream, filePath);
                        break;

                    default:
                        Debug.WriteLine("File is not image");
                        break;
                }
            }
        }
        private VtfImageFormat GetSelectedImageFormatForVTF() //Read what is selected from a dropdown menu, default is RGBA8888
        {
            var format = VtfImageFormat.Dxt5;
            return format;
        }
        private bool GetAutoResizeForVTF() //Read if checkbox is checked, resize to power of 2
        {
            return false;
        }
        private void VTFtoPNG(FileStream stream, string filePath)
        {
            VtfFile vtfFile = new VtfFile(stream);
            ImageSixLabor image = vtfFile.ToImage<Rgba32>();
            stream.Close();
            int length = filePath.Length;
            string[] imageFilePath = filePath.Split(".");
            image.SaveAsPng(imageFilePath[0] + ".png");
        }
        private void ImageToVTF(FileStream stream, string filePath)
        {
            VtfFile vtfFile = new VtfFile();
            ImageSixLabor image = ImageSixLabor.Load(stream);
            image.ToVtfFile();


            stream.Close();
            int length = filePath.Length;
            string[] imageFilePath = filePath.Split(".");
            using (var output = File.Create(imageFilePath[0] + ".vtf"))
            {
                vtfFile.Write(output);
            }
        }
        private bool IsFileImage(string filepath)
        {
            if (filepath.EndsWith(".png") || 
                filepath.EndsWith(".jpg") || 
                filepath.EndsWith(".jpeg") || 
                filepath.EndsWith(".gif") || 
                filepath.EndsWith(".bmp") || 
                filepath.EndsWith(".webp"))
            {
                return true;
            }
            return false;
        }

        private async Task<string[]> GetSelectedFilePathsAsync()
        {
            var selectedFiles = await files;
            return selectedFiles.Select(f => f.Path.LocalPath).ToArray();
        }

        public static class FilePickerFileTypes
        {
            public static FilePickerFileType VTFAndImages { get; } = new("VTF")
            {
                Patterns = new[] { "*.vtf", "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" }
            };
        }
    }
}