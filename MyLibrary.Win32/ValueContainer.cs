namespace MyLibrary.Win32
{
    public class ValueContainer<T> : IValueContainer
    {
        public ValueContainer()
        {

        }

        public ValueContainer(T value, string description)
        {
            Value = value;
            Description = description;
        }

        public T Value { get; set; }
        public string Description { get; set; }

        public object GetValue()
        {
            return Value;
        }

        public override string ToString()
        {
            if (Description == null)
            {
                return Value.ToString();
            }
            return Description;
        }
    }
}
