namespace MyLibrary.Win32
{
    public class SelectItem<T>
    {
        public SelectItem()
        {

        }

        public SelectItem(T value, string description)
        {
            Value = value;
            Description = description;
        }

        public T Value { get; set; }
        public string Description { get; set; }

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
