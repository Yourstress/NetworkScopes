
namespace NetworkScopes
{
	public class BindableValue<T>
	{
		public bool hasValue { get; private set; }
		public T value { get; private set; }
		
		public delegate void ValueFetchedDelegate(T value);
		public event ValueFetchedDelegate OnValueFetched = delegate {};

		public void SetValue(T value)
		{
			this.hasValue = true;
			this.value = value;
			OnValueFetched(value);
		}

		public void Bind(ValueFetchedDelegate onValueFetched)
		{
			if (hasValue)
				onValueFetched(value);

			OnValueFetched += onValueFetched;
		}

		public void Unbind(ValueFetchedDelegate onValueFetched)
		{
			OnValueFetched -= onValueFetched;
		}

		internal void ValueUpdated()
		{
			OnValueFetched(value);
		}
	}
}
