using System.Collections.Generic;

// MIT No Attribution
// 
// Copyright 2019 M. Levra
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify,
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace HeraldryPicker.Utils
{
    /// <summary>
    /// String comparer that applies a “natural sort” algorithm.
    /// </summary>
    /// <remarks>
    /// Original Java code by Stanislav Bokach (https://stackoverflow.com/a/58249974)
    /// </remarks>
    public sealed class NaturalStringComparer : IComparer<string>
    {
        /// <summary>
        /// Compares two strings and returns a value indicating whether one is less than,
        /// equal to, or greater than the other, according to a “natural sort” algorithm.
        /// </summary>
        /// <param name="x">The first string to compare.</param>
        /// <param name="y">The second string to compare.</param>
        /// <returns>A signed integer that indicates the relative values of x and y.
        /// Less than zero: x is less than y.
        /// Zero: x equals y.
        /// Greater than zero: x is greater than y.</returns>
        public int Compare(string x, string y)
        {
            int indexX = 0;
            int indexY = 0;
            while (true)
            {
                // Handle the case when one string has ended.
                if (indexX == x.Length)
                {
                    return indexY == y.Length ? 0 : -1;
                }
                if (indexY == y.Length)
                {
                    return 1;
                }

                char charX = x[indexX];
                char charY = y[indexY];
                if (char.IsDigit(charX) && char.IsDigit(charY))
                {
                    // Skip leading zeroes in numbers.
                    while (indexX < x.Length && x[indexX] == '0')
                    {
                        indexX++;
                    }
                    while (indexY < y.Length && y[indexY] == '0')
                    {
                        indexY++;
                    }

                    // Find the end of numbers
                    int endNumberX = indexX;
                    int endNumberY = indexY;
                    while (endNumberX < x.Length && char.IsDigit(x[endNumberX]))
                    {
                        endNumberX++;
                    }
                    while (endNumberY < y.Length && char.IsDigit(y[endNumberY]))
                    {
                        endNumberY++;
                    }

                    int digitsLengthX = endNumberX - indexX;
                    int digitsLengthY = endNumberY - indexY;

                    // If the lengths are different, then the longer number is bigger
                    if (digitsLengthX != digitsLengthY)
                    {
                        return digitsLengthX - digitsLengthY;
                    }
                    // Compare numbers digit by digit
                    while (indexX < endNumberX)
                    {
                        if (x[indexX] != y[indexY])
                            return x[indexX] - y[indexY];
                        indexX++;
                        indexY++;
                    }
                }
                else
                {
                    // Plain characters comparison
                    int compareResult = char.ToUpperInvariant(charX).CompareTo(char.ToUpperInvariant(charY));
                    if (compareResult != 0)
                    {
                        return compareResult;
                    }
                    indexX++;
                    indexY++;
                }
            }
        }
    }
}