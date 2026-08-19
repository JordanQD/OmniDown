using System;
using System.Net;

namespace OmniDown.Services.Rpc;

public enum Aria2RpcFailureKind
{
    Unavailable,
    Timeout,
    HttpError,
    RpcRejected,
    InvalidResponse
}

public sealed class Aria2RpcException : Exception
{
    public Aria2RpcException(
        Aria2RpcFailureKind failureKind,
        string message,
        Exception? innerException = null,
        int? rpcCode = null,
        HttpStatusCode? httpStatusCode = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        RpcCode = rpcCode;
        HttpStatusCode = httpStatusCode;
    }

    public Aria2RpcFailureKind FailureKind { get; }

    public int? RpcCode { get; }

    public HttpStatusCode? HttpStatusCode { get; }
}
