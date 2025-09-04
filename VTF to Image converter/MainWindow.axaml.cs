using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Sledge.Formats.Texture.ImageSharp;
using Sledge.Formats.Texture.Vtf;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageSixLabor = SixLabors.ImageSharp.Image;
using Point = SixLabors.ImageSharp.Point;

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
            CheckBox aspectRatioCheckbox = this.FindControl<CheckBox>("AspectRatioCheckbox");
            return aspectRatioCheckbox.IsChecked ?? false;
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
        private int RoundUpToPowerOfTwo(int value)
        {
            var po2 = 1;
            while (po2 < value) po2 *= 2;
            return po2;
        }
        private void ImageToVTF(FileStream stream, string filePath)
        {
            VtfFile vtfFile = new VtfFile();

            ImageSixLabor image = ImageSixLabor.Load(stream);
            stream.Close();
            if (MaintainAspectRatioForVTF()) //If false, AutoResizeToPowerOfTwo will handle it.
            {
                //Resize to nearest po2 while maintaining aspect ratio, fill in missing pixels as transparent
                var newWidth = RoundUpToPowerOfTwo(image.Width);
                var newHeight = RoundUpToPowerOfTwo(image.Height);

                // Calculate scale to fit image inside new dimensions
                float scale = Math.Min((float)newWidth / image.Width, (float)newHeight / image.Height);
                int scaledWidth = (int)(image.Width * scale);
                int scaledHeight = (int)(image.Height * scale);

                // Create a new transparent image with power-of-two dimensions
                var canvas = new Image<Rgba32>(newWidth, newHeight, new Rgba32(0, 0, 0, 0));

                // Center the scaled image
                int offsetX = (newWidth - scaledWidth) / 2;
                int offsetY = (newHeight - scaledHeight) / 2;

                // Resize and draw the image onto the canvas
                image.Mutate(x => x.Resize(scaledWidth, scaledHeight));
                canvas.Mutate(x => x.DrawImage(image, new Point(offsetX, offsetY), 1f));

                image = canvas;

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
            if (files == null)
            {
                return Array.Empty<string>();
            }
            var selectedFiles = await files;
            return selectedFiles.Select(f => f.Path.LocalPath).ToArray();
        }

        public static class FilePickerFileTypes
        {
            public static FilePickerFileType VTFAndImages { get; } = new("VTF and Images")
            {
                Patterns = new[] { "*.vtf", "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" }
            };
        }
    }
}