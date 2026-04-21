using System.Collections;

namespace GVisionWpf.Models.Visions
{
    public class Result<T>
    {
        public EResultType Type;
        public T? Value;

        public Result()
        {
            this.Type = EResultType.Good;
            this.Value = default;
        }

        public Result(EResultType type, T? value)
        {
            this.Type = type;
            this.Value = value;
        }

        public override string ToString()
        {
            string resultStatus = this.Type == EResultType.Good ? "OK" : "NG";

            if (this.Value is string stringValue)
            {
                return $"{stringValue} ({resultStatus})";
            }

            if (this.Value is not IEnumerable enumerable)
            {
                return $"{this.Value?.ToString() ?? "null"} ({resultStatus})";
            }

            var items = new List<string>();
            foreach (object? item in enumerable)
            {
                items.Add(item?.ToString() ?? "null");
            }
            string listContents = string.Join(", ", items);

            return $"[{listContents}] ({resultStatus})";
        }

        public static implicit operator Result<T>(HObject v)
        {
            throw new NotImplementedException();
        }

    }
}