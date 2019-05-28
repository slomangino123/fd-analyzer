using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace fd_analyzer.Extensions
{
    public static class AggregateAnalyzerExtensions
    {
        public static bool ParentClassInheritsFromAggregateRoot(this ClassDeclarationSyntax parentClass)
        {
            var baseTypes = parentClass.BaseList?.Types;

            // Class does not inherit from anything
            if (!baseTypes.HasValue || !baseTypes.Value.Any())
            {
                return false;
            }

            // Loop over base types and look for occurences of 'AggregateRoot<...>' and 'AggregateRoot'
            foreach (var baseType in baseTypes)
            {
                var typeString = baseType.Type.ToString();
                var match = Regex.IsMatch(typeString, @"\bAggregateRoot<[^;].*>|\bAggregateRoot");
                if (match)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
