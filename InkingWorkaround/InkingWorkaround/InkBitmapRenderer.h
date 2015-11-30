#pragma once

#include <inkrenderer.h>

namespace InkingWorkaround
{
	public ref class InkBitmapRenderer sealed
	{
	public:
		InkBitmapRenderer();
		virtual ~InkBitmapRenderer();

		InkBitmapRenderer(bool hardwareAccelerated);

		static InkBitmapRenderer^ CreateWithDirect3D11Device(
			Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice^ sharedDevice
			);
		void UpdateDevice(
			Windows::Graphics::DirectX::Direct3D11::IDirect3DDevice^ sharedDevice
			);

		Windows::Foundation::IAsyncOperation<Windows::Graphics::Imaging::SoftwareBitmap^>^ RenderStrokesAsync(
			Windows::Foundation::Collections::IIterable<Windows::UI::Input::Inking::InkStroke^>^ inkStrokes,
			double width,
			double height
			);

	private:
		InkBitmapRenderer(
			Microsoft::Graphics::Canvas::CanvasDevice^ canvasDevice,
			bool ownsDevice
			);
		void RecreateDevice();
		void OnDeviceLost(Microsoft::Graphics::Canvas::CanvasDevice^ sender, Platform::Object^ args);

		bool m_ownsDevice;
		Microsoft::Graphics::Canvas::CanvasDevice^ m_canvasDevice;
		Microsoft::WRL::ComPtr<IInkD2DRenderer> m_inkRenderer;
	};
}


