namespace Xamarin.Forms
{
	public interface IInterpolatable
	{
		object InterpolateTo(object from, object target, double interpolation);
	}

	public interface IInterpolatable<T> : IInterpolatable
	{
		T InterpolateTo(T from, T target, double interpolation);
	}
}
