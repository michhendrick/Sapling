namespace Sapling.Nodes;
using Sapling.Logging;

/// <summary>
/// This class represents a node within a sapling program's AST
/// </summary>
internal abstract class SlNode
{
    /// <summary>
    /// The logger used by the node
    /// </summary>
    private Logger _logger;

    /// <summary>
    /// The logger used by the node
    /// </summary>
    public Logger Logger => _logger;

    /// <summary>
    /// The module used by the node
    /// </summary>
    private LLVMSharp.LLVMModuleRef _module;

    /// <summary>
    /// The module used by the node
    /// </summary>
    public LLVMSharp.LLVMModuleRef Module => _module;

    /// <summary>
    /// The scope used by the node
    /// </summary>
    private SlScope _scope;

    /// <summary>
    /// The scope used by the node
    /// </summary>
    public SlScope Scope => _scope;

    /// <summary>
    /// Construct a new SlNode
    /// </summary>
    public SlNode(Logger logger, LLVMSharp.LLVMModuleRef module, SlScope scope)
    {
        _logger = logger;
        _module = module;
        _scope = scope;
    }

    /// <summary>
    /// Base method for LLVM generation
    /// </summary>
    public void GenerateCode(Logger logger)
    {
        logger.Add("Calling code generation base method");
        throw new Exception("Code Generation Base Method Called; Something went wrong for us to get here.");
    }
}