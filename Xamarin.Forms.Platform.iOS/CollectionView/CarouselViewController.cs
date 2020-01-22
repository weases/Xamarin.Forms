using System;
using System.Collections;
using Foundation;
using UIKit;

namespace Xamarin.Forms.Platform.iOS
{
	public class CarouselViewController : ItemsViewController<CarouselView>
	{
		readonly CarouselView _carouselView;
		bool _viewInitialized;
		internal int currentNegative;
		internal int currentIndex;
		internal int currentMaxItemsCount;

		public CarouselViewController(CarouselView itemsView, ItemsViewLayout layout) : base(itemsView, layout)
		{
			_carouselView = itemsView;
			CollectionView.AllowsSelection = false;
			CollectionView.AllowsMultipleSelection = false;
		}

		protected override UICollectionViewDelegateFlowLayout CreateDelegator()
		{
			return new CarouselViewDelegator(ItemsViewLayout, this);
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			currentMaxItemsCount = Math.Max((currentIndex + 1), ItemsSource.ItemCount) + 1 + currentNegative;

			return _carouselView.Loop ? currentMaxItemsCount : base.GetItemsCount(collectionView, section);
		}


		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{

			currentIndex = _carouselView.Loop ? GetloopIndex(indexPath, currentIndex, currentNegative, ItemsSource.ItemCount) : indexPath.Row;

			var cell = base.GetCell(collectionView, NSIndexPath.FromItemSection(currentIndex, indexPath.LongSection));

			var element = (cell as CarouselTemplatedCell)?.VisualElementRenderer?.Element;
			if (element != null)
				VisualStateManager.GoToState(element, CarouselView.DefaultItemVisualState);
			return cell;

		}

		int GetloopIndex(NSIndexPath indexPath, int existingIndex, int existingNegativeIndex, int itemSourceCount)
		{
			var index = indexPath.Row % itemSourceCount;

			if (existingIndex == 0 && existingNegativeIndex > 0)
			{
				index = ItemsSource.ItemCount - (indexPath.Row % itemSourceCount) - existingNegativeIndex;
			}

			System.Diagnostics.Debug.WriteLine($"IndexPath {indexPath.Row} currentIndex {index} and {existingNegativeIndex}");
			return index;
		}

		// Here because ViewDidAppear (and associates) are not fired consistently for this class
		// See a more extensive explanation in the ItemsViewController.ViewWillLayoutSubviews method
		public override void ViewWillLayoutSubviews()
		{
			base.ViewWillLayoutSubviews();

			if (!_viewInitialized)
			{
				UpdateInitialPosition();

				_viewInitialized = true;
			}
		}

		protected override bool IsHorizontal => (_carouselView?.ItemsLayout as ItemsLayout)?.Orientation == ItemsLayoutOrientation.Horizontal;

		protected override string DetermineCellReuseId()
		{
			if (_carouselView.ItemTemplate != null)
			{
				return CarouselTemplatedCell.ReuseId;
			}
			return base.DetermineCellReuseId();
		}

		protected override void RegisterViewTypes()
		{
			CollectionView.RegisterClassForCell(typeof(CarouselTemplatedCell), CarouselTemplatedCell.ReuseId);
			base.RegisterViewTypes();
		}

		internal void TearDown()
		{
		}

		public override void DraggingStarted(UIScrollView scrollView)
		{
			_carouselView.SetIsDragging(true);
		}

		public override void DraggingEnded(UIScrollView scrollView, bool willDecelerate)
		{
			_carouselView.SetIsDragging(false);
		}

		internal void UpdateIsScrolling(bool isScrolling)
		{
			_carouselView.IsScrolling = isScrolling;
		}

		void UpdateInitialPosition()
		{
			if (_carouselView.CurrentItem != null)
			{
				int position = 0;

				var items = _carouselView.ItemsSource as IList;

				for (int n = 0; n < items?.Count; n++)
				{
					if (items[n] == _carouselView.CurrentItem)
					{
						position = n;
						break;
					}
				}

				var initialPosition = position;
				_carouselView.Position = initialPosition;
			}

			if (_carouselView.Position != 0)
				_carouselView.ScrollTo(_carouselView.Position, -1, ScrollToPosition.Center, false);
		}
	}
}