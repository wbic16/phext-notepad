namespace TerseNotepad
{
    public class TypedCoordinate
    {
        private short _value;

        public TypedCoordinate(short value)
        {
            _value = value;
        }

        public static implicit operator short(TypedCoordinate value)
        {
            return value._value;
        }
    }
}
