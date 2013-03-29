﻿namespace Spike.Tests
{
    using System;

    using NFluent;

    using Spike.Core;

    public static class ComparableFluentAssertionExtensions
    {
        public static IFluentAssertion<IComparable> IsBefore(this IFluentAssertion<IComparable> fluentAssertionInterface, IComparable other)
        {
            if (fluentAssertionInterface.Sut.CompareTo(other) >= 0)
            {
                throw new FluentAssertionException("is not before.");
            }
            
            return fluentAssertionInterface;
        }
    }
}