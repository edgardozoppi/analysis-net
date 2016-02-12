# Analysis.NET #

Control-flow and data-flow analysis framework for .NET programs.

+ Static analysis framework for .NET
    * Bytecode level
    * No need for source code
    * Can analyze standard libraries
    + Intermediate representations
        * Simplified bytecode
        * Three address code
        * Static single assignment
        * Visitor pattern
    + Control-flow analysis
        * Dominance
        * Dominance frontier
        * Natural loops
    + Data-flow analysis
        * Def-Use and Use-Def chains
        * Reaching definitions
        * Copy propagation
        * Points-to analysis
    * Type inference
    * Web analysis
    + Serialization
        * DOT
        * DGML