using System;

public class ZException : Exception {

    internal ZException(string message) : base(message) { }

    internal ZException(string message, Exception inner) : base(message, inner) { }
}
