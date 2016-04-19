using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.UI.Input.Inking;
using System.Numerics;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace InkRendering
{

    public class InkBitmapRenderer
    {
        private CanvasDevice _canvasDevice;

        public InkBitmapRenderer() : this(false)
        {
        }

        public InkBitmapRenderer(bool forceSoftwareRenderer)
        {
            _canvasDevice = CanvasDevice.GetSharedDevice(forceSoftwareRenderer);
            _canvasDevice.DeviceLost += HandleDeviceLost;
        }

        
        public  async Task<CanvasImageSource> RenderToImageSourceAsync(
            RenderTargetBitmap backgroundBitmap,
            IReadOnlyList<InkStroke> inkStrokes, IReadOnlyList<InkStroke> highlightStrokes, double width, double height)
        {

            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var imageSource = new CanvasImageSource(_canvasDevice, (float) width, (float) height, dpi);


            using (var drawingSession = imageSource.CreateDrawingSession(Colors.White))
            {
                try
                {
                    var pixels = await backgroundBitmap.GetPixelsAsync();
                    var bitmap = SoftwareBitmap.CreateCopyFromBuffer(pixels, BitmapPixelFormat.Bgra8,
                        backgroundBitmap.PixelWidth, backgroundBitmap.PixelHeight);
                    var convertedImage = SoftwareBitmap.Convert(
                        bitmap,
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied
                    );
                    var background = CanvasBitmap.CreateFromSoftwareBitmap(_canvasDevice, convertedImage);
                    drawingSession.DrawImage(background,new Rect(0,0,
                        width, 
                        height));
                    drawingSession.DrawInk(inkStrokes);
           
                    return imageSource;
                }
                catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
                {
                    _canvasDevice.RaiseDeviceLost();
                }

            return null;

            }
        }


        public async Task<SoftwareBitmap> RenderToBitmapAsync(
            RenderTargetBitmap renderTargetBitmap,
            IReadOnlyList<InkStroke> inkStrokes,
            IReadOnlyList<InkStroke> highlightStrokes, double width, double height)
        {

            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var renderTarget = new CanvasRenderTarget(_canvasDevice, (float)width, (float)height, dpi);
            using (renderTarget)
            {
                try
                {
                    using (var drawingSession = renderTarget.CreateDrawingSession())
                    {
                        var pixels = await renderTargetBitmap.GetPixelsAsync();
                        var bitmap = SoftwareBitmap.CreateCopyFromBuffer(pixels, BitmapPixelFormat.Bgra8, 
                            renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight);
                        var convertedImage = SoftwareBitmap.Convert(
                            bitmap,
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied
                        );
                        var background = CanvasBitmap.CreateFromSoftwareBitmap(_canvasDevice, convertedImage);
                        drawingSession.DrawImage(background, new Rect(0,0,
                            width, height));
                        drawingSession.DrawInk(inkStrokes);
                    }

                    return await SoftwareBitmap.CreateCopyFromSurfaceAsync(renderTarget, BitmapAlphaMode.Premultiplied);
                }
                catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
                {
                    _canvasDevice.RaiseDeviceLost();
                }

            }
            return null;
        }
    
        public void Trim()
        {
            _canvasDevice.Trim();
        }
       
        private void HandleDeviceLost(CanvasDevice sender, object args)
        {
            if (sender == _canvasDevice)
            {
                RecreateDevice();
            }
        }

        private void RecreateDevice()
        {
            _canvasDevice.DeviceLost -= HandleDeviceLost;

            _canvasDevice = CanvasDevice.GetSharedDevice(_canvasDevice.ForceSoftwareRenderer);
            _canvasDevice.DeviceLost += HandleDeviceLost;
        }
    }
}
