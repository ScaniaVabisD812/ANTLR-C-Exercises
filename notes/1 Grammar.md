https://tomassetti.me/antlr-mega-tutorial/#allcont

# ANTLR är en parser-generator.

ANTLR är en parser-generator. Den tar text och transformerar denna till en organiserad struktur. Detta kallas för ett parser-tree. Man ger exempel på att parsea HTML med REGEX, vilket inte är en bra idé eftersom det finns för många olika varianter av syntax.

Parsers går att göra för hand, men ANLTR gör det snabbare och smidigare eftersom samma grammatik kan parsea på flera språk.

## Lexer och parser

Lexer = Tar individuella karaktärer och transformerar dem till tokens.
Parser =
Syntax = Namn, kolon, definitionen och semikolon

Exempelvis att siffror som kommer utan mellanrum representerar tal.
Lexer-regler definieras med VERSALER.
Parser-regler definieras med gemener.

Vi vill kunna räkna 437 + 734 som input.

```
/*
 * Parser Rules
 */
operation  : NUMBER '+' NUMBER ;
/*
 * Lexer Rules
 */
NUMBER     : [0-9]+ ;
WHITESPACE : ' ' -> skip ;
```

Förvirrande nog kommer lexer-reglerna oftast efter parser-reglerna trots att de appliceras innan. Ordningen inom lexer-reglerna är däremot som den kommer. Det är exempelvis därför ord som är keywords i SQL definieras som det först.
Följande två regler i ordingsföljd definierar vilken token som skapas:
    1) Longest match --> Kan en token inkludera störst mängd text vinner den
    2) Ordningsföljden bland reglerna --> Förutsatt att matchen är lika lång kommer den första i ordningen att gälla

Lexer-regeln för NUMBER innehåller ett + som säger att det kan vara flera siffror 0-9 som utgör regeln.
Lexer-regln för whitespace finns där för att säga att vi inte behöver hantera det. Skulle den inte vara där skille parser-regeln sett ut såhär:
```
operation  : WHITESPACE* NUMBER WHITESPACE* '+' WHITESPACE* NUMBER;
```

## Lexer

```
/*
 * Lexer Rules
 */
fragment A          : ('A'|'a') ;
fragment S          : ('S'|'s') ;
fragment Y          : ('Y'|'y') ;
fragment H          : ('H'|'h') ;
fragment O          : ('O'|'o') ;
fragment U          : ('U'|'u') ;
fragment T          : ('T'|'t') ;
fragment LOWERCASE  : [a-z] ;
fragment UPPERCASE  : [A-Z] ;
SAYS                : S A Y S ;
SHOUTS              : S H O U T S;
WORD                : (LOWERCASE | UPPERCASE | '_')+ ;
WHITESPACE          : (' ' | '\t') ;
NEWLINE             : ('\r'? '\n' | '\r')+ ;
TEXT                : ('['|'(') ~[\])]+ (']'|')');
```

Text-definitionen är till för att användas i links senare.

Fragments = Återanvändbara delar i lexern. I detta fallet görs det för att inte göra programmet case sensitive.

## Syntax Betydelse

| Syntax | Betydelse |
|---|---|
| [0-9] | Teckenklass: matchar ett enda tecken i intervallet 0–9 |
| '0'..'9' | Range‑operator: matchar tecken från '0' till '9' |
| . | Tecken |
| + | En eller flera repetitioner |
| * | Noll eller flera repetitioner |
| ? | Noll eller en förekomst (valfritt) |
| Straight-pipe | OR |
| ( ... ) | Grupp/subrule |
| [...] | Teckenklass (endast i lexer) |
| ~[...] | Negerad teckenklass: matcha alla tecken som inte finns i klassen |
| fragment | Regel som inte genererar token, används som byggsten |
| RULE : ... ; | Lexer‑regel (stora bokstäver) |
| rule : ... ; | Parser‑regel (små bokstäver) |
| .*? | Nongreedy match (lexer) |

## Rekursion

Innuti måsvingar kollas antingen efter fler måsvingar eller bokstäver. Detta kan användas för att fånga exempelvis hela JSON-filer. Detta innebär att en ACTION-token består av en struktur som både innehåller nästlade strukturer och andra tecken.

```
ACTION : '{' ( ACTION | ~[{}] )* '}' ;
```

Exempelvis är en action token:

```
{
     "a": 1,
     "b": { "c": 2 }
}
```

## Non-greedy

.*? Betyder att matcha följd av tecken (.) fram tills att nästa del i regeln går att applicera.
Exempelvis i

```
COMMENT : '/*' .*? '*/' ;
```
För strängen "/* Hej */" appliceras det fram tills att '*/' börjar gälla.

## Parser

```
/*
 * Parser Rules
 */
chat                : line+ EOF ;
line                : name command message NEWLINE;
message             : (emoticon | link | color | mention | WORD | WHITESPACE)+ ;
name                : WORD WHITESPACE;
command             : (SAYS | SHOUTS) ':' WHITESPACE ;
                                        
emoticon            : ':' '-'? ')'
                    | ':' '-'? '('
                    ;
link                : TEXT TEXT ;
color               : '/' WORD '/' message '/';
mention             : '@' WORD ;
```

Detta är vad programmet kommer att interagera mest med.
EOF = End Of File
Message kan vara vad som helst av reglerna vi definierat i vilken ordning som helst.
Link ändrade senare i dokumentationen eftersom den ursprungliga text-definitionen hade gjort all input till en enda token.

Reglerna är byggda enligt Top-down: helhet --> delar.
“En chatt består av rader. En rad består av namn, kommando och meddelande. Ett meddelande består av olika element.” Exempelvis att först definiera message och sen de byggstenar som utgör message.
Detta spelar däremot ingen roll för parsern.

Color är indirekt rekursiv eftersom messages can innehålla colors och tvärt om.
/red/ this is /blue/ very /green/ nested / /blue/ text /

Innan man börjar använda sin grammar måste man definiera ett namn för den i början av filen

```
grammar Chat;
```

## Aritmetisk parser

### Lexer-regler

```
fragment DIGIT  : [0-9];
NUMBER          : DIGIT+;
PLUS            : '+';
MINUS           : '-';
MULT            : '*';
DIV             : '/';
PARO            : '(';
PARC            : ')';
WHITESPACE      : (' '|'\t')+ -> skip ;
```

### Parser-regler

```
program : expr;
expr   : term ((PLUS | MINUS) term)*;
term   : factor ((MULT | DIV) factor)*;
factor : NUMBER | (PARO expr PARC);
```

Eftersom jag använde ANTLR lab blev paranteserna lite konstiga. Jag kunde inte göra implicit token-definition i parsern så var tvungen att göra lexer-regler.
Det intressanta är ordningen på reglerna i parsern. Prioriteringsreglerna implementeras på detta sättet:
    1) Ett expr är en term eventuellt plus eller minus en annan term
    2) En term är en faktor eventuellt multiplicerat eller dividerat med en annan faktor
    3) En faktor är antingen ett nummer eller ett expr inom paranteser.
Detta gör att prioriteringsreglerna följs ^^

Man får tänka lite som en människa när man ser ett tal 4 * 3 + 2 som (4 * 3) + 2. Då vi adderar med två ser vi parantesen som en helhet.

För "1 + 2 * 3 - (4 + 5)";