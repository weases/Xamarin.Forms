﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xamarin.Forms
{
	[ContentProperty(nameof(Children))]
	public class Storyboard : Timeline
	{
		Dictionary<VisualElement, List<Animation>> _animationsPerElement = new Dictionary<VisualElement, List<Animation>>();
		List<Animation> _runningAnimations = new List<Animation>();
		public TimelineCollection Children { get; }
		public static readonly BindableProperty TargetNameProperty = BindableProperty.CreateAttached("TargetName", typeof(VisualElement), typeof(Timeline), null,
			propertyChanged: (bindable, oldValue, newValue) =>
			{

				foreach (var item in (bindable as Storyboard)?.Children)
				{
					(bindable as Storyboard).SetChildInheritedBindingContext(item, (newValue as VisualElement).BindingContext);
				}

			});

		public static readonly BindableProperty TargetPropertyProperty = BindableProperty.CreateAttached("TargetProperty", typeof(BindableProperty), typeof(Timeline), null
			);

		public static void SetTarget(BindableObject bindable, VisualElement value)
		{
			bindable.SetValue(TargetNameProperty, value);
			SetInheritedBindingContext(bindable, value?.BindingContext);
		}

		public static VisualElement GetTarget(BindableObject bindable)
		{
			return (VisualElement)bindable.GetValue(TargetNameProperty);
		}

		public static void SetTargetProperty(BindableObject bindable, BindableProperty value)
		{
			bindable.SetValue(TargetPropertyProperty, value);
		}

		public static BindableProperty GetTargetProperty(BindableObject bindable)
		{
			return (BindableProperty)bindable.GetValue(TargetPropertyProperty);
		}

		public Storyboard()
		{
			Children = new TimelineCollection(this);
		}

		public void Add(Timeline animation)
		{
			Children.Add(animation);
		}


		public void Remove(Timeline animation)
		{
			animation.Parent = null;
			Children.Remove(animation);
		}

		public void Begin()
		{
			_animationsPerElement.Clear();
			_runningAnimations.Clear();
			var maxDuration = Duration;
			var defaultTarget = Storyboard.GetTarget(this);
			var defaultTargetProperty = Storyboard.GetTargetProperty(this);
			foreach (var item in Children)
			{
				if (item is Animation<Color> timeline)
				{
					var animationTarget = Storyboard.GetTarget(timeline);
					if (animationTarget == null)
						animationTarget = defaultTarget;

					var animationTargetProperty = Storyboard.GetTargetProperty(timeline);
					if (animationTargetProperty == null)
						animationTargetProperty = defaultTargetProperty;

					var initialValue = animationTarget.GetValue(animationTargetProperty);

					var finalValue = timeline.To;
					var newAnimation = new Animation(v =>
					{
						object newValue = null;

						if (timeline.From is IInterpolatable interpolatable)
						{
							newValue = interpolatable.InterpolateTo(initialValue, finalValue, v);
						}

						animationTarget.SetValue(animationTargetProperty, newValue);
					}, 0, 1, timeline.Easing, () => timeline?.Completed?.Invoke(this, new EventArgs()));

					//mainAnimation.Add(animation.BeginTime, 1, newAnimation);
					maxDuration = Math.Max(maxDuration, timeline.Duration);

					if (!_animationsPerElement.ContainsKey(animationTarget))
					{
						var list = new List<Animation>();
						list.Add(newAnimation);
						_animationsPerElement.Add(animationTarget, list);
					}
					else
					{
						_animationsPerElement[animationTarget].Add(newAnimation);
					}
				}

				if (item is DoubleAnimation doubleAnimation)
				{
					var animationTarget = Storyboard.GetTarget(doubleAnimation);
					if (animationTarget == null)
						animationTarget = defaultTarget;

					var animationTargetProperty = Storyboard.GetTargetProperty(doubleAnimation);
					if (animationTargetProperty == null)
						animationTargetProperty = defaultTargetProperty;

					
					//maxDuration = Math.Max(maxDuration, doubleAnimation.Duration);
					AddToInternalAnimationList(doubleAnimation, animationTarget, animationTargetProperty);
				}

			}

			foreach (var anim in _animationsPerElement)
			{
				var elementAnimation = new Animation();
				foreach (var animation in anim.Value)
				{
					elementAnimation.Add(0, 1, animation);

				}
				_runningAnimations.Add(elementAnimation);
				elementAnimation.Commit(anim.Key, "ChildAnimations", 16, maxDuration, finished: (v, c) => Completed?.Invoke(this, new EventArgs()));
			}
			//
		}

		void AddToInternalAnimationList(DoubleAnimation doubleAnimation, VisualElement animationTarget, BindableProperty animationTargetProperty, Animation newAnimation = null)
		{
			if (animationTarget == null)
				throw new InvalidOperationException("You didn't specify a Target");


			if (animationTarget == null)
				throw new InvalidOperationException("You didn't specify a Target");

			//not sure 
			SetInheritedBindingContext(doubleAnimation, animationTarget.BindingContext);

			var initialValue = (double)animationTarget.GetValue(animationTargetProperty);

			var finalValue = (double)doubleAnimation.To;
			if (newAnimation == null)
			{
				newAnimation = new Animation(v =>
				{
					animationTarget.SetValue(animationTargetProperty, v);
				}, initialValue, finalValue, Easing,
				() => doubleAnimation?.Completed?.Invoke(this, new EventArgs()));

			}

			//mainAnimation.Add(animation.BeginTime, 1, newAnimation);

			if (!_animationsPerElement.ContainsKey(animationTarget))
			{
				var list = new List<Animation>();
				list.Add(newAnimation);
				_animationsPerElement.Add(animationTarget, list);
			}
			else
			{
				_animationsPerElement[animationTarget].Add(newAnimation);
			}
		}

		public void End()
		{

		}


		public static string GetTargetName(object t)
		{
			throw new NotImplementedException();
		}

		//Storyboard.SetTarget(animOpacity, Control_);
		//  Storyboard.SetTarget(animTransform, Control_);
		//  Storyboard.SetTargetProperty(animOpacity, new PropertyPath("Opacity"));
		//  Storyboard.SetTargetProperty(animTransform, new PropertyPath(

		//public void Pause();
		//public void Resume();
		//public static void SetTarget(this Storyboard s, string target);
		//public static void SetTargetProperty(this Storyboard s, string property);
	}

	public interface IInterpolatable
	{
		object InterpolateTo(object from, object target, double interpolation);
	}

	public interface IInterpolatable<T> : IInterpolatable
	{
		T InterpolateTo(T from, T target, double interpolation);
	}

	public abstract class Animation<T> : Timeline where T : IInterpolatable<T>
	{
		public T From { get; set; }
		public T To { get; set; }
	}

	public class DoubleAnimation : Timeline
	{
		public static readonly BindableProperty FromProperty = BindableProperty.Create(nameof(From), typeof(double), typeof(DoubleAnimation), .0);

		public static readonly BindableProperty ToProperty = BindableProperty.Create(nameof(To), typeof(double), typeof(DoubleAnimation), .0);

		public double From
		{
			get { return (double)GetValue(FromProperty); }
			set { SetValue(FromProperty, value); }
		}

		public double To
		{
			get { return (double)GetValue(ToProperty); }
			set { SetValue(ToProperty, value); }
		}
	}

	public abstract class Timeline : VisualElement
	{
		const uint defaultDuration = 300;

		protected Timeline()
		{
			Duration = defaultDuration;
			Easing = Easing.SpringOut	;
		}

		public uint BeginTime { get; set; }

		public uint Duration { get; set; }

		public bool AutoReverse { get; set; }

		public Easing Easing { get; set; }

		public EventHandler Completed { get; set; }

	}

	public class TimelineCollection : IList<Timeline>
	{
		Storyboard _parent;
		public TimelineCollection(Storyboard parent)
		{
			_parent = parent;
		}
		List<Timeline> _timelines = new List<Timeline>();

		public Timeline this[int index] { get => _timelines[index]; set => _timelines[index] = value; }

		public int Count => _timelines.Count;

		public bool IsReadOnly => true;

		public void Add(Timeline item)
		{
			item.Parent = _parent;
			Element.SetInheritedBindingContext(item, _parent.BindingContext);

			item.PropertyChanged += AnimationPropertyChanged;


			_timelines.Add(item);
		}

		void AnimationPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == DoubleAnimation.ToProperty.PropertyName)
			{
				_parent.Begin();
			}
		}

		public void Clear()
		{
			_timelines.Clear();
		}

		public bool Contains(Timeline item)
		{
			return _timelines.Contains(item);
		}

		public void CopyTo(Timeline[] array, int arrayIndex)
		{
			_timelines.CopyTo(array, arrayIndex);
		}

		public IEnumerator<Timeline> GetEnumerator()
		{
			return _timelines.GetEnumerator();
		}

		public int IndexOf(Timeline item)
		{
			return _timelines.IndexOf(item);
		}

		public void Insert(int index, Timeline item)
		{
			_timelines.Insert(index, item);
		}

		public bool Remove(Timeline item)
		{
			return _timelines.Remove(item);
		}

		public void RemoveAt(int index)
		{
			_timelines.RemoveAt(index);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _timelines.GetEnumerator();
		}
	}
}
