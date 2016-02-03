# Analysis.NET #

Control-flow and data-flow analysis framework for .NET programs.

+ Static analysis framework for .NET
    * Bytecode level
    * No need for source code
    * Can analyze standard libraries
    + Intermediate representation
        * Three address code
        * Static single assignment
        * Visitor pattern
    + Control-flow analysis
        * Dominance
        * Dominance frontier
        * Natural loops
    + Data-flow analysis
        * Def-Use and Use-Def chains
        * Copy propagation
        * Points-to analysis
    * Type inference
    * Web analysis
    + Serialization
        * DOT
        * DGML