﻿using System.Collections.Generic;
using FluentAssertions;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.Core;

namespace WebEssentialsTests
{
    [TestClass]
    public class MarkdownParserTests
    {
        static List<string> ParseCodeBlocks(string markdown)
        {
            var retVal = new List<string>();
            var parser = new MarkdownParser(new CharacterStream(markdown));
            parser.ArtifactFound += (s, e) => retVal.Add(markdown.Substring(e.Artifact.InnerRange.Start, e.Artifact.InnerRange.Length));
            parser.Parse();
            return retVal;
        }


        [TestMethod]
        public void TestInlineCodeBlocks()
        {
            ParseCodeBlocks(@"Hi there! `abc` Bye!").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@"Hi there! `abc``def` Bye!").Should().Equal(new[] { "abc", "def" });
            ParseCodeBlocks(@"a` b `c").Should().Equal(new[] { " b " });
            ParseCodeBlocks(@"`abc`").Should().Equal(new[] { "abc" });
            ParseCodeBlocks("\n`abc`\n").Should().Equal(new[] { "abc" });

            ParseCodeBlocks(@"a ``abc`").Should().Equal(new[] { "" });
            ParseCodeBlocks(@"a ``v").Should().Equal(new[] { "" });
            ParseCodeBlocks(@"a \`v`").Should().BeEmpty();
        }

        [TestMethod]
        public void TestIndentedCodeBlocks()
        {
            ParseCodeBlocks(@"Hi there!

    abc
Bye!").Should().Equal(new[] { "abc" });

            ParseCodeBlocks(@"Hi there!

    
Bye!").Should().Equal(new[] { "" }, "Empty lines become empty artifacts");
            ParseCodeBlocks(@"
Three lines of four spaces each (boundary, then code):
    
    
Bye!").Should().Equal(new[] { "" }, "Unlimited whitespace is allowed in the block boundary line");
            ParseCodeBlocks(@"
Five spaces, no other content:

     
Five spaces, surrounded by content:

    a
     
    b
Bye!").Should().Equal(new[] { " ", "a", " ", "b" }, "Whitespace-only code is reported");

            ParseCodeBlocks(@"Hi there!

    abc
    def
Bye!").Should().Equal(new[] { "abc", "def" });
            //            ParseCodeBlocks(@"Hi there!
            // 1. List!
            //
            //        abc
            // * List!
            //
            //        def
            //
            // - More
            //    Not code!
            //Bye!").Should().Equal(new[] { "abc", "def" });
            //ParseCodeBlocks(" 1. abc\n\n  \t  Code!").Should().Equal(new[] { "Code!" });

            // ParseCodeBlocks("Hi there!\n\tabc\nBye!").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@"Hi there!
    abc
Bye!").Should().BeEmpty();
            ParseCodeBlocks(@"
    abc
").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@"
    abc").Should().Equal(new[] { "abc" });
        }

        [TestMethod]
        public void TestQuotedIndentedCodeBlocks()
        {
            ParseCodeBlocks(@"Hi there!

>     abc
Bye!").Should().Equal(new[] { "abc" }, "Unquoted blank line counts as block boundary");
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>
>     abc
Bye!").Should().Equal(new[] { "abc" }, "Quoted blank line counts as block boundary");
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>
>>>     abc
     def
>     ghi
Bye!").Should().Equal(new[] { "abc", " def", "ghi" }, "Quoted blank line counts as block boundary");
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!

>>>>     abc
Bye!").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>     > abc
Bye!").Should().BeEmpty("Missing blank line");
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>>     > abc
Bye!").Should().Equal(new[] { "> abc" }, "Deeper indent counts as block boundary");
            ParseCodeBlocks(@"Hi there!
>>>>
>     abc
Bye!").Should().Equal(new[] { "abc" }, "Less-deep indent still counts");

            ParseCodeBlocks(@"
>     abc").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@">     abc").Should().Equal(new[] { "abc" });

            //            ParseCodeBlocks(" >\t> \tabc").Should().Equal(new[] { "abc" });
            //            ParseCodeBlocks(" >\t > \tabc").Should().Equal(new[] { "abc" });
            //            ParseCodeBlocks(" >  \t > \tabc").Should().Equal(new[] { "abc" });
            //            ParseCodeBlocks(" >  \t  > abc").Should().Equal(new[] { "> abc" });
            //            ParseCodeBlocks(" >\t  > abc").Should().Equal(new[] { "> abc" });
        }

        [TestMethod]
        public void TestFencedCodeBlocks()
        {
            ParseCodeBlocks(@"Hi there!

```
abc
```
Bye!").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@"Hi there!
~~~

~~~
Bye!").Should().Equal(new[] { "" }, "Empty lines become empty artifacts");
            ParseCodeBlocks(@"Hi there!

~~~
~~~
Bye!").Should().BeEmpty("No artifacts are created if there are no blocks");
            ParseCodeBlocks(@"Hi there!

~~~
abc
def
~~~

Bye!").Should().Equal(new[] { "abc", "def" }, "Trailing blank lines don't break anything");
            ParseCodeBlocks(@"Hi there!

```
abc


~~~
```
Bye!").Should().Equal(new[] { "abc", "", "", "~~~" }, "Alternate fences & blank lines are handled correctly");
            ParseCodeBlocks(@"Hi there!
```
    abc
```
Bye!").Should().Equal(new[] { "    abc" }, "Leading whitespace is preserved");

            ParseCodeBlocks(@"Hi there!
```
abc
```   123
```
Bye!").Should().Equal(new[] { "abc", "```   123" }, "Closing fence cannot have content following");
            ParseCodeBlocks(@"Hi there!
```
abc
```        
```
Bye!").Should().Equal(new[] { "abc", "Bye!" }, "Closing fence can have unlimited whitespace following");

            ParseCodeBlocks(@"```
abc
```").Should().Equal(new[] { "abc" }, "Lack of surrounding characters doesn't break anything");
            ParseCodeBlocks(@"```
abc").Should().Equal(new[] { "abc" }, "Ending fence is optional");
            ParseCodeBlocks(@"```
abc
").Should().Equal(new[] { "abc", "" }, "Trailing blank line without fence is reported");
        }

        [TestMethod]
        public void TestQuotedFencedCodeBlocks()
        {
            ParseCodeBlocks(@"Hi there!
> ```
> abc
```
Bye!").Should().Equal(new[] { "abc" }, "Unquoted fence counts as block boundary");
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>```
>     abc
>>>>```
Bye!").Should().Equal(new[] { "    abc" }, "Quoted fence counts as block boundary");

            // TODO: GitHub counts this as a quote; not sure why.
            // Apparently, they treat it as wrapping instead of a terminator.
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
```
>>>>abc
```
Bye!").Should().Equal(new[] { ">>>>abc" });
            ParseCodeBlocks(@"Hi there!
>>>> I'm in a quote!
>>>>```
>>>>>     > abc
>>>>```
Bye!").Should().Equal(new[] { ">     > abc" }, "Deeper indent doesn't count as block boundary");
            ParseCodeBlocks(@"Hi there!
>>>>```
>     abc
```
Bye!").Should().Equal(new[] { "    abc" }, "Less-deep indent still counts");

            ParseCodeBlocks(@">```
>abc
```").Should().Equal(new[] { "abc" });
            ParseCodeBlocks(@">```
>abc").Should().Equal(new[] { "abc" });
        }
    }
}
