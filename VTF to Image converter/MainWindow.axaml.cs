using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Sledge.Formats.Texture.Vtf;
using Sledge.Formats.Texture.ImageSharp;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageSixLabor = SixLabors.ImageSharp.Image;

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
                FileTypeFilter = [FilePickerFileTypes.VTF]
            });
            var filePaths = await GetSelectedFilePathsAsync();
            foreach (var filePath in filePaths)
            {
                var stream = File.OpenRead(filePath);

                VtfFile vtfFile = new VtfFile(stream);
                ImageSixLabor image = vtfFile.ToImage<Rgba32>();
                stream.Close();
                int length = filePath.Length;
                string[] imageFilePath = filePath.Split(".");
                image.SaveAsPng(imageFilePath[0] + ".png");
            }
        }

        private async Task<string[]> GetSelectedFilePathsAsync()
        {
            var selectedFiles = await files;
            return selectedFiles.Select(f => f.Path.LocalPath).ToArray();
        }

        public static class FilePickerFileTypes
        {
            public static FilePickerFileType VTF { get; } = new("VTF")
            {
                Patterns = new[] { "*.vtf" }
            };
        }
    }
}