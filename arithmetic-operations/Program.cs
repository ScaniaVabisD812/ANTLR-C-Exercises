using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

class Program
{
    private static void Main(string[] args)
    {
        string? input = "";
        do
        {
            Console.WriteLine("Input the chat.");
            input = Console.ReadLine();
            if(input != null)
            {
                try
                {
                    AntlrInputStream inputStream = new AntlrInputStream(input.ToString());
                    ArithmetricLexer arithmetricLexer = new ArithmetricLexer(inputStream);
                    CommonTokenStream commonTokenStream = new CommonTokenStream(arithmetricLexer);
                    ArithmetricParser arithmetricParser = new ArithmetricParser(commonTokenStream);
                    var tree = arithmetricParser.expr();

                    MyVisitor myVisitor = new MyVisitor();
                    decimal d = myVisitor.VisitExpr(tree);

                    Console.WriteLine(d);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex);
                }
            }
            else
            {
                Console.WriteLine("You need to write an input");
            }
        } while(input != "exit");
    }
}