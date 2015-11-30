//
// MainPage.xaml.cpp
// Implementation of the MainPage class.
//

#include "pch.h"
#include "MainPage.xaml.h"

using namespace InkingWorkaround;

using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Graphics::Imaging;
using namespace Windows::UI::Core;
using namespace Windows::UI::Input::Inking;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Controls::Primitives;
using namespace Windows::UI::Xaml::Data;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Windows::UI::Xaml::Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

MainPage::MainPage() : m_inkBitmapRenderer(ref new InkBitmapRenderer())
{
	InitializeComponent();

	m_collectedStrokes = ref new Vector<InkStroke^>();

	InkCanvas->InkPresenter->StrokesCollected +=
		ref new TypedEventHandler<InkPresenter^, InkStrokesCollectedEventArgs^>(this, &MainPage::OnStrokesCollected);
	InkCanvas->InkPresenter->InputDeviceTypes =
		CoreInputDeviceTypes::Touch | CoreInputDeviceTypes::Pen;
}


void MainPage::OnStrokesCollected(InkPresenter ^sender, InkStrokesCollectedEventArgs ^args)
{
	for (auto stroke : args->Strokes)
	{
		m_collectedStrokes->Append(stroke);
	}

	auto renderStrokes = m_inkBitmapRenderer->RenderStrokesAsync(
		m_collectedStrokes,
		InkCanvas->ActualWidth,
		InkCanvas->ActualHeight
		);
	concurrency::create_task(renderStrokes).then([this](SoftwareBitmap^ softwareBitmap)
	{
		if (softwareBitmap != nullptr)
		{
			// Convert result to a bitmap compatable with SoftwareBitmapSource
			auto convertedBitmap = SoftwareBitmap::Convert(
				softwareBitmap,
				BitmapPixelFormat::Bgra8,
				BitmapAlphaMode::Premultiplied
				);
			InkStrokeImageSource->SetBitmapAsync(convertedBitmap);
		}
	});
}
