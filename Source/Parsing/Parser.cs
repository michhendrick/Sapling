namespace Sapling;
using Sapling.Logging;
using Sapling.Tokens;
using Sapling.Nodes;
using static Sapling.UtilFuncs;

/// <summary>
/// Class <c>Parser</c> converts a list of tokens into an abstract syntax tree.
/// </summary>
internal class Parser
{
    /// <summary>
    /// The tokens to parse.
    /// </summary>
    private LinkedList<Token> _tokens;
    
    /// <summary>
    /// The _current token.
    /// </summary>
    private LinkedListNode<Token>? _current;

    /// <summary>
    /// The logger to use.
    /// </summary>
    private Logger _logger;

    /// <summary>
    /// The base module to use
    /// </summary>
    private LLVMSharp.LLVMModuleRef _module = LLVMSharp.LLVM.ModuleCreateWithName("root");

    /// <summary>
    /// The valid literal types
    /// </summary>
    private List<string> _literals = new List<string> { 
        nameof(Sapling.Tokens.Boolean), 
        nameof(Sapling.Tokens.Character), 
        nameof(Sapling.Tokens.String), 
        nameof(Sapling.Tokens.Float), 
        nameof(Sapling.Tokens.Integer),
    };

    /// <summary>
    /// The valid operators
    /// </summary>
    private List<string> _operators = new List<string> { 
        nameof(Sapling.Tokens.BooleanOperator), 
        nameof(Sapling.Tokens.ArithmeticOperator), 
        nameof(Sapling.Tokens.ComparisonOperator), 
    };

    /// <summary>
    /// This construsts a new Parser.
    /// <example>
    /// For example:
    /// <code>
    /// Parser p = new Parser(tokens, log);
    /// </code>
    /// will create a new Parser, which can then be used to generate the AST for a set of tokens.
    /// </example>
    /// </summary>
    public Parser(IEnumerable<Token> tokens, Logger logger)
    {
        _tokens = new LinkedList<Token>(tokens);
        _logger = logger;
        _current = GetFirstNode();
    }

    /// <summary>
    /// Log a token.
    /// </summary>
    private void LogToken(Token token)
    {
        _logger.Add($"{token.GetType()} \"{token.Value}\" at {token.StartIndex} to {token.EndIndex}");
    }

    /// <summary>
    /// This method gets the next node from the linked list.
    /// </summary>
    private LinkedListNode<Token>? GetNextNode()
    {
        // Return null if it was already null
        if (_current is null) return null;

        // Get the next node
        _current = _current.Next;
        if (_current is null) return null;

        // Ignore the comments
        if (TypeEquivalence(typeof(Tokens.Comment), _current.Value.GetType())) 
        {    
            _current = GetNextNode();
            return _current;
        }

        // Get the _current token
        Token token = _current.Value;
        LogToken(token);
        
        // Return the _current token
        return _current;
    }

    /// <summary>
    /// This method gets the first node from the linked list.
    /// </summary>
    private LinkedListNode<Token>? GetFirstNode()
    {
        // Get the next node
        _current = _tokens.First;
        if (_current is null) return null;

        // Ignore the comments
        if (TypeEquivalence(typeof(Tokens.Comment), _current.Value.GetType())) 
        {    
            _current = GetNextNode();
            return _current;
        }

        // Get the _current token
        Token token = _current.Value;
        LogToken(token);
        
        // Return the _current token
        return _current;
    }

    /// <summary>
    /// This method generates the AST from the tokens returned by the lexer.
    /// </summary>
    public AST Parse()
    {
        // Create the global scope
        SlScope global = new SlScope();

        // Create a new instance of an abstract syntax tree
        SlMethod root = new SlMethod(_logger, _module, global, "int");
        AST ast = new AST(root, _module, global, _logger);

        // Parse the tokens and create AST nodes from them
        while (_current != null)
        {
            AddNextNode(root, global);
        }

        // Return the tree
        return ast;
    }

    /// <summary>
    /// This method adds a new node to an SlMethod
    /// </summary>
    public void AddNextNode(SlMethod method, SlScope scope)
    {
        if (_current is null) throw new Exception("Gadzooks! We are trying to add a statement when there are none to be had!!");

        switch (_current.Value.GetType().Name)
        {

            case (nameof(Sapling.Tokens.SaplingType)):

                // Assign to methods and classes as needed, else assign to a property
                if (_current.Value.Value == "method") method.Add(ParseAssignMethod(scope));
                else if (_current.Value.Value == "class") method.Add(ParseAssignClass(scope));
                else method.Add(ParseAssignProperty(scope));
                break;

            case (nameof(Sapling.Tokens.Keyword)):

                // Parse the return statement if the keyword is return, if it isnt, throw an error
                if (_current.Value.Value == "return") method.Add(ParseReturn(scope));
                else throw new Exception($"Unexpected {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}."); 
                break;

            case (nameof(Sapling.Tokens.ID)):
            case (nameof(Sapling.Tokens.Builtin)):

                // Parse a identifier as a method if it is immediately followed by a left parenthesis, otherwise parse it as an expression
                if (_current.Next is not null && _current.Next.Value.Value == "(") method.Add(ParseMethodCall(scope));
                else throw new Exception($"Unexpected {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}."); 
                break;

            default:
                throw new Exception($"Gadzooks! There was an unexpected token of type {_current.Value.GetType().Name} at the end of the parser!! Double check your syntax!");

        }
    }

    /// <summary>
    /// This method parses an assignment operator for a property and adds the needed nodes to the AST.
    /// </summary>
    private SlAssignProperty ParseAssignProperty(SlScope scope)
    {
        if (_current is null) throw new Exception("Trying to parse null type!!");
        string type = _current.Value.Value;
        GetNextNode(); // Consume type

        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        string identifier = _current.Value.Value;
        GetNextNode(); // Consume identifier

        if (_current is null) throw new Exception("Trying to parse null assignment!!");
        else if (!(nameof(Sapling.Tokens.Assign) == _current.Value.GetType().Name)) throw new Exception($"Missing Assignment Operator!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume = sign

        if (_current is null) throw new Exception("Trying to parse null expression!!");
        SlExpression expression = ParseExpression(scope); // Consume the expression

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == ";")) throw new Exception($"Missing Semicolon!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume ;

        return new SlAssignProperty(_logger, _module, type, identifier, expression, scope);
    }

    /// <summary>
    /// This method parses an assignment operator for a method and adds the needed nodes to the AST.
    /// </summary>
    private SlAssignMethod ParseAssignMethod(SlScope scope)
    {
        // TODO currently method syntax only allows int return with no input args
        // TODO somehow replace method type with something which can actually get the return type of the method 
        // TODO replace = sign with the method arguments

        if (_current is null) throw new Exception("Trying to parse null type!!");
        string type = "int";
        GetNextNode(); // Consume method type

        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        string identifier = _current.Value.Value;
        GetNextNode(); // Consume identifier

        if (_current is null) throw new Exception("Trying to parse null assignment!!");
        else if (!(nameof(Sapling.Tokens.Assign) == _current.Value.GetType().Name)) throw new Exception($"Missing Assignment Operator!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume = sign

        if (_current is null) throw new Exception("Trying to parse null expression!!");
        SlMethod method = ParseMethod(scope, type); // Consume the method

        return new SlAssignMethod(_logger, _module, identifier, method, scope);
    }

    /// <summary>
    /// This method parses an assignment operator for a function and adds the needed nodes to the AST.
    /// </summary>
    private SlAssignClass ParseAssignClass(SlScope scope)
    {
        // A list of subclasses, methods, and properties
        List<SlStatement> statements = new List<SlStatement>();

        if (_current is null) throw new Exception("Trying to parse null type!!");
        string type = _current.Value.Value;
        GetNextNode(); // Consume type

        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        string identifier = _current.Value.Value;
        GetNextNode(); // Consume identifier

        if (_current is null) throw new Exception("Trying to parse null assignment!!");
        else if (!(nameof(Sapling.Tokens.Assign) == _current.Value.GetType().Name)) throw new Exception($"Missing Assignment Operator!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume = sign

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == "{}")) throw new Exception($"Missing Opening Brace!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume {
            
        if (_current is null) throw new Exception("Trying to parse null class!!");
        SlClass slClass = ParseClass(scope); // Consume the class

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == "}")) throw new Exception($"Missing Closing Brace!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume }

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == ";")) throw new Exception($"Missing Semicolon!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume ;

        return new SlAssignClass(_logger, _module, identifier, slClass, scope);
    }

    /// <summary>
    /// This method parses a method and adds the needed nodes to the AST.
    /// </summary>
    public SlMethod ParseMethod(SlScope parentScope, string retType)
    {
        // Create the method scope
        SlScope scope = new SlScope(parentScope);

        // Create a new instance of a method
        SlMethod method = new SlMethod(_logger, _module, scope, retType);

        GetNextNode(); // Remove the {

        // Parse the tokens and create AST nodes from them
        while (_current != null && _current.Value.Value != "}")
        {
            AddNextNode(method, scope);
        }

        GetNextNode(); // Remove the }
        return method;
    }

    /// <summary>
    /// This method parses a class and adds the needed nodes to the AST.
    /// </summary>
    public SlClass ParseClass(SlScope parentScope)
    {
        // Create the class scope
        SlScope scope = new SlScope(parentScope);
        SlClass slClass = new SlClass(_logger, _module, scope);

        while(_current != null && _current.Value.GetType().Name == typeof(Sapling.Tokens.SaplingType).Name)
        {
            if (_current.Value.Value == "method") slClass.Add(ParseAssignMethod(scope)); // Add a method
            else if (_current.Value.Value == "class") slClass.Add(ParseAssignClass(scope)); // Add a subclass
            else slClass.Add(ParseAssignProperty(scope)); // Add a property
        }

        return slClass;
    }

    /// <summary>
    /// This method parses an expression and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseExpression(SlScope scope)
    {
        return HandleExpressionLookahead(scope, ParseSingleExpression(scope));
    }

    /// <summary>
    /// This method parses a single expression and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseSingleExpression(SlScope scope)
    {
        // THIS NEEDS TO EXIST FOR THE OPTREE. It ensures we parse one expression at a time, without just making the entire optree evaluate left to right.
        // This shouldn't ever be reached, I just want to get rid of the warning here
        if (_current is null) throw new Exception("Trying to parse null expression!!");
        else if (nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == "(")
        {
            // This is an expression in parentheses
            return ParseParenExpression(scope);
        }
        else if (_literals.Contains(_current.Value.GetType().Name))
        {
            // This is just a literal
            SlExpression expression = new SlLiteralExpression(_logger, _module, _current.Value.GetType().Name, _current.Value.Value, scope);
            GetNextNode(); // Consume the literal
            return expression;
        }
        else if (nameof(Sapling.Tokens.ID) == _current.Value.GetType().Name && _current.Next is not null && _current.Next.Value.Value != "(")
        {
            // This is an identifier
            return ParseIdentifier(scope);
        }
        else if (nameof(Sapling.Tokens.ID) == _current.Value.GetType().Name || nameof(Sapling.Tokens.Builtin) == _current.Value.GetType().Name)
        {
            // This is a method call as an expression
            return ParseMethodCallAsExpression(scope);
        }
        else throw new Exception("Invalid Expression!!");
    }

    /// <summary>
    /// Handle expression lookahead.
    /// </summary>
    private SlExpression HandleExpressionLookahead(SlScope scope, SlExpression ex)
    {
        if (_current is not null && _current.Value.Value == "?")
        {
            // Time to parse a ternary
            return ParseTernary(scope, ex);
        }
        else if (_current is not null && _current.Next is not null && _operators.Contains(_current.Value.GetType().Name))
        {
            // Time to parse an optree
            return ParseOptree(scope, ex);
        }
        // Its just a normal expression
        else return ex;
    }

    /// <summary>
    /// This method parses a ternary expression and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseTernary(SlScope scope, SlExpression cond)
    {
        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Ternary) == _current.Value.GetType().Name && _current.Value.Value == "?")) throw new Exception($"Missing Ternary Operator (?)!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume ?
            
        if (_current is null) throw new Exception("Trying to parse null expression!!");
        SlExpression valIfTrue = ParseExpression(scope); // Consume the first expression

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Ternary) == _current.Value.GetType().Name && _current.Value.Value == ":")) throw new Exception($"Missing Ternary Else Operator (:)!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume :
            
        if (_current is null) throw new Exception("Trying to parse null expression!!");
        SlExpression valIfFalse = ParseExpression(scope); // Consume the second expression

        return HandleExpressionLookahead(scope, new SlTernaryExpression(_logger, _module, cond, valIfTrue, valIfFalse, scope));
    }

    /// <summary>
    /// This method parses an optree and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseOptree(SlScope scope, SlExpression ex)
    {
        // A list of expressions in the optree. The optree should start with the initial left hand expression ex
        List<SlExpression> expressions = new List<SlExpression>{ex}; 
        List<SlOperator> operators = new List<SlOperator>(); 

        // While the current node is an operator, add it and the expression following it
        while (_current is not null && _operators.Contains(_current.Value.GetType().Name))
        {
            operators.Add(ParseOperator(scope));
            expressions.Add(ParseSingleExpression(scope));
        }

        // This will be used to actually build a tree from the expressions and operators
        SlOptree rawOptree = SlOptreeFactory.CreateInstance(_logger, _module, expressions, operators, scope);

        // Create a parsedOptree from the raw optree
        return HandleExpressionLookahead(scope, new SlParsedOptree(_logger, _module, rawOptree, scope));
    }

    /// <summary>
    /// This method parses a parensthetical expression and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseParenExpression(SlScope scope)
    {
        if (_current is null) throw new Exception("Trying to parse null delimeter.");
        else if (_current.Value.Value != "(") throw new Exception($"Was expecting (, instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume the (

        SlExpression expression = ParseExpression(scope); // Parse the inner Expression
        
        if (_current is null) throw new Exception("Trying to parse null delimeter.");
        else if (_current.Value.Value != ")") throw new Exception($"Was expecting ), instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume the )

        return HandleExpressionLookahead(scope, expression);
    }

    /// <summary>
    /// This method parses an identifier and adds the needed nodes to the AST.
    /// </summary>
    private SlIdentifierExpression ParseIdentifier(SlScope scope)
    {
        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        SlIdentifierExpression identifierExpression = new SlIdentifierExpression(_logger, _module, _current.Value.Value, scope);
        GetNextNode();
        return identifierExpression;
    }

    /// <summary>
    /// This method parses an operator and adds the needed nodes to the AST.
    /// </summary>
    private SlOperator ParseOperator(SlScope scope)
    {
        if (_current is null) throw new Exception("Trying to parse null operator!!");
        SlOperator op = new SlOperator(_logger, _module, _current.Value.Value, scope);
        GetNextNode(); // Consume operator
        return op;
    }

    /// <summary>
    /// This method parses a return statement and adds the needed nodes to the AST.
    /// </summary>
    private SlReturn ParseReturn(SlScope scope)
    {
        GetNextNode(); // Consume Return   
        SlExpression expression = ParseExpression(scope);
        GetNextNode(); // Consume Delimiter
        return new SlReturn(_logger, _module, expression, scope);
    }

    /// <summary>
    /// This method calls a method and adds the needed nodes to the AST.
    /// </summary>
    private SlMethodCall ParseMethodCall(SlScope scope)
    {
        // The arguements to the method
        List<SlExpression> args = new List<SlExpression>{}; 

        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        string identifier = _current.Value.Value;
        GetNextNode(); // Consume identifier

        if (_current is null) throw new Exception("Trying to parse null delimeter.");
        else if (_current.Value.Value != "(") throw new Exception($"Was expecting (, instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume (

        if (_current is null) throw new Exception("Trying to parse null method args.");
        else if (_current.Value.Value != ")")
        {
            // Get the args of the method
            args.Add(ParseExpression(scope));
            while (_current is not null && _current.Value.Value == ",")
            {
                GetNextNode(); // Consume ,
                args.Add(ParseExpression(scope));
            }
        }

        if (_current is null) throw new Exception("Trying to parse null delimeter.");
        else if (_current.Value.Value != ")") throw new Exception($"Was expecting ), instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume )

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == ";")) throw new Exception($"Missing Semicolon!! Instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume ;

        return new SlMethodCall(_logger, _module, identifier, args, scope);
    }

    /// <summary>
    /// This method calls a method (gets the returned expression) and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseMethodCallAsExpression(SlScope scope)
    {
        // The arguements to the method
        List<SlExpression> args = new List<SlExpression>{}; 

        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        string identifier = _current.Value.Value;
        GetNextNode(); // Consume identifier

        if (_current is null) throw new Exception("Trying to parse null delimeter.");
        else if (_current.Value.Value != "(") throw new Exception($"Was expecting (, instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume (

        if (_current is null) throw new Exception("Trying to parse null method args.");
        else if (_current.Value.Value != ")")
        {
            // Get the args of the method
            args.Add(ParseExpression(scope));
            while (_current is not null && _current.Value.Value == ",")
            {
                GetNextNode(); // Consume ,
                args.Add(ParseExpression(scope));
            }
        }

        if (_current is null) throw new Exception("Trying to parse null delimeter.");
        
        else if (_current.Value.Value != ")") throw new Exception($"Was expecting ), instead got {_current.Value.GetType().Name} '{_current.Value.Value}' in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}.");
        GetNextNode(); // Consume )
        
        // It is important to note we actually should not consume the semicolon here, as the expression will consume it
        return new SlMethodCallAsExpression(_logger, _module, identifier, args, scope);
    }
}