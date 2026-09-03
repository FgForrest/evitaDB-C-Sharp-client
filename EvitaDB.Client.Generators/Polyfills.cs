// Source generators must target netstandard2.0, because the compiler loads them into its own process.
// That framework predates several types the modern C# compiler expects to find when certain language
// features are used, so they are declared here. Declaring them is the documented approach - the compiler
// only requires the type to exist, and an internal declaration in this assembly satisfies it without
// affecting anyone who references the generator.

#if !NET5_0_OR_GREATER

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Required by the compiler to declare <c>init</c>-only setters, which <c>record</c> declarations use.
/// Without it every positional record in this project fails with CS0518.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal static class IsExternalInit;

#endif
