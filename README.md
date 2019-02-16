# BIT, a Hardware Description Language

BIT is a hardware description language that I designed and wrote in 1992.
Sadly, the original C++ source code has been lost forever.  In 2011, I
resurrected the project and re-wrote BIT in C#.  The new version includes 
a context sensitive editor, real time language parser,  optimizing compiler, 
and CPU simulator.

In addition to BIT (the hardware description language), I also designed a 32
bit CPU.  You can read more about the hardware description language and CPU 
here http://gosub.com/Bit

## Architectural Overview

# ![](Architecture.png)

## Lexer

The `Lexer` breaks the text into the tokens of the language.  Once tokenized, 
they can be enumerated and marked up by the `Parser` or displayed by the 
`Editor`.  Each token carries enough information to display its color (i.e. 
token type) or give the user feedback from the parser and code generator. 
`ReplaceText` can be used to insert or delete text, and the tokens in the 
affected area will be regenerated.

## Editor

The `Editor` control uses the `Lexer` to enumerate the tokens, draw them
on the screen, and edit the text.  User edits are sent to the lexer
which then sends a `TextChanged2` event back to the application so it can
recompile the code.  Text changes are recorded to implement undo/redo.

The application uses `TokenColorOverrides` to draw different color 
backgrounds as the user moves the mouse over the text.  It also hooks
`MouseHoverTokenChanged` to display information reported by the parser
and code generator, such as error or type information.

## Parser

The `Parser` is recursive descent, and generates a syntax tree composed of
`SyntaxBox`, `SyntaxDecl`, and `SyntaxExpr`.  As it parses, the tokens
are marked with information such as error text and connecting parenthesis.
I know it doesn't mean much in todays world, but the parser is pretty fast, 
taking just 7 milliseconds to parse 1370 lines of text on my laptop.  

![](SyntaxTree.png)

Syntax tree for boxes that contain the statement `A=(B+C+d)*E*F`

## CodeBox and Code Generation

`CodeBox` has two distinct passes.  The first pass walks the syntax tree, 
collects type information, generates *intermediate* code, and marks up 
tokens with more information for the user.  The *intermediate* code 
includes only the logic gates of the box being compiled, but not any gates
of the referenced boxes.  It is stripped of higher level constructs
such as `set`, `dup`, `==`, and number constants.  Arrays are 
expanded to individual bits.  `if` is converted to sum of products.

The second pass, executed by `LinkCode`, recursively includes all of the 
referenced boxes and synthesizes all the gates necessary for simulating the 
circuit.  The final un-optimized output, ready for simulation, is just a list
of boolean expressions `List<OpCodeExpr>` stored in `LinkedCode`.  

Honestly, this is the most messy part of the project, and a lot of it is 
because the class should be split into several pieces.  There should 
be at least three classes: `TypeAnalysis`, intermediate `CodeGeneration`, 
and final `GateSynthesis`.  

## Optimizer

The `Optimizer` removes unnecessary wires and gates, thereby reducing
the 32 bit CPU from 13510 to 11864 gates.  The following rules are used:

    Remove wires, embed expressions
    Remove duplicate expressions
    Remove constants: A*1=A, A+0=A, A#0=A, A#1=!A, A*0=0, A+1=1
    Remove identities: a+a=a, a*a=a, a+!a=1, a*!a=0
    Remove parenthesis: a+(b+c)=a+b+c, a*(b*c)=a*b*c, a#(b#c)=a#b#c, a#!(b#c)=!(a#b#c)
    Demorgan's law: a+!(b*c)=a+!b+!c, a*!(b+c)=a*!b*!c

This optimizer isn't all that powerful compared to the 1992 version which
implemented Quine–McCluskey https://en.wikipedia.org/wiki/Quine%E2%80%93McCluskey_algorithm

## OpCodes

The final output is a list of single bit Boolean expressions stored in a tree
structure.  Each node of the tree can represent a constant, parameter, math
operation, terminal, or expression.  Symbolic information is recorded so the 
simulator can show the inputs, output, and "local variable" names.   During
gate synthesis, many thousands of anonymous expressions are created and they 
are assigned names, such as `X3999`.  You can view the final output from the 
main menu by clicking Simulate...View Code.  Here is a sample of the output from 
the 32 bit CPU:

    ...
    X3399 = !(clock * !(X3403 * X3399));
    X3402 = !(X3403 * clock * X3399);
    X3403 = !(((X1042 * dataInBus.1) + (!X1042 * pushPopFieldNext.1)) * X3402);
    X3406 = !(X3399 * !(X3402 * X3406));
    X3417 = !(clock * !(X3421 * X3417));
    X3420 = !(X3421 * clock * X3417);
    X3421 = !(((X1042 * dataInBus.2) + (!X1042 * pushPopFieldNext.2)) * X3420);
    ...
    X15296 = ((X15338 * X15484) + (source1Reg.2 * X15485) + ((X16010 # X16042 # (X16167 + (X16166 * X16171) + (X16007 * X16170 * X16171))) * X15408) + (source1Reg.2 * X15338 * X15487) + ((source1Reg.2 + X15338) * X15488) + ((source1Reg.2 # X15338) * X15489) + (X15338 * X15335) + (((X15416 * source1Reg.2) + (!X15416 * X15338)) * X15411));
    X15297 = ((X15339 * X15484) + (source1Reg.3 * X15485) + ((X16011 # X16043 # (X16168 + (X16167 * X16172) + (X16166 * X16171 * X16172) + (X16007 * X16170 * X16171 * X16172))) * X15408) + (source1Reg.3 * X15339 * X15487) + ((source1Reg.3 + X15339) * X15488) + ((source1Reg.3 # X15339) * X15489) + (X15339 * X15335) + (((X15416 * source1Reg.3) + (!X15416 * X15339)) * X15411));
    X15298 = ((X15340 * X15484) + (source1Reg.4 * X15485) + ((X16012 # X16044 # X16174) * X15408) + (source1Reg.4 * X15340 * X15487) + ((source1Reg.4 + X15340) * X15488) + ((source1Reg.4 # X15340) * X15489) + (X15340 * X15335) + (((X15416 * source1Reg.4) + (!X15416 * X15340)) * X15411));
    ...
    
## Simulating the Circuit

Simulating is straight forward, and that's all I'll say about the implementation.
What's really needed now is an automated unit test system.  Manually testing
the CPU for each instruction is tedious, time consuming, and prone to error.  









