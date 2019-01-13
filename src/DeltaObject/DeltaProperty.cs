using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeltaObject
{
    public class DeltaProperty<TValue>
    {
        private TValue _value;

        public bool IsSet { get; private set; }

        public TValue Value
        {
            get { return _value; }
            private set
            {
                IsSet = true;
                _value = value;
            }
        }

        internal void SetValue(object value)
        {
            if (value is JValue)
            {
                Value = ((JToken)value).ToObject<TValue>();
            }
            else if (value is JToken)
            {
                Value = JsonConvert.DeserializeObject<TValue>(value.ToString());
            }
            else
            {
                Value = (TValue)value;
            }
        }
    }
}