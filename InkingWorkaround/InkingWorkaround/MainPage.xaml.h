//
// MainPage.xaml.h
// Declaration of the MainPage class.
//

#pragma once

#include "MainPage.g.h"

namespace InkingWorkaround
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public ref class MainPage sealed
	{
	public:
		MainPage();

	private:
		InkBitmapRenderer^ m_inkBitmapRenderer;
		Platform::Collections::Vector< Windows::UI::Input::Inking::InkStroke^>^ m_collectedStrokes;

		void OnStrokesCollected(Windows::UI::Input::Inking::InkPresenter ^sender, Windows::UI::Input::Inking::InkStrokesCollectedEventArgs ^args);
	};
}
