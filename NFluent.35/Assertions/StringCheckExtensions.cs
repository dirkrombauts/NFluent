// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="StringCheckExtensions.cs" company="">
// //   Copyright 2013 Thomas PIERRAIN, Cyrille DUPUYDAUBY
// //   Licensed under the Apache License, Version 2.0 (the "License");
// //   you may not use this file except in compliance with the License.
// //   You may obtain a copy of the License at
// //       http://www.apache.org/licenses/LICENSE-2.0
// //   Unless required by applicable law or agreed to in writing, software
// //   distributed under the License is distributed on an "AS IS" BASIS,
// //   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //   See the License for the specific language governing permissions and
// //   limitations under the License.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------
namespace NFluent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NFluent.Extensibility;

    /// <summary>
    /// Provides check methods to be executed on a string instance.
    /// </summary>
    public static class StringCheckExtensions
    {
        /// <summary>
        /// Checks that the checker value is equal to another expected value.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="expected">The expected value.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The checker value is not equal to the expected value.</exception>
        public static ICheckLink<ICheck<string>> IsEqualTo(this ICheck<string> check, object expected)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var messageText = AssessEquals(checker, expected, checker.Negated);
            if (!string.IsNullOrEmpty(messageText))
            {
                throw new FluentCheckException(messageText);
            }

            return new CheckLink<ICheck<string>>(check);
        }

        /// <summary>
        /// Checks that the checker value is equal to another expected value.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="expected">The expected value.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The checker value is not equal to the expected value.</exception>
        public static ICheckLink<ICheck<string>> IsEqualTo(this ICheck<string> check, string expected)
        {
            return IsEqualTo(check, (object)expected);
        }

        /// <summary>
        /// Checks that the checker value is not equal to another expected value.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="expected">The expected value.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The checker value is equal to the expected value.</exception>
        public static ICheckLink<ICheck<string>> IsNotEqualTo(this ICheck<string> check, object expected)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var messageText = AssessEquals(checker, expected, !checker.Negated);
            if (!string.IsNullOrEmpty(messageText))
            {
                throw new FluentCheckException(messageText);
            }

            return new CheckLink<ICheck<string>>(check);
        }

        /// <summary>
        /// Checks that the checker value is not equal to another expected value.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="expected">The expected value.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The checker value is equal to the expected value.</exception>
        public static ICheckLink<ICheck<string>> IsNotEqualTo(this ICheck<string> check, string expected)
        {
            return IsNotEqualTo(check, (object)expected);
        }

        /// <summary>
        /// Checks that the checker value is one of these possible elements.
        /// </summary>
        /// <param name="check">The check.</param>
        /// <param name="possibleElements">The possible elements.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The checker value is NOT one of the elements.</exception>
        public static ICheckLink<ICheck<string>> IsOneOfThese(this ICheck<string> check, params string[] possibleElements)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            return checker.ExecuteCheck(
                () =>
                {
                    string errorMessage;
                    if (possibleElements == null)
                    {
                        // the rare case where possible elements is null
                        if (checker.Value == null)
                        {
                            return;
                        }
                        else
                        {
                            errorMessage = checker.BuildMessage("The {0} is not one of the possible elements.")
                                            .On(checker.Value)
                                            .And.Expected(possibleElements).Label("The possible elements:")
                                            .ToString();
                            throw new FluentCheckException(errorMessage);
                        }
                    }

                    if (possibleElements.Any(possibleElement => string.Equals(possibleElement, checker.Value)))
                    {
                        return;
                    }

                    errorMessage = checker.BuildMessage("The {0} is not one of the possible elements.")
                                            .Expected(possibleElements).Label("The possible elements:")
                                            .ToString();
                    throw new FluentCheckException(errorMessage);
                },
                checker.BuildMessage("The {0} is one of the possible elements whereas it must not.").Expected(possibleElements).Label("The possible elements:").ToString());
        }

        /// <summary>
        /// Checks that the string contains the given expected values, in any order.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="values">The expected values to be found.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string  contains all the given strings in any order.</exception>
        public static IExtendableCheckLink<string, string[]> Contains(this ICheck<string> check, params string[] values)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var result = ContainsImpl(checker, values, checker.Negated, false);

            if (string.IsNullOrEmpty(result))
            {
                return new ExtendableCheckLink<string, string[]>(check, values);
            }

            throw new FluentCheckException(result);
        }

        /// <summary>
        /// Checks that the string does not contain any of the given expected values.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="values">The values not to be present.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string contains at least one of the given strings.</exception>
        public static ICheckLink<ICheck<string>> DoesNotContain(this ICheck<string> check, params string[] values)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var result = ContainsImpl(checker, values, checker.Negated, true);

            if (string.IsNullOrEmpty(result))
            {
                return new CheckLink<ICheck<string>>(check);
            }

            throw new FluentCheckException(result);
        }

        private static string AssessEquals(IChecker<string, ICheck<string>> checker, object expected, bool negated, bool ignoreCase = false)
        {
            if (string.Equals(checker.Value, (string)expected, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) != negated)
            {
                return null;
            }

            string messageText;
            if (negated)
            {
                messageText = checker.BuildShortMessage("The {0} is equal to the {1} whereas it must not.")
                                    .For("string")
                                    .Expected(expected)
                                    .Comparison("different from")
                                    .ToString();
            }
            else
            {
                // we try to refine the difference
                var expectedString = expected as string;
                var message = "The {0} is different from {1}.";
                var isCrlfAndLfDifference = false;
                var isTabAndWhiteSpaceDifference = false;
                var firstDiffPos = 0;

                // TODO: refactor to reduce method lines
                var value = checker.Value;
                if (expectedString != null && value != null)
                {
                    var firstDiff = 0;
                    var blockStart = 0;
                    var blockLen = 0;

                    var minLength = Math.Min(value.Length, expectedString.Length);

                    for (; firstDiff < minLength; firstDiff++)
                    {
                        if (value[firstDiff] != expectedString[firstDiff])
                        {
                            firstDiffPos = firstDiff;
                            isCrlfAndLfDifference = IsACRLFDifference(firstDiff, expectedString, value);
                            isTabAndWhiteSpaceDifference = IsATabAndWhiteSpaceDifference(firstDiff, expectedString, value);
                        
                            blockStart = Math.Max(0, firstDiff - 10);
                            blockLen = Math.Min(minLength - blockStart, 20);
                            break;
                        }
                    }

                    if (expectedString.Length == value.Length)
                    {
                        // same length
                        if (string.Compare(value, expectedString, StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            message = "The {0} is different from the {1} but only in case.";
                        }
                        else
                        {
                            message = "The {0} is different from the {1} but has same length.";
                        }   

                        var prefix = blockStart == 0 ? string.Empty : "...";
                        var suffix = (blockStart + blockLen) == minLength ? string.Empty : "...";
                        message += string.Format(
                                                    " At {0}, expected '{3}{1}{4}' was '{3}{2}{4}'",
                                                    firstDiff,
                                                    expectedString.Substring(blockStart, blockLen),
                                                    value.Substring(blockStart, blockLen),
                                                    prefix,
                                                    suffix);
                    }
                    else
                    {
                        if (expectedString.Length > value.Length)
                        {
                            if (expectedString.StartsWith(value, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture))
                            {
                                message = "The {0} is different from {1}, it is missing the end.";
                            }
                        }
                        else
                        {
                            if (value.StartsWith(expectedString, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture))
                            {
                                message = "The {0} is different from {1}, it contains extra text at the end.";
                            }
                        }
                    }
                }

                if (isCrlfAndLfDifference)
                {
                    value = HighlightFirstCrlfOrLfIfAny(value, firstDiffPos);
                    expectedString = HighlightFirstCrlfOrLfIfAny(expectedString, firstDiffPos);
                }

                if (isTabAndWhiteSpaceDifference)
                {
                    value = HighlightTabsIfAny(value);
                    expectedString = HighlightTabsIfAny(expectedString);    
                }

                messageText = checker.BuildMessage(message).For("string").On(value).And.Expected(expectedString).ToString();
            }

            return messageText;
        }

        private static bool IsATabAndWhiteSpaceDifference(int firstDiff, string expected, string actual)
        {
            return (actual[firstDiff].Equals(' ') && expected[firstDiff].Equals('\t')) || (actual[firstDiff].Equals('\t') && expected[firstDiff].Equals(' '));
        }

        private static bool IsACRLFDifference(int firstDiff, string expected, string actual)
        {
            return (actual[firstDiff].Equals('\n') && expected[firstDiff].Equals('\r')) || (actual[firstDiff].Equals('\r') && expected[firstDiff].Equals('\n'));
        }

        /// <summary>
        /// Inserts &lt;&lt;CRLF&gt;&gt; before the first CRLF or &lt;&lt;LF&gt;&gt; before the first LF.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="firstDiffPos">The first difference position.</param>
        /// <returns>The same string but with &lt;&lt;CRLF&gt;&gt; inserted before the first CRLF or &lt;&lt;LF&gt;&gt; inserted before the first LF.</returns>
        private static string HighlightFirstCrlfOrLfIfAny(string str, int firstDiffPos)
        {
            if (str.Substring(firstDiffPos).StartsWith("\r\n"))
            {
                str = str.Insert(firstDiffPos, "<<CRLF>>");
            }
            else if (str.Substring(firstDiffPos).StartsWith("\n"))
            {
                str = str.Insert(firstDiffPos, "<<LF>>");
            }

            return str;
        }

        /// <summary>
        /// Replace every tab char by "&lt;&lt;tab&gt;&gt;".
        /// </summary>
        /// <param name="str">The original string.</param>
        /// <returns>The original string with every \t replaced with "&lt;&lt;tab&gt;&gt;".</returns>
        private static string HighlightTabsIfAny(string str)
        {
            return str.Replace("\t", "<<tab>>");
        }

        private static string ContainsImpl(IChecker<string, ICheck<string>> checker, IEnumerable<string> values, bool negated, bool notContains)
        {
            var checkedValue = checker.Value;

            // special case if checkedvalue is null
            if (checkedValue == null)
            {
                return (negated || notContains) ? null : checker.BuildShortMessage("The {0} is null.").For("string").Expected(values).Label("The {0} substring(s):").ToString();
            }

            var items = values.Where(item => checkedValue.Contains(item) == notContains).ToList();

            if (negated == (items.Count > 0))
            {
                return null;
            }

            if (!notContains && negated)
            {
                items = values.ToList();
            }

            if (negated != notContains)
            {
                return
                    checker.BuildMessage(
                        "The {0} contains unauthorized value(s): " + items.ToEnumeratedString())
                                 .For("string")
                                 .Expected(values)
                                 .Label("The unauthorized substring(s):")
                                 .ToString();
            }

            return
                checker.BuildMessage(
                    "The {0} does not contains the expected value(s): " + items.ToEnumeratedString())
                             .For("string")
                             .Expected(values)
                             .Label("The {0} substring(s):")
                             .ToString();
        }

        /// <summary>
        /// Checks that the string starts with the given expected prefix.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="expectedPrefix">The expected prefix.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string does not start with the expected prefix.</exception>
        public static ICheckLink<ICheck<string>> StartsWith(this ICheck<string> check, string expectedPrefix)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var result = StartsWithImpl(checker, expectedPrefix, checker.Negated);
            if (string.IsNullOrEmpty(result))
            {
                return new CheckLink<ICheck<string>>(check);
            }

            throw new FluentCheckException(result);
        }

        private static string StartsWithImpl(IChecker<string, ICheck<string>> checker, string starts, bool negated)
        {
            var checkedValue = checker.Value;

            // special case if checkedvalue is null
            if (checkedValue == null)
            {
                return negated ? null : checker.BuildShortMessage("The {0} is null.").For("string").Expected(starts).Comparison("starts with").ToString();
            }

            if (checkedValue.StartsWith(starts) != negated)
            {
                // success
                return null;
            }

            if (negated)
            {
                return
                    checker.BuildMessage("The {0} starts with {1}, whereas it must not.")
                                    .For("string")
                                    .Expected(starts)
                                    .Comparison("does not start with")
                                    .ToString();
            }

            return
                checker.BuildMessage("The {0}'s start is different from the {1}.")
                .For("string").Expected(starts)
                             .Comparison("starts with")
                             .ToString();
        }

        /// <summary>
        /// Checks that the string ends with the given expected suffix.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="expectedEnd">The expected suffix.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string does not end with the expected prefix.</exception>
        public static ICheckLink<ICheck<string>> EndsWith(this ICheck<string> check, string expectedEnd)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check); 

            var result = EndsWithImpl(checker, expectedEnd, checker.Negated);
            if (string.IsNullOrEmpty(result))
            {
                return new CheckLink<ICheck<string>>(check);
            }

            throw new FluentCheckException(result);
        }

        private static string EndsWithImpl(IChecker<string, ICheck<string>> checker, string ends, bool negated)
        {
            var checkedValue = checker.Value;

            // special case if checkedvalue is null
            if (checkedValue == null)
            {
                return negated ? null : checker.BuildShortMessage("The {0} is null.").For("string").Expected(ends).Comparison("ends with").ToString();
            }

            if (checkedValue.EndsWith(ends) != negated)
            {
                // success
                return null;
            }

            if (negated)
            {
                return
                    checker.BuildMessage("The {0} ends with {1}, whereas it must not.")
                    .For("string").Expected(ends)
                                 .Comparison("does not end with")
                                 .ToString();
            }

            return
                checker.BuildMessage("The {0}'s end is different from the {1}.")
                .For("string")
                             .Expected(ends)
                             .Comparison("ends with")
                             .ToString();
        }

        /// <summary>
        /// Checks that the string matches a given regular expression.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="regExp">The regular expression.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string does not end with the expected prefix.</exception>
        public static ICheckLink<ICheck<string>> Matches(
            this ICheck<string> check, string regExp)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var result = MatchesImpl(checker, regExp, checker.Negated);
            if (!string.IsNullOrEmpty(result))
            {
                throw new FluentCheckException(result);
            }

            return new CheckLink<ICheck<string>>(check);
        }

        /// <summary>
        /// Checks that the string does not match a given regular expression.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="regExp">The regular expression prefix.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string does not end with the expected prefix.</exception>
        public static ICheckLink<ICheck<string>> DoesNotMatch(
            this ICheck<string> check, string regExp)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var result = MatchesImpl(checker, regExp, !checker.Negated);
            if (!string.IsNullOrEmpty(result))
            {
                throw new FluentCheckException(result);
            }

            return new CheckLink<ICheck<string>>(check);
        }

        private static string MatchesImpl(IChecker<string, ICheck<string>> checker, string regExp, bool negated)
        {
            var checkedValue = checker.Value;

            // special case if checkedvalue is null
            if (checkedValue == null)
            {
                return negated ? null : checker.BuildShortMessage("The {0} is null.").For("string").Expected(regExp).Comparison("matches").ToString();
            }

            var exp = new Regex(regExp);
            if (exp.IsMatch(checkedValue) != negated)
            {
                // success
                return null;
            }

            if (negated)
            {
                return
                    checker.BuildMessage("The {0} matches {1}, whereas it must not.")
                    .For("string")
                                 .Expected(regExp)
                                 .Comparison("does not match")
                                 .ToString();
            }

            return
                checker.BuildMessage("The {0} does not match the {1}.")
                .For("string")
                             .Expected(regExp)
                             .Comparison("matches")
                             .ToString();
        }

        /// <summary>
        /// Checks that the string is empty.
        /// </summary>
        /// <param name="check">The fluent check.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string is not empty.</exception>
        public static ICheckLink<ICheck<string>> IsEmpty(
            this ICheck<string> check)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var result = IsEmptyImpl(checker, false, checker.Negated);
            if (!string.IsNullOrEmpty(result))
            {
                throw new FluentCheckException(result);
            }

            return new CheckLink<ICheck<string>>(check);
        }

        /// <summary>
        /// Checks that the string is empty or null.
        /// </summary>
        /// <param name="check">The fluent check.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string is neither empty or null.</exception>
        public static ICheckLink<ICheck<string>> IsNullOrEmpty(this ICheck<string> check)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check); 

            var result = IsEmptyImpl(checker, true, checker.Negated);
            if (!string.IsNullOrEmpty(result))
            {
                throw new FluentCheckException(result);
            }

            return new CheckLink<ICheck<string>>(check);
        }

        /// <summary>
        /// Checks that the string is not empty.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string is empty.</exception>
        public static ICheckLink<ICheck<string>> IsNotEmpty(
            this ICheck<string> check)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check); 

            var result = IsEmptyImpl(checker, false, !checker.Negated);
            if (!string.IsNullOrEmpty(result))
            {
                throw new FluentCheckException(result);
            }

            return new CheckLink<ICheck<string>>(check);
        }

        /// <summary>
        /// Checks that the string has content.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string is empty or null.</exception>
        public static ICheckLink<ICheck<string>> HasContent(this ICheck<string> check)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var result = IsEmptyImpl(checker, true, !checker.Negated);
            if (!string.IsNullOrEmpty(result))
            {
                throw new FluentCheckException(result);
            }

            return new CheckLink<ICheck<string>>(check);
        }

        private static string IsEmptyImpl(IChecker<string, ICheck<string>> checker, bool canBeNull, bool negated)
        {
            var checkedValue = checker.Value;
            
            // special case if checkedvalue is null
            if (checkedValue == null)
            {
                if (canBeNull != negated)
                {
                    return null;
                }

                return negated ? checker.BuildShortMessage("The {0} is null whereas it must have content.").For("string").ToString()
                    : checker.BuildShortMessage("The {0} is null instead of being empty.").For("string").ToString();
            }

            if (string.IsNullOrEmpty(checkedValue) != negated)
            {
                // success
                return null;
            }

            if (negated)
            {
                return
                    checker.BuildShortMessage("The {0} is empty, whereas it must not.")
                    .For("string")
                                 .ToString();
            }

            return
                checker.BuildMessage("The {0} is not empty or null.")
                .For("string")
                             .On(checkedValue)
                             .ToString();
        }

        /// <summary>
        /// Checks that the string is equals to another one, disregarding case.
        /// </summary>
        /// <param name="check">The fluent check to be extended.</param>
        /// <param name="comparand">The string to compare to.</param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">The string is not equal to the comparand.</exception>
        public static ICheckLink<ICheck<string>> IsEqualIgnoringCase(
            this ICheck<string> check, string comparand)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);

            var result = AssessEquals(checker, comparand, checker.Negated, true);
            if (!string.IsNullOrEmpty(result))
            {
                throw new FluentCheckException(result);
            }

            return new CheckLink<ICheck<string>>(check);
        }
    }
}