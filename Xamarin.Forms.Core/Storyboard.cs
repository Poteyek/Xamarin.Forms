using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

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
			var storyboardAnimation = new Animation();

			void UpdateProperty(WeakReference<VisualElement> weakTarget, BindableProperty targetProperty, double f)
			{
				if (weakTarget.TryGetTarget(out VisualElement v))
				{
					v.SetValue(targetProperty, f);
				}
			}

			foreach (var timeline in Children)
			{
				var target = GetTarget(timeline) ?? timeline;
				var weakTarget = new WeakReference<VisualElement>(target);

				if (!weakTarget.TryGetTarget(out var targetView))
				{
					continue;
				}
				var targetProperty = GetTargetProperty(targetView);

				if (targetProperty == null)
				{
					continue;
				}


				var timelineAnimation = new Animation(v => UpdateProperty(weakTarget, targetProperty, v), easing: timeline.Easing, finished: () => timeline.Completed.Invoke(timeline, EventArgs.Empty));
				storyboardAnimation.Add(timeline.BeginTime, timeline.BeginTime + timeline.Duration, timelineAnimation);
			}

			storyboardAnimation.Commit(this, Name, 16, Duration, Easing, (f, v) => Completed.Invoke(this, EventArgs.Empty), () => AutoReverse);
		}

		public void End()
		{
			//TODO: Implement
			throw new NotImplementedException();
		}

		public void Pause()
		{
			//TODO: Implement
			throw new NotImplementedException();
		}

		public void Resume()
		{
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
}