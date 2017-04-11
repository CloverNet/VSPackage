// Guids.cs
// MUST match guids.h
using System;

namespace Microsoft.Clover_BuildSolution
{
    static class GuidList
    {
        public const string guidClover_BuildSolutionPkgString = "64b5e5d4-b754-412c-909c-c99fc53ed5cf";
        public const string guidClover_BuildSolutionCmdSetString = "e3576645-566b-459a-92a6-d7d466e4e7ee";

        public static readonly Guid guidClover_BuildSolutionCmdSet = new Guid(guidClover_BuildSolutionCmdSetString);
    };
}