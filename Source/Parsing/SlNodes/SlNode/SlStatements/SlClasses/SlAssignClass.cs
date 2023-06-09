namespace Sapling.Nodes;
using Sapling.Logging;

/// <summary>
/// A valid assign class statement within the sapling programming language
/// </summary>
internal class SlAssignClass: SlStatement
{
    /// <summary>
    /// The identifier of the class
    /// </summary>
    private string _classIdentifier;
    
    /// <summary>
    /// The class
    /// </summary>
    private SlClass _slClass;

    /// <summary>
    /// Construct a new SlAssignClass
    /// </summary>
    public SlAssignClass(Logger logger, LLVMSharp.LLVMModuleRef module, string identifier, SlClass slClass, SlScope scope): base(logger, module, scope)
    {
        _classIdentifier = identifier;
        _slClass = slClass;
    }
    
    /// <summary>
    /// Generate code for a LLVM assign class statement
    /// <example>
    public override void GenerateCode(LLVMSharp.LLVMBuilderRef builder, LLVMSharp.LLVMBasicBlockRef entry)
    {
        // TODO
        Logger.Add("Generating code for SlAssignClass");
    }
}