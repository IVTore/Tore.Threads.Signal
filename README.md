# Tore.Threads.Signal
Thread Signaling class library for C# By İ. Volkan Töre.

Language: C#.

Nuget package: [Tore.Core](https://www.nuget.org/packages/Tore.Threads.Signal/)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Dependencies: <br/>
net7.0<br/>


## Signal.cs :
Signal generates a waitable and cancellable object for asynchronous communications among threads.
It can use an external CancellationTokenSource otherwise it builds an internal one.

Assume there exists threads A and B in which A would wait for an operation in B,
also A and B do not terminate when they are done but repeat the operation. 
By sharing a Signal object:
If A issues an await signal.Wait() it will wait until    
B calls signal.EndWait() or signal.Cancel().             
After signal.EndWait() it can be reused by signal.Wait().
After signal.Cancel() reuse is not possible.             
Dispose after use. 
