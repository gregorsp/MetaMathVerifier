using System;
using System.Collections.Generic;

namespace MetaMathVerifier
{
    static class Extension
    {
        public static T Do<T, U>(this U me, Func<U, T> action)
        {
            return action(me);
        }
        public static IEnumerable<T> UC<T>(this IEnumerable<T> me)
        {
            return (IEnumerable<T>)me;
        }
    }
}