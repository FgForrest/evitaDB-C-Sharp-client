using System.Text;
using EvitaDB.Client.Exceptions;

namespace EvitaDB.Storefront.Services;

/// <summary>
/// Renders an exception into something diagnosable.
///
/// The driver's <see cref="IEvitaError"/> types carry two messages: a sanitized <c>PublicMessage</c> - which
/// is what <c>Exception.Message</c> returns - and a <c>PrivateMessage</c> holding the actual cause. Showing
/// only <c>.Message</c> produces useless text like "Unexpected internal Evita error occurred.", so this
/// unwraps the private message and the whole inner-exception chain.
/// </summary>
public static class ErrorDetail
{
    public static string Describe(Exception exception)
    {
        StringBuilder builder = new();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (builder.Length > 0)
            {
                builder.Append("\n  caused by: ");
            }
            builder.Append(current.GetType().Name).Append(": ");

            // the private message is the informative one; fall back to Message when they are identical
            string message = current is IEvitaError evitaError && !string.IsNullOrWhiteSpace(evitaError.PrivateMessage)
                ? evitaError.PrivateMessage
                : current.Message;
            builder.Append(message);
        }
        return builder.ToString();
    }

    /// <summary>
    /// Writes the full detail, including the stack trace, to the browser console - Blazor WebAssembly maps
    /// <see cref="Console"/> output there. The UI shows the condensed form from <see cref="Describe"/>.
    /// </summary>
    public static void LogToConsole(string context, Exception exception)
    {
        Console.WriteLine($"[evitaShop] {context}: {Describe(exception)}");
        Console.WriteLine(exception.ToString());
    }
}
