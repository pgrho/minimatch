# Shipwreck.Minimatch

C# implementation of minimatch.

## Usage

- [NuGet](https://www.nuget.org/packages/Shipwreck.Minimatch)

1. Instantiate `MatcherFactory` and configure properties.
2. Call `MatcherFactory.Compile(string)` to retrieve delegate to match specified pattern.
3. The delegate will return `true` if matched. Otherwise `null`.

```csharp
using Shipwreck.Minimatch;

var factory = new MatcherFactory()
{
    AllowBackslash = true,
    IgnoreCase = true
};
Func<string, bool?> predicate = factory.Compile("**/*.cs");

predicate("test.cs") // true
predicate("foo/bar/baz.cs") // true
predicate("test.txt") // null
```

If the pattern starts with `!`, `Matcher` returns `false` or `null`.

```csharp
Func<string, bool?> negation = factory.Compile("!foo.cs");

negation("foo.cs") // false
negation("bar.cs") // null
```

`MatcherFactory.Compile` also supports `IEnumerable<string>` argument.
That overload will evaluate all patterns and returns last non-null result.

```csharp
Func<string, bool> complex = factory.Compile(new [] { "**/*.cs", "!**/foo.cs", "bar/foo.cs" });

complex("test.cs") // true
complex("test.txt") // false
complex("foo.cs") // false
complex("bar.cs") // true
complex("bar/foo.cs") // true
complex("baz/foo.cs") // false

```