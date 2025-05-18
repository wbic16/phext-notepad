namespace PhextNotepad
{
    public class TypedCoordinate : IComparable
    {
        private short _value;

        public TypedCoordinate(short value)
        {
            _value = value;
        }

        public int CompareTo(object? obj)
        {
            var other = obj as TypedCoordinate;
            if (other == null) { return 1; }

            return _value.CompareTo(other._value);
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public static implicit operator short(TypedCoordinate value)
        {
            return value._value;
        }
    }
}
