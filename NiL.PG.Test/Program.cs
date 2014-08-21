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
        /*
         * char '*' after field definition allow repeat this field.
         * In this case this field in result will be have index in name.
         * For sample: *func(func)* give fields "func0", "func1"...
         * 
         */

        static void programmingLanguageTest()
        {
            // invalid syntax
            var parser = new Parser(@"
rule name
    {a..z}|_({a..z, 0..9, _})*    

rule num
    {0..9}({0..9})*[.{0..9}({0..9})*]

rule operator
    ((+|-|*|/|%|<|>|=|^)[=])

rule decincoperators
    (++)|(--)

fragment expression

fragment callarg
    *exp(expression) ,
    *exp(expression)

fragment expression    
    (*brexp(expression)) *op(operator) *exp(expression)
    (*brexp(expression))
    *op(operator) *exp(expression)
    *first(name, num) *op(operator) *two(expression)
    *proc(name) ( )
    *proc(name) ( *arg(callarg)* )
    *value(name, num) *op(decincoperators)
    *value(name, num)

fragment vardeclenum
    *name(name) = *exp(expression) ,
    *name(name) = *exp(expression)
    *name(name) ,
    *name(name)

fragment vardecl
    *type(name) *var(vardeclenum)*    

fragment forinit
    *type(name) *var(vardeclenum)*
    *var(vardeclenum)*

fragment forpost
    *exp(expression) ,
    *exp(expression)

fragment codeline
    { *line(codeline)* }
    if (*condition(expression)) *exp(codeline) else *elseexp(codeline)
    if (*condition(expression)) *exp(codeline)
    while (*condition(expression)) *exp(codeline)
    for (*init(forinit) ; *condition(expression) ; *post(forpost)* ) *exp(codeline)
    return *exp(expression) ;
    *vardecl(vardecl) ;
    *exp(expression) ;
    ;

fragment paramdefine
    *type(name) *name(name) , *next(paramdefine)
    *type(name) *name(name)

fragment func
    *type(name) *name(name) ( *prms(paramdefine) ) { *line(codeline)* }
    *type(name) *name(name) ( ) { *line(codeline)* }

fragment root
    *func(func)*
");

            var tree = parser.Parse(@"
int sqrt(int x)
{
    return x * x;
}

int main(int a) {
    sqrt(a + 2, b);
 }");
            for (var i = 0; i < tree.NextNodes.Count; i++)
            {
                var func = tree.NextNodes[i];
                Console.WriteLine(func["type"].Value + " " + func["name"].Value);
            }
            return;
        }

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
