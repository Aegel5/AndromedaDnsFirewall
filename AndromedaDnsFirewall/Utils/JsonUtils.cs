using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Text.Json.JsonElement;

namespace AndromedaDnsFirewall.Utils
{
    class JWrite {
        Dictionary<string, object> root = new();

        public object this[string key] {
            set {
                root[key] = value;
            }
        }
        public Dictionary<string, object> obj(string key) {
            if (root.TryGetValue(key, out object value)) {
                var val = value as Dictionary<string, object>;
                if (val == null)
                    throw new Exception("bad type");
                return val;
            }
            var cur = new Dictionary<string, object>();
            root[key] = cur;
            return cur;
        }
        public override string ToString() {
            return System.Text.Json.JsonSerializer.Serialize(root);
        }
    }
    public class JRead : IDisposable, IEnumerable<JRead> {

        public JsonElement Cur { get; private set; }
        public bool IsNull => Cur.ValueKind == JsonValueKind.Null || Cur.ValueKind == JsonValueKind.Undefined;
        public bool IsContainer => IsObject || Cur.ValueKind == JsonValueKind.Array;

        public bool IsObject => Cur.ValueKind == JsonValueKind.Object;

        public bool IsList => Cur.ValueKind == JsonValueKind.Array;

        public bool IsBoolAndTrue => Cur.ValueKind == JsonValueKind.True;

        JsonDocument doc = null;

        public void Dispose() {
            if (doc != null)
                doc.Dispose();
        }

        private JRead(JsonElement cur) {
            this.Cur = cur;
        }
        private JRead() {
        }

        public override string ToString() {
            return Cur.ToString();
        }

        public void StealDocFrom(JRead other) {
            if (other.doc == null)
                throw new Exception("doc is null");
            doc = other.doc;
            other.doc = null;
        }


        public static JRead Parse(string json, JsonDocumentOptions opt = default) {
            JsonDocument doc = JsonDocument.Parse(json, opt);
            var res = new JRead(doc.RootElement);
            res.doc = doc;
            return res;
        }

        public IEnumerable<(string, JRead)> EnumKeyVal() {
            foreach (var elem in Cur.EnumerateObject()) {
                yield return (elem.Name, new JRead(elem.Value));
            }
            yield break;
        }

        public IEnumerable<JRead> EnumList() {
            foreach (var elem in Cur.EnumerateArray()) {
                yield return new JRead(elem);
            }
            yield break;
        }

        public JRead this[string key] {
            get {
                if (!Cur.TryGetProperty(key, out var value))
                    return new JRead();
                return new JRead(value);
            }
        }

        public JRead this[int key] {
            get => new JRead(Cur[key]);
        }

        public ArrayEnumerator InnerArr() { 
            return Cur.EnumerateArray();
        }

        // get from self

        public string STR => Cur.STR();
        public string RAW => Cur.RAW();
        public decimal DEC => Cur.DEC();
        public long LONG => Cur.LONG();
        public bool BOOL => Cur.GetBoolean();

        public bool TryLong(string field, out long val) {
            val = 0;
            var tok = this[field];
            if (tok.IsNull) return false;
            val = tok.LONG;
            return true;
        }

        public bool TryStr(string field, out string val) {
            val = "";
            var tok = this[field];
            if (tok.IsNull) return false;
            val = tok.STR;
            return true;
        }


        // quick get from object

        public long Long(string name, long? def = null) {
            var tok = this[name];
            if (tok.IsNull) return def ?? throw new Exception($"field {name} not found");
            return tok.LONG;
        }
        public string Str(string name, string def = null) {
            var tok = this[name];
            if (tok.IsNull) return def ?? throw new Exception($"field {name} not found");
            return tok.STR;
        }

        public decimal Dec(string name, decimal? def = null) {
            var tok = this[name];
            if (tok.IsNull) return def ?? throw new Exception($"field {name} not found");
            return tok.DEC;
        }

        public bool Bool(string name, bool? def = null) {
            var tok = this[name];
            if (tok.IsNull) return def ?? throw new Exception($"field {name} not found");
            return tok.BOOL;
        }

        public IEnumerator<JRead> GetEnumerator() {
            return EnumList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new Exception("unssupport");
        }
    }

    public static class JsonExtensions2 {


        public static decimal DEC(this JsonElement tok) {
            if (tok.ValueKind == JsonValueKind.Number)
                return tok.GetDecimal();
            else if (tok.ValueKind == JsonValueKind.String)
            {
                return Decimal.Parse(tok.GetString().Replace(',', '.'), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            throw new Exception("wrong type for decimal");
        }

        public static long LONG(this JsonElement tok) {
            if (tok.ValueKind == JsonValueKind.Number)
                return tok.GetInt64();
            else if (tok.ValueKind == JsonValueKind.String)
                return long.Parse(tok.GetString());
            throw new Exception("wrong type for long");
        }

        public static string RAW(this JsonElement tok) {
            return tok.GetRawText();
        }

        public static string STR(this JsonElement tok) {
            if (tok.ValueKind == JsonValueKind.Number)
                return tok.GetInt64().ToString();
            else if (tok.ValueKind == JsonValueKind.String)
                return tok.GetString();
            throw new Exception("wrong type for string");
        }
    }
}
