using System;
using Mcma.Core.Serialization;
using Mcma.Aws;

namespace Mcma.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            McmaTypes.Add<S3Locator>();
            
            SerializationTests.RunAll();
        }
    }
}
