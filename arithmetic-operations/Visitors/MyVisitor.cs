using Antlr4.Runtime.Tree;

class MyVisitor : ArithmetricBaseVisitor<decimal>
{
    public override decimal VisitExpr(ArithmetricParser.ExprContext context)
    {
        // Hämtar alla TermContexts som finns för mitt expression
        var termContexts = context.term();
        
        // Hämta alla children av terminalnod-typs GetText() till en lista
        var operators = context.children
        .OfType<ITerminalNode>()
        .Select(n => n.GetText())
        .ToList();

        // Sätt en utgångspunkt
        decimal left = VisitTerm(termContexts[0]);
        
        // För varje resterande term, använd rätt operator mellan left och right
        for(int i = 1; i < termContexts.Length; i++)
        {
            var op = operators[i-1];
            decimal right = VisitTerm(termContexts[i]);

            left = op switch
            {
                "+" => left + right,
                "-" => left - right,
                _   => throw new Exception($"Unknown operator: {op}")
            };
        }

        return left;
    }
    public override decimal VisitTerm(ArithmetricParser.TermContext context)
    {
        // Hämtar alla FactorContexts som finns för min Term
        var factorContexts = context.factor();
        
        // Hämta alla children av terminalnod-typs GetText() till en lista
        var operators = context.children
        .OfType<ITerminalNode>()
        .Select(n => n.GetText())
        .ToList();

        // Sätt en utgångspunkt
        decimal left = VisitFactor(factorContexts[0]);
        
        // För varje resterande term, använd rätt operator mellan left och right
        for(int i = 1; i < factorContexts.Length; i++)
        {
            var op = operators[i-1];
            decimal right = VisitFactor(factorContexts[i]);

            left = op switch
            {
                "*" => left * right,
                "/" => left / right,
                _   => throw new Exception($"Unknown operator: {op}")
            };
        }

        return left;
    }
    public override decimal VisitFactor(ArithmetricParser.FactorContext context)
    {
        if(context.NUMBER() != null)
        {
            return decimal.Parse(context.NUMBER().GetText());
        }
        if(context.expr() != null)
        {
            return VisitExpr(context.expr());
        }

        throw new Exception("Invalid factor");
    }
}