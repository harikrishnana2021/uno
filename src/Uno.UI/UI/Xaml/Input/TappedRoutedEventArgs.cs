﻿using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public sealed partial class TappedRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly Point _position;

		public TappedRoutedEventArgs() 
			: this(null, PointerDeviceType.Mouse, new Point())
		{
		}
		
		internal TappedRoutedEventArgs(object originalSource, PointerDeviceType pointerType, Point position)
			: base(originalSource)
		{
			PointerDeviceType = pointerType;
			_position = position;
		}

		public bool Handled { get; set; }

		public Point GetPosition() => _position;

		public PointerDeviceType PointerDeviceType { get; internal set; }

		public global::Windows.Foundation.Point GetPosition(global::Windows.UI.Xaml.UIElement relativeTo)
		{
			if(relativeTo == null)
			{
				return _position;
			}
			else
			{
				return relativeTo.GetPosition(_position, relativeTo);
			}
		}
	}
}
