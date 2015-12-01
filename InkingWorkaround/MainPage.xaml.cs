using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


namespace InkingWorkaround
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly InkBitmapRenderer _inkBitmapRenderer = new InkBitmapRenderer();
        private InkPresenter _inkPresenter;

        public MainPage()
        {
            this.InitializeComponent();

            _inkPresenter = AnnotationInkCanvas.InkPresenter;
            _inkPresenter.InputDeviceTypes =
                CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch | CoreInputDeviceTypes.Pen;

            var desiredDrawingAttributes = new InkDrawingAttributes()
            {
                Color = Colors.Pink,
                DrawAsHighlighter = highlight,


            };

            _inkPresenter.UpdateDefaultDrawingAttributes(desiredDrawingAttributes);
            _inkPresenter.StrokesCollected += _inkPresenter_StrokesCollected;
        }

        private InkStrokeContainer highlightStrokes = new InkStrokeContainer();
        private async void _inkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            foreach (var stroke in args.Strokes)
            {
                if (stroke.DrawingAttributes.DrawAsHighlighter == true)
                {
                    var newstroke = stroke.Clone();
                    var da = new InkDrawingAttributes
                    {
                        DrawAsHighlighter = false,
                        Color = Colors.Red

                    };
                    newstroke.DrawingAttributes = da;
                    
                    highlightStrokes.AddStroke(newstroke);
                }
            }

            var renderTargetBitmap = await generateBackground();


            var renderedImageSource = await _inkBitmapRenderer.RenderToImageSourceAsync(
                renderTargetBitmap,
                _inkPresenter.StrokeContainer.GetStrokes(),
                highlightStrokes.GetStrokes(),
                AnnotationInkCanvas.ActualWidth,
                AnnotationInkCanvas.ActualHeight
                );
            TargetImage.Source = renderedImageSource;
        }

        private async Task<RenderTargetBitmap> generateBackground()
        {

            var renderTargetBitmap = new RenderTargetBitmap();
            var currentDpi = DisplayInformation.GetForCurrentView().LogicalDpi;

            // Create image without ink
            AnnotationInkCanvas.Visibility = Visibility.Collapsed;

            await renderTargetBitmap.RenderAsync(InkingRoot);
            AnnotationInkCanvas.Visibility = Visibility.Visible;
            return renderTargetBitmap;



        }
        private async void SaveScreenShot_Click(object sender, RoutedEventArgs e)
        {
            var renderTargetBitmap = await generateBackground();



            // Send image to ink renderer
            var renderedImage = await _inkBitmapRenderer.RenderToBitmapAsync(
                renderTargetBitmap,
                _inkPresenter.StrokeContainer.GetStrokes(),
                highlightStrokes.GetStrokes(),
                AnnotationInkCanvas.ActualWidth,
                AnnotationInkCanvas.ActualHeight
                );

            var currentDpi = DisplayInformation.GetForCurrentView().LogicalDpi;

            if (renderedImage != null)
            {

                var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("output.png", CreationCollisionOption.ReplaceExisting);


                if (file != null)
                {
                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                        encoder.SetSoftwareBitmap(renderedImage);
                        

                        await encoder.FlushAsync();
                    }
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var renderedImage = await _inkBitmapRenderer.RenderToBitmapAsync(
                _inkPresenter.StrokeContainer.GetStrokes(),
                highlightStrokes.GetStrokes(),
                AnnotationInkCanvas.ActualWidth,
                AnnotationInkCanvas.ActualHeight
                );



            

            if (renderedImage != null)
            {
                // Convert to a format appropriate for SoftwareBitmapSource.
                var convertedImage = SoftwareBitmap.Convert(
                    renderedImage,
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied
                    );
                await InkImageSource.SetBitmapAsync(convertedImage);

                var renderTargetBitmap = new RenderTargetBitmap();
                var currentDpi = DisplayInformation.GetForCurrentView().LogicalDpi;

                // Prepare for RenderTargetBitmap by hiding the InkCanvas and displaying the
                // rasterized strokes instead.
                AnnotationInkCanvas.Visibility = Visibility.Collapsed;
                //TargetImage.Visibility = Visibility.Collapsed;
                InkImage.Visibility = Visibility.Visible;
                await renderTargetBitmap.RenderAsync(InkingRoot);
                var pixelData = await renderTargetBitmap.GetPixelsAsync();

                // Restore the original layout now that we have created the RenderTargetBitmap image.
                AnnotationInkCanvas.Visibility = Visibility.Visible;
                //TargetImage.Visibility = Visibility.Visible;

                InkImage.Visibility = Visibility.Collapsed;

                var savePicker = new FileSavePicker();
                savePicker.DefaultFileExtension = ".png";
                savePicker.FileTypeChoices.Add("Portable Network Graphics", new[] { ".png" });
                //var file = await savePicker.PickSaveFileAsync();

                var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("output.png", CreationCollisionOption.ReplaceExisting);


                if (file != null)
                {
                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied,
                            (uint)renderTargetBitmap.PixelWidth,
                            (uint)renderTargetBitmap.PixelHeight,
                            currentDpi,
                            currentDpi,
                            pixelData.ToArray()
                            );

                        await encoder.FlushAsync();
                        //stream.Seek(0);
                        //BitmapImage generatedImage = new BitmapImage();
                        //generatedImage.SetSource(stream);
                        //TargetImage.Source = generatedImage;
                        
                    }

                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapImage generatedImage = new BitmapImage();
                        generatedImage.SetSource(stream);
                        //TargetImage.Source = generatedImage;
                    }
                }
            }
        }

        private bool highlight = false;

        private void InkButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Pen)
            {
                highlight = false;
                var desiredDrawingAttributes = new InkDrawingAttributes()
                {
                    Color = Colors.Pink,
                    DrawAsHighlighter = highlight,


                };

                _inkPresenter.UpdateDefaultDrawingAttributes(desiredDrawingAttributes);
            }
        }

        private void HighlightButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Pen)
            {
                highlight = true;
                var desiredDrawingAttributes = new InkDrawingAttributes()
                {
                    Color = Colors.Yellow,
                    DrawAsHighlighter = highlight,


                };

                _inkPresenter.UpdateDefaultDrawingAttributes(desiredDrawingAttributes);
            }
        }
    }
}
