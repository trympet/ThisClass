using System;

namespace SampleApp;

[ThisClass]
partial class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"Full name: {ThisClass.FullName}");
    }

    [ThisClass]
    partial record Record
    {

    }

    [ThisClass]
    partial record struct RecordStruct
    {

    }

    [ThisClass]
    partial struct Struct
    {

    }
}
