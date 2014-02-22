using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiL.PG.Test
{
    class Program
    {
        #region Parser define
           NiL.PG.Parser parser = new Parser(
@"
rule name
    {a..z}|_({a..z, 0..9, _})*    

rule num
    {0..9}({0..9})*[.{0..9}({0..9})*]

rule operator
    ((+|-|*|/|%|<|>|=|^)[=])

rule decincoperators
    (++)|(--)

fragment expression    
    (*brexp(expression)) *op(operator) *exp(expression)
    (*brexp(expression))
    *op(operator) *exp(expression)
    *first(name, num) *op(operator) *two(expression)
    *value(name, num) *op(decincoperators)
    *op(decincoperators) *value(name, num) 
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
    *exp(expression) ;
    return *rexp(expression) ;
    *vardecl(vardecl) ;
    if (*condition(expression)) *exp(codeline) else *elseexp(codeline)
    if (*condition(expression)) *exp(codeline)
    while (*condition(expression)) *exp(codeline)
    for (*init(forinit) ; *condition(expression) ; *post(forpost)* ) *exp(codeline)
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
#endregion

        static void Main(string[] args)
        {

        }
    }
}
