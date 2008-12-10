// <copyright file="FibonacciHeapFactory.cs" company="MSIT">Copyright © MSIT 2007</copyright>

using System;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Factories;
using QuickGraph.Collections;

namespace QuickGraph.Collections
{
    [PexFactoryClass]
    public partial class FibonacciHeapFactory
    {
        [PexFactoryMethod(typeof(FibonacciHeap<int, int>))]
        public static FibonacciHeap<int, int> Create()
        {
            FibonacciHeap<int, int> fibonacciHeap
               = new FibonacciHeap<int, int>();
            return fibonacciHeap;
        }
    }
}
