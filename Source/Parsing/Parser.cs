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
    /// <example>
    private void LogToken(Token token)
    {
        _logger.Add($"{token.GetType()} \"{token.Value}\" at {token.StartIndex} to {token.EndIndex}");
    }

    /// <summary>
    /// Get the next node from the linked list.
    /// <example>
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
    /// Get the first node from the linked list.
    /// <example>
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
        // Create a new instance of an abstract syntax tree
        SlMethod root = new SlMethod();
        AST ast = new AST(root, _logger);

        // Parse the tokens and create AST nodes from them
        while (_current != null)
        {
            AppendNextNode(root);
        }

        // Return the tree
        return ast;
    }

    /// <summary>
    /// This method appends a new node to an SlMethod
    /// </summary>
    public void AppendNextNode(SlMethod method)
    {
        if (_current is null) throw new Exception("Gadzooks! We are trying to add a statement when there are none to be had!!");

        switch (_current.Value.GetType().Name)
        {

            case (nameof(Sapling.Tokens.SaplingType)):

                // Assign to methods and classes as needed, else assign to a property
                if (_current.Value.Value == "method") method.Append(ParseAssignMethod());
                else if (_current.Value.Value == "class") method.Append(ParseAssignClass());
                else method.Append(ParseAssignProperty());
                break;

            case (nameof(Sapling.Tokens.Keyword)):

                // Parse the return statement if the keyword is return, if it isnt, throw an error
                if (_current.Value.Value == "return") method.Append(ParseReturn());
                else throw new Exception($"Unexpected keyword \"{_current.Value.Value}\" in input string from {_current.Value.StartIndex} to {_current.Value.EndIndex}."); 
                break;

            case (nameof(Sapling.Tokens.ID)):

                // Parse a identifier as a method if it is immediately followed by a left parenthesis, otherwise parse it as an expression
                if (_current.Next is not null && _current.Next.Value.Value == "(") method.Append(ParseMethodCall());
                else throw new Exception($"Unexpected identifier \"{_current.Value.Value}\" in input string at {_current.Value.StartIndex} to {_current.Value.EndIndex}."); 
                break;

            default:
                throw new Exception("Gadzooks! There was an unexpected token at the end of the parser!!");

        }
    }

    /// <summary>
    /// This method parses an assignment operator for a property and adds the needed nodes to the AST.
    /// </summary>
    private SlAssignProperty ParseAssignProperty()
    {
        if (_current is null) throw new Exception("Trying to parse null type!!");
        string type = _current.Value.Value;
        GetNextNode(); // Consume type

        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        string identifier = _current.Value.Value;
        GetNextNode(); // Consume identifier

        if (_current is null) throw new Exception("Trying to parse null assignment!!");
        else if (!(nameof(Sapling.Tokens.Assign) == _current.Value.GetType().Name)) throw new Exception($"Missing Assignment Operator!! Instead got {_current.Value.Value}");
        GetNextNode(); // Consume = sign

        if (_current is null) throw new Exception("Trying to parse null expression!!");
        SlExpression expression = ParseExpression(); // Consume the expression

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == ";")) throw new Exception($"Missing Semicolon!! Instead got {_current.Value.Value}");
        GetNextNode(); // Consume ;

        return new SlAssignProperty(type, identifier, expression);
    }

    /// <summary>
    /// This method parses an assignment operator for a method and adds the needed nodes to the AST.
    /// </summary>
    private SlAssignMethod ParseAssignMethod()
    {
        if (_current is null) throw new Exception("Trying to parse null type!!");
        string type = _current.Value.Value;
        GetNextNode(); // Consume type

        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        string identifier = _current.Value.Value;
        GetNextNode(); // Consume identifier

        if (_current is null) throw new Exception("Trying to parse null assignment!!");
        else if (!(nameof(Sapling.Tokens.Assign) == _current.Value.GetType().Name)) throw new Exception($"Missing Assignment Operator!! Instead got {_current.Value.Value}");
        GetNextNode(); // Consume = sign

        if (_current is null) throw new Exception("Trying to parse null expression!!");
        SlMethod method = ParseMethod(); // Consume the method

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == ";")) throw new Exception($"Missing Semicolon!! Instead got {_current.Value.Value}");
        GetNextNode(); // Consume ;

        return new SlAssignMethod(identifier, method);
    }

    /// <summary>
    /// This method parses an assignment operator for a function and adds the needed nodes to the AST.
    /// </summary>
    private SlAssignClass ParseAssignClass()
    {
        // A list of subclasses
        List<SlAssignClass> subclasses = new List<SlAssignClass>();
        // A list of methods
        List<SlAssignMethod> methods = new List<SlAssignMethod>();
        // A list of properties
        List<SlAssignProperty> properties = new List<SlAssignProperty>();

        if (_current is null) throw new Exception("Trying to parse null type!!");
        string type = _current.Value.Value;
        GetNextNode(); // Consume type

        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        string identifier = _current.Value.Value;
        GetNextNode(); // Consume identifier

        if (_current is null) throw new Exception("Trying to parse null assignment!!");
        else if (!(nameof(Sapling.Tokens.Assign) == _current.Value.GetType().Name)) throw new Exception($"Missing Assignment Operator!! Instead got {_current.Value.Value}");
        GetNextNode(); // Consume = sign

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == "{}")) throw new Exception($"Missing Opening Brace!! Instead got {_current.Value.Value}");
        GetNextNode(); // Consume {

        while(_current.Value.GetType().Name == typeof(Sapling.Tokens.SaplingType).Name)
        {
            if (_current.Value.Value == "method") methods.Append(ParseAssignMethod()); // Add to methods
            else if (_current.Value.Value == "class") subclasses.Append(ParseAssignClass()); // Add to subclasses
            else properties.Append(ParseAssignProperty()); // Add to properties
        }

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == "}")) throw new Exception($"Missing Closing Brace!! Instead got {_current.Value.Value}");
        GetNextNode(); // Consume }

        if (_current is null) throw new Exception("Trying to parse null delimiter!!");
        else if (!(nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == ";")) throw new Exception($"Missing Semicolon!! Instead got {_current.Value.Value}");
        GetNextNode(); // Consume ;

        return new SlAssignClass(identifier, subclasses, methods, properties);
    }

    /// <summary>
    /// This method parses an optree and adds the needed nodes to the AST.
    /// </summary>
    private SlOptree ParseOptree()
    {
        // A list of expressions in the optree
        List<SlExpression> expressions = new List<SlExpression>(); 
        List<SlOperator> operators = new List<SlOperator>(); 

        // Add the initial expression to the optree
        expressions.Append(ParseSingleExpression());

        // While the current node is an operator, append it and the expression following it
        while (_current is not null && _operators.Contains(_current.Value.GetType().Name))
        {
            operators.Append(ParseOperator());
            expressions.Append(ParseSingleExpression());
        }

        // Create an Optree of the expressions and operators
        return new SlOptree(expressions, operators);
    }

    /// <summary>
    /// This method parses a method and adds the needed nodes to the AST.
    /// </summary>
    public SlMethod ParseMethod()
    {
        // Create a new instance of a method
        SlMethod method = new SlMethod();

        GetNextNode(); // Remove the {

        // Parse the tokens and create AST nodes from them
        while (_current != null && _current.Value.Value != "}")
        {
            AppendNextNode(method);
        }

        GetNextNode(); // Remove the }
        return method;
    }

    /// <summary>
    /// This method parses a full expression and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseExpression()
    {
        // This shouldn't ever be reached, I just want to get rid of the warning here
        if (_current is null) throw new Exception("Trying to parse null expression!!");
        else if (_current.Next is not null && _current.Next.Next is not null && _operators.Contains(_current.Next.Value.GetType().Name))
        {
            return ParseOptree();
        }
        else return ParseSingleExpression();
    }

    /// <summary>
    /// This method parses a single expression and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseSingleExpression()
    {
        // This shouldn't ever be reached, I just want to get rid of the warning here
        if (_current is null) throw new Exception("Trying to parse null expression!!");
        else if (nameof(Sapling.Tokens.ID) == _current.Value.GetType().Name)
        {
            // This is an identifier
            return ParseIdentifier();
        }
        else if (nameof(Sapling.Tokens.Delimeter) == _current.Value.GetType().Name && _current.Value.Value == "(")
        {
            // This is an expression in parentheses
            return ParseParenExpression();
        }
        else if (_literals.Contains(_current.Value.GetType().Name))
        {
            // This is just a value
            SlExpression expression = new SlLiteralExpression(_current.Value.GetType().Name, _current.Value.Value);
            GetNextNode(); // Consume the value
            return expression;
        }
        else throw new Exception("Invalid Expression!!");
    }

    /// <summary>
    /// This method parses a parensthetical expression and adds the needed nodes to the AST.
    /// </summary>
    private SlExpression ParseParenExpression()
    {
        GetNextNode(); // Consume the (
        SlExpression expression = ParseExpression(); // Parse the inner Expression
        GetNextNode(); // Consume the )
        return expression;
    }

    /// <summary>
    /// This method parses an identifier and adds the needed nodes to the AST.
    /// </summary>
    private SlIdentifierExpression ParseIdentifier()
    {
        if (_current is null) throw new Exception("Trying to parse null identifier!!");
        SlIdentifierExpression identifierExpression = new SlIdentifierExpression(_current.Value.Value);
        GetNextNode();
        return identifierExpression;
    }

    /// <summary>
    /// This method parses an operator and adds the needed nodes to the AST.
    /// </summary>
    private SlOperator ParseOperator()
    {
        if (_current is null) throw new Exception("Trying to parse null operator!!");
        SlOperator op = new SlOperator(_current.Value.Value);
        GetNextNode(); // Consume operator
        return op;
    }

    /// <summary>
    /// This method parses a return statement and adds the needed nodes to the AST.
    /// </summary>
    private SlReturn ParseReturn()
    {
        GetNextNode(); // Consume Return
        SlExpression expression = ParseExpression();
        GetNextNode(); // Consume Delimiter
        return new SlReturn(expression);
    }

    /// <summary>
    /// This method calls a method and adds the needed nodes to the AST.
    /// </summary>
    private SlMethodCall ParseMethodCall()
    {
        // This one will be complicated because we also have to parse arguments and consume all of them, then actually execute the right code
        // TODO
        return new SlMethodCall();
    }
}