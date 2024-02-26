namespace Ignite.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class IgnorePauseAttribute : Attribute
    {
    }
}
