# Analysis.NET #

Static analysis framework for .NET programs.

Features:

* Bytecode level
* No need for source code
* Can analyze standard libraries
+ Intermediate representations
    * Simplified bytecode
    * Three address code
    * Static single assignment
	* Aggregated expressions
+ Control-flow analysis
    * Normal
    * Exceptional
    * Dominance
    * Dominance frontier
    * Natural loops
+ Data-flow analysis
    * Reaching definitions
    * Def-use and use-def chains
	* Live variables
    * Copy propagation
    * Points-to
+ Call-graph analysis
    * Class hierarchy
+ Transformations
    * Webs
    * Inlining
* Type inference
+ Serialization
    * DOT
    * DGML
