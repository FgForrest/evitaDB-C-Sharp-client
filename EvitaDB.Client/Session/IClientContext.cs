using Client.Utils;

namespace Client.Session;

public interface IClientContext
{
    private static readonly ThreadLocal<Stack<Context>> CurrentClientContext = new();

    public void ExecuteWithClientAndRequestId(string clientId, string requestId, ThreadStart lambda)
    {
        Stack<Context>? context = CurrentClientContext.Value;
        try
        {
            if (context == null)
            {
                context = new Stack<Context>();
                CurrentClientContext.Value = context;
            }

            context.Push(new Context(clientId, requestId));
            lambda.Invoke();
        }
        finally
        {
            context.Pop();
        }
    }

    public void ExecuteWithClientId(string clientId, ThreadStart lambda)
    {
        Stack<Context>? context = CurrentClientContext.Value;
        try
        {
            if (context == null)
            {
                context = new Stack<Context>();
                CurrentClientContext.Value = context;
            }

            context.Push(new Context(clientId, null));
            lambda.Invoke();
        }
        finally
        {
            context.Pop();
        }
    }

    public void ExecuteWithRequestId(string requestId, ThreadStart lambda)
    {
        Stack<Context>? context = CurrentClientContext.Value;
        try
        {
            Assert.IsTrue(!(context == null || !context.Any()),
                "When changing the request ID, the client ID must be set first!");
            context.Push(new Context(context.Peek().ClientId, requestId));
            lambda.Invoke();
        }
        finally
        {
            context.Pop();
        }
    }

    public T ExecuteWithClientAndRequestId<T>(string clientId, string requestId, Func<T> lambda)
    {
        Stack<Context>? context = CurrentClientContext.Value;
        try
        {
            if (context == null)
            {
                context = new Stack<Context>();
                CurrentClientContext.Value = context;
            }

            context.Push(new Context(clientId, requestId));
            return lambda.Invoke();
        }
        finally
        {
            context.Pop();
        }
    }

    public T ExecuteWithClientId<T>(string clientId, Func<T> lambda)
    {
        Stack<Context>? context = CurrentClientContext.Value;
        try
        {
            if (context == null)
            {
                context = new Stack<Context>();
                CurrentClientContext.Value = context;
            }

            context.Push(new Context(clientId, null));
            return lambda.Invoke();
        }
        finally
        {
            context.Pop();
        }
    }

    public T ExecuteWithRequestId<T>(string requestId, Func<T> lambda)
    {
        Stack<Context>? context = CurrentClientContext.Value;
        try
        {
            Assert.IsTrue(!(context == null || !context.Any()),
                "When changing the request ID, the client ID must be set first!");
            context.Push(new Context(context.Peek().ClientId, requestId));
            return lambda.Invoke();
        }
        finally
        {
            context.Pop();
        }
    }

    public string? GetClientId()
    {
        return CurrentContext?.ClientId;
    }

    public string? GetRequestId()
    {
        return CurrentContext?.RequestId;
    }
    
    private static Context? CurrentContext => CurrentClientContext.Value?.TryPeek(out Context? result) == true ? result : null;

    private record Context(string ClientId, string? RequestId);
}