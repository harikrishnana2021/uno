﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.PointersTests
{
	[SampleControlInfo("Pointers", "Sequence")]
	public sealed partial class EventsSequences : Page
	{
		private readonly List<(object evt, RoutedEventArgs args)> _tapResult = new List<(object, RoutedEventArgs)>();
		private readonly List<(object evt, RoutedEventArgs args)> _clickResult = new List<(object, RoutedEventArgs)>();
		private readonly List<(object evt, RoutedEventArgs args)> _hyperlinkResult = new List<(object, RoutedEventArgs)>();
		private readonly List<(object evt, RoutedEventArgs args)> _listViewResult = new List<(object, RoutedEventArgs)>();

		private static readonly object ClickEvent = new object();

		[Flags]
		private enum EventsKind
		{
			Pointers = 1,
			// Manipulation
			Gestures = 4,
			Click = 8
		}

		public EventsSequences()
		{
			this.InitializeComponent();

			SetupEvents(TestTapTarget, _tapResult, EventsKind.Pointers | EventsKind.Gestures);
			SetupEvents(TestClickTarget, _clickResult, EventsKind.Pointers | EventsKind.Gestures | EventsKind.Click);
			SetupEvents(TestHyperlinkTarget, _hyperlinkResult, EventsKind.Pointers | EventsKind.Gestures);
			SetupEvents(TestHyperlinkInner, _hyperlinkResult, EventsKind.Click);
			SetupEvents(TestListViewTarget, _listViewResult, EventsKind.Pointers | EventsKind.Gestures | EventsKind.Click);

			_pointerType.ItemsSource = Enum.GetNames(typeof(PointerDeviceType));

			// Values for automated tests
#if __ANDROID__ || __IOS__
			_pointerType.SelectedValue = PointerDeviceType.Touch.ToString();
#else
			_pointerType.SelectedValue = PointerDeviceType.Mouse.ToString();
#endif
		}

#if __IOS__ // On iOS pen is handled exactly as if it was a finger ...
		private bool PenSupportsHover = false;
#else
		private bool PenSupportsHover = true;
#endif

		private PointerDeviceType PointerType => (PointerDeviceType)Enum.Parse(typeof(PointerDeviceType), _pointerType.SelectedValue.ToString());

		private void ResetTapTest(object sender, RoutedEventArgs e) => Clear(_tapResult, TestTapResult);
		private void ValidateTapTest(object sender, RoutedEventArgs e)
		{
			var args = new EventSequenceValidator(_tapResult);
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result =
						args.One(PointerEnteredEvent)
						&& args.Some(PointerMovedEvent) // Could be "Maybe" but WASM UI test generates it and we want to validate it
						&& args.One(PointerPressedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerReleasedEvent)
						&& args.One(TappedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
					result =
						args.One(PointerEnteredEvent)
						&& args.One(PointerPressedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerReleasedEvent)
						&& args.One(TappedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;
			}

			TestTapResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void ResetClickTest(object sender, RoutedEventArgs e) => Clear(_clickResult, TestClickResult);
		private void ValidateClickTest(object sender, RoutedEventArgs e)
		{
			// Pointer pressed and released are handled by the ButtonBase

			var args = new EventSequenceValidator(_clickResult);
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result = args.One(PointerEnteredEvent)
						&& args.Some(PointerMovedEvent) // Could be "Maybe" but WASM UI test generates it and we want to validate it
						&& args.One(ClickEvent)
						&& args.One(PointerCaptureLostEvent)
						&& args.One(TappedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
					result = args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(ClickEvent)
						&& args.One(PointerCaptureLostEvent)
						&& args.One(TappedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;
			}

			TestClickResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void ResetHyperlinkTest(object sender, RoutedEventArgs e) => Clear(_hyperlinkResult, TestHyperlinkResult);
		private void ValidateHyperlinkTest(object sender, RoutedEventArgs e)
		{
			// We subscribed at booth, the TextBlock (Pointers and Gestures) and the Hyperlink (Click)
			// Pointer pressed is handled by the TextBlock, but NOT the released
			// We MUST not receive a Tapped (when clicking on an hyperlink) neither a CaptureLost

			var args = new EventSequenceValidator(_hyperlinkResult);
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result =
						args.One(PointerEnteredEvent)
						&& args.Some(PointerMovedEvent) // Could be "Maybe" but WASM UI test generates it and we want to validate it
#if NETFX_CORE
						&& args.One(PointerReleasedEvent)
						&& args.One(ClickEvent)
#elif __WASM__ // KNOWN ISSUE: We don't get a released if not previously pressed, but pressed are muted by the Hyperlink which is a UIElement on wasm
						&& args.One(ClickEvent)
#else
						&& args.One(ClickEvent)
						&& args.One(PointerReleasedEvent)
#endif
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
#if __IOS__
					// KNOWN ISSUE:
					//	On iOS as the Entered/Exited are generated on Pressed/Released, which are Handled by the Hyperlink,
					//	we do not receive the expected Entered/Exited on parent control.
					//	As a side effect we will also not receive the Tap as it is an interpretation of those missing Pointer events.
					result =
						args.One(ClickEvent)
						&& args.End();
#else
					result =
						args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerMovedEvent)
#if NETFX_CORE
						&& args.One(PointerReleasedEvent)
						&& args.One(ClickEvent)
#elif __WASM__ // KNOWN ISSUE: We don't get a released if not previously pressed, but pressed are muted by the Hyperlink which is a UIElement on wasm
						&& args.One(ClickEvent)
#else
						&& args.One(ClickEvent)
						&& args.One(PointerReleasedEvent)
#endif
						&& args.One(PointerExitedEvent)
						&& args.End();
#endif
					break;
			}

			TestHyperlinkResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void ResetListViewTest(object sender, RoutedEventArgs e) => Clear(_listViewResult, TestListViewResult);
		private void ValidateListViewTest(object sender, RoutedEventArgs e)
		{
			// We subscribed at booth, the TextBlock (Pointers and Gestures) and the Hyperlink (Click)
			// Pointer pressed and released are handled by the TextBlock

			var args = new EventSequenceValidator(_listViewResult);
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result =
						args.One(PointerEnteredEvent)
						&& args.Some(PointerMovedEvent) // Could be "Maybe" but WASM UI test generate it and we want to validate it
						&& args.One(ClickEvent)
#if NETFX_CORE // We should get a Tapped on all platforms but ListView is a weird/complex control ...
						&& args.One(TappedEvent)
#endif
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
#if __IOS__ || __ANDROID__
					// KNOWN ISSUE:
					//	On iOS and Android as the Entered/Exited are generated on Pressed/Released, which are Handled by the ListViewItem,
					//	so we do not receive the expected Entered/Exited on parent control.
					//	As a side effect we will also not receive the Tap as it is an interpretation of those missing Pointer events.
					result =
						args.One(ClickEvent)
						&& args.End();
#else
					result =
						args.One(PointerEnteredEvent)
						&& args.One(ClickEvent)
#if NETFX_CORE // We should get a Tapped on all platforms but ListView is a weird/complex control ...
						&& args.One(TappedEvent)
#endif
						&& args.One(PointerExitedEvent)
						&& args.End();
#endif
					break;
			}

			TestListViewResult.Text = result ? "SUCCESS" : "FAILED";
		}

#region Common helpers
		private void Clear(IList events, TextBlock result)
		{
			events.Clear();
			result.Text = "** no result **";
			Output.Text = "";
		}
		private void SetupEvents(DependencyObject target, IList<(object, RoutedEventArgs)> events, EventsKind kind, string name = null, bool captureOnPress = false)
		{
			name = name ?? (target as FrameworkElement)?.Name ?? $"{target.GetType().Name}:{target.GetHashCode():X6}";

			if (kind.HasFlag(EventsKind.Pointers) && target is UIElement pointerTarget)
			{
				pointerTarget.PointerEntered += (snd, e) => OnPointerEvent(PointerEnteredEvent, "Entered", e);
				pointerTarget.PointerPressed += (snd, e) =>
				{
					OnPointerEvent(PointerPressedEvent, "Pressed", e);
					if (captureOnPress)
					{
						var captured = pointerTarget.CapturePointer(e.Pointer);
						Log($"[{name}] Captured: {captured}");
					}
				};
				pointerTarget.PointerMoved += (snd, e) => OnPointerEvent(PointerMovedEvent, "Moved", e);
				pointerTarget.PointerReleased += (snd, e) => OnPointerEvent(PointerReleasedEvent, "Released", e);
				pointerTarget.PointerCanceled += (snd, e) => OnPointerEvent(PointerCanceledEvent, "Canceled", e);
				pointerTarget.PointerExited += (snd, e) => OnPointerEvent(PointerExitedEvent, "Exited", e);
				pointerTarget.PointerCaptureLost += (snd, e) => OnPointerEvent(PointerCaptureLostEvent, "CaptureLost", e);
			}

			if (kind.HasFlag(EventsKind.Gestures) && target is UIElement gestureTarget)
			{
				// Those events are built using the GestureRecognizer
				gestureTarget.Tapped += (snd, e) => OnEvent(TappedEvent, "Tapped", e);
				gestureTarget.DoubleTapped += (snd, e) => OnEvent(DoubleTappedEvent, "DoubleTapped", e);
			}

			if (kind.HasFlag(EventsKind.Click))
			{
				if (target is ButtonBase button)
					button.Click += (snd, e) => OnEvent(ClickEvent, "Click", e);

				if (target is Hyperlink hyperlink)
					hyperlink.Click += (snd, e) => OnEvent(ClickEvent, "Click", e);

				if (target is ListViewBase listView)
					listView.ItemClick += (snd, e) => OnEvent(ClickEvent, "Click", e);
			}

			void OnEvent(object evt, string evtName, RoutedEventArgs e)
			{
				events.Add((evt, e));
				Log($"[{name}] {evtName}");
			}

			void OnPointerEvent(RoutedEvent evt, string evtName, PointerRoutedEventArgs e)
			{
				events.Add((evt, e));

				var point = e.GetCurrentPoint(this);
				Log($"[{name}] {evtName}: id={e.Pointer.PointerId} "
					+ $"| frame={point.FrameId}"
					+ $"| type={e.Pointer.PointerDeviceType} "
					+ $"| position={point.Position} "
					+ $"| rawPosition={point.RawPosition} "
					+ $"| inContact={point.IsInContact} "
					+ $"| inRange={point.Properties.IsInRange} "
					+ $"| primary={point.Properties.IsPrimary}"
					+ $"| intermediates={e.GetIntermediatePoints(this)?.Count.ToString() ?? "null"} ");
			}
		}

		private void Log(string message)
		{
			System.Diagnostics.Debug.WriteLine(message);
			Output.Text += message + "\r\n";
		}

		private class EventSequenceValidator
		{
			private readonly IList<(object evt, RoutedEventArgs args)> _args;
			private int _index = 0;

			public EventSequenceValidator(IList<(object evt, RoutedEventArgs args)> args)
			{
				_args = args;
			}

			/// <summary>
			/// [1..1]
			/// </summary>
			public bool One(object evt)
				=> _index < _args.Count && _args[_index++].evt == evt;

			/// <summary>
			/// [1..*]
			/// </summary>
			public bool Some(RoutedEvent evt)
				=> One(evt) && MaybeSome(evt);

			/// <summary>
			/// [0..1]
			/// </summary>
			public bool MaybeOne(RoutedEvent evt)
			{
				if (_index < _args.Count &&  _args[_index].evt == evt)
				{
					++_index;
				}
				return true;
			}

			/// <summary>
			/// [0..*]
			/// </summary>
			public bool MaybeSome(RoutedEvent evt)
			{
				while (_index < _args.Count && _args[_index].evt == evt)
				{
					++_index;
				}
				return true;
			}

			public bool End()
				=> _index >= _args.Count;
		}
#endregion
	}
}
