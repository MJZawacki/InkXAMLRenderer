using System;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;


namespace InkRendering
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
                CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen;

            var desiredDrawingAttributes = new InkDrawingAttributes()
            {
                Color = Colors.Pink,
                DrawAsHighlighter = _highlight,
            };

            _inkPresenter.UpdateDefaultDrawingAttributes(desiredDrawingAttributes);
            _inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
        }

        
        /// <summary>
        ///  Render XAML and Ink to preview image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {


            var renderTargetBitmap = await generateBackground();


            var renderedImageSource = await _inkBitmapRenderer.RenderToImageSourceAsync(
                renderTargetBitmap,
                _inkPresenter.StrokeContainer.GetStrokes(),
                _highlightStrokes.GetStrokes(),
                AnnotationInkCanvas.ActualWidth,
                AnnotationInkCanvas.ActualHeight
                );
            TargetImage.Source = renderedImageSource;
        }

        /// <summary>
        /// Helper method to generate XAML rendering without ink
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Save XAML and ink to file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveScreenShot_Click(object sender, RoutedEventArgs e)
        {
            var renderTargetBitmap = await generateBackground();

            // Send image to ink renderer
            var renderedImage = await _inkBitmapRenderer.RenderToBitmapAsync(
                renderTargetBitmap,
                _inkPresenter.StrokeContainer.GetStrokes(),
                _highlightStrokes.GetStrokes(),
                AnnotationInkCanvas.ActualWidth,
                AnnotationInkCanvas.ActualHeight
                );

            var currentDpi = DisplayInformation.GetForCurrentView().LogicalDpi;

            if (renderedImage != null)
            {

                var savePicker = new FileSavePicker();
                savePicker.DefaultFileExtension = ".png";
                savePicker.FileTypeChoices.Add("Portable Network Graphics", new[] { ".png" });
                var file = await savePicker.PickSaveFileAsync();

                // use local file during debug
                //var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("output.png", CreationCollisionOption.ReplaceExisting);

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
          

        /// <summary>
        /// Changes ink to solid color
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkButton_Tapped(object sender, TappedRoutedEventArgs e)
        {

                _highlight = false;
                var desiredDrawingAttributes = new InkDrawingAttributes()
                {
                    Color = Colors.Pink,
                    DrawAsHighlighter = _highlight,
                };

                _inkPresenter.UpdateDefaultDrawingAttributes(desiredDrawingAttributes);
     
        }

        /// <summary>
        /// Changes ink to highlighter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HighlightButton_Tapped(object sender, TappedRoutedEventArgs e)
        {

                _highlight = true;
                var desiredDrawingAttributes = new InkDrawingAttributes()
                {
                    Color = Colors.Yellow,
                    DrawAsHighlighter = _highlight,
                };

                _inkPresenter.UpdateDefaultDrawingAttributes(desiredDrawingAttributes);
        }

        private InkStrokeContainer _highlightStrokes = new InkStrokeContainer();
        private bool _highlight = false;

    }
}
