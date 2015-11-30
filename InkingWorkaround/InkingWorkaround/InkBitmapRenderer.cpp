#include "pch.h"
#include "InkBitmapRenderer.h"

#include <Microsoft.Graphics.Canvas.native.h>
#include <d2d1_1.h>

using namespace InkingWorkaround;

using namespace Microsoft::Graphics::Canvas;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Graphics::DirectX;
using namespace Windows::Graphics::DirectX::Direct3D11;
using namespace Windows::Graphics::Display;
using namespace Windows::Graphics::Imaging;
using namespace Windows::UI;
using namespace Windows::UI::Input::Inking;

InkBitmapRenderer::InkBitmapRenderer() : InkBitmapRenderer(true)
{
}


InkBitmapRenderer::InkBitmapRenderer(bool hardwareAccelerated)
	: InkBitmapRenderer(
		ref new CanvasDevice(
			CanvasDebugLevel::None,
			hardwareAccelerated ? CanvasHardwareAcceleration::Auto : CanvasHardwareAcceleration::Off
			),
		true
		)
{
}

InkBitmapRenderer::~InkBitmapRenderer()
{
}

InkBitmapRenderer^ InkBitmapRenderer::CreateWithDirect3D11Device(IDirect3DDevice^ sharedDevice)
{
	auto canvasDevice = CanvasDevice::CreateFromDirect3D11Device(
		sharedDevice,
		CanvasDebugLevel::None
		);
	return ref new InkBitmapRenderer(canvasDevice, false);
}

InkBitmapRenderer::InkBitmapRenderer(CanvasDevice^ canvasDevice, bool ownsDevice)
	: m_canvasDevice(canvasDevice), m_ownsDevice(ownsDevice)
{
	if (m_ownsDevice)
	{
		m_canvasDevice->DeviceLost += ref new TypedEventHandler<CanvasDevice^, Platform::Object^>(
			this,
			&InkBitmapRenderer::OnDeviceLost
			);
	}

	CoCreateInstance(
		__uuidof(InkD2DRenderer),
		nullptr,
		CLSCTX_INPROC_SERVER,
		IID_PPV_ARGS(&m_inkRenderer)
		);
}

void InkBitmapRenderer::UpdateDevice(IDirect3DDevice^ sharedDevice)
{
	m_canvasDevice = CanvasDevice::CreateFromDirect3D11Device(sharedDevice, CanvasDebugLevel::None);
	m_ownsDevice = false;
}

void InkBitmapRenderer::OnDeviceLost(CanvasDevice^ sender, Object^ args)
{
	if (m_ownsDevice && Object::ReferenceEquals(sender, m_canvasDevice))
	{
		RecreateDevice();
	}
}

void InkBitmapRenderer::RecreateDevice()
{
	m_canvasDevice = ref new CanvasDevice(
		CanvasDebugLevel::None,
		m_canvasDevice->HardwareAcceleration
		);
	m_canvasDevice->DeviceLost += ref new TypedEventHandler<CanvasDevice^, Platform::Object^>(
		this,
		&InkBitmapRenderer::OnDeviceLost
		);
}

IAsyncOperation<SoftwareBitmap^>^ InkBitmapRenderer::RenderStrokesAsync(
	IIterable<InkStroke^>^ inkStrokes,
	double width,
	double height
	)
{
	auto dpi = DisplayInformation::GetForCurrentView()->LogicalDpi;
	try
	{
		auto renderTarget = ref new CanvasRenderTarget(
			m_canvasDevice,
			static_cast<float>(width),
			static_cast<float>(height),
			dpi
			);
		{
			auto drawingSession = renderTarget->CreateDrawingSession();
			auto deviceContext = GetWrappedResource<ID2D1DeviceContext>(drawingSession);

			deviceContext->Clear();
			auto hr = m_inkRenderer->Draw(
				deviceContext.Get(),
				reinterpret_cast<IUnknown*>(inkStrokes),
				false
				);

			if (FAILED(hr))
			{
				throw ref new Platform::Exception(hr);
			}
		}

		return SoftwareBitmap::CreateCopyFromSurfaceAsync(renderTarget);
	}
	catch (Exception^ e)
	{
		if (m_canvasDevice->IsDeviceLost(e->HResult) && m_ownsDevice)
		{
			m_canvasDevice->RaiseDeviceLost();

			// Return null on device lost
			return concurrency::create_async([]() -> SoftwareBitmap^
			{
				return nullptr;
			});
		}
		else
		{
			// If the client provided their own device, they should catch the device lost HResult
			throw;
		}
	}
}