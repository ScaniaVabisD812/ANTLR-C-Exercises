
# Setup

Man installererar ANTLR grammar syntax support för VS Code. För varje projekt måste man lägga in NuGet-paketet Antlr4.Runtime.Standard. Även extension kommer med sitt egna commandline-tool. Däremot kan det finnas problem kopplade till versionen av ANTLR detta stödjer.

Sätt in detta i settings.json i VS code…
External gör att extension endast genererar grammar för intern användning, exempelvis för det att rita diagram.

```
"antlr4.generation": {
    "mode": "external",
    "language": "CSharp",
    "listeners": false,
    "visitors": true
}
```

Skapa ett solution och lämplig mängd projekt. Lägga in NuGet-paketet i ett projekt och referens i ett annat såhär:

```bash
# installing the ANTLR4 runtime package in the main console project
dotnet add AntlrCSharp package Antlr4.Runtime.Standard
# adding a reference to the main project in the test one
dotnet add AntlrCSharpTests reference AntlrCSharp 
```

När man nu skapar grammar .g4-filer kommer det att genereras C#-kod.

## Vad som genereras i C#

- ArithmeticLexer — lexern (text → tokens)
    ○ Logik för att göra lexer-delarna, att omvandla text till tokens
- ArithmeticParser — parsern (tokens → parse tree)
    ○ Bygger ett parse-träd av tokens. För varje regel finns en metod och en context-klass
- ArithmeticParser.<RuleName>Context — en context‑klass per parserregel
    ○ Tidigare nämnd contextklass ^^
    ○ Har metoder för underregler och terminaler (tokens)
    ○ Ärver även från ParserRuleContext vilket ger metoder för Children, Start- och Stop-token samt GetText()
    ○ Jag kan fråga varje regel i grammatiken om dess childs
- ArithmeticVisitor<T> — ett interface för visitorn
    ○ Detta är ett interface. Jag tror inte jag som utvecklare har så stor nytta av det.
- ArithmeticBaseVisitor<T> — en bas‑visitor med tomma Visit‑metoder
    ○ Min klass ärver av denna och specificerar en return-typ som alla overrides av metoder returnerar. Exempelvis för Aritmetiska exemplet handlade det om decimal.
- ArithmeticListener — ett interface för listenern
- ArithmeticBaseListener — en bas‑listener med tomma enter/exit‑metoder
- Token‑relaterade saker:
    ○ tokenkonstanter (t.ex. PLUS, MINUS, NUMBER)
    ○ Vocabulary (namn ↔ ID)
    ○ Metoder för GetSymbolicName(int type) från vocabulary ^^
    ○ ev. ruleNames, channelNames, modeNames

Allt detta är “bara” C#‑klasser och interfaces. Man kan öppna och läsa!

## Initiering

```
AntlrInputStream inputStream = new AntlrInputStream(input.ToString());
ArithmetricLexer arithmetricLexer = new ArithmetricLexer(inputStream);
CommonTokenStream commonTokenStream = new CommonTokenStream(arithmetricLexer);
ArithmetricParser arithmetricParser = new ArithmetricParser(commonTokenStream);
var tree = arithmetricParser.expr();
```

I detta fallet vet jag att det är expr jag vill ha eftersom jag känner till min hierarki. Jag kommer ha exakt en expr och denna kommer innehålla flera childs. Exempelvis träd;


## MyVisitor-klass

Nu vill jag kunna göra beräkningar med den strukturen/ordningen parsern har gjort av min input. Man kan säga att ANTLR har stått för att tala om i vilken ordning jag ska räkna allting och C# behöver användas för att göra själva beräkningen.

I detta fallet har jag tre intressanta parser-regler jag behöver kunna hantera:
- Expr (+ eller -) med:
- Terms (* eller /) med:
- Factors (nummer eller paranteser)

För Expr och Term behöver jag kunna räkna med respektive två räknesätt mellan en obestämd mängd tal.
För Faktor behöver jag kunna avgöra om jag kan göra en direkt omvandling från en token NUMBER till en decimal eller besöka expression.

I ArithmeticBaseVisitor finns virtuella visit-metoder för samtliga parser-regler. Dessa overridar jag med den return-typen min klass ska använda och den inputen som finns, vilket är context för den specifika typen. Detta gör jag i min egna MyVisitor-klass, som ärver av:

```
class MyVisitor : ArithmetricBaseVisitor<decimal>
```

I mina Visit-metoder är poängen att besöka ett Expr/Term/Factor och returnera den decimal som helheten består av.
För VisitExpr och VisitTerm sker det genom att jag hämtar alla childs i form av Terms för Expr och Factors för Terms. 

I trädet ovan syns hur operand-grenarna är TerminalNodes (rena tokens). För att hämta alla dem vill jag alltså hämta alla children of type ITerminalNode, kör dess .GetText()-metod för att få ut en string av vad det är och skapar en lista. Att göra en .GetText() är möjligt för både tokens och parse-regler.
Själva uträkningen är ganska C#-ren. Jag hämtar utgångsvärdet genom att besöka första Term och utför operationer gentemot en right.

```
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
```

Eftersom en factor antingen kan vara ett expr inom parantes (som går att besöka och få en decimal) kollar jag först om det innehåller en number-token, om inte kollar jag om det finns en expr-token och returnerar decimalen av att besöka den. Här skulle jag också exempelvis kunna få fram en context.PARO och en context.PARC eftersom det också kan vara tokens.

```
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
```

## Lärdomar

- Arbetssättet är att utgå från vilka parser-regler jag vill kunna besöka och omvandla enheten i trädet till. Man skapar en VisitorClass med rätt typ och overridar de metoder man vill ha. 
- För varje context (input) går det att hämta listor av eventuella childs genom att skriva .[namn](). Man kan alternativt hämta .children och filtrera på typ för att få ut exempelvis terminalnodes då man vill ha en lista över exempelvis tokens som inte är samma, som operanderna.
- Visitor pattern är bra här för att logik för exempelvis för att hämta decimaler av ett träd inte kräver någon som helst implementation i de faktiska klasserna, utan i en separat Visitor-klass.
