namespace Sapling.Nodes;
using Sapling.Logging;

/// <summary>
/// A valid method call statement within the sapling programming language
/// </summary>
internal class SlMethodCallAsExpression: SlExpression
{
    /// <summary>
    /// The method call
    /// </summary>
    private SlMethodCall _methodCall;

    /// <summary>
    /// Construct a new SlMethodCall
    /// </summary>
    public SlMethodCallAsExpression(Logger logger, LLVMSharp.LLVMModuleRef module, string identifier, List<SlExpression> args, SlScope scope): base(logger, module, scope.FindFunctionTypeString(logger, scope.GetFunctionType(logger, identifier)), scope)
    {
        _methodCall = new SlMethodCall(logger, module, identifier, args, scope);
    }
    
    /// <summary>
    /// Generate a value for an SlMethodCallAsExpression
    /// </summary>
    public override LLVMSharp.LLVMValueRef GenerateValue(LLVMSharp.LLVMBuilderRef builder, LLVMSharp.LLVMBasicBlockRef entry)
    {
        return _methodCall.GetResult(builder, entry);
    }
}