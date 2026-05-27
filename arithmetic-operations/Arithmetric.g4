grammar Arithmetric;
/*
 * Parser Rules
 */
expr   : term ((PLUS | MINUS) term)*;
term   : factor ((MULT | DIV) factor)*;
factor : NUMBER | (PARO expr PARC);

/*
 * Lexer Rules
 */
fragment DIGIT  : [0-9];
NUMBER          : DIGIT+;
PLUS            : '+';
MINUS           : '-';
MULT            : '*';
DIV             : '/';
PARO            : '(';
PARC            : ')';
WHITESPACE      : (' '|'\t')+ -> skip ;
NEWLINE         : ('\r'? '\n' | '\r')+ -> skip;