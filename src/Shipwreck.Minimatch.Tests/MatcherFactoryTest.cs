﻿using Xunit;
using Xunit.Abstractions;

namespace Shipwreck.Minimatch;

public class MatcherFactoryTest
{
    private readonly ITestOutputHelper Out;

    public MatcherFactoryTest(ITestOutputHelper o) => Out = o;

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a", "A", null)]
    [InlineData("!a", "a", false)]
    [InlineData("!a", "!a", null)]
    [InlineData("#a", "a", null)]
    [InlineData("#a", "#a", null)]
    [InlineData(@"\#a", "#a", true)]
    [InlineData("a?c", "aac", true)]
    [InlineData("a?c", "abc", true)]
    [InlineData("a?c", "a/c", null)]
    [InlineData("a?c", "aaac", null)]
    [InlineData("a*c", "aac", true)]
    [InlineData("a*c", "abc", true)]
    [InlineData("a*c", "a/c", null)]
    [InlineData("a*c", "ac", true)]
    [InlineData("a*c", "aaac", true)]
    [InlineData("**", "hoge", true)]
    [InlineData("**", "hoge/", null)]
    [InlineData("**", "hoge/fuga", true)]
    [InlineData("**", "hoge/fuga/", null)]
    [InlineData("**", "hoge/fuga/piyo", true)]
    [InlineData("**/", "hoge", null)]
    [InlineData("**/", "hoge/", true)]
    [InlineData("**/", "hoge/fuga", null)]
    [InlineData("**/", "hoge/fuga/", true)]
    [InlineData("**/", "hoge/fuga/piyo", null)]
    [InlineData("**/*", "hoge/", null)]
    [InlineData("**/*", "hoge/fuga", true)]
    [InlineData("**/*", "hoge/fuga/", null)]
    [InlineData("**/*", "hoge/fuga/piyo", true)]
    public void TestSlashCaseSensitive(string pattern, string path, bool? result)
    {
        var m = new MatcherFactory().Create(pattern);

        if (m.Regex != null)
        {
            Out.WriteLine(m.Regex.ToString());
        }

        Assert.Equal(result, m.IsMatch(path));
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a", "A", true)]
    public void TestIgnoreCase(string pattern, string path, bool? result)
    {
        var m = new MatcherFactory()
        {
            IgnoreCase = true
        }.Create(pattern);

        if (m.Regex != null)
        {
            Out.WriteLine(m.Regex.ToString());
        }

        Assert.Equal(result, m.IsMatch(path));
    }

    [Theory]
    [InlineData(@"a/b", @"a/b", true)]
    [InlineData(@"a/b", @"a\b", true)]
    [InlineData(@"a\b", @"a/b", true)]
    [InlineData(@"a\b", @"a\b", true)]
    [InlineData(@"a/**/b", @"a/b", true)]
    [InlineData(@"a/**/b", @"a/c/b", true)]
    [InlineData(@"a/**/b", @"a/c/d/b", true)]
    [InlineData(@"a/**/b", @"a\b", true)]
    [InlineData(@"a/**/b", @"a\c\b", true)]
    [InlineData(@"a/**/b", @"a\c\d\b", true)]
    [InlineData(@"a\**\b", @"a/b", true)]
    [InlineData(@"a\**\b", @"a/c/b", true)]
    [InlineData(@"a\**\b", @"a/c/d/b", true)]
    public void TestBackslash(string pattern, string path, bool? result)
    {
        var m = new MatcherFactory()
        {
            AllowBackslash = true
        }.Create(pattern);

        if (m.Regex != null)
        {
            Out.WriteLine(m.Regex.ToString());
        }

        Assert.Equal(result, m.IsMatch(path));
    }

    [Theory]
    [InlineData("{hoge}", "hoge", true)]
    [InlineData("{hoge,fuga/*,**/piyo}", "hoge", true)]
    [InlineData("{hoge,fuga/*,**/piyo}", "fuga", null)]
    [InlineData("{hoge,fuga/*,**/piyo}", "fuga/", null)]
    [InlineData("{hoge,fuga/*,**/piyo}", "fuga/hoge", true)]
    [InlineData("{hoge,fuga/*,**/piyo}", "piyo", true)]
    [InlineData("{hoge,fuga/*,**/piyo}", "hoge/piyo", true)]
    [InlineData("{hoge,fuga/*,**/piyo}", "hoge/fuga/piyo", true)]
    public void TestBrace(string pattern, string path, bool? result)
    {
        var m = new MatcherFactory().Create(pattern);

        if (m.Regex != null)
        {
            Out.WriteLine(m.Regex.ToString());
        }

        Assert.Equal(result, m.IsMatch(path));
    }

    [Theory]
    [InlineData("hoge?(fuga)piyo", "hogepiyo", true)]
    [InlineData("hoge?(fuga)piyo", "hogefugapiyo", true)]
    [InlineData("hoge?(fuga)piyo", "hogefugafugapiyo", null)]
    [InlineData("hoge*(fuga)piyo", "hogepiyo", true)]
    [InlineData("hoge*(fuga)piyo", "hogefugapiyo", true)]
    [InlineData("hoge*(fuga)piyo", "hogefugafugapiyo", true)]
    [InlineData("hoge+(fuga)piyo", "hogepiyo", null)]
    [InlineData("hoge+(fuga)piyo", "hogefugapiyo", true)]
    [InlineData("hoge+(fuga)piyo", "hogefugafugapiyo", true)]
    [InlineData("hoge@(fuga)piyo", "hogepiyo", null)]
    [InlineData("hoge@(fuga)piyo", "hogefugapiyo", true)]
    [InlineData("hoge@(fuga)piyo", "hogefugafugapiyo", null)]
    [InlineData("hoge!(fuga)piyo", "hogepiyo", true)]
    [InlineData("hoge!(fuga)piyo", "hogefugapiyo", null)]
    [InlineData("hoge!(fuga)piyo", "hogefugafugapiyo", true)]
    [InlineData("hoge?(foo|bar)piyo", "hogepiyo", true)]
    [InlineData("hoge?(foo|bar)piyo", "hogefoopiyo", true)]
    [InlineData("hoge?(foo|bar)piyo", "hogebarpiyo", true)]
    [InlineData("hoge?(foo|bar)piyo", "hogebazpiyo", null)]
    [InlineData("hoge!(foo|bar)piyo", "hogepiyo", true)]
    [InlineData("hoge!(foo|bar)piyo", "hogefoopiyo", null)]
    [InlineData("hoge!(foo|bar)piyo", "hogebarpiyo", null)]
    [InlineData("hoge!(foo|bar)piyo", "hogebazpiyo", true)]
    public void TestParenthesis(string pattern, string path, bool? result)
    {
        var m = new MatcherFactory().Create(pattern);

        if (m.Regex != null)
        {
            Out.WriteLine(m.Regex.ToString());
        }

        Assert.Equal(result, m.IsMatch(path));
    }

    [Theory]
    [InlineData("{1..3}", 1, 3)]
    [InlineData("{1..100}", 1, 100)]
    [InlineData("{11..13}", 11, 13)]
    [InlineData("{11..23}", 11, 23)]
    [InlineData("{11..33}", 11, 33)]
    [InlineData("{111..333}", 111, 333)]
    [InlineData("{189..310}", 189, 310)]
    [InlineData("{198..301}", 198, 301)]
    [InlineData("{199..300}", 199, 300)]
    public void TestNumberRange(string pattern, int min, int max)
    {
        var m = new MatcherFactory().Create(pattern);

        if (m.Regex != null)
        {
            Out.WriteLine(m.Regex.ToString());
        }

        for (var n = 1; n < 10000; n++)
        {
            var v = n.ToString();
            Assert.Equal(min <= n && n <= max ? true : (bool?)null, m.IsMatch(v));
        }
    }
}