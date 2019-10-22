﻿using System;
using System.ComponentModel;
using System.Windows.Input;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Views;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using static Xamarin.Forms.SwipeView;
using AButton = Android.Support.V7.Widget.AppCompatButton;
using APointF = Android.Graphics.PointF;
using AView = Android.Views.View;
using Specifics = Xamarin.Forms.PlatformConfiguration.AndroidSpecific.SwipeView;

namespace Xamarin.Forms.Platform.Android
{
	public class SwipeViewRenderer : ViewRenderer<SwipeView, AView>, GestureDetector.IOnGestureListener
	{
		internal const string SwipeView = "Xamarin.SwipeView";
		internal const string CloseSwipeView = "Xamarin.CloseSwipeView";

		const int SwipeThreshold = 250;
		const int SwipeItemWidth = 80;
		const long SwipeAnimationDuration = 200;

		readonly Context _context;
		GestureDetector _detector;
		AView _contentView;
		LinearLayoutCompat _actionView;
		SwipeTransitionMode _swipeTransitionMode;
		float _downX;
		float _downY;
		float _density;
		bool _isTouchDown;
		bool _isSwiping;
		APointF _initialPoint;
		SwipeDirection _swipeDirection;
		float _swipeOffset;
		float _swipeThreshold;
		bool _isDisposed;

		public SwipeViewRenderer(Context context) : base(context)
		{
			_context = context;

			AutoPackage = false;
			ClipToOutline = true;
		}

		protected override void OnElementChanged(ElementChangedEventArgs<SwipeView> e)
		{
			if (e.NewElement != null)
			{
				if (Control == null)
				{
					MessagingCenter.Subscribe<string>(SwipeView, CloseSwipeView, OnClose);

					_density = Resources.DisplayMetrics.Density;
					_detector = new GestureDetector(Context, this);

					SetNativeControl(CreateNativeControl());
				}

				UpdateContent();
				UpdateSwipeTransitionMode();
				UpdateBackgroundColor();
			}

			base.OnElementChanged(e);
		}

		protected override AView CreateNativeControl()
		{
			return new AView(_context);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == ContentView.ContentProperty.PropertyName)
				UpdateContent();
			else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateBackgroundColor();
			else if (e.PropertyName == Specifics.SwipeTransitionModeProperty.PropertyName)
				UpdateSwipeTransitionMode();
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			base.OnLayout(changed, l, t, r, b);

			var width = r - l;
			var height = b - t;

			var pixelWidth = _context.FromPixels(width);
			var pixelHeight = _context.FromPixels(height);

			if (changed)
			{
				if (Element.Content != null)
					Element.Content.Layout(new Rectangle(0, 0, pixelWidth, pixelHeight));

				_contentView?.Layout(0, 0, width, height);
			}
		}

		protected override Size MinimumSize()
		{
			return new Size(40, 40);
		}

		protected override void UpdateBackgroundColor()
		{
			if (Element.BackgroundColor != Color.Default)
			{
				var backgroundColor = Element.BackgroundColor.ToAndroid();

				SetBackgroundColor(backgroundColor);

				if (_contentView != null && Element.Content == null && HasSwipeItems())
					_contentView.SetBackgroundColor(backgroundColor);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				MessagingCenter.Unsubscribe<string>(SwipeView, CloseSwipeView);

				if (_detector != null)
				{
					_detector.Dispose();
					_detector = null;
				}

				if (_contentView != null)
				{
					_contentView.RemoveFromParent();
					_contentView.Dispose();
					_contentView = null;
				}

				if (_actionView != null)
				{
					_actionView.RemoveFromParent();
					_actionView.Dispose();
					_actionView = null;
				}
			}

			_isDisposed = true;

			base.Dispose(disposing);
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			base.OnTouchEvent(e);

			var density = Resources.DisplayMetrics.Density;
			float x = Math.Abs((_downX - e.GetX()) / density);
			float y = Math.Abs((_downY - e.GetY()) / density);

			if (e.Action != MotionEventActions.Move | (x > 10f || y > 10f))
			{
				_detector.OnTouchEvent(e);
			}

			ProcessSwipingInteractions(e);

			return true;
		}

		public override bool OnInterceptTouchEvent(MotionEvent ev)
		{
			return true;
		}

		public bool OnDown(MotionEvent e)
		{
			return true;
		}

		public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
		{
			return true;
		}

		public void OnLongPress(MotionEvent e)
		{

		}

		public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
		{
			return true;
		}

		public void OnShowPress(MotionEvent e)
		{

		}

		public bool OnSingleTapUp(MotionEvent e)
		{
			return true;
		}

		void UpdateContent()
		{
			if (Element.Content == null)
				_contentView = CreateEmptyContent();
			else
				_contentView = CreateContent();

			AddView(_contentView, new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent));
		}

		AView CreateEmptyContent()
		{
			var emptyContentView = new AView(_context);
			emptyContentView.SetBackgroundColor(Color.Default.ToAndroid());

			return emptyContentView;
		}

		AView CreateContent()
		{
			var renderer = Platform.CreateRenderer(Element.Content, _context);
			Platform.SetRenderer(Element.Content, renderer);

			return renderer?.View;
		}

		SwipeItems GetSwipeItemsByDirection()
		{
			SwipeItems swipeItems = null;

			switch (_swipeDirection)
			{
				case SwipeDirection.Left:
					swipeItems = Element.RightItems;
					break;
				case SwipeDirection.Right:
					swipeItems = Element.LeftItems;
					break;
				case SwipeDirection.Up:
					swipeItems = Element.BottomItems;
					break;
				case SwipeDirection.Down:
					swipeItems = Element.TopItems;
					break;
			}

			return swipeItems;
		}

		bool HasSwipeItems()
		{
			return Element != null && (Element.LeftItems != null || Element.RightItems != null || Element.TopItems != null || Element.BottomItems != null);
		}

		bool ProcessSwipingInteractions(MotionEvent e)
		{
			bool? handled = true;
			var point = new APointF(e.GetX() / _density, e.GetY() / _density);

			switch (e.Action)
			{
				case MotionEventActions.Down:
					_downX = e.RawX;
					_downY = e.RawY;

					handled = HandleTouchInteractions(GestureStatus.Started, point);
					break;
				case MotionEventActions.Up:
					handled = HandleTouchInteractions(GestureStatus.Completed, point);

					if (Parent == null)
						break;

					Parent.RequestDisallowInterceptTouchEvent(false);
					break;
				case MotionEventActions.Move:
					handled = HandleTouchInteractions(GestureStatus.Running, point);

					if (handled == true || Parent == null)
						break;

					Parent.RequestDisallowInterceptTouchEvent(true);
					break;
				case MotionEventActions.Cancel:
					handled = HandleTouchInteractions(GestureStatus.Canceled, point);

					if (Parent == null)
						break;

					Parent.RequestDisallowInterceptTouchEvent(false);
					break;
			}

			if (handled.HasValue)
				return !handled.Value;

			return false;
		}

		bool HandleTouchInteractions(GestureStatus status, APointF point)
		{
			switch (status)
			{
				case GestureStatus.Started:
					return !ProcessTouchDown(point);
				case GestureStatus.Running:
					return !ProcessTouchMove(point);
				case GestureStatus.Completed:
					ProcessTouchUp();
					break;
			}

			_isTouchDown = false;

			return true;
		}

		bool ProcessTouchDown(APointF point)
		{
			if (_isSwiping || _isTouchDown || _contentView == null)
				return false;

			bool touchContent = TouchInsideContent(_contentView.Left + _contentView.TranslationX, _contentView.Top + _contentView.TranslationY, _contentView.Width, _contentView.Height, _context.ToPixels(point.X), _context.ToPixels(point.Y));

			if (touchContent)
				ResetSwipe();
			else
				ProcessTouchSwipeItems(point);

			_initialPoint = point;
			_isTouchDown = true;

			return true;
		}

		bool ProcessTouchMove(APointF point)
		{
			if (_contentView == null)
				return false;

			if (!_isSwiping)
			{
				_swipeDirection = SwipeDirectionHelper.GetSwipeDirection(new Point(_initialPoint.X, _initialPoint.Y), new Point(point.X, point.Y));
				RaiseSwipeStarted();
				_isSwiping = true;
			}

			if (!ValidateSwipeDirection())
				return false;

			_swipeOffset = GetSwipeOffset(_initialPoint, point);
			UpdateSwipeItems();

			if (Math.Abs(_swipeOffset) > double.Epsilon)
				Swipe();
			else
				ResetSwipe();

			return true;
		}

		bool ProcessTouchUp()
		{
			_isTouchDown = false;

			if (!_isSwiping)
				return false;

			_isSwiping = false;

			RaiseSwipeEnded();

			if (!ValidateSwipeDirection())
				return false;

			ValidateSwipeThreshold();

			return false;
		}

		bool TouchInsideContent(double x1, double y1, double x2, double y2, double x, double y)
		{
			if (x > x1 && x < (x1 + x2) && y > y1 && y < (y1 + y2))
				return true;

			return false;
		}

		bool ValidateSwipeDirection()
		{
			var swipeItems = GetSwipeItemsByDirection();
			return swipeItems != null;
		}

		float GetSwipeOffset(APointF initialPoint, APointF endPoint)
		{
			float swipeOffset = 0;

			switch (_swipeDirection)
			{
				case SwipeDirection.Left:
				case SwipeDirection.Right:
					swipeOffset = endPoint.X - initialPoint.X;
					break;
				case SwipeDirection.Up:
				case SwipeDirection.Down:
					swipeOffset = endPoint.Y - initialPoint.Y;
					break;
			}

			return swipeOffset;
		}

		void UpdateSwipeItems()
		{
			if (_contentView == null || _actionView != null)
				return;

			var items = GetSwipeItemsByDirection();

			if (items == null)
				return;
	
			_actionView = new LinearLayoutCompat(_context);

			using (var layoutParams = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent))
				_actionView.LayoutParameters = layoutParams;

			_actionView.Orientation = LinearLayoutCompat.Horizontal;

			int i = 0;
			foreach (var swipeItem in items)
			{
				var swipeButton = new AButton(_context)
				{
					Background = new ColorDrawable(swipeItem.BackgroundColor.ToAndroid()),
					Text = swipeItem.Text
				};

				var textColor = GetSwipeItemColor(swipeItem.BackgroundColor);
				swipeButton.SetTextColor(textColor.ToAndroid());

				_ = this.ApplyDrawableAsync(swipeItem, MenuItem.IconImageSourceProperty, Context, drawable =>
				{
					int h = _contentView.Height / 3;
					int w = _contentView.Height / 3;
					drawable.SetBounds(0, 0, w, h);

					drawable.SetColorFilter(textColor.ToAndroid(), PorterDuff.Mode.SrcAtop);
					swipeButton.SetCompoundDrawables(null, drawable, null, null);
				});

				swipeButton.SetOnTouchListener(null);

				_actionView.AddView(swipeButton);

				i++;
			}

			AddView(_actionView);
			_contentView?.BringToFront();

			_actionView.Layout(0, 0, _contentView.Width, _contentView.Height);
			LayoutSwipeItems();
		}

		void UpdateSwipeTransitionMode()
		{
			if (Element.IsSet(Specifics.SwipeTransitionModeProperty))
				_swipeTransitionMode = Element.OnThisPlatform().GetSwipeTransitionMode();
			else
				_swipeTransitionMode = SwipeTransitionMode.Reveal;
		}

		Color GetSwipeItemColor(Color backgroundColor)
		{
			var luminosity = 0.2126 * backgroundColor.R + 0.7152 * backgroundColor.G + 0.0722 * backgroundColor.B;

			return luminosity < 0.75 ? Color.White : Color.Black;
		}

		void LayoutSwipeItems()
		{
			if (_actionView == null)
				return;
	
			for (int i = 0; i < _actionView.ChildCount; i++)
			{
				var child = _actionView.GetChildAt(i);

				SwipeItems items = GetSwipeItemsByDirection();

				int swipeItemWidth;

				if (_swipeDirection == SwipeDirection.Left || _swipeDirection == SwipeDirection.Right)
					swipeItemWidth = items.Mode == SwipeMode.Execute ? _contentView.Width / items.Count : (int)_context.ToPixels(SwipeItemWidth);
				else
					swipeItemWidth = _contentView.Width / items.Count;

				switch (_swipeDirection)
				{
					case SwipeDirection.Left:
						child.Layout(_contentView.Width - ((i + 1) * swipeItemWidth), 0, _contentView.Width, _contentView.Height);
						break;
					case SwipeDirection.Right:
					case SwipeDirection.Up:
					case SwipeDirection.Down:
						child.Layout(i * swipeItemWidth, 0, (i + 1) * swipeItemWidth, _contentView.Height);
						break;
				}
			}
		}

		void DisposeSwipeItems()
		{
			if (_actionView != null)
			{
				RemoveView(_actionView);
				_actionView.Dispose();
				_actionView = null;
			}
		}

		void Swipe()
		{
			var offset = _context.ToPixels(ValidateSwipeOffset(_swipeOffset));

			if (_swipeTransitionMode == SwipeTransitionMode.Reveal)
			{
				switch (_swipeDirection)
				{
					case SwipeDirection.Left:
					case SwipeDirection.Right:
						_contentView.TranslationX = offset;
						break;
					case SwipeDirection.Up:
					case SwipeDirection.Down:
						_contentView.TranslationY = offset;
						break;
				}
			}

			if(_swipeTransitionMode == SwipeTransitionMode.Drag)
			{
				int actionSize;
				switch (_swipeDirection)
				{
					case SwipeDirection.Left:
						_contentView.TranslationX = offset;
						actionSize = (int)_context.ToPixels(Element.RightItems.Count * SwipeItemWidth);
						_actionView.TranslationX = actionSize - Math.Abs(offset);
						break;
					case SwipeDirection.Right:
						_contentView.TranslationX = offset;
						actionSize = (int)_context.ToPixels(Element.LeftItems.Count * SwipeItemWidth);
						_actionView.TranslationX = -actionSize + offset;
						break;
					case SwipeDirection.Up:
						_contentView.TranslationY = offset;
						actionSize = _contentView.Height;
						_actionView.TranslationY = actionSize - Math.Abs(offset);
						break;
					case SwipeDirection.Down:
						_contentView.TranslationY = offset;
						actionSize = _contentView.Height;
						_actionView.TranslationY = -actionSize + Math.Abs(offset);
						break;
				}
			}
		}

		void ResetSwipe()
		{
			switch (_swipeDirection)
			{
				case SwipeDirection.Left:
				case SwipeDirection.Right:
					_contentView.Animate().TranslationX(0).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() =>
					{
						DisposeSwipeItems();
						_isSwiping = false;
						_swipeThreshold = 0;
					}));
					break;
				case SwipeDirection.Up:
				case SwipeDirection.Down:
					_contentView.Animate().TranslationY(0).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() =>
					{
						DisposeSwipeItems();
						_isSwiping = false;
						_swipeThreshold = 0;
					}));
					break;
			}
		}

		void CompleteSwipe()
		{
			float swipeThreshold;

			var swipeItems = GetSwipeItemsByDirection();
			swipeThreshold = _context.ToPixels(GetSwipeThreshold(swipeItems));

			if (_swipeTransitionMode == SwipeTransitionMode.Reveal)
			{
				switch (_swipeDirection)
				{
					case SwipeDirection.Left:
						_contentView.Animate().TranslationX(-swipeThreshold).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() => { _isSwiping = false; }));
						break;
					case SwipeDirection.Right:
						_contentView.Animate().TranslationX(swipeThreshold).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() => { _isSwiping = false; }));
						break;
					case SwipeDirection.Up:
						_contentView.Animate().TranslationY(-swipeThreshold).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() => { _isSwiping = false; }));
						break;
					case SwipeDirection.Down:
						_contentView.Animate().TranslationY(swipeThreshold).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() => { _isSwiping = false; }));
						break;
				}
			}

			if (_swipeTransitionMode == SwipeTransitionMode.Drag)
			{
				int actionSize;
				switch (_swipeDirection)
				{
					case SwipeDirection.Left:
						_contentView.Animate().TranslationX(-swipeThreshold).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() => { _isSwiping = false; }));
						actionSize = (int)_context.ToPixels(Element.RightItems.Count * SwipeItemWidth);
						_actionView.Animate().TranslationX(actionSize - swipeThreshold).SetDuration(SwipeAnimationDuration);
						break;
					case SwipeDirection.Right:
						_contentView.Animate().TranslationX(swipeThreshold).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() => { _isSwiping = false; }));
						actionSize = (int)_context.ToPixels(Element.LeftItems.Count * SwipeItemWidth);
						_actionView.Animate().TranslationX(-actionSize + swipeThreshold).SetDuration(SwipeAnimationDuration);
						break;
					case SwipeDirection.Up:
						_contentView.Animate().TranslationY(-swipeThreshold).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() => { _isSwiping = false; }));
						actionSize = _contentView.Height;
						_actionView.Animate().TranslationY(actionSize - Math.Abs(swipeThreshold)).SetDuration(SwipeAnimationDuration);
						break;
					case SwipeDirection.Down:
						_contentView.Animate().TranslationY(swipeThreshold).SetDuration(SwipeAnimationDuration).WithEndAction(new Java.Lang.Runnable(() => { _isSwiping = false; }));
						actionSize = _contentView.Height;
						_actionView.Animate().TranslationY(-actionSize + Math.Abs(swipeThreshold)).SetDuration(SwipeAnimationDuration);
						break;
				}
			}
		}
  
		float ValidateSwipeOffset(float offset)
		{
			var swipeThreshold = GetSwipeThreshold();

			switch (_swipeDirection)
			{
				case SwipeDirection.Left:
					if (offset > 0)
						offset = 0;

					if (Math.Abs(offset) > swipeThreshold)
						return -swipeThreshold;
					break;
				case SwipeDirection.Right:
					if (offset < 0)
						offset = 0;

					if (Math.Abs(offset) > swipeThreshold)
						return swipeThreshold;
					break;
				case SwipeDirection.Up:
					if (offset > 0)
						offset = 0;

					if (Math.Abs(offset) > swipeThreshold)
						return -swipeThreshold;
					break;
				case SwipeDirection.Down:
					if (offset < 0)
						offset = 0;

					if (Math.Abs(offset) > swipeThreshold)
						return swipeThreshold;
					break;
			}

			return offset;
		}

		void ValidateSwipeThreshold()
		{
			var swipeThresholdPercent = 0.6 * GetSwipeThreshold();

			if (Math.Abs(_swipeOffset) >= swipeThresholdPercent)
			{
				var swipeItems = GetSwipeItemsByDirection();

				if (swipeItems == null)
					return;

				if (swipeItems.Mode == SwipeMode.Execute)
				{
					foreach (var swipeItem in swipeItems)
					{
						ExecuteSwipeItem(swipeItem);
					}

					if (swipeItems.SwipeBehaviorOnInvoked != SwipeBehaviorOnInvoked.RemainOpen)
						ResetSwipe();
				}
				else
					CompleteSwipe();
			}
			else
			{
				ResetSwipe();
			}
		}

		float GetSwipeThreshold()
		{
			if (Math.Abs(_swipeThreshold) > double.Epsilon)
				return _swipeThreshold;

			var swipeItems = GetSwipeItemsByDirection();
			_swipeThreshold = GetSwipeThreshold(swipeItems);

			return _swipeThreshold;
		}

		float GetSwipeThreshold(SwipeItems swipeItems)
		{
			float swipeThreshold = 0;

			if (swipeItems == null)
				return 0;

			float contentHeight = (float)_context.FromPixels(_contentView.Height);
			bool isHorizontal = _swipeDirection == SwipeDirection.Left || _swipeDirection == SwipeDirection.Right;

			if (swipeItems.Mode == SwipeMode.Reveal)
			{
				if (isHorizontal)
				{
					foreach (var swipeItem in swipeItems)
						swipeThreshold += SwipeItemWidth;
				}
				else
					swipeThreshold = (SwipeThreshold > contentHeight) ? contentHeight : SwipeThreshold;
			}
			else
			{
				if (isHorizontal)
					swipeThreshold = SwipeThreshold;
				else
					swipeThreshold = (SwipeThreshold > contentHeight) ? contentHeight : SwipeThreshold;
			}

			return swipeThreshold;
		}

		void ProcessTouchSwipeItems(APointF point)
		{
			var swipeItems = GetSwipeItemsByDirection();

			if (swipeItems == null || _actionView == null)
				return;

			for (int i = 0; i < _actionView.ChildCount; i++)
			{
				var swipeButton = _actionView.GetChildAt(i);

				var swipeItemX = swipeButton.Left / _density;
				var swipeItemY = swipeButton.Top / _density;
				var swipeItemHeight = swipeButton.Height / _density;
				var swipeItemWidth = swipeButton.Width / _density;


				if (TouchInsideContent(swipeItemX, swipeItemY, swipeItemWidth, swipeItemHeight, point.X, point.Y))
				{
					var swipeItem = swipeItems[i];

					ExecuteSwipeItem(swipeItem);

					if (swipeItems.SwipeBehaviorOnInvoked != SwipeBehaviorOnInvoked.RemainOpen)
						ResetSwipe();

					break;
				}
			}
		}
  
		void ExecuteSwipeItem(SwipeItem swipeItem)
		{
			if (swipeItem == null)
				return;

			ICommand cmd = swipeItem.Command;
			object parameter = swipeItem.CommandParameter;

			if (cmd != null && cmd.CanExecute(parameter))
				cmd.Execute(parameter);

			swipeItem.OnInvoked();
		}

		void OnClose(object sender)
		{
			if (sender == null)
				return;

			ResetSwipe();
		}

		void RaiseSwipeStarted()
		{
			var swipeStartedEventArgs = new SwipeStartedEventArgs(_swipeDirection, _swipeOffset);
			Element.SendSwipeStarted(swipeStartedEventArgs);
		}

		void RaiseSwipeEnded()
		{
			var swipeEndedEventArgs = new SwipeEndedEventArgs(_swipeDirection);
			Element.SendSwipeEnded(swipeEndedEventArgs);
		}
	}
}