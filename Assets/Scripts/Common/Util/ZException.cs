public class ZException : System.Exception {

    internal ZException(string message) : base(message) { }

    internal ZException(string message, System.Exception inner) : base(message, inner) { }

    internal ZException(string message, params object[] objs) : base(string.Format(message, objs)) { }
}
