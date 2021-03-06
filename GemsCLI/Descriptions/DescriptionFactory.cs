﻿using System;
using System.Collections.Generic;
using System.Linq;
using GemsCLI.Enums;
using GemsCLI.Exceptions;
using GemsCLI.Helper;
using GemsCLI.Properties;
using GemsCLI.Types;

namespace GemsCLI.Descriptions
{
    public static class DescriptionFactory
    {
        /// <summary>
        /// Converts the pattern expression into a collection of descriptions
        /// of parameters.
        /// </summary>
        /// <param name="pOptions">Parser options to use.</param>
        /// <param name="pHelpProvider"></param>
        /// <param name="pPattern">A string containing the syntax pattern for the application's argument.</param>
        /// <returns>A collection of descriptions.</returns>
        public static List<Description> Create(CliOptions pOptions, iHelpProvider pHelpProvider, string pPattern)
        {
            string[] strings = pPattern.Split(' ');
            return (from str in strings
                    where !string.IsNullOrWhiteSpace(str)
                    select Parse(pOptions, pHelpProvider, str)).ToList();
        }

        /// <summary>
        /// Converts the pattern for a single parameter description into an
        /// initialized description object.
        /// </summary>
        /// <param name="pOptions">Parsing options to use.</param>
        /// <param name="pHelpProvider"></param>
        /// <param name="pPattern">A string containing the syntax of a single argument.</param>
        /// <returns>A description object</returns>
        /// <exception cref="SyntaxErrorException"></exception>
        public static Description Parse(CliOptions pOptions, iHelpProvider pHelpProvider, string pPattern)
        {
            if (string.IsNullOrWhiteSpace(pPattern))
            {
                throw new SyntaxErrorException(Errors.DescriptionFactoryNoPattern);
            }

            string pattern = pPattern.Trim();

            eSCOPE scope = pattern.StartsWith("[") && pattern.EndsWith("]") ? eSCOPE.OPTIONAL : eSCOPE.REQUIRED;
            pattern = scope == eSCOPE.OPTIONAL ? pattern.Substring(1, pattern.Length - 2) : pattern;

            eROLE role = pattern.StartsWith(pOptions.Prefix) ? eROLE.NAMED : eROLE.PASSED;
            pattern = role == eROLE.NAMED ? pattern.Substring(pOptions.Prefix.Length) : pattern;

            eMULTIPLICITY multi = pattern.EndsWith("#") ? eMULTIPLICITY.MULTIPLE : eMULTIPLICITY.ONCE;
            pattern = multi == eMULTIPLICITY.MULTIPLE ? pattern.Substring(0, pattern.Length - 1) : pattern;

            int equal = pattern.IndexOf(pOptions.EqualChar, StringComparison.Ordinal);
            string type = equal == -1 ? null : pattern.Substring(equal + 1).ToLower();
            string name = equal == -1 ? pattern : pattern.Substring(0, equal);

            iParamType paramType = type == null ? null : ParamTypeFactory.Create(type);
            if (paramType == null && role == eROLE.PASSED)
            {
                paramType = new ParamString();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new SyntaxErrorException(Errors.DescriptionName);
            }

            string help = pHelpProvider.Get(name);

            return new Description(name, help, role, paramType, scope, multi);
        }
    }
}