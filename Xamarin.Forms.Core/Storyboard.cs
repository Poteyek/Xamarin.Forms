using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Linq;
using System.Reflection;

namespace Xamarin.Forms
{
	[ContentProperty(nameof(Children))]
	public class Storyboard : Timeline
	{
		public string Name { get; internal set; }

		public TimelineCollection Children { get; }

		public static readonly BindableProperty TargetNameProperty = BindableProperty.CreateAttached("TargetName", typeof(VisualElement), typeof(Timeline), null);

		public static readonly BindableProperty TargetPropertyProperty = BindableProperty.CreateAttached("TargetProperty", typeof(BindableProperty), typeof(Timeline), null);

		public static void SetTarget(BindableObject bindable, VisualElement value)
		{
			bindable.SetValue(TargetNameProperty, value);
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
			Children = new TimelineCollection();
			Name = new Guid().ToString("B");
		}

		public void Add(Timeline animation)
		{
			Children.Add(animation);
		}

		public void Remove(Timeline animation)
		{
			Children.Remove(animation);
		}

		public void Begin()
		{
			//TODO: Move to CreateAnimations
			uint maximalDuration = 0;
			if (Duration > 0)
			{
				maximalDuration = AutoReverse ? 2 * Duration : Duration;
			} else
			{
				maximalDuration = GetMaxDurationFromChildren(Children);
			}

			var animation = CreateAnimations(Children, maximalDuration, AutoReverse, Duration);

			animation.Commit(this, Name, 16, Duration, Easing, (f, v) => Completed.Invoke(this, EventArgs.Empty));
		}

		uint GetMaxDurationFromChildren(TimelineCollection timelines)
		{
			return timelines.ToArray().Max(x => x.AutoReverse ? 2 * (x.BeginTime + x.Duration) : x.BeginTime + x.Duration);
		}

		Animation CreateAnimations(TimelineCollection timelines, uint maximalDuration, bool autoRevers, uint storyboardDuration)
		{
			var storyboardAnimation = new Animation();

			foreach (var timeline in Children)
			{
				var target = GetTarget(timeline);
				if (target == null)
				{
					continue;
				}

				var targetProperty = GetTargetProperty(timeline);
				if (targetProperty == null)
				{
					continue;
				}

				var beginTime = timeline.BeginTime > 0 ? (double)timeline.BeginTime / maximalDuration : 0;
				var duration = storyboardDuration > 0 ? storyboardDuration : (double)timeline.Duration / maximalDuration;

				if (timeline is DoubleAnimation doubleAnimation)
				{
					if (targetProperty.ReturnType != typeof(double))
					{
						throw new InvalidCastException();
					}

					var animation = CreateAnimationFromDoubleAnimation(target, targetProperty, doubleAnimation.From, doubleAnimation.To, doubleAnimation.Easing, () => doubleAnimation.Completed.Invoke(target, EventArgs.Empty));

					storyboardAnimation.Add(beginTime, duration, animation);

					if (doubleAnimation.AutoReverse)
					{
						var animationReverted = CreateAnimationFromDoubleAnimation(target, targetProperty, doubleAnimation.To, doubleAnimation.From, doubleAnimation.Easing, () => doubleAnimation.Completed.Invoke(target, EventArgs.Empty));

						var newBeginTime = (double)timeline.BeginTime + timeline.Duration;

						storyboardAnimation.Add(newBeginTime, duration, animation);
					}
				}
				else if(timeline.IsSubclassOfGenericAnimation())
				{
					var animationType = timeline.GetGenericTypeOfAnimation();

					var interpolableObject = (IInterpolatable)Activator.CreateInstance(animationType);

					if (interpolableObject == null)
					{
						continue;
					}

					//var animation = CreateAnimationFromGenericAnimation(target, targetProperty, )

					//TODO: Implement Animation<T>
				}
			}

			return storyboardAnimation;
		}

		Animation CreateAnimationFromGenericAnimation(VisualElement target, BindableProperty targetProperty, Easing easing, Action completed, Func<double, object> interpolateTo)
		{
			void UpdateProperty(WeakReference<VisualElement> weakTarget, double f, Func<double, object> interpolateToInternal)
			{
				if (weakTarget.TryGetTarget(out VisualElement v))
				{
					var objValue = interpolateToInternal(f);
					v.SetValue(targetProperty, objValue);
				}
			}

			var wTarget = new WeakReference<VisualElement>(target);

			return new Animation(x => UpdateProperty(wTarget, x, interpolateTo), easing: easing, finished: completed);
		}

		Animation CreateAnimationFromDoubleAnimation(VisualElement target, BindableProperty targetProperty, double from, double to, Easing easing, Action completed)
		{
			void UpdateProperty(WeakReference<VisualElement> weakTarget, double f)
			{
				if (weakTarget.TryGetTarget(out VisualElement v))
				{
					v.SetValue(targetProperty, f);
				}
			}

			var wTarget = new WeakReference<VisualElement>(target);

			return new Animation(x => UpdateProperty(wTarget, x), from, to, easing, completed);
		}

		public void End()
		{
			this.AbortAnimation(Name);
		}

		public void Pause()
		{
			//TODO: Implement
			throw new NotImplementedException();
		}

		public void Resume()
		{
			//TODO: Extend Animation API (There is no Resume)
			//TODO: Implement
			throw new NotImplementedException();
		}

		public void Loop()
		{
			//TODO: Implement
			throw new NotImplementedException();
		}
	}

	public static class StoryboardExtensions
	{
		public static bool IsSubclassOfGenericAnimation(this Timeline timeline)
		{
#if NETSTANDARD1_0
			
			var toCheck = timeline.GetType();
			while (toCheck != null && toCheck != typeof(object))
			{
				var typeInfo = toCheck.GetTypeInfo(); 
				var cur = typeInfo.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (typeof(Animation<>) == cur)
				{
					return true;
				}
				toCheck = typeInfo.BaseType;
			}
			return false;
#else
			var toCheck = timeline.GetType();
			while (toCheck != null && toCheck != typeof(object))
			{
				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (typeof(Animation<>) == cur)
				{
					return true;
				}
				toCheck = toCheck.BaseType;
			}
			return false;
#endif
		}

		public static Type GetGenericTypeOfAnimation(this Timeline timeline)
		{
#if NETSTANDARD1_0
			var typeInfo = timeline.GetType().GetTypeInfo();
			var args = typeInfo.IsGenericTypeDefinition 
						? typeInfo.GenericTypeParameters 
						: typeInfo.GenericTypeArguments;
			return args.FirstOrDefault();
#else
			var args = timeline.GetType().GetGenericArguments();
			return args.FirstOrDefault();
#endif
		}

		static Task<bool> AnimateTo(this VisualElement view, double start, double end, string name,
			Action<VisualElement, double> updateAction, uint length = 250, Easing easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;

			var tcs = new TaskCompletionSource<bool>();

			var weakView = new WeakReference<VisualElement>(view);

			void UpdateProperty(double f)
			{
				if (weakView.TryGetTarget(out VisualElement v))
				{
					updateAction(v, f);
				}
			}

			new Animation(UpdateProperty, start, end, easing).Commit(view, name, 16, length, finished: (f, a) => tcs.SetResult(a));

			return tcs.Task;
		}
	}

	public abstract class Timeline : VisualElement
	{
		const uint defaultDuration = 4000;
		protected Timeline()
		{
			Duration = defaultDuration;
			Easing = Easing.Linear;
		}

		public uint BeginTime { get; set; }

		public uint Duration { get; set; }

		public bool AutoReverse { get; set; }

		public Easing Easing { get; set; }

		public EventHandler Completed { get; set; }
	}

	public class TimelineCollection : List<Timeline>
	{
	}

	public class Animation<T> : Timeline where T : IInterpolatable<T>
	{
		public T From { get; set; }
		public T To { get; set; }
	}

	public class DoubleAnimation : Timeline
	{
		public double From { get; set; }
		public double To { get; set; }
	}

	public class ColorAnimation : Animation<Color>
	{		
	}
}