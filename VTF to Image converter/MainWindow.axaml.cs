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
using Avalonia;

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
        }
        private async void ConvertFiles(object sender, RoutedEventArgs e)
        {
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
            ComboBox formatDropdown = this.FindControl<ComboBox>("FormatDropdown");
            var selectedItem = formatDropdown.SelectedItem as ComboBoxItem;
            // Get the text/content of the selected item
            var selectedText = selectedItem?.Content?.ToString();
            Debug.WriteLine(selectedText);
            switch (selectedText)
            {
                case "RGBA8888":
                    return VtfImageFormat.Rgba8888;
                case "ABGR8888":
                    return VtfImageFormat.Abgr8888;
                case "ARGB8888":
                    return VtfImageFormat.Argb8888;
                case "BGRA8888":
                    return VtfImageFormat.Bgra8888;
                case "DXT1":
                    return VtfImageFormat.Dxt1;
                case "DXT3":
                    return VtfImageFormat.Dxt3;
                case "DXT5":
                    return VtfImageFormat.Dxt5;
                case "RGBA16161616F":
                    return VtfImageFormat.Rgba16161616F;
                case "RGBA16161616":
                    return VtfImageFormat.Rgba16161616;
                default:
                    return VtfImageFormat.Rgba8888;
            }
        }
        private bool MaintainAspectRatioForVTF() //Read if checkbox is checked, resize to power of 2
        {
            bool a = false;
            return a;
        }
        private void VTFtoPNG(FileStream stream, string filePath)
        {
            VtfFile vtfFile = new VtfFile(stream);
            stream.Close();

            ImageSixLabor image = vtfFile.ToImage<Rgba32>();

            int length = filePath.Length;
            string[] imageFilePath = filePath.Split(".");
            image.SaveAsPng(imageFilePath[0] + ".png");
        }
        private void ImageToVTF(FileStream stream, string filePath)
        {
            VtfFile vtfFile = new VtfFile();

            ImageSixLabor image = ImageSixLabor.Load(stream);
            stream.Close();
            if (MaintainAspectRatioForVTF()) //If false, AutoResizeToPowerOfTwo will handle it.
            {
                //Resize to nearest po2 while maintaining aspect ratio, fill in missing pixels as transparent
            }

            VtfImageBuilderOptions options = new VtfImageBuilderOptions
            {
                ImageFormat = GetSelectedImageFormatForVTF(),
                AutoResizeToPowerOfTwo = true //Needs to be po2 to work
            };
            vtfFile.AddImage(image.ToVtfImage(options));

            
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