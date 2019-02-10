using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiL.PG;

namespace NiL.PG.Test
{
    class Program
    {
        static void htmlTest()
        {
            #region Invalid syntax
            string html = @"<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8""/>
    <title></title>
</head>
<body>
    <header>Header</header>
    Hello, world!
    <div id=""test""></div>
</body>
</html>
";
            var parser = new Parser(@"

rule name
    ({ ..!, #.._, a.. })*

rule anyWithoutCB
    ({ ..=, ?.. })*

rule anyWithoutOB
    ({ ..;, =.. })*

rule anyWithoutDQ
    ({ ..;, =.. })*

rule anyWithoutSQ
    ({ .._, a.. })*

fragment tag

fragment attribute
    *name(name) = "" *value(anyWithoutDQ) ""
    *name(name) = ' *value(anyWithoutSQ) '

fragment tagcontent
    *node(tag)
    *text(anyWithoutOB)

fragment tag
    <!-- *comment(anyWithoutCB) --!>
    <*name(name) *attribute(attribute)* > *inner(tagcontent)* </ *cname(name) >

fragment root
    <! *doctype(anyWithoutCB) > *html(tag)
    *html(tag)

");
            var tree = parser.Parse(html);
            #endregion
        }

        static void Main(string[] args)
        {
            htmlTest();
        }
    }
}
