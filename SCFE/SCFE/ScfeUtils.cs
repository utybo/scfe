/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SCFE
{
    public static class ScfeUtils
    {
        public static List<string> GetHumanReadableDate(DateTime date)
        {
            var now = DateTime.Now;
            var timeEllapsed = now.Subtract(date);
            if (timeEllapsed < TimeSpan.FromSeconds(10)) return new List<string> {"now", "now", "just now"};

            if (timeEllapsed < TimeSpan.FromMinutes(1))
            {
                var seconds = timeEllapsed.Seconds;
                return new List<string> {seconds + "s", seconds + " sec", seconds + " sec ago"};
            }

            if (timeEllapsed < TimeSpan.FromHours(1))
            {
                var minutes = timeEllapsed.Minutes;
                return new List<string> {minutes + "m", minutes + " min"};
            }

            if (timeEllapsed < TimeSpan.FromDays(1))
            {
                var hours = timeEllapsed.Hours;
                var moreThanOne = hours > 1;
                return new List<string>
                {
                    hours + "h",
                    hours + (moreThanOne ? " hours" : " hour")
                };
            }

            if (timeEllapsed < TimeSpan.FromDays(7))
            {
                var days = timeEllapsed.Days;
                var moreThanOne = days > 1;
                return new List<string>
                {
                    days + "d",
                    days + (moreThanOne ? " days" : " day")
                };
            }

            if (timeEllapsed < TimeSpan.FromDays(5 * 7))
            {
                var weeks = timeEllapsed.Days / 7;
                var moreThanOne = weeks > 1;
                return new List<string>
                {
                    weeks + "w",
                    weeks + (moreThanOne ? " weeks" : " week")
                };
            }

            if (timeEllapsed < TimeSpan.FromDays(366))
                return new List<string>
                {
                    date.ToString("dd/MM"), date.ToString("d MMM")
                };

            return new List<string> {date.ToString("dd/MM/yy"), date.ToString("d")};
        }
    }

    public class FileComparer
    {
        public string Name { get; }

        public string NormalComparerOrder { get; }

        public string ReversedComparerOrder { get; }

        public IComparer<File> Comparer { get; }

        public IComparer<File> ReversedComparer { get; }

        public FileComparer(string name, string normalOrderName, IComparer<File> comparer, string reversedOrderName,
            IComparer<File> reversedComparer)
        {
            Name = name;
            NormalComparerOrder = normalOrderName;
            Comparer = comparer;
            ReversedComparerOrder = reversedOrderName;
            ReversedComparer = reversedComparer;
        }
    }

    public static class ReversedComparerUtils
    {
        public static IComparer<T> Reversed<T>(this IComparer<T> comparer)
        {
            return new ReversedComparer<T>(comparer);
        }
    }

    public class ReversedComparer<T> : IComparer<T>
    {
        private readonly IComparer<T> _comparer;


        public ReversedComparer(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public int Compare(T x, T y)
        {
            return -_comparer.Compare(x, y);
        }
    }

    public class CompositeComparer<T> : IComparer<T>
    {
        private readonly IComparer<T>[] _comparers;

        public CompositeComparer(params IComparer<T>[] comparers)
        {
            _comparers = comparers;
        }

        public int Compare(T x, T y)
        {
            foreach (var comp in _comparers)
            {
                var res = comp.Compare(x, y);
                if (res != 0)
                    return res;
            }

            return 0;
        }
    }

    public class NameComparer : IComparer<File>
    {
        public int Compare(File x, File y)
        {
            return string.Compare(x?.GetFileName(), y?.GetFileName(), StringComparison.CurrentCultureIgnoreCase);
        }
    }

    public class NameNaturalComparer : IComparer<File>
    {
        private int CompareNatural(string strA, string strB)
        {
            return CompareNatural(strA, strB, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
        }

        private int CompareNatural(string strA, string strB, CultureInfo culture, CompareOptions options)
        {
            CompareInfo cmp = culture.CompareInfo;
            int iA = 0;
            int iB = 0;
            int softResult = 0;
            int softResultWeight = 0;
            while (iA < strA.Length && iB < strB.Length)
            {
                bool isDigitA = char.IsDigit(strA[iA]);
                bool isDigitB = char.IsDigit(strB[iB]);
                if (isDigitA != isDigitB)
                {
                    return cmp.Compare(strA, iA, strB, iB, options);
                }

                if (!isDigitA)
                {
                    int jA = iA + 1;
                    int jB = iB + 1;
                    while (jA < strA.Length && !char.IsDigit(strA[jA])) jA++;
                    while (jB < strB.Length && !char.IsDigit(strB[jB])) jB++;
                    int cmpResult = cmp.Compare(strA, iA, jA - iA, strB, iB, jB - iB, options);
                    if (cmpResult != 0)
                    {
                        // Certain strings may be considered different due to "soft" differences that are
                        // ignored if more significant differences follow, e.g. a hyphen only affects the
                        // comparison if no other differences follow
                        string sectionA = strA.Substring(iA, jA - iA);
                        string sectionB = strB.Substring(iB, jB - iB);
                        if (cmp.Compare(sectionA + "1", sectionB + "2", options) ==
                            cmp.Compare(sectionA + "2", sectionB + "1", options))
                        {
                            return cmp.Compare(strA, iA, strB, iB, options);
                        }

                        if (softResultWeight < 1)
                        {
                            softResult = cmpResult;
                            softResultWeight = 1;
                        }
                    }

                    iA = jA;
                    iB = jB;
                }
                else
                {
                    char zeroA = (char) (strA[iA] - (int) Char.GetNumericValue(strA[iA]));
                    char zeroB = (char) (strB[iB] - (int) Char.GetNumericValue(strB[iB]));
                    int jA = iA;
                    int jB = iB;
                    while (jA < strA.Length && strA[jA] == zeroA) jA++;
                    while (jB < strB.Length && strB[jB] == zeroB) jB++;
                    int resultIfSameLength = 0;
                    do
                    {
                        isDigitA = jA < strA.Length && Char.IsDigit(strA[jA]);
                        isDigitB = jB < strB.Length && Char.IsDigit(strB[jB]);
                        int numA = isDigitA ? (int) Char.GetNumericValue(strA[jA]) : 0;
                        int numB = isDigitB ? (int) Char.GetNumericValue(strB[jB]) : 0;
                        if (isDigitA && (char) (strA[jA] - numA) != zeroA) isDigitA = false;
                        if (isDigitB && (char) (strB[jB] - numB) != zeroB) isDigitB = false;
                        if (isDigitA && isDigitB)
                        {
                            if (numA != numB && resultIfSameLength == 0)
                            {
                                resultIfSameLength = numA < numB ? -1 : 1;
                            }

                            jA++;
                            jB++;
                        }
                    } while (isDigitA && isDigitB);

                    if (isDigitA != isDigitB)
                    {
                        // One number has more digits than the other (ignoring leading zeros) - the longer
                        // number must be larger
                        return isDigitA ? 1 : -1;
                    }

                    if (resultIfSameLength != 0)
                    {
                        // Both numbers are the same length (ignoring leading zeros) and at least one of
                        // the digits differed - the first difference determines the result
                        return resultIfSameLength;
                    }

                    int lA = jA - iA;
                    int lB = jB - iB;
                    if (lA != lB)
                    {
                        // Both numbers are equivalent but one has more leading zeros
                        return lA > lB ? -1 : 1;
                    }

                    if (zeroA != zeroB && softResultWeight < 2)
                    {
                        softResult = cmp.Compare(strA, iA, 1, strB, iB, 1, options);
                        softResultWeight = 2;
                    }

                    iA = jA;
                    iB = jB;
                }
            }

            if (iA < strA.Length || iB < strB.Length)
            {
                return iA < strA.Length ? 1 : -1;
            }

            if (softResult != 0)
            {
                return softResult;
            }

            return 0;
        }

        public int Compare(File x, File y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            if (y == null)
                return 1;
            
            return CompareNatural(x.GetFileName(), y.GetFileName());
        }
    }

    public class ExtensionComparer : IComparer<File>
    {
        public int Compare(File x, File y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            if (y == null)
                return 1;

            var bitsX = x.GetFileName().Split('.');
            var bitsY = y.GetFileName().Split('.');

            var extX = bitsX.Length == 1 ? null : bitsX.Last();
            var extY = bitsY.Length == 1 ? null : bitsY.Last();
            if (extX == null)
                return extY == null ? 0 : -1;
            if (extY == null)
                return 1;

            return string.Compare(extX, extY, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    public class SizeComparer : IComparer<File>
    {
        public int Compare(File x, File y)
        {
            if (x?.IsFolder() == true && y?.IsFolder() == true)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;
            return x.GetSize().CompareTo(y.GetSize());
        }
    }

    public class TypeComparer : IComparer<File>
    {
        public int Compare(File x, File y)
        {
            if (x?.IsFolder() == y?.IsFolder())
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;
            if (x.IsFolder() && !y.IsFolder())
                return -1;
            return 1;
        }
    }

    public class DateComparer : IComparer<File>
    {
        public int Compare(File x, File y)
        {
            if (x == null)
                return -1;
            if (y == null)
                return 1;
            return x.GetModificationDate().CompareTo(y.GetModificationDate());
        }
    }

    public interface IFilter<in T>
    {
        bool GoesThrough(T t);
    }

    public class AndFilter<T> : IFilter<T>
    {
        public readonly List<IFilter<T>> Filters = new List<IFilter<T>>();

        public AndFilter(params IFilter<T>[] filters)
        {
            foreach (var f in filters)
                Filters.Add(f);
        }

        public bool GoesThrough(T t)
        {
            return Filters.All(f => f.GoesThrough(t));
        }
    }

    public class OrFilter<T> : IFilter<T>
    {
        public readonly List<IFilter<T>> Filters = new List<IFilter<T>>();

        public OrFilter(params IFilter<T>[] filters)
        {
            foreach (var f in filters)
                Filters.Add(f);
        }

        public bool GoesThrough(T t)
        {
            return Filters.Any(f => f.GoesThrough(t));
        }
    }

    public class BranchingFilter<T> : IFilter<T>
    {
        private readonly Func<bool> _condition;
        private readonly IFilter<T> _filterIfTrue;
        private readonly IFilter<T> _filterIfFalse;

        public BranchingFilter(Func<bool> condition, IFilter<T> ifTrue, IFilter<T> ifFalse)
        {
            _condition = condition;
            _filterIfTrue = ifTrue;
            _filterIfFalse = ifFalse;
        }

        public bool GoesThrough(T t)
        {
            if (_condition())
                return _filterIfTrue.GoesThrough(t);
            return _filterIfFalse.GoesThrough(t);
        }
    }

    public class TrueFilter<T> : IFilter<T>
    {
        public bool GoesThrough(T t)
        {
            return true;
        }
    }

    public class FalseFilter<T> : IFilter<T>
    {
        public bool GoesThrough(T t)
        {
            return false;
        }
    }

    public class BoolFilter<T> : IFilter<T>
    {
        private readonly Func<T, bool> _cond;

        public BoolFilter(Func<T, bool> cond)
        {
            _cond = cond;
        }

        public bool GoesThrough(T t)
        {
            return _cond(t);
        }
    }

    public class StringFilter<T> : IFilter<T>
    {
        private readonly Func<string> _criterion;
        private readonly Func<T, string> _transformer;

        public StringFilter(Func<T, string> selector, Func<string> criterionGetter)
        {
            _transformer = selector;
            _criterion = criterionGetter;
        }


        public bool GoesThrough(T t)
        {
            var crit = _criterion();

            if (string.IsNullOrWhiteSpace(crit))
                return true;

            return _transformer(t).Contains(crit);
        }
    }
}
