// Guids.cs
// MUST match guids.h
using System;

namespace PerniciousGames.OpenFileInSolution
{
    static class GuidList
    {
        public const string guidOpenFileInSolutionPkgString = "6bb18fff-9e74-4deb-97df-6a94ddafb74e";
        public const string guidOpenFileInSolutionCmdSetString = "0570a35e-896b-4615-b4ca-0c661f920368";

        public static readonly Guid guidOpenFileInSolutionCmdSet = new Guid(guidOpenFileInSolutionCmdSetString);
    };
}