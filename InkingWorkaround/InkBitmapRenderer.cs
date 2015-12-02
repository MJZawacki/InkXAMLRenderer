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

namespace InkingWorkaround
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
                    //DrawHighlightInk(drawingSession, highlightStrokes);
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

                        //DrawHighlightInk(drawingSession, highlightStrokes);
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

        public async Task<SoftwareBitmap> RenderToBitmapAsync(IReadOnlyList<InkStroke> inkStrokes, 
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
                        var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(renderTarget, BitmapAlphaMode.Premultiplied);
                        var background = CanvasBitmap.CreateFromSoftwareBitmap(_canvasDevice, bitmap);
                        drawingSession.DrawImage(background);
                        drawingSession.DrawInk(inkStrokes);

                        DrawHighlightInk(drawingSession, highlightStrokes);
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


        private void DrawHighlightInk(CanvasDrawingSession ds, IReadOnlyList<InkStroke> strokes)
        {

            //Color newColor = Color.FromArgb(255, 255, 0, 0);
            Color newColor = Colors.Yellow;
            newColor.A = 125;
            var inkGeometry = CanvasGeometry.CreateInk(ds, strokes);
            ds.DrawGeometry(inkGeometry, Colors.Yellow);
            //
            // This shows off the fact that apps can use the custom drying path
            // to render dry ink using Win2D, and not necessarily 
            // rely on the built-in rendering in CanvasDrawingSession.DrawInk.
            //
            //foreach (var stroke in strokes)
            //{
                
            //    var color = stroke.DrawingAttributes.Color;
            //    //color.A = 125; 
            //    //var strokeWidth = stroke.DrawingAttributes.Size.Width;
            //    //Color newColor = Color.FromArgb(50, 100,100,100);
            //    //var brush = new Microsoft.Graphics.Canvas.Brushes.CanvasSolidColorBrush(_canvasDevice, color);
                    

                
            //    var inkPoints = stroke.GetInkPoints();
            //    if (inkPoints.Count > 0)
            //    {
            //        CanvasPathBuilder pathBuilder = new CanvasPathBuilder(ds);
            //        pathBuilder.BeginFigure(inkPoints[0].Position.ToVector2());
            //        for (int i = 1; i < inkPoints.Count; i++)
            //        {
            //            pathBuilder.AddLine(inkPoints[i].Position.ToVector2());
            //            ds.DrawCircle(inkPoints[i].Position.ToVector2(), inkPoints[i].Pressure * 5, color);
            //        }
            //        pathBuilder.EndFigure(CanvasFigureLoop.Open);
            //        CanvasGeometry geometry = CanvasGeometry.CreatePath(pathBuilder);
                    
            //        ds.DrawGeometry(geometry, color);
            //    }
            //}
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
